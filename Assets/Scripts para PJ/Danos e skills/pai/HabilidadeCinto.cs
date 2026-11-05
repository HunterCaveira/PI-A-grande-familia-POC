// Importa bibliotecas essenciais da Unity e do Photon
using UnityEngine;
using Photon.Pun;
using Photon.Realtime; // Necessário para gerir jogadores reais e autoridade (Player)

// A classe herda do Photon e implementa a interface da UI para os tempos de recarga
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

    [Header("Filtragem de Time")]
    [SerializeField] private int idDoTime = 1;
    public int IdDoTime => idDoTime;

    private enum EstadoCinto { Inativo, Avancando, PuxandoInimigo, RecolhendoVazio }
    private EstadoCinto estadoAtual = EstadoCinto.Inativo;

    private Transform inimigoCapturado;
    private PhotonView pvInimigo;

    // NOVA VARIÁVEL: Guarda o verdadeiro dono do boneco para devolvermos no final!
    private Player donoOriginalDoInimigo;

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
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            // Busca no Pai do objeto tocado para evitar erros se bater na arma do inimigo
            Sistemadevida vidaAlvo = col.GetComponentInParent<Sistemadevida>();
            int timeAlvo = -1;
            if (vidaAlvo != null) timeAlvo = vidaAlvo.TimeID;

            if (timeAlvo != -1 && timeAlvo != this.idDoTime)
            {
                // Guarda a raiz do corpo do inimigo
                inimigoCapturado = vidaAlvo.transform;
                pvInimigo = inimigoCapturado.GetComponent<PhotonView>();

                if (pvInimigo != null)
                {
                    // 1. Salva quem era a vítima verdadeira
                    donoOriginalDoInimigo = pvInimigo.Owner;

                    // 2. Avisa TODOS na sala que esta vítima específica (usando o ActorNumber) foi apanhada
                    pvInimigo.RPC("RPC_ReceberPuxao", RpcTarget.All, true, donoOriginalDoInimigo.ActorNumber);

                    // 3. ROUBA a autoridade do boneco da vítima para a podermos arrastar sem o jogo bugar
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
            if (pvInimigo != null && donoOriginalDoInimigo != null)
            {
                // Avisa o PC da vítima para destravar o teclado
                pvInimigo.RPC("RPC_ReceberPuxao", RpcTarget.All, false, donoOriginalDoInimigo.ActorNumber);

                // DEVOLVE O BONECO PARA O VERDADEIRO DONO
                pvInimigo.TransferOwnership(donoOriginalDoInimigo);
            }

            inimigoCapturado = null;
            pvInimigo = null;
            donoOriginalDoInimigo = null;
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

        // Prevenção extra: se algo der errado a meio, tenta soltar o inimigo
        if (pvInimigo != null && donoOriginalDoInimigo != null)
        {
            pvInimigo.RPC("RPC_ReceberPuxao", RpcTarget.All, false, donoOriginalDoInimigo.ActorNumber);
            pvInimigo.TransferOwnership(donoOriginalDoInimigo);
        }

        inimigoCapturado = null;
        pvInimigo = null;
        donoOriginalDoInimigo = null;

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

    // ==========================================
    // 🌐 RPC PROCESSADO NA VÍTIMA (Corrigido com ActorNumber)
    // ==========================================
    [PunRPC]
    public void RPC_ReceberPuxao(bool sendoPuxado, int idDoDonoOriginal)
    {
        // NOVA LÓGICA INFALÍVEL: Em vez de verificar "photonView.IsMine" (que ficou falso quando o boneco foi roubado),
        // verificamos se o nosso número de jogador local (ActorNumber) é o mesmo da pessoa que foi apanhada.
        if (PhotonNetwork.LocalPlayer.ActorNumber == idDoDonoOriginal)
        {
            MovimentodoPlayer mov = GetComponent<MovimentodoPlayer>();
            if (mov != null) mov.enabled = !sendoPuxado;

            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = !sendoPuxado;

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