using System;
using UnityEngine;
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

    public float PorcentagemReducaoDano { get; set; } = 0f;

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

    // Chamado em TODOS os clientes quando o personagem chega pela rede
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

    // Chamado por quem detectou o acerto (tiro, etc.).
    // Faz o filtro de fogo amigo e entrega o dano ao DONO do personagem atingido.
    public void ReceberDano(InformacaoDano dano)
    {
        if (vidaAtual <= 0) return;

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
    private void RPC_AplicarDano(float quantidade)
    {
        AplicarDano(quantidade);
    }

    // Só roda no dono do personagem (autoridade da vida)
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

    public void Renascer()
    {
        if (dadosDoHeroi == null) return;

        vidaAtual = dadosDoHeroi.vidaMaxima;
        PorcentagemReducaoDano = 0f;
        NotificarMudancaDeVida();

        if (photonView.IsMine)
            photonView.RPC(nameof(RPC_SincronizarVida), RpcTarget.Others, vidaAtual);
    }

    private void NotificarMudancaDeVida()
    {
        if (dadosDoHeroi != null)
            OnVidaMudou?.Invoke(vidaAtual, dadosDoHeroi.vidaMaxima);
    }

    private void Morrer()
    {
        if (!photonView.IsMine) return;

        Debug.Log($"[MORTE] {gameObject.name} viewID={photonView.ViewID} time={timeID}");

        if (Respawn.Instancia != null)
        {
            // Fluxo multiplayer: recria o personagem com PhotonNetwork.Instantiate
            Respawn.Instancia.SolicitarRespawn(timeID, "Pai");
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            // Fluxo antigo (fallback)
            GerenciadorRespawn gerenciador = FindFirstObjectByType<GerenciadorRespawn>();
            if (gerenciador != null)
                gerenciador.ProcessarMorte(gameObject);
            else
                Debug.LogWarning("Nenhum Respawn nem GerenciadorRespawn encontrado na cena!");
        }
    }
}