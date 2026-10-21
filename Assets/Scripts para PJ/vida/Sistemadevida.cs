using System;
using UnityEngine;
using Photon.Pun; // Adicionado para Multiplayer

public class Sistemadevida : MonoBehaviourPun // Herda de MonoBehaviourPun
{
    [Header("Configurações do Herói")]
    [SerializeField] private AtributosPersonagem dadosDoHeroi;

    [Header("PvP e Partida")]
    [SerializeField] private int timeID = 1;

    [SerializeField] public float vidaAtual;

    public int TimeID => timeID;
    public float PorcentagemReducaoDano { get; set; } = 0f;

    public event Action<float, float> OnVidaMudou;

    private void Awake()
    {
        if (dadosDoHeroi != null) vidaAtual = dadosDoHeroi.vidaMaxima;
    }

    private void Start()
    {
        NotificarMudancaDeVida();
    }

    public void AtualizarFichaAtributos(AtributosPersonagem novaFicha)
    {
        if (novaFicha != null)
        {
            dadosDoHeroi = novaFicha;
            NotificarMudancaDeVida();
        }
    }

    // MULTIPLAYER: Esse método empacota as informações e as envia pela rede via RPC.
    public void ReceberDano(InformacaoDano dano)
    {
        if (vidaAtual <= 0) return;

        int atacanteViewID = -1;
        if (dano.atacante != null)
        {
            PhotonView pvAtacante = dano.atacante.GetComponent<PhotonView>();
            if (pvAtacante != null) atacanteViewID = pvAtacante.ViewID;
        }

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
            Morrer();
        }
    }

    public void Renascer()
    {
        // Se eu renasci, aviso todos na rede para atualizarem minha barra de vida na tela deles
        photonView.RPC("RenascerRPC", RpcTarget.All);
    }

    [PunRPC]
    public void RenascerRPC()
    {
        if (dadosDoHeroi != null)
        {
            vidaAtual = dadosDoHeroi.vidaMaxima;
            PorcentagemReducaoDano = 0f;
            NotificarMudancaDeVida();
        }
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
        GerenciadorRespawn gerenciador = FindFirstObjectByType<GerenciadorRespawn>();
        if (gerenciador != null)
        {
            gerenciador.ProcessarMorte(this.gameObject);
        }
    }
}