using System; // Importa suporte para delegates e eventos (Action).
using UnityEngine; // Importa a biblioteca de componentes da Unity.

public class Sistemadevida : MonoBehaviour // Declara a classe pública Sistemadevida herdando de MonoBehaviour.
{
    // ==========================================
    // 1. ATRIBUTOS E VARIÁVEIS
    // ==========================================

    [Header("Configurações do Herói")] // Seção do Inspector da Unity.
    [SerializeField] private AtributosPersonagem dadosDoHeroi; // Referência à ficha de atributos para ler a vida máxima.

    [Header("PvP e Partida")] // Seção de identificação de partidas no Inspector.
    [SerializeField] private int timeID = 1; // ID do time para controle de Fogo Amigo.

    [SerializeField] public float vidaAtual; // Quantidade atual de pontos de vida mantida em memória.

    public int TimeID => timeID; // Propriedade pública de apenas leitura para consultar o ID de time do herói.

    // [CORREÇÃO DO ERRO CS1061]: Propriedade pública para armazenar a taxa de mitigação/redução de dano sofrido (0f = 0%, 0.3f = 30%)
    public float PorcentagemReducaoDano { get; set; } = 0f;

    // EVENTO: Transmite (vidaAtual, vidaMaxima) para scripts da UI ouvirem e atualizarem as barras na tela
    public event Action<float, float> OnVidaMudou;


    // ==========================================
    // 2. CICLO DE VIDA DA UNITY
    // ==========================================

    private void Awake() // Método invocado assim que o objeto é carregado na memória da cena.
    {
        if (dadosDoHeroi != null) // SE a ficha de atributos foi atribuída no Inspector.
        {
            vidaAtual = dadosDoHeroi.vidaMaxima; // Inicializa a vida atual com o valor máximo configurado na ficha.
        }
    }

    private void Start() // Chamado antes da primeira atualização de frame.
    {
        NotificarMudancaDeVida(); // Dispara o evento inicial para preencher as barras de vida na UI do jogo.
    }


    // ==========================================
    // 3. PROCESSAMENTO DE DANO E MORTE
    // ==========================================

    public void ReceberDano(InformacaoDano dano) // Método público invocado quando o herói sofre um golpe contendo os dados do dano.
    {
        if (vidaAtual <= 0) return; // SE o herói já estiver morto, ignora novos processamentos de dano.

        // Lógica de Fogo Amigo: Checa se o atacante é um aliado do mesmo time
        if (dano.atacante != null && dano.atacante.TryGetComponent<Sistemadevida>(out Sistemadevida atacanteVida))
        {
            if (atacanteVida.TimeID == this.timeID && dano.atacante != this.gameObject) // SE for do mesmo time e não for dano próprio.
            {
                return; // Cancela e ignora a recepção de dano.
            }
        }

        // Calcula o dano final aplicando o desconto da PorcentagemReducaoDano (Ex: se for 0.3, multiplica o dano por 0.7)
        float danoFinal = dano.quantidade * (1f - PorcentagemReducaoDano);

        // Subtrai o dano mitigado da vida atual do herói
        vidaAtual -= danoFinal;
        vidaAtual = Mathf.Max(vidaAtual, 0f); // Garante que a vida nunca fique menor do que zero.

        NotificarMudancaDeVida(); // Notifica a interface gráfica (UI) sobre a perda de vida.

        if (vidaAtual <= 0) // SE a vida atual zerar após a dedução.
        {
            Morrer(); // Dispara a função de eliminação do herói.
        }
    }

    public void Renascer() // Método invocado no momento do respawn.
    {
        if (dadosDoHeroi != null) // SE a ficha de atributos for válida.
        {
            vidaAtual = dadosDoHeroi.vidaMaxima; // Restaura os pontos de vida total.
            PorcentagemReducaoDano = 0f; // Reseta qualquer modificador de mitigação ativo.
            NotificarMudancaDeVida(); // Notifica a UI sobre a restauração da vida.
        }
    }

    private void NotificarMudancaDeVida() // Método privado auxiliar de disparo de eventos.
    {
        // Dispara o evento OnVidaMudou passando (vidaAtual, vidaMaxima) apenas se houver algum script escutando (?.Invoke)
        if (dadosDoHeroi != null)
        {
            OnVidaMudou?.Invoke(vidaAtual, dadosDoHeroi.vidaMaxima);
        }
    }

    private void Morrer() // Lógica de tratamento da morte e envio para a fila de respawn.
    {
        Debug.Log($"{gameObject.name} foi eliminado!"); // Exibe mensagem no Console da Unity.

        GerenciadorRespawn gerenciador = FindFirstObjectByType<GerenciadorRespawn>(); // Procura a instância do GerenciadorRespawn na cena.

        if (gerenciador != null) // SE o gerenciador for encontrado na cena.
        {
            gerenciador.ProcessarMorte(this.gameObject); // Envia o GameObject do jogador eliminado para o gerenciador.
        }
        else // SENÃO (se o gerenciador de respawn não for encontrado).
        {
            Debug.LogWarning("Nenhum GerenciadorRespawn encontrado na cena!"); // Emite um aviso no Console.
        }
    }
}