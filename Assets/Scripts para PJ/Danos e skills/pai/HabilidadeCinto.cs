using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class HabilidadeCinto : MonoBehaviourPun, IasUI
{
    [Header("Configurações da Câmera e Alcance")]
    private Camera cameraPrincipal;
    [SerializeField] private float distanciaMaxima = 15f;
    [Tooltip("Ponto de onde o cinto sai (ex: altura do peito ou mão). Se vazio, usa a altura do peito automaticamente.")]
    [SerializeField] private Transform pontoOrigemDisparo;

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

    private Sistemadevida meuSistemaDeVida;

    // 🌐 FILTRAGEM DINÂMICA DE EQUIPA
    public int IdDoTime
    {
        get
        {
            if (meuSistemaDeVida == null)
                meuSistemaDeVida = GetComponentInParent<Sistemadevida>();

            return meuSistemaDeVida != null ? meuSistemaDeVida.TimeID : 1;
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
    private Quaternion rotacaoInicialLocal;
    private float distanciaAtualVoo = 0f;

    private void Awake()
    {
        if (gerenciadorDeEquipamentos == null) gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
        meuSistemaDeVida = GetComponentInParent<Sistemadevida>();
        cameraPrincipal = Camera.main;

        if (prefabCinto != null)
        {
            posicaoInicialLocal = prefabCinto.transform.localPosition;
            rotacaoInicialLocal = prefabCinto.transform.localRotation;
        }
    }

    // Usamos LateUpdate para a fivela acompanhar perfeitamente a câmera sem tremer
    private void LateUpdate()
    {
        if (photonView != null && !photonView.IsMine) return;
        if (estadoAtual == EstadoCinto.Inativo) return;

        if (cameraPrincipal == null) cameraPrincipal = Camera.main;

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

    private void AtualizarTrajetoriaComCamera()
    {
        if (prefabCinto == null || cameraPrincipal == null) return;

        Vector3 origem = pontoOrigemDisparo != null ? pontoOrigemDisparo.position : transform.position + (Vector3.up * 1.2f);
        Vector3 direcaoMira = cameraPrincipal.transform.forward;

        prefabCinto.transform.position = origem + (direcaoMira * distanciaAtualVoo);
        prefabCinto.transform.rotation = Quaternion.LookRotation(direcaoMira);
    }

    private void ProcessarAvanco()
    {
        if (prefabCinto == null || cameraPrincipal == null) return;

        distanciaAtualVoo += velocidadeAvanco * Time.deltaTime;
        AtualizarTrajetoriaComCamera();

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
        AtualizarTrajetoriaComCamera();

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
            // 1. Ignora a si mesmo
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            // 2. Busca o Sistemadevida. Se o objeto tocado não tem vida (chão, obstáculos normais do cenário), ignora e passa reto.
            Sistemadevida vidaAlvo = col.GetComponentInParent<Sistemadevida>();
            if (vidaAlvo == null) continue;

            // 🛑 3. A CORREÇÃO: É um jogador ou uma parede?
            // Como a parede também tem Sistemadevida, checamos se o alvo consegue andar (MovimentodoPlayer)
            MovimentodoPlayer movAlvo = col.GetComponentInParent<MovimentodoPlayer>();

            if (movAlvo == null)
            {
                // Se tem vida mas NÃO tem script de movimento, é a Parede! O cinto bate nela e recolhe vazio sem tentar puxar.
                estadoAtual = EstadoCinto.RecolhendoVazio;
                return false;
            }

            // 4. É um jogador válido e que se move! Vamos verificar a equipa.
            int timeAlvo = vidaAlvo.TimeID;
            if (timeAlvo != -1 && timeAlvo != this.IdDoTime)
            {
                inimigoCapturado = vidaAlvo.transform;
                pvInimigo = vidaAlvo.GetComponent<PhotonView>();

                if (pvInimigo != null)
                {
                    donoOriginalActorNr = pvInimigo.OwnerActorNr;
                    viewIDVitima = pvInimigo.ViewID;

                    photonView.RPC("RPC_AvisarVitima", RpcTarget.All, viewIDVitima, donoOriginalActorNr, true);
                    pvInimigo.RequestOwnership();
                }

                controllerInimigo = vidaAlvo.GetComponent<CharacterController>();
                if (controllerInimigo != null) controllerInimigo.enabled = false;

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
        AtualizarTrajetoriaComCamera();

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
            prefabCinto.transform.localRotation = rotacaoInicialLocal;
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
            prefabCinto.transform.localRotation = rotacaoInicialLocal;
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