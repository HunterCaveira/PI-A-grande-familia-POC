using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

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

    [Header("Filtragem de Time")]
    [SerializeField] private int idDoTime = 1;
    public int IdDoTime => idDoTime;

    private enum EstadoCinto { Inativo, Avancando, PuxandoInimigo, RecolhendoVazio }
    private EstadoCinto estadoAtual = EstadoCinto.Inativo;

    private Transform inimigoCapturado;
    private PhotonView pvInimigo;

    // VARIÁVEIS DE REDE PARA O FEEDBACK
    private int donoOriginalActorNr;
    private int viewIDVitima;

    private Vector3 posicaoInicialLocal;
    private float distanciaAtualVoo = 0f;

    private void Awake()
    {
        if (gerenciadorDeEquipamentos == null) gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();

        VerificarEConectarReferenciasDaCena();

        if (prefabCinto != null)
            posicaoInicialLocal = prefabCinto.transform.localPosition;
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
        if (prefabCinto == null || cameraPrincipal == null) return;

        distanciaAtualVoo += velocidadeAvanco * Time.deltaTime;
        prefabCinto.transform.position = transform.position + (cameraPrincipal.transform.forward * distanciaAtualVoo);

        if (ChecarEPrenderInimigo()) return;

        if (distanciaAtualVoo >= distanciaMaxima)
            estadoAtual = EstadoCinto.RecolhendoVazio;
    }

    private void ProcessarRecolhimento()
    {
        if (prefabCinto == null || cameraPrincipal == null) return;

        distanciaAtualVoo -= velocidadeAvanco * Time.deltaTime;
        prefabCinto.transform.position = transform.position + (cameraPrincipal.transform.forward * distanciaAtualVoo);

        if (ChecarEPrenderInimigo()) return;

        if (distanciaAtualVoo <= 0f) FinalizarHabilidade();
    }

    private bool ChecarEPrenderInimigo()
    {
        Vector3 posicaoCheck = pontoFivelaPontos != null ? pontoFivelaPontos.position : prefabCinto.transform.position;
        Collider[] hits = Physics.OverlapSphere(posicaoCheck, 0.6f);

        foreach (var col in hits)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            Sistemadevida vidaAlvo = col.GetComponentInParent<Sistemadevida>();
            int timeAlvo = -1;
            if (vidaAlvo != null) timeAlvo = vidaAlvo.TimeID;

            if (timeAlvo != -1 && timeAlvo != this.idDoTime)
            {
                inimigoCapturado = vidaAlvo.transform;
                pvInimigo = inimigoCapturado.GetComponent<PhotonView>();

                if (pvInimigo != null)
                {
                    donoOriginalActorNr = pvInimigo.OwnerActorNr;
                    viewIDVitima = pvInimigo.ViewID;

                    photonView.RPC("RPC_AvisarVitima", RpcTarget.All, viewIDVitima, donoOriginalActorNr, true);
                    pvInimigo.RequestOwnership();
                }

                estadoAtual = EstadoCinto.PuxandoInimigo;
                return true;
            }
        }
        return false;
    }

    private void ProcessarPuxao()
    {
        if (prefabCinto == null) return;

        distanciaAtualVoo -= velocidadeRetorno * Time.deltaTime;
        distanciaAtualVoo = Mathf.Max(0, distanciaAtualVoo);
        prefabCinto.transform.position = transform.position + (cameraPrincipal.transform.forward * distanciaAtualVoo);

        if (inimigoCapturado != null)
        {
            Vector3 posicaoFivela = pontoFivelaPontos != null ? pontoFivelaPontos.position : prefabCinto.transform.position;
            inimigoCapturado.position = posicaoFivela;
        }

        if (distanciaAtualVoo <= 0.2f) FinalizarHabilidade();
    }

    private void FinalizarHabilidade()
    {
        estadoAtual = EstadoCinto.Inativo;
        distanciaAtualVoo = 0f;

        if (inimigoCapturado != null)
        {
            if (pvInimigo != null)
            {
                photonView.RPC("RPC_AvisarVitima", RpcTarget.All, viewIDVitima, donoOriginalActorNr, false);

                Player donoAntigo = PhotonNetwork.CurrentRoom.GetPlayer(donoOriginalActorNr);
                if (donoAntigo != null) pvInimigo.TransferOwnership(donoAntigo);
            }

            inimigoCapturado = null;
            pvInimigo = null;
        }

        CancelarHabilidade();
        if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0);
    }

    public void CancelarHabilidade()
    {
        estadoAtual = EstadoCinto.Inativo;
        distanciaAtualVoo = 0f;

        if (pvInimigo != null)
        {
            photonView.RPC("RPC_AvisarVitima", RpcTarget.All, viewIDVitima, donoOriginalActorNr, false);
            Player donoAntigo = PhotonNetwork.CurrentRoom.GetPlayer(donoOriginalActorNr);
            if (donoAntigo != null) pvInimigo.TransferOwnership(donoAntigo);
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

    public bool EstaPronta() => Time.time >= tempoProximoUso && estadoAtual == EstadoCinto.Inativo;
    public bool PodeUsar() => EstaPronta();

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

        // Toca o som de lançar
        if (audioSource != null && somLançar != null)
        {
            audioSource.PlayOneShot(somLançar);
        }

        distanciaAtualVoo = 0f;
        tempoProximoUso = Time.time + tempoCooldown;
        estadoAtual = EstadoCinto.Avancando;
    }

    [PunRPC]
    public void RPC_AvisarVitima(int idObjetoVitima, int idDonoOriginal, bool sendoPuxado)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == idDonoOriginal)
        {
            PhotonView pvVitima = PhotonView.Find(idObjetoVitima);
            if (pvVitima != null)
            {
                MovimentodoPlayer mov = pvVitima.GetComponent<MovimentodoPlayer>();
                if (mov != null) mov.enabled = !sendoPuxado;

                CharacterController cc = pvVitima.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = !sendoPuxado;

                if (sendoPuxado)
                    Debug.Log("<color=red>⚠️ VOCÊ FOI LAÇADO E ESTÁ SENDO PUXADO!</color>");
                else
                    Debug.Log("<color=green>Você foi solto e pode voltar a andar.</color>");
            }
        }
    }
}