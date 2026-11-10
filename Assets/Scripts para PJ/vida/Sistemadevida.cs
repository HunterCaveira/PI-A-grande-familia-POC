// Importa a biblioteca padrão do C# para manipulação de eventos e dados
using System;
using System.Collections;
// Importa a biblioteca principal da Unity
using UnityEngine;
// Importa a biblioteca do Photon para rede e sincronização multiplayer
using Photon.Pun;

public class Sistemadevida : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    // ==========================================
    // 1. ATRIBUTOS E VARIÁVEIS
    // ==========================================

    [Header("Configurações do Herói")]
    [SerializeField] private AtributosPersonagem dadosDoHeroi;

    [Header("PvP e Partida")]
    [SerializeField] private int timeID = 1;
    [SerializeField] public float vidaAtual;

    [Header("Debug")]
    [SerializeField] private bool logsDeRede = true;

    // Setter público mantém compatível com GerenciadorDeSpawn e Respawn (vida.TimeID = x)
    public int TimeID
    {
        get => timeID;
        set => timeID = value;
    }

    // Porcentagem de redução de dano vinda de buffs (ex: Tónico)
    public float PorcentagemReducaoDano { get; set; } = 0f;

    // Evento C# que avisa a barra de vida na UI sempre que a vida muda (Vida Atual, Vida Máxima)
    public event Action<float, float> OnVidaMudou;

    // ==========================================
    // 2. CICLO DE VIDA
    // ==========================================

    private void Awake()
    {
        if (dadosDoHeroi != null)
            vidaAtual = dadosDoHeroi.vidaMaxima;
    }

    private void Start()
    {
        NotificarMudancaDeVida();
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] dados = info.photonView.InstantiationData;
        if (dados != null && dados.Length > 0)
            timeID = (int)dados[0];

        if (logsDeRede)
            Debug.Log($"[SPAWN REDE] viewID={info.photonView.ViewID} dono={info.photonView.Owner?.NickName} souEu={info.photonView.IsMine} time={timeID}");
    }

    private void OnDestroy()
    {
        if (!logsDeRede || photonView == null) return;
        Debug.Log($"[DESTROY] viewID={photonView.ViewID} souEu={photonView.IsMine}\n{Environment.StackTrace}");
    }

    // ==========================================
    // 3. FICHA DE ATRIBUTOS
    // ==========================================

    public void AtualizarFichaAtributos(AtributosPersonagem novaFicha)
    {
        if (novaFicha != null)
        {
            dadosDoHeroi = novaFicha;
            NotificarMudancaDeVida();
            Debug.Log($"[SISTEMA DE VIDA] Ficha de atributos atualizada! Vida máxima da ficha: {dadosDoHeroi.vidaMaxima}");
        }
    }

    // ==========================================
    // 4. DANO E MORTE (rede)
    // ==========================================

    public void ReceberDano(InformacaoDano dano)
    {
        if (vidaAtual <= 0) return;

        // 🛡️ Filtro de Fogo Amigo (Objeto atacante direto)
        if (dano.atacante != null && dano.atacante.TryGetComponent<Sistemadevida>(out Sistemadevida atacanteVida))
        {
            if (atacanteVida.TimeID == timeID && dano.atacante != gameObject)
                return;
        }

        if (photonView.IsMine)
            AplicarDano(dano.quantidade);
        else
            photonView.RPC(nameof(RPC_AplicarDano), photonView.Owner, dano.quantidade);
    }

    [PunRPC]
    public void ReceberDanoRPC(float quantidadeDano, int viewIDAtirador)
    {
        if (vidaAtual <= 0) return;

        // 🛡️ Filtro de Fogo Amigo (via ViewID do Atirador na rede)
        PhotonView pvAtirador = PhotonView.Find(viewIDAtirador);
        if (pvAtirador != null && pvAtirador.TryGetComponent<Sistemadevida>(out Sistemadevida atacanteVida))
        {
            if (atacanteVida.TimeID == timeID && pvAtirador.gameObject != gameObject)
                return;
        }

        if (photonView.IsMine)
        {
            AplicarDano(quantidadeDano);
        }
    }

    [PunRPC]
    private void RPC_AplicarDano(float quantidade)
    {
        AplicarDano(quantidade);
    }

    private void AplicarDano(float quantidade)
    {
        if (vidaAtual <= 0) return;

        float danoFinal = quantidade * (1f - PorcentagemReducaoDano);
        vidaAtual = Mathf.Max(vidaAtual - danoFinal, 0f);

        NotificarMudancaDeVida();
        photonView.RPC(nameof(RPC_SincronizarVida), RpcTarget.Others, vidaAtual);

        if (vidaAtual <= 0)
            Morrer();
    }

    [PunRPC]
    private void RPC_SincronizarVida(float novaVida)
    {
        vidaAtual = novaVida;
        NotificarMudancaDeVida();
    }

    // ==========================================
    // 5. RESPAWN E RENASCER
    // ==========================================

    private void Morrer()
    {
        if (!photonView.IsMine) return;

        Debug.Log($"[MORTE] {gameObject.name} viewID={photonView.ViewID} time={timeID}");

        // 1. Oculta modelo 3D, desativa colisões, movimentação, armas e habilidades via RPC
        photonView.RPC(nameof(RPC_AlternarEstadoJogador), RpcTarget.All, false);

        // 2. Solicita ao GerenciadorDeSpawn que aguarde o tempo de respawn
        if (GerenciadorDeSpawn.Instancia != null)
        {
            GerenciadorDeSpawn.Instancia.SolicitarRespawnReaproveitado(this, timeID, 3f);
        }
    }

    public void Renascer(Vector3 posicaoSpawn, Quaternion rotacaoSpawn)
    {
        if (dadosDoHeroi != null)
            vidaAtual = dadosDoHeroi.vidaMaxima;

        PorcentagemReducaoDano = 0f;
        NotificarMudancaDeVida();

        transform.position = posicaoSpawn;
        transform.rotation = rotacaoSpawn;

        // Reativa os visuais, colisores, movimentação, equipamentos e habilidades em toda a rede
        photonView.RPC(nameof(RPC_AlternarEstadoJogador), RpcTarget.All, true);

        if (photonView.IsMine)
            photonView.RPC(nameof(RPC_SincronizarVida), RpcTarget.Others, vidaAtual);
    }

    [PunRPC]
    private void RPC_AlternarEstadoJogador(bool estado)
    {
        // 1. Visuais (Renderers)
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = estado;
        }

        // 2. Colisores
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = estado;
        }

        // 3. CharacterController
        if (TryGetComponent<CharacterController>(out CharacterController cc))
        {
            cc.enabled = estado;
        }

        // 4. Script de Movimento
        if (TryGetComponent<MovimentodoPlayer>(out MovimentodoPlayer mov))
        {
            mov.enabled = estado;
        }

        // 5. Câmera e Rotação do Personagem
        if (TryGetComponent<CorrecaoCameraPersonagem>(out CorrecaoCameraPersonagem camRot))
        {
            camRot.enabled = estado;
        }

        // 6. Gerenciador de Equipamentos (Armas/Mãos)
        GerenciadorDeEquipamentos gerEq = GetComponent<GerenciadorDeEquipamentos>();
        if (gerEq != null)
        {
            gerEq.enabled = estado;
            gerEq.photonView.RPC("RPC_AlternarMaos", RpcTarget.All, estado);
        }

        // 7. Habilidade do Cinto
        HabilidadeCinto cinto = GetComponent<HabilidadeCinto>();
        if (cinto != null)
        {
            if (!estado) cinto.CancelarHabilidade();
            cinto.enabled = estado;
        }

        // 8. Habilidade da Parede
        HabilidadeParede parede = GetComponent<HabilidadeParede>();
        if (parede != null)
        {
            if (!estado) parede.CancelarHabilidade();
            parede.enabled = estado;
        }

        // 9. Habilidade do Tónico
        HabilidadeTonico tonico = GetComponent<HabilidadeTonico>();
        if (tonico != null)
        {
            tonico.enabled = estado;
        }

        // 10. Script de Disparo (Pistola / Armas)
        tiroPistola scriptTiro = GetComponentInChildren<tiroPistola>();
        if (scriptTiro != null)
        {
            scriptTiro.enabled = estado;
        }
    }

    private void NotificarMudancaDeVida()
    {
        if (dadosDoHeroi != null)
            OnVidaMudou?.Invoke(vidaAtual, dadosDoHeroi.vidaMaxima);
    }
}