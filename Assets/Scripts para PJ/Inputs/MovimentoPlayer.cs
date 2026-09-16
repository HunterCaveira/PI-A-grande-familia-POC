using UnityEngine; // Importa a biblioteca principal do motor da Unity para acessar MonoBehaviour, CharacterController, etc.

[RequireComponent(typeof(CharacterController))] // Garante a presença automática do CharacterController no GameObject.
public class MovimentodoPlayer : MonoBehaviour // Declara a classe pública herdando de MonoBehaviour.
{
    // ==========================================
    // 1. VARIÁVEIS DE COMPONENTES E INPUT
    // ==========================================

    private CharacterController characterController; // Referência para o controlador de movimento físico da Unity.
    private AcoesPlayer acoesPlay; // Referência para o sistema de inputs do jogador.

    [Header("Configurações de Movimento")] // Seção de organização no Inspector.
    [SerializeField] private float velocidade; // Velocidade privada de fallback do Inspector.
    [SerializeField] private float gravidade = -9.81f; // Força de gravidade aplicada ao personagem no ar.
    [SerializeField] private float forcaPulo = 2f; // Força de impulso vertical do pulo.
    [SerializeField] private float velocidadeVertical; // Acumulador de aceleração vertical no eixo Y.
    private float velocidadeAtual; // Guarda a velocidade calculada para o frame atual (andando ou correndo).

    [Header("Ficha de Atributos")]
    [SerializeField] private AtributosPersonagem atributos; // Referência ao ScriptableObject de atributos do personagem.


    // ==========================================
    // 2. CICLO DE VIDA DA UNITY
    // ==========================================

    private void Awake() // Método de inicialização de instâncias.
    {
        acoesPlay = new AcoesPlayer(); // Instancia a classe de comandos de Input do jogador.
    }

    void Start() // Método chamado antes do primeiro frame de execução.
    {
        characterController = GetComponent<CharacterController>(); // Atribui o componente CharacterController do GameObject.

        // Registra o evento da tecla de pulo chamando o método SolicitarPulo
        acoesPlay.MovimentoPlayer.Pular.started += ctx => SolicitarPulo();
    }

    private void OnEnable() // Executado quando o script é ativado.
    {
        acoesPlay.MovimentoPlayer.Enable(); // Ativa o mapa de ações de input do jogador.
    }

    private void OnDisable() // Executado quando o script é desativado.
    {
        acoesPlay.MovimentoPlayer.Disable(); // Desativa o mapa de ações para poupar processamento e evitar vazamentos.
    }

    void Update() // Executado uma vez por frame do jogo.
    {
        AplicarGravidade(); // Executa o cálculo da física de queda e grounded.
        Correr(); // Recalcula a velocidade base considerando a ficha de atributos e o botão de corrida.
        Andar(); // Aplica a movimentação tridimensional final no CharacterController.
    }


    // ==========================================
    // 3. MÉTODOS DE CÁLCULO FÍSICO E MOVIMENTO
    // ==========================================

    void AplicarGravidade() // Trata o acúmulo da gravidade e a estabilização no chão.
    {
        if (characterController.isGrounded && velocidadeVertical < 0) // SE o jogador estiver pisando firme no solo.
        {
            velocidadeVertical = -2f; // Reseta a gravidade para um valor negativo baixo mantendo o herói preso ao chão.
        }
        else // SENÃO (se estiver caindo ou pulando no ar).
        {
            velocidadeVertical += gravidade * Time.deltaTime; // Acumula a força de atração da gravidade no tempo.
        }
    }

    void SolicitarPulo() // Método responsável por aplicar a força de impulso do pulo.
    {
        if (characterController.isGrounded) // SE o personagem estiver pisando no solo.
        {
            // Obtém a força do pulo direto da ficha de atributos (ou usa o valor do Inspector como fallback)
            float forcaPuloFinal = (atributos != null) ? atributos.forcaPulo : forcaPulo;
            velocidadeVertical = Mathf.Sqrt(forcaPuloFinal * -2f * gravidade); // Aplica a fórmula física de velocidade de pulo.
        }
    }

    void Correr() // Calcula a velocidade do movimento em tempo real.
    {
        // Lê a VelocidadeFinal (Base x Multiplicador) direto da ficha de atributos a cada frame
        float velocidadeBase = (atributos != null) ? atributos.VelocidadeFinal : 5f;

        bool botaoCorrerPressionado = acoesPlay.MovimentoPlayer.Correr.IsPressed(); // Checa se o botão de corrida está pressionado.

        if (botaoCorrerPressionado) // SE estiver correndo.
        {
            velocidadeAtual = velocidadeBase * 2f; // Dobra a velocidade base calculada.
        }
        else // SENÃO (caminhada normal).
        {
            velocidadeAtual = velocidadeBase; // Aplica a velocidade base normal.
        }
    }

    void Andar() // Aplica o deslocamento tridimensional no motor físico.
    {
        Vector2 leituraInput = acoesPlay.MovimentoPlayer.Andar.ReadValue<Vector2>(); // Lê a direção das teclas WASD ou analógico.

        // Recalcula a velocidade no próprio frame do movimento para garantir atualização imediata
        Correr();

        // Calcula a direção local orientada pela rotação do herói
        Vector3 direcao = (transform.right * leituraInput.x) + (transform.forward * leituraInput.y);

        // Aplica o vetor de movimento horizontal com a velocidade recém-calculada
        Vector3 movimentoHorizontal = direcao * velocidadeAtual;

        // Combina o vetor horizontal com a aceleração vertical da gravidade no eixo Y
        Vector3 movimentoFinal = new Vector3(movimentoHorizontal.x, velocidadeVertical, movimentoHorizontal.z);

        // Move o CharacterController no mundo multiplicado pelo delta time
        characterController.Move(movimentoFinal * Time.deltaTime);
    }
}