using UnityEngine;
using Photon.Pun;

public class HabilidadeCinto : MonoBehaviourPun, IasUI
{
    [Header("Configurações da Câmera e Alcance")]
    [SerializeField] private Camera cameraPrincipal;
    [SerializeField] private float distanciaMaxima = 15f;

    [Header("Controle de Tempo e Velocidades")]
    [SerializeField] private float tempoCooldown = 3.0f;
    private float tempoProximoUso = 0f;
    public float TempoProximouso => tempoProximoUso;

    [SerializeField] private float velocidadeAvanco = 30f;
    [SerializeField] private float velocidadeRetorno = 20f;

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
    [SerializeField] private Transform pontoDestinoPuxao;

    [Header("Filtragem de Time")]
    [SerializeField] private int idDoTime = 1;
    public int IdDoTime => idDoTime;

    private enum EstadoCinto { Inativo, Avancando, PuxandoInimigo, RecolhendoVazio }
    private EstadoCinto estadoAtual = EstadoCinto.Inativo;

    private Transform inimigoCapturado;
    private CharacterController controllerInimigo;
    private Rigidbody rbInimigo;
    private PhotonView pvInimigo;

    private Vector3 posicaoInicialLocal;
    private Vector3 pontoFinalMundo;

    private void Awake()
    {
        if (gerenciadorDeEquipamentos == null) gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
        if (cameraPrincipal == null) cameraPrincipal = Camera.main;

        if (prefabCinto != null)
        {
            posicaoInicialLocal = prefabCinto.transform.localPosition;
        }
    }

    private void Update()
    {
        if (photonView != null && !photonView.IsMine) return;
        if (estadoAtual == EstadoCinto.Inativo) return;

        switch (estadoAtual)
        {
            case EstadoCinto.Avancando:
                ProcessarAvanco();
                break;
            case EstadoCinto.PuxandoInimigo:
                ProcessarPuxao();
                break;
            case EstadoCinto.RecolhendoVazio:
                ProcessarRecolhimento();
                break;
        }
    }

    private void ProcessarAvanco()
    {
        if (prefabCinto == null) return;

        prefabCinto.transform.position = Vector3.MoveTowards(prefabCinto.transform.position, pontoFinalMundo, velocidadeAvanco * Time.deltaTime);

        if (ChecarEPrenderInimigo()) return;

        if (Vector3.Distance(prefabCinto.transform.position, pontoFinalMundo) <= 0.2f)
        {
            estadoAtual = EstadoCinto.RecolhendoVazio;
        }
    }

    private void ProcessarRecolhimento()
    {
        if (prefabCinto == null) return;

        Vector3 destinoMao = pontoDestinoPuxao != null ? pontoDestinoPuxao.position : transform.position;

        prefabCinto.transform.position = Vector3.MoveTowards(prefabCinto.transform.position, destinoMao, velocidadeAvanco * Time.deltaTime);

        if (ChecarEPrenderInimigo()) return;

        if (Vector3.Distance(prefabCinto.transform.position, destinoMao) <= 0.2f)
        {
            FinalizarHabilidade();
        }
    }

    private bool ChecarEPrenderInimigo()
    {
        Vector3 posicaoCheck = pontoFivelaPontos != null ? pontoFivelaPontos.position : prefabCinto.transform.position;
        Collider[] hits = Physics.OverlapSphere(posicaoCheck, 0.6f);

        foreach (var col in hits)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            Sistemadevida vidaAlvo = col.GetComponent<Sistemadevida>();
            HabilidadeCinto scriptAlvo = col.GetComponent<HabilidadeCinto>();

            int timeAlvo = -1;
            if (vidaAlvo != null) timeAlvo = vidaAlvo.TimeID;
            else if (scriptAlvo != null) timeAlvo = scriptAlvo.IdDoTime;

            if (timeAlvo != -1 && timeAlvo != this.idDoTime)
            {
                inimigoCapturado = col.transform;

                pvInimigo = col.GetComponent<PhotonView>();
                if (pvInimigo != null)
                {
                    pvInimigo.RequestOwnership();
                }

                controllerInimigo = col.GetComponent<CharacterController>();
                if (controllerInimigo != null) controllerInimigo.enabled = false;

                rbInimigo = col.GetComponent<Rigidbody>();
                if (rbInimigo != null) rbInimigo.isKinematic = true;

                estadoAtual = EstadoCinto.PuxandoInimigo;
                return true;
            }
        }

        return false;
    }

    private void ProcessarPuxao()
    {
        if (prefabCinto == null) return;

        Vector3 destinoMao = pontoDestinoPuxao != null ? pontoDestinoPuxao.position : transform.position;

        prefabCinto.transform.position = Vector3.MoveTowards(prefabCinto.transform.position, destinoMao, velocidadeRetorno * Time.deltaTime);

        if (inimigoCapturado != null)
        {
            Vector3 posicaoFivela = pontoFivelaPontos != null ? pontoFivelaPontos.position : prefabCinto.transform.position;
            inimigoCapturado.position = posicaoFivela;
        }

        if (Vector3.Distance(prefabCinto.transform.position, destinoMao) <= 0.8f)
        {
            FinalizarHabilidade();
        }
    }

    private void FinalizarHabilidade()
    {
        estadoAtual = EstadoCinto.Inativo;

        if (controllerInimigo != null)
        {
            controllerInimigo.enabled = true;
            controllerInimigo = null;
        }

        if (rbInimigo != null)
        {
            rbInimigo.isKinematic = false;
            rbInimigo = null;
        }

        inimigoCapturado = null;
        pvInimigo = null;

        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.transform.localRotation = Quaternion.identity;
        }

        CancelarHabilidade();

        if (gerenciadorDeEquipamentos != null)
        {
            gerenciadorDeEquipamentos.TrocarEquipamento(0);
        }
    }

    public bool EstaPronta()
    {
        return Time.time >= tempoProximoUso && estadoAtual == EstadoCinto.Inativo;
    }

    public bool PodeUsar()
    {
        return EstaPronta();
    }

    public void AtivarHabilidade()
    {
        if (photonView != null && !photonView.IsMine) return;

        if (!PodeUsar())
        {
            if (gerenciadorDeEquipamentos != null)
            {
                gerenciadorDeEquipamentos.TrocarEquipamento(0);
            }
            return;
        }

        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.SetActive(true);
        }

        if (cameraPrincipal != null)
        {
            Ray ray = cameraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            pontoFinalMundo = ray.origin + ray.direction * distanciaMaxima;

            if (Physics.Raycast(ray, out RaycastHit hit, distanciaMaxima))
            {
                pontoFinalMundo = hit.point;
            }
        }

        if (prefabCinto != null)
        {
            prefabCinto.transform.LookAt(pontoFinalMundo);
        }

        tempoProximoUso = Time.time + tempoCooldown;
        estadoAtual = EstadoCinto.Avancando;
    }

    public void CancelarHabilidade()
    {
        estadoAtual = EstadoCinto.Inativo;

        if (controllerInimigo != null)
        {
            controllerInimigo.enabled = true;
            controllerInimigo = null;
        }

        if (rbInimigo != null)
        {
            rbInimigo.isKinematic = false;
            rbInimigo = null;
        }

        inimigoCapturado = null;
        pvInimigo = null;

        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.transform.localRotation = Quaternion.identity;
            prefabCinto.SetActive(false);
        }
    }
}