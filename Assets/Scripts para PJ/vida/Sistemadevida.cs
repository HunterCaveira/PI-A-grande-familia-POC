using System; // Importa suporte para eventos (Action).
using UnityEngine; // Importa a biblioteca base da Unity.
using Photon.Pun; // IMPORTANTE: Importa as ferramentas de rede do Photon.

// 1. A classe agora herda de MonoBehaviourPun para usar o photonView
public class Sistemadevida : MonoBehaviourPun, IPunObservable
{
    // ==========================================
    // 1. ATRIBUTOS E VARIÁVEIS
    // ==========================================

    [Header("Configurações do Herói")]
    [SerializeField] private AtributosPersonagem dadosDoHeroi;

    [Header("PvP e Partida")]
    [SerializeField] private int timeID = 1;

    [SerializeField] public float vidaAtual;

    // CORREÇÃO: Permitido get e set público para que o script de respawn possa alterar o time livremente
    public int TimeID
    {
        get => timeID;
        set => timeID = value;
    }

    public float PorcentagemReducaoDano { get; set; } = 0f;

    public event Action<float, float> OnVidaMudou;


    // ==========================================
    // 2. INICIALIZAÇÃO
    // ==========================================

    private void Awake()
    {
        // 1. Inicializa a vida máxima baseada na ficha
        if (dadosDoHeroi != null)
        {
            vidaAtual = dadosDoHeroi.vidaMaxima;
        }

        // 2. CORREÇÃO MULTIPLAYER: Descobre o time verdadeiro do dono deste personagem
        // Lê as propriedades customizadas do jogador que é dono deste PhotonView
        if (photonView != null && photonView.Owner != null)
        {
            if (photonView.Owner.CustomProperties.TryGetValue("TimeJogador", out object timeObj))
            {
                timeID = (int)timeObj; // Sobrescreve o timeID do Inspector com o time real (1 ou 2)
                Debug.Log($"[SISTEMA DE VIDA] O jogador {photonView.Owner.NickName} nasceu no Time {timeID}");
            }
        }
    }

    private void Start()
    {
        NotificarMudancaDeVida();
    }


    // ==========================================
    // 3. MÉTODOS DE DANO E REDE (MULTIPLAYER)
    // ==========================================

    public void AtualizarFichaAtributos(AtributosPersonagem novaFicha)
    {
        if (novaFicha != null)
        {
            dadosDoHeroi = novaFicha;
            NotificarMudancaDeVida();
        }
    }

    /// <summary>
    /// Calcula o dano localmente e avisa TODOS OS COMPUTADORES para aplicarem a perda de vida.
    /// </summary>
    public void ReceberDano(InformacaoDano dano)
    {
        if (vidaAtual <= 0) return;

        // Lógica de Fogo Amigo
        if (dano.atacante != null && dano.atacante.TryGetComponent<Sistemadevida>(out Sistemadevida atacanteVida))
        {
            if (atacanteVida.TimeID == this.timeID && dano.atacante != this.gameObject)
            {
                return; // Ignora o dano (Fogo amigo)
            }
        }

        // Calcula o dano final descontando a armadura/mitigação
        float danoFinal = dano.quantidade * (1f - PorcentagemReducaoDano);

        // MULTIPLAYER: Em vez de descontar a vida só aqui, manda a ordem de dano para TODOS na sala!
        photonView.RPC("AplicarDanoPelaRede", RpcTarget.All, danoFinal);
    }

    /// <summary>
    /// [PunRPC] é uma função que roda simultaneamente no computador de todos os jogadores da sala.
    /// </summary>
    [PunRPC]
    private void AplicarDanoPelaRede(float danoFinal)
    {
        if (vidaAtual <= 0) return; // Evita morte dupla

        // Subtrai a vida e garante que não fica menor que zero
        vidaAtual -= danoFinal;
        vidaAtual = Mathf.Max(vidaAtual, 0f);

        NotificarMudancaDeVida(); // Atualiza a barra de vida flutuante do personagem na tela

        // APENAS o computador que é "dono" deste personagem chama a lógica de morrer e respawnar,
        // para evitar que 5 computadores tentem renascer o mesmo jogador ao mesmo tempo.
        if (vidaAtual <= 0 && photonView.IsMine)
        {
            Morrer();
        }
    }

    /// <summary>
    /// Avisa todos os computadores que este jogador renasceu com a vida cheia.
    /// </summary>
    public void Renascer()
    {
        photonView.RPC("RenascerPelaRede", RpcTarget.All);
    }

    [PunRPC]
    private void RenascerPelaRede()
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
        Debug.Log($"{gameObject.name} foi eliminado!");

        GerenciadorRespawn gerenciador = FindFirstObjectByType<GerenciadorRespawn>();

        if (gerenciador != null)
        {
            gerenciador.ProcessarMorte(this.gameObject);
        }
    }

    // ==========================================
    // 4. SINCRONIZAÇÃO CONTÍNUA (GARANTIA)
    // ==========================================

    /// <summary>
    /// Garante que se alguém entrar atrasado na sala, veja a vida atualizada deste jogador.
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(vidaAtual); // O Dono envia a sua vida
        }
        else
        {
            vidaAtual = (float)stream.ReceiveNext(); // Os outros leem a vida
            NotificarMudancaDeVida(); // Atualizam as barrinhas
        }
    }
}