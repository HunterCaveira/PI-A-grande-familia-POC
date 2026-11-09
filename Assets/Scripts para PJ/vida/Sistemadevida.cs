using System;
using UnityEngine;
<<<<<<< HEAD
using Photon.Pun; // Adicionado para Multiplayer
=======
using Photon.Pun;
>>>>>>> parent of a587532 (Finalizado o multiplayer)

public class Sistemadevida : MonoBehaviourPun // Herda de MonoBehaviourPun
{
<<<<<<< HEAD
=======
    // ==========================================
    // 1. ATRIBUTOS E VARIÁVEIS
    // ==========================================

>>>>>>> parent of a587532 (Finalizado o multiplayer)
    [Header("Configurações do Herói")]
    [SerializeField] private AtributosPersonagem dadosDoHeroi;

    [Header("PvP e Partida")]
    [SerializeField] private int timeID = 1;

    [SerializeField] public float vidaAtual;

<<<<<<< HEAD
    public int TimeID => timeID;
=======
    [Header("Debug")]
    [SerializeField] private bool logsDeRede = true;

    // Setter público mantém compatível com GerenciadorDeSpawn e Respawn (vida.TimeID = x)
    public int TimeID
    {
        get => timeID;
        set => timeID = value;
    }

>>>>>>> parent of a587532 (Finalizado o multiplayer)
    public float PorcentagemReducaoDano { get; set; } = 0f;

    public event Action<float, float> OnVidaMudou;

    private void Awake()
    {
<<<<<<< HEAD
        if (dadosDoHeroi != null) vidaAtual = dadosDoHeroi.vidaMaxima;
=======
        if (dadosDoHeroi != null)
            vidaAtual = dadosDoHeroi.vidaMaxima;
>>>>>>> parent of a587532 (Finalizado o multiplayer)
    }

    private void Start()
    {
        NotificarMudancaDeVida();
    }

<<<<<<< HEAD
=======
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

>>>>>>> parent of a587532 (Finalizado o multiplayer)
    public void AtualizarFichaAtributos(AtributosPersonagem novaFicha)
    {
        if (novaFicha != null)
        {
            dadosDoHeroi = novaFicha;
            NotificarMudancaDeVida();
<<<<<<< HEAD
        }
    }

    // MULTIPLAYER: Esse método empacota as informações e as envia pela rede via RPC.
=======
            Debug.Log($"[SISTEMA DE VIDA] Ficha de atributos atualizada! Vida máxima da ficha: {dadosDoHeroi.vidaMaxima}");
        }
    }

    // ==========================================
    // 4. DANO E MORTE (rede)
    // ==========================================

    // Chamado por quem detectou o acerto (tiro, etc.).
    // Faz o filtro de fogo amigo e entrega o dano ao DONO do personagem atingido.
>>>>>>> parent of a587532 (Finalizado o multiplayer)
    public void ReceberDano(InformacaoDano dano)
    {
        if (vidaAtual <= 0) return;

<<<<<<< HEAD
        int atacanteViewID = -1;
        if (dano.atacante != null)
=======
        if (dano.atacante != null && dano.atacante.TryGetComponent<Sistemadevida>(out Sistemadevida atacanteVida))
>>>>>>> parent of a587532 (Finalizado o multiplayer)
        {
            PhotonView pvAtacante = dano.atacante.GetComponent<PhotonView>();
            if (pvAtacante != null) atacanteViewID = pvAtacante.ViewID;
        }

<<<<<<< HEAD
        // Manda o aviso de dano para TODOS os computadores da sala (assim a UI de vida de todos é atualizada)
        photonView.RPC("ReceberDanoRPC", RpcTarget.All, dano.quantidade, atacanteViewID);
    }

    [PunRPC]
    public void ReceberDanoRPC(float quantidadeDano, int atacanteViewID)
    {
        if (vidaAtual <= 0) return;

        // Recupera o GameObject do atacante com base no ID da rede
        GameObject atacanteObj = null;
        if (atacanteViewID != -1)
        {
            PhotonView pv = PhotonView.Find(atacanteViewID);
            if (pv != null) atacanteObj = pv.gameObject;
        }

        // Lógica de Fogo Amigo
        if (atacanteObj != null && atacanteObj.TryGetComponent<Sistemadevida>(out Sistemadevida atacanteVida))
        {
            if (atacanteVida.TimeID == this.timeID && atacanteObj != this.gameObject) return;
        }

        float danoFinal = quantidadeDano * (1f - PorcentagemReducaoDano);
        vidaAtual -= danoFinal;
        vidaAtual = Mathf.Max(vidaAtual, 0f);

        NotificarMudancaDeVida();

        // Apenas o dono do personagem gerencia o aviso da própria morte
        if (vidaAtual <= 0 && photonView.IsMine)
        {
=======
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
>>>>>>> parent of a587532 (Finalizado o multiplayer)
            Morrer();
        }
    }

<<<<<<< HEAD
=======
    [PunRPC]
    private void RPC_SincronizarVida(float novaVida)
    {
        vidaAtual = novaVida;
        NotificarMudancaDeVida();
    }

>>>>>>> parent of a587532 (Finalizado o multiplayer)
    public void Renascer()
    {
        // Se eu renasci, aviso todos na rede para atualizarem minha barra de vida na tela deles
        photonView.RPC("RenascerRPC", RpcTarget.All);
    }

<<<<<<< HEAD
    [PunRPC]
    public void RenascerRPC()
    {
        if (dadosDoHeroi != null)
        {
            vidaAtual = dadosDoHeroi.vidaMaxima;
            PorcentagemReducaoDano = 0f;
            NotificarMudancaDeVida();
        }
=======
        vidaAtual = dadosDoHeroi.vidaMaxima;
        PorcentagemReducaoDano = 0f;
        NotificarMudancaDeVida();

        if (photonView.IsMine)
            photonView.RPC(nameof(RPC_SincronizarVida), RpcTarget.Others, vidaAtual);
>>>>>>> parent of a587532 (Finalizado o multiplayer)
    }

    private void NotificarMudancaDeVida()
    {
        if (dadosDoHeroi != null)
        {
            OnVidaMudou?.Invoke(vidaAtual, dadosDoHeroi.vidaMaxima);
        }
    }

    private void Morrer()
    {
<<<<<<< HEAD
        GerenciadorRespawn gerenciador = FindFirstObjectByType<GerenciadorRespawn>();
        if (gerenciador != null)
        {
            gerenciador.ProcessarMorte(this.gameObject);
=======
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
>>>>>>> parent of a587532 (Finalizado o multiplayer)
        }
    }
}