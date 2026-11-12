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

    // 🌐 Identificação de Time (Visível no Inspector)
    [Header("Identificação de Time (Debug)")]
    [SerializeField] private int idDoTimeInspector;
    private Sistemadevida sistemaVidaCache;

    public int IdDoTime => idDoTimeInspector;

    private void Start()
    {
        AtualizarTimeID();
    }

    public void AtualizarTimeID()
    {
        // 1. Tenta ler direto das Custom Properties do Controller/Owner do Photon
        if (photonView != null && photonView.Controller != null)
        {
            if (photonView.Controller.CustomProperties.TryGetValue("TeamID", out object teamIdObj) && teamIdObj != null)
            {
                idDoTimeInspector = (int)teamIdObj;
                Debug.Log($"[HabilidadeCinto] TimeID obtido via Photon Controller para {gameObject.name}: {idDoTimeInspector}");
                return;
            }
        }

        if (photonView != null && photonView.Owner != null)
        {
            if (photonView.Owner.CustomProperties.TryGetValue("TeamID", out object teamIdObjOwner) && teamIdObjOwner != null)
            {
                idDoTimeInspector = (int)teamIdObjOwner;
                Debug.Log($"[HabilidadeCinto] TimeID obtido via Photon Owner para {gameObject.name}: {idDoTimeInspector}");
                return;
            }
        }

        // 2. Fallback: Busca no componente Sistemadevida local
        if (sistemaVidaCache == null)
        {
            sistemaVidaCache = GetComponent<Sistemadevida>();
            if (sistemaVidaCache == null) sistemaVidaCache = GetComponentInParent<Sistemadevida>();
            if (sistemaVidaCache == null) sistemaVidaCache = GetComponentInChildren<Sistemadevida>();
        }

        if (sistemaVidaCache != null)
        {
            idDoTimeInspector = sistemaVidaCache.TimeID;
            Debug.Log($"[HabilidadeCinto] TimeID obtido via Sistemadevida para {gameObject.name}: {idDoTimeInspector}");
            return;
        }

        Debug.LogWarning($"[HabilidadeCinto] ATENÇÃO: Nenhum TimeID encontrado para {gameObject.name}! Retornando 0.");
        idDoTimeInspector = 0;
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
            Debug.Log("[HabilidadeCinto] Distância máxima atingida sem colisões. Recolhendo vazio.");
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
            // Ignora o próprio player ou os filhos do próprio player
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            // 🛑 1. DETECÇÃO DE PAREDE POR LAYER (Nome da Layer exata: "Parede")
            // Certifique-se de que os players NÃO estão na Layer "Parede"
            int layerParede = LayerMask.NameToLayer("Paredes");
            if (col.gameObject.layer == layerParede || col.GetComponent<HabilidadeParede>() != null)
            {
                Debug.Log($"<color=orange>[HabilidadeCinto] Cinto bateu em uma PAREDE pela Layer ({col.gameObject.name}). Recolhendo vazio.</color>");
                estadoAtual = EstadoCinto.RecolhendoVazio;
                return false;
            }

            // 🛑 2. VERIFICA SE É UM JOGADOR (PhotonView)
            PhotonView pvAlvo = col.GetComponentInParent<PhotonView>();
            if (pvAlvo == null) continue; // Ignora objetos comuns que não são players nem paredes

            // Ignora o próprio PhotonView de quem atirou
            if (pvAlvo == photonView) continue;

            int timeAlvo = -1;

            // Tenta ler o TeamID do alvo
            if (pvAlvo.Controller != null && pvAlvo.Controller.CustomProperties.TryGetValue("TeamID", out object teamIdObj) && teamIdObj != null)
            {
                timeAlvo = (int)teamIdObj;
            }
            else if (pvAlvo.Owner != null && pvAlvo.Owner.CustomProperties.TryGetValue("TeamID", out object teamIdObjOwner) && teamIdObjOwner != null)
            {
                timeAlvo = (int)teamIdObjOwner;
            }
            else
            {
                // Fallback no componente Sistemadevida do alvo
                Sistemadevida vidaAlvo = pvAlvo.GetComponent<Sistemadevida>();
                if (vidaAlvo == null) vidaAlvo = pvAlvo.GetComponentInChildren<Sistemadevida>();
                if (vidaAlvo == null) vidaAlvo = pvAlvo.GetComponentInParent<Sistemadevida>();

                if (vidaAlvo != null)
                {
                    timeAlvo = vidaAlvo.TimeID;
                }
            }

            // Garante que o TimeID está atualizado antes de comparar
            AtualizarTimeID();

            Debug.Log($"[HabilidadeCinto] Colidiu com o player: {pvAlvo.gameObject.name} | Time do Alvo: {timeAlvo} | Meu Time: {this.IdDoTime}");

            // 🛡️ 3. COMPARAÇÃO DE TIME (Aliado vs Inimigo)
            if (timeAlvo != -1 && timeAlvo == this.IdDoTime)
            {
                Debug.Log($"<color=cyan>[HabilidadeCinto] Ignorando ALIADO do mesmo time ({timeAlvo}).</color>");
                continue; // É do mesmo time, ignora
            }

            // 🎯 4. INIMIGO ENCONTRADO COM SUCESSO!
            Debug.Log($"<color=green>[HabilidadeCinto] INIMIGO CAPTURADO com sucesso: {pvAlvo.gameObject.name} (Time {timeAlvo})!</color>");

            inimigoCapturado = col.transform;
            pvInimigo = pvAlvo;

            donoOriginalActorNr = pvInimigo.OwnerActorNr;
            viewIDVitima = pvInimigo.ViewID;

            if (!pvInimigo.IsMine)
            {
                pvInimigo.RequestOwnership();
            }

            photonView.RPC("RPC_AvisarVitima", RpcTarget.All, viewIDVitima, donoOriginalActorNr, true);

            controllerInimigo = pvAlvo.GetComponent<CharacterController>();
            if (controllerInimigo == null)
            {
                controllerInimigo = pvAlvo.GetComponentInChildren<CharacterController>();
            }

            if (controllerInimigo != null)
            {
                controllerInimigo.enabled = false;
            }

            estadoAtual = EstadoCinto.PuxandoInimigo;
            return true;
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

        AtualizarTimeID(); // Atualiza ao ativar a habilidade

        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.SetActive(true);
        }

        distanciaAtualVoo = 0f;
        tempoProximoUso = Time.time + tempoCooldown;
        estadoAtual = EstadoCinto.Avancando;
        Debug.Log($"[HabilidadeCinto] Habilidade ativada! Meu TimeID atual: {IdDoTime}");
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