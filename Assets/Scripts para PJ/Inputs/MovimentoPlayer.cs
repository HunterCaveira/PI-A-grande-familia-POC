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

    void Andar()
    {
        Vector2 leituraInput = acoesPlay.MovimentoPlayer.Andar.ReadValue<Vector2>();

        // O eixo Y do Vector3 agora recebe a sua velocidade vertical calculada
        Vector3 eixos = leituraInput.x * transform.right + leituraInput.y * transform.forward;

        Vector3 movimento = new Vector3(eixos.x, velocidadeVertical, eixos.z);

        characterController.Move(movimento * velocidade * Time.deltaTime);
    }

    // CORREÇÃO 4: Removemos a função Pular() antiga que estava no Update e mantivemos esta
    void SolicitarPulo()
    {
        if (characterController.isGrounded)
        {
            // CORREÇÃO 3: Mudamos de 'alturaDoPulo' para 'forcaPulo' para bater com sua variável
            velocidadeVertical = Mathf.Sqrt(forcaPulo * -2f * gravidade);
        }
    }

    // Resolução conta: (leituraInput.x * transform.right) + (leituraInput.y * transform.forward);
    // se eu apertar a tecla D ou a  SETA PARA DIREITA ele retorna um valor positivo em x = 1
    // leituraInput. = 1
    // transform.right = vector3 (1, 0, 0)
    // leituraInput.x (1) x transform.right(1,0,0) = (1,0,0)

    // se eu aperta a tecla W ou a SETA PARA CIMA ELE retorna um valor positivo em y = 1
    // leituraInput.y = 1
    // tranform.forward = Vector3 (0, 0, 1)
    // leituraInput.y (1) X transdorm.forward (0, 0, 1) = (0, 0, 1)

    //soma as duas partes
    // (1, 0, 0,) + (0, 0, 1) = (1, 0, 1)





}