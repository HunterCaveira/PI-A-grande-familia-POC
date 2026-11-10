using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class HabilidadeCinto : MonoBehaviourPun, IasUI
{
    [Header("Configurações da Câmera e Alcance")]
    private Camera cameraPrincipal;
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

    // 🌐 FILTRAGEM DINÂMICA DE EQUIPA
    public int IdDoTime
    {
        get
        {
            Sistemadevida meuVida = GetComponentInParent<Sistemadevida>();
            return meuVida != null ? meuVida.TimeID : 1;
        }
    }

    private enum EstadoCinto { Inativo, Avancando, PuxandoInimigo, RecolhendoVazio }
    private EstadoCinto estadoAtual = EstadoCinto.Inativo;

    private Transform inimigoCapturado;
    private CharacterController controllerInimigo;
    private PhotonView pvInimigo;

    private int donoOriginalActorNr;
    private int viewIDVitima;

    private Vector3 posicaoInicialLocal;
    private float distanciaAtualVoo = 0f;

    private void Awake()
    {
        if (gerenciadorDeEquipamentos == null) gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
        cameraPrincipal = Camera.main;

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
        if (prefabCinto == null || cameraPrincipal == null) return;

        distanciaAtualVoo += velocidadeAvanco * Time.deltaTime;
        prefabCinto.transform.position = transform.position + (cameraPrincipal.transform.forward * distanciaAtualVoo);

        if (ChecarEPrenderInimigo()) return;

        if (distanciaAtualVoo >= distanciaMaxima)
        {
            estadoAtual = EstadoCinto.RecolhendoVazio;
        }
    }

    private void ProcessarRecolhimento()
    {
        if (prefabCinto == null) return;

        distanciaAtualVoo -= velocidadeAvanco * Time.deltaTime;
        prefabCinto.transform.position = transform.position + (cameraPrincipal.transform.forward * distanciaAtualVoo);

        if (ChecarEPrenderInimigo()) return;

        if (distanciaAtualVoo <= 0f)
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
            // 1. Ignora o meu próprio personagem e objetos filhos dele
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            // 🛑 2. TRAVA DE PAREDE E OBSTÁCULOS: Se for a parede ou obstáculo do mapa, o cinto bate e volta vazio
            if (col.gameObject.name.Contains("Parede") || col.CompareTag("Parede") || col.GetComponent<HabilidadeParede>() != null)
            {
                estadoAtual = EstadoCinto.RecolhendoVazio;
                return false;
            }

            // 3. Procura o CharacterController para garantir que o alvo é um PERSONAGEM e NÃO a parede
            CharacterController CCAlvo = col.GetComponentInParent<CharacterController>();
            if (CCAlvo == null) continue; // 👈 Ignora objetos com Sistemadevida que não são jogadores (como a Parede)

            Sistemadevida vidaAlvo = col.GetComponentInParent<Sistemadevida>();
            int timeAlvo = -1;
            if (vidaAlvo != null) timeAlvo = vidaAlvo.TimeID;

            // 4. Se encontrou um jogador válido de outro time
            if (timeAlvo != -1 && timeAlvo != this.IdDoTime)
            {
                inimigoCapturado = col.transform;
                pvInimigo = col.GetComponentInParent<PhotonView>();

                if (pvInimigo != null)
                {
                    donoOriginalActorNr = pvInimigo.OwnerActorNr;
                    viewIDVitima = pvInimigo.ViewID;

                    photonView.RPC("RPC_AvisarVitima", RpcTarget.All, viewIDVitima, donoOriginalActorNr, true);
                    pvInimigo.RequestOwnership();
                }

                controllerInimigo = CCAlvo;
                controllerInimigo.enabled = false;

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

        if (distanciaAtualVoo <= 0.2f)
        {
            FinalizarHabilidade();
        }
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

            if (controllerInimigo != null) controllerInimigo.enabled = true;

            inimigoCapturado = null;
            controllerInimigo = null;
            pvInimigo = null;
        }

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

        if (pvInimigo != null)
        {
            photonView.RPC("RPC_AvisarVitima", RpcTarget.All, viewIDVitima, donoOriginalActorNr, false);
            Player donoAntigo = PhotonNetwork.CurrentRoom.GetPlayer(donoOriginalActorNr);
            if (donoAntigo != null) pvInimigo.TransferOwnership(donoAntigo);
        }

        if (controllerInimigo != null)
        {
            controllerInimigo.enabled = true;
            controllerInimigo = null;
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

    public bool EstaPronta()
    {
        return Time.time >= tempoProximoUso && estadoAtual == EstadoCinto.Inativo;
    }
    public bool PodeUsar() { return EstaPronta(); }

    public void AtivarHabilidade()
    {
        if (photonView != null && !photonView.IsMine) return;

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

                if (sendoPuxado)
                {
                    Debug.Log("<color=red>⚠️ VOCÊ FOI LAÇADO E ESTÁ SENDO PUXADO!</color>");
                }
                else
                {
                    Debug.Log("<color=green>Você foi solto e pode voltar a andar.</color>");
                }
            }
        }
    }
}