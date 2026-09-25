// biblioteca de acesso da unity
using UnityEngine;

// um classe chamada MovimentoPlayer
// : -> significao herança
// Monobehaviour - Classe de comportamento unico dentro da unity, que permite a criação de scripts para objetos do jogo.
[RequireComponent(typeof(CharacterController))]
public class MovimentodoPlayer : MonoBehaviour
{
    private CharacterController characterController;
    private AcoesPlayer acoesPlay;

    [Header("Configurações de Movimento")]
    [SerializeField] private float velocidade;
    [SerializeField] private float gravidade = -9.81f;
    [SerializeField] private float forcaPulo = 2f;
    [SerializeField] private float velocidadeVertical;
    private float velocidadeAtual;
    [SerializeField] private AtributosPersonagem atributos;
    private void Awake()
    {
        acoesPlay = new AcoesPlayer();
    }

    // É chamado antes do primeiro frame de execução
    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // CORREÇÃO 1: Forma correta de registrar o evento usando a lambda (ctx =>)
        acoesPlay.MovimentoPlayer.Pular.started += ctx => SolicitarPulo();
    }

    private void OnEnable()
    {
        acoesPlay.MovimentoPlayer.Enable();
    }

    private void OnDisable()
    {
        acoesPlay.MovimentoPlayer.Disable();
    }

    // É chamado uma vez a cada frame.
    void Update()
    {
        // CORREÇÃO 2: O Update agora apenas CHAMA as funções na ordem certa
        AplicarGravidade();
        Correr();
        Andar();
    }

    // CORREÇÃO 2: Criamos a função isolada, fora do Update
    void AplicarGravidade()
    {
        if (characterController.isGrounded && velocidadeVertical < 0)
        {
            velocidadeVertical = -2f; // Reseta a gravidade se estiver no chão
        }
        else
        {
            // Se não estiver no chão, a gravidade acumula com o tempo
            velocidadeVertical += gravidade * Time.deltaTime;
        }
    }
    // CORREÇÃO 4: Removemos a função Pular() antiga que estava no Update e mantivemos esta
    void SolicitarPulo()
    {
        if (characterController.isGrounded)
        {
            // Obtém a força do pulo direto do ScriptableObject
            float forcaPulo = (atributos != null) ? atributos.forcaPulo : 2f;
            velocidadeVertical = Mathf.Sqrt(forcaPulo * -2f * gravidade);
        }
    }
    void Correr()
    {
        // Só altera a velocidade horizontal se o jogador estiver pisando no chão

        float velocidadeBase = (atributos != null) ? atributos.velocidadeMovimento : 5f;
        bool botaoCorrerPressionado = acoesPlay.MovimentoPlayer.Correr.IsPressed();

        if (botaoCorrerPressionado)
        {
            velocidadeAtual = velocidadeBase * 2f;
        }
        else
        {
            velocidadeAtual = velocidadeBase;
        }

        // Se estiver no ar, ele mantém a velocidade do momento em que pulou (conservação de impulso)
    }

    void Andar()
    {
        Vector2 leituraInput = acoesPlay.MovimentoPlayer.Andar.ReadValue<Vector2>();

        // Calcula a direção no plano horizontal
        Vector3 direcao = (transform.right * leituraInput.x) + (transform.forward * leituraInput.y);

        // Multiplica a direção pela velocidade (normal ou de corrida)
        Vector3 movimentoHorizontal = direcao * velocidadeAtual;

        // Junta a velocidade horizontal com a gravidade vertical
        Vector3 movimentoFinal = new Vector3(movimentoHorizontal.x, velocidadeVertical, movimentoHorizontal.z);

        // Aplica o movimento
        characterController.Move(movimentoFinal * Time.deltaTime);
    }
}