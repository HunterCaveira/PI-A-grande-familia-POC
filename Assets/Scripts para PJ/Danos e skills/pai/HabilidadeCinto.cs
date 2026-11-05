using UnityEngine;
using Photon.Pun;

public class HabilidadeCinto : MonoBehaviourPun, IasUI
{
    [Header("Configurações da Câmera e Alcance")]
    [Tooltip("Nome exato do GameObject da Câmera na cena")]
    [SerializeField] private string nomeDaCamera = "Main Camera";
    private Camera cameraPrincipal;
    [SerializeField] private float distanciaMaxima = 15f;

    [Header("Sons da Habilidade")]
    public AudioSource audioSource;
    public AudioClip somLançar;

    [Header("Controle de Tempo e Velocidades")]
    [SerializeField] private float tempoCooldown = 3.0f;
    private float tempoProximoUso = 0f;
    public float TempoProximouso => tempoProximoUso;

    [SerializeField] private float velocidadeAvanco = 30f;

    [Header("Configuração de Slot e UI")]
    [SerializeField] private int slotIndex = 1;
    public int SlotIndex => slotIndex;

    [SerializeField] private Sprite fotoPronta;
    public Sprite FotoPronta => fotoPronta;
    [SerializeField] private Sprite fotoRecarregando;
    public Sprite FotoRecarregando => fotoRecarregando;

    [Header("Controle De Referência e Visuais")]
    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;
    [SerializeField] private GameObject prefabCinto;
    [SerializeField] private Transform pontoFivelaPontos;

    [Header("Filtragem de Time")]
    [SerializeField] private int idDoTime = 1;
    public int IdDoTime => idDoTime;

    private enum EstadoCinto { Inativo, Avancando, RecolhendoVazio }
    private EstadoCinto estadoAtual = EstadoCinto.Inativo;

    private Vector3 posicaoInicialLocal;
    private float distanciaAtualVoo = 0f;

    private void Awake()
    {
        if (gerenciadorDeEquipamentos == null) gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();

        VerificarEConectarReferenciasDaCena();

        if (prefabCinto != null)
        {
            posicaoInicialLocal = prefabCinto.transform.localPosition;
        }
    }

    private void VerificarEConectarReferenciasDaCena()
    {
        if (cameraPrincipal == null)
        {
            GameObject camObj = GameObject.Find(nomeDaCamera);

            if (camObj == null)
            {
                Camera[] camerasNaCena = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                foreach (Camera cam in camerasNaCena)
                {
                    if (cam.gameObject.name == nomeDaCamera || cam.CompareTag("MainCamera"))
                    {
                        camObj = cam.gameObject;
                        break;
                    }
                }
            }

            if (camObj != null)
            {
                cameraPrincipal = camObj.GetComponent<Camera>();
            }
        }

        if (audioSource == null && cameraPrincipal != null)
        {
            audioSource = cameraPrincipal.GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = cameraPrincipal.gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void Update()
    {
        if (photonView != null && !photonView.IsMine) return;

        if (cameraPrincipal == null)
        {
            VerificarEConectarReferenciasDaCena();
        }

        if (estadoAtual == EstadoCinto.Inativo) return;

        switch (estadoAtual)
        {
            case EstadoCinto.Avancando:
                ProcessarAvanco();
                break;
            case EstadoCinto.RecolhendoVazio:
                ProcessarRecolhimento();
                break;
        }
    }

    private void ProcessarAvanco()
    {
        if (prefabCinto == null || cameraPrincipal == null) return;

        distanciaAtualVoo += velocidadeAvanco * Time.deltaTime;
        prefabCinto.transform.position = transform.position + (cameraPrincipal.transform.forward * distanciaAtualVoo);

        if (ChecarEImpactarInimigo())
        {
            estadoAtual = EstadoCinto.RecolhendoVazio;
            return;
        }

        if (distanciaAtualVoo >= distanciaMaxima)
        {
            estadoAtual = EstadoCinto.RecolhendoVazio;
        }
    }

    private void ProcessarRecolhimento()
    {
        if (prefabCinto == null || cameraPrincipal == null) return;

        distanciaAtualVoo -= velocidadeAvanco * Time.deltaTime;
        prefabCinto.transform.position = transform.position + (cameraPrincipal.transform.forward * distanciaAtualVoo);

        if (distanciaAtualVoo <= 0f)
        {
            FinalizarHabilidade();
        }
    }

    private bool ChecarEImpactarInimigo()
    {
        Vector3 posicaoCheck = pontoFivelaPontos != null ? pontoFivelaPontos.position : prefabCinto.transform.position;
        Collider[] hits = Physics.OverlapSphere(posicaoCheck, 0.6f);

        foreach (var col in hits)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            Sistemadevida vidaAlvo = col.GetComponent<Sistemadevida>();
            int timeAlvo = -1;
            if (vidaAlvo != null) timeAlvo = vidaAlvo.TimeID;

            if (timeAlvo != -1 && timeAlvo != this.idDoTime)
            {
                return true;
            }
        }
        return false;
    }

    private void FinalizarHabilidade()
    {
        CancelarHabilidade();

        if (gerenciadorDeEquipamentos != null)
        {
            gerenciadorDeEquipamentos.TrocarEquipamento(0);
        }
    }

    public void CancelarHabilidade()
    {
        estadoAtual = EstadoCinto.Inativo;
        distanciaAtualVoo = 0f;

        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.transform.localRotation = Quaternion.identity;
            prefabCinto.SetActive(false);
        }
    }

    public bool EstaPronta()
    {
        return Time.time >= tempoProximoUso && estadoAtual == EstadoCinto.Inativo;
    }
    public bool PodeUsar() { return EstaPronta(); }

    public void AtivarHabilidade()
    {
        if (photonView != null && !photonView.IsMine) return;

        VerificarEConectarReferenciasDaCena();

        if (!PodeUsar())
        {
            if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0);
            return;
        }

        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.SetActive(true);
        }

        // Toca apenas o som de lançar/jogar
        if (audioSource != null && somLançar != null)
        {
            audioSource.PlayOneShot(somLançar);
        }

        distanciaAtualVoo = 0f;
        tempoProximoUso = Time.time + tempoCooldown;
        estadoAtual = EstadoCinto.Avancando;
    }
}