using UnityEngine;
using Photon.Pun; // Importa a biblioteca do Photon PUN para multiplayer.

// Agora herda de MonoBehaviourPun para usar funções de rede
public class HabilidadeCinto : MonoBehaviourPun, IasUI
{
    // ==========================================
    // 1. CONFIGURAÇÕES DA CÂMERA E ALCANCE
    // ==========================================

    [Header("Configurações da Câmera e Alcance")]
    [SerializeField] private Camera cameraPrincipal;
    [SerializeField] private float distanciaMaxima = 15f;


    // ==========================================
    // 2. TEMPOS E VELOCIDADES
    // ==========================================

    [Header("Controle de Tempo e Velocidades")]
    [SerializeField] private float tempoCooldown = 3.0f;
    private float tempoProximoUso = 0f;
    public float TempoProximouso => tempoProximoUso;

    [SerializeField] private float velocidadeAvanco = 30f;
    [SerializeField] private float velocidadeRetorno = 20f;


    // ==========================================
    // 3. CONFIGURAÇÃO DE SLOT E UI (OBRIGATÓRIO)
    // ==========================================

    [Header("Configuração de Slot e UI")]
    [SerializeField] private int slotIndex = 1; // 1 = Q, 2 = E, 3 = F
    public int SlotIndex => slotIndex;

    [SerializeField] private Sprite fotoPronta;
    public Sprite FotoPronta => fotoPronta;

    [SerializeField] private Sprite fotoRecarregando;
    public Sprite FotoRecarregando => fotoRecarregando;


    // ==========================================
    // 4. REFERÊNCIAS DE COMPONENTES E VISUAIS
    // ==========================================

    [Header("Controle De Referência e Visuais")]
    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;
    [SerializeField] private GameObject prefabCinto;
    public Transform pontoFivelaPontos; // Tornado public para o alvo conseguir ler a posição
    [SerializeField] private Transform pontoDestinoPuxao;


    // ==========================================
    // 5. COMBATE E MÁQUINA DE ESTADOS
    // ==========================================

    [Header("Filtragem de Time")]
    [SerializeField] private int idDoTime = 1;
    public int IdDoTime => idDoTime;

    private enum EstadoCinto { Inativo, Avancando, PuxandoInimigo, RecolhendoVazio }
    private EstadoCinto estadoAtual = EstadoCinto.Inativo;

    private Transform inimigoCapturado;
    private Vector3 posicaoInicialLocal;
    private Vector3 pontoFinalMundo;

    // Variáveis para a "vítima" gerenciar o próprio puxão
    private Transform fivelaQueMePuxa;
    private CharacterController controllerLocal;
    private Rigidbody rbLocal;

    // ==========================================
    // 6. CICLO DE VIDA DA UNITY
    // ==========================================

    private void Awake()
    {
        if (gerenciadorDeEquipamentos == null) gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
        if (cameraPrincipal == null) cameraPrincipal = Camera.main;

        if (prefabCinto != null)
        {
            posicaoInicialLocal = prefabCinto.transform.localPosition;
        }

        // Cache das físicas locais caso esse jogador seja puxado
        controllerLocal = GetComponent<CharacterController>();
        rbLocal = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // SE EU FOR A VÍTIMA: Eu sigo a fivela do jogador que me puxou
        if (fivelaQueMePuxa != null && photonView.IsMine)
        {
            transform.position = fivelaQueMePuxa.position;
        }

        // SE NÃO FOR MEU PERSONAGEM: Ignora o resto da lógica (apenas o dono controla o cinto)
        if (!photonView.IsMine) return;

        if (estadoAtual == EstadoCinto.Inativo) return;

        // MÁQUINA DE ESTADOS DO CINTO
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


    // ==========================================
    // 7. LÓGICA DE DETECÇÃO, AVANÇO E PUXÃO
    // ==========================================

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

                // MULTIPLAYER: Avisa o PC do inimigo que ele foi pego, passando a minha ID (ViewID)
                PhotonView inimigoView = col.GetComponent<PhotonView>();
                if (inimigoView != null)
                {
                    inimigoView.RPC("ReceberPuxaoRPC", RpcTarget.All, photonView.ViewID, true);
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

        Vector3 destinoMao = pontoDestinoPuxao != null ? pontoDestinoPuxao.position : transform.position;
        prefabCinto.transform.position = Vector3.MoveTowards(prefabCinto.transform.position, destinoMao, velocidadeRetorno * Time.deltaTime);

        // Visualmente, forçamos o inimigo a ficar na fivela na nossa tela
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

        // MULTIPLAYER: Avisa o inimigo para se soltar e reativar as físicas
        if (inimigoCapturado != null)
        {
            PhotonView inimigoView = inimigoCapturado.GetComponent<PhotonView>();
            if (inimigoView != null)
            {
                inimigoView.RPC("ReceberPuxaoRPC", RpcTarget.All, photonView.ViewID, false);
            }
            inimigoCapturado = null;
        }

        CancelarHabilidade();

        if (gerenciadorDeEquipamentos != null)
        {
            gerenciadorDeEquipamentos.TrocarEquipamento(0);
        }
    }


    // ==========================================
    // 8. MÉTODOS DA INTERFACE IasUI / ISkills
    // ==========================================

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
        if (!PodeUsar())
        {
            if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0);
            return;
        }

        // MULTIPLAYER: Sincroniza visual do cinto aparecendo
        photonView.RPC("SincronizarCintoVisualRPC", RpcTarget.All, true);

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

        // Libera o inimigo caso a habilidade seja cancelada bruscamente
        if (inimigoCapturado != null)
        {
            PhotonView inimigoView = inimigoCapturado.GetComponent<PhotonView>();
            if (inimigoView != null) inimigoView.RPC("ReceberPuxaoRPC", RpcTarget.All, photonView.ViewID, false);
            inimigoCapturado = null;
        }

        // MULTIPLAYER: Sincroniza visual do cinto sumindo
        photonView.RPC("SincronizarCintoVisualRPC", RpcTarget.All, false);
    }


    // ==========================================
    // 9. EVENTOS RPC DO PHOTON (REDE)
    // ==========================================

    [PunRPC]
    public void ReceberPuxaoRPC(int puxadorViewID, bool prender)
    {
        if (prender)
        {
            // Procura o jogador que atirou o cinto para podermos seguir a fivela dele
            PhotonView puxadorView = PhotonView.Find(puxadorViewID);
            if (puxadorView != null)
            {
                HabilidadeCinto scriptPuxador = puxadorView.GetComponent<HabilidadeCinto>();
                if (scriptPuxador != null) fivelaQueMePuxa = scriptPuxador.pontoFivelaPontos;
            }

            // Apenas o PC da vítima desativa as suas próprias físicas
            if (photonView.IsMine)
            {
                if (controllerLocal != null) controllerLocal.enabled = false;
                if (rbLocal != null) rbLocal.isKinematic = true;
            }
        }
        else
        {
            fivelaQueMePuxa = null; // Solta a referência da fivela

            // O PC da vítima reativa as próprias físicas
            if (photonView.IsMine)
            {
                if (controllerLocal != null) controllerLocal.enabled = true;
                if (rbLocal != null) rbLocal.isKinematic = false;
            }
        }
    }

    [PunRPC]
    public void SincronizarCintoVisualRPC(bool ativar)
    {
        if (prefabCinto != null)
        {
            if (ativar)
            {
                prefabCinto.transform.localPosition = posicaoInicialLocal;
                prefabCinto.SetActive(true);
            }
            else
            {
                prefabCinto.transform.localPosition = posicaoInicialLocal;
                prefabCinto.transform.localRotation = Quaternion.identity;
                prefabCinto.SetActive(false);
            }
        }
    }
}