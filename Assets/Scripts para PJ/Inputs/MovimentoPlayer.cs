using UnityEngine; // Importa a biblioteca da Unity para acessar MonoBehaviour, CharacterController, Vector3, etc.

[RequireComponent(typeof(CharacterController))] // Garante a inclusão automática do componente CharacterController no GameObject do jogador.
public class MovimentodoPlayer : MonoBehaviour // Declara a classe pública MovimentodoPlayer herdando de MonoBehaviour.
{
    // ==========================================
    // 1. VARIÁVEIS E REFERÊNCIAS
    // ==========================================

    private CharacterController characterController; // Referência privada para o componente físico de movimento da Unity.
    private AcoesPlayer acoesPlay; // Referência privada para a classe de mapa de controles (New Input System).

    [Header("Configurações de Movimento")] // Organização das seções no Inspector.
    [SerializeField] private float velocidade; // Variável privada de fallback exibida no Inspector para caso não haja ficha de atributos.
    [SerializeField] private float gravidade = -9.81f; // Força de gravidade aplicada ao eixo vertical Y.
    [SerializeField] private float forcaPulo = 2f; // Força base de impulso para o pulo do herói.
    [SerializeField] private float velocidadeVertical; // Guarda a velocidade acumulada de queda/pulo no eixo Y.
    private float velocidadeAtual; // Guarda o valor final de velocidade calculada no frame atual (andando ou correndo).

    [Header("Ficha de Atributos")]
    [SerializeField] private AtributosPersonagem atributos; // Referência para o ScriptableObject da ficha de atributos do herói.


    // ==========================================
    // 2. CICLO DE VIDA DA UNITY
    // ==========================================

    private void Awake() // Método chamado assim que o script é carregado na memória.
    {
        acoesPlay = new AcoesPlayer(); // Cria uma nova instância da classe gerada pelo New Input System.
    }

    void Start() // Método chamado no primeiro frame de execução do jogo.
    {
        characterController = GetComponent<CharacterController>(); // Busca e atribui o componente CharacterController do GameObject.

        // Registra o evento de acionamento do botão de pulo para disparar a função SolicitarPulo
        acoesPlay.MovimentoPlayer.Pular.started += ctx => SolicitarPulo();
    }

    private void OnEnable() // Executado quando o GameObject é ativado na cena.
    {
        acoesPlay.MovimentoPlayer.Enable(); // Ativa os ouvintes de teclado/controle do New Input System.
    }

    private void OnDisable() // Executado quando o GameObject é desativado na cena.
    {
        acoesPlay.MovimentoPlayer.Disable(); // Desativa os ouvintes de controle para poupar recursos.
    }

    void Update() // Executado a cada frame do jogo.
    {
        AplicarGravidade(); // Executa o cálculo da física de queda e aderência ao chão.
        Correr(); // Determina a velocidade atual com base na ficha e na tecla de corrida.
        Andar(); // Executa o movimento físico tridimensional no mapa.
    }


    // ==========================================
    // 3. MÉTODOS PÚBLICOS DE GERENCIAMENTO DE FICHA
    // ==========================================

    /// <summary>
    /// Permite trocar a ficha de atributos ativa em tempo de execução (Usado pelo Tônico para trocar entre a ficha original e a duplicada).
    /// </summary>
    public void AtualizarFichaAtributos(AtributosPersonagem novaFicha)
    {
        if (novaFicha != null)
        {
            atributos = novaFicha; // Atribui a nova ficha (duplicada com buff ou restaurada original).
            Debug.Log($"[MOVIMENTO PLAYER] Ficha de atributos atualizada! Nova velocidade base: {atributos.velocidadeMovimento} | % Speed: {atributos.porcentagemSpeed}%");
        }
    }


    // ==========================================
    // 4. LÓGICA DE MOVIMENTO E FÍSICA
    // ==========================================

    void AplicarGravidade() // Trata o cálculo da força da gravidade e estado tocando o chão.
    {
        if (characterController.isGrounded && velocidadeVertical < 0) // SE o herói estiver encostando firme no solo.
        {
            velocidadeVertical = -2f; // Fixa a aceleração vertical em um valor baixo negativo para prender o herói ao chão.
        }
        else // SENÃO (se estiver no ar caindo ou pulando).
        {
            velocidadeVertical += gravidade * Time.deltaTime; // Acumula a atração da gravidade no tempo.
        }
    }

    void SolicitarPulo() // Função responsável por calcular e aplicar a força do pulo.
    {
        if (characterController.isGrounded) // SE o personagem estiver pisando no chão.
        {
            // Lê a força do pulo direto da ficha de atributos (ou usa a variável do Inspector como fallback)
            float forcaPuloFinal = (atributos != null) ? atributos.forcaPulo : forcaPulo;
            velocidadeVertical = Mathf.Sqrt(forcaPuloFinal * -2f * gravidade); // Aplica a fórmula matemática de aceleração do pulo.
        }
    }

    void Correr() // Determina a velocidade de deslocamento horizontal.
    {
        float velocidadeBase = 5f; // Valor padrão de fallback.

        if (atributos != null)
        {
            // Calcula a velocidade somando o valor base com a porcentagemSpeed configurada na ficha
            velocidadeBase = atributos.velocidadeMovimento + (atributos.velocidadeMovimento * (atributos.porcentagemSpeed / 100f));
        }

        bool botaoCorrerPressionado = acoesPlay.MovimentoPlayer.Correr.IsPressed(); // Verifica se a tecla de corrida está pressionada.

        if (botaoCorrerPressionado) // SE estiver correndo.
        {
            velocidadeAtual = velocidadeBase * 2f; // Dobra a velocidade base.
        }
        else // SENÃO (caminhada normal).
        {
            velocidadeAtual = velocidadeBase; // Aplica a velocidade base normal.
        }
    }

    void Andar() // Aplica o vetor de movimento no CharacterController da Unity.
    {
        Vector2 leituraInput = acoesPlay.MovimentoPlayer.Andar.ReadValue<Vector2>(); // Lê os valores das teclas WASD ou do analógico.

        // Calcula a direção tridimensional baseada na rotação do próprio herói
        Vector3 direcao = (transform.right * leituraInput.x) + (transform.forward * leituraInput.y);

        // Multiplica a direção pela velocidade recalculada
        Vector3 movimentoHorizontal = direcao * velocidadeAtual;

        // Combina o movimento X/Z com a aceleração vertical da gravidade no eixo Y
        Vector3 movimentoFinal = new Vector3(movimentoHorizontal.x, velocidadeVertical, movimentoHorizontal.z);

        // Desloca o CharacterController no mundo considerando o intervalo de tempo entre frames (deltaTime)
        characterController.Move(movimentoFinal * Time.deltaTime);
    }
}