using UnityEngine; // Importa a biblioteca da Unity para acessar MonoBehaviour, CharacterController, Vector3, etc.

[RequireComponent(typeof(CharacterController))] // Garante a inclus�o autom�tica do componente CharacterController no GameObject do jogador.
public class MovimentodoPlayer : MonoBehaviour // Declara a classe p�blica MovimentodoPlayer herdando de MonoBehaviour.
{
    // ==========================================
    // 1. VARI�VEIS E REFER�NCIAS
    // ==========================================

    private CharacterController characterController; // Refer�ncia privada para o componente f�sico de movimento da Unity.
    private AcoesPlayer acoesPlay; // Refer�ncia privada para a classe de mapa de controles (New Input System).

    [Header("Configura��es de Movimento")] // Organiza��o das se��es no Inspector.
    [SerializeField] private float velocidade; // Vari�vel privada de fallback exibida no Inspector para caso n�o haja ficha de atributos.
    [SerializeField] private float gravidade = -9.81f; // For�a de gravidade aplicada ao eixo vertical Y.
    [SerializeField] private float forcaPulo = 2f; // For�a base de impulso para o pulo do her�i.
    [SerializeField] private float velocidadeVertical; // Guarda a velocidade acumulada de queda/pulo no eixo Y.
    private float velocidadeAtual; // Guarda o valor final de velocidade calculada no frame atual (andando ou correndo).

    [Header("Ficha de Atributos")]
    [SerializeField] private AtributosPersonagem atributos; // Refer�ncia para o ScriptableObject da ficha de atributos do her�i.

    // PROPRIEDADE P�BLICA EXPOSTA (Permite que o ProjetilLentidao acesse a ficha atual)
    public AtributosPersonagem AtributosAtuais => atributos;

    [Header("Animação")]
    [SerializeField] private Animator playerAnimation;

    // ==========================================
    // 2. CICLO DE VIDA DA UNITY
    // ==========================================

    private void Awake() // M�todo chamado assim que o script � carregado na mem�ria.
    {
        acoesPlay = new AcoesPlayer(); // Cria uma nova inst�ncia da classe gerada pelo New Input System.
    }

    void Start() // M�todo chamado no primeiro frame de execu��o do jogo.
    {
        characterController = GetComponent<CharacterController>(); // Busca e atribui o componente CharacterController do GameObject.

        // Registra o evento de acionamento do bot�o de pulo para disparar a fun��o SolicitarPulo
        acoesPlay.MovimentoPlayer.Pular.started += ctx => SolicitarPulo();
    }

    private void OnEnable() // Executado quando o GameObject � ativado na cena.
    {
        acoesPlay.MovimentoPlayer.Enable(); // Ativa os ouvintes de teclado/controle do New Input System.
    }

    private void OnDisable() // Executado quando o GameObject � desativado na cena.
    {
        acoesPlay.MovimentoPlayer.Disable(); // Desativa os ouvintes de controle para poupar recursos.
    }

    void Update() // Executado a cada frame do jogo.
    {
        AplicarGravidade(); // Executa o c�lculo da f�sica de queda e ader�ncia ao ch�o.
        Correr(); // Determina a velocidade atual com base na ficha e na tecla de corrida.
        Andar(); // Executa o movimento f�sico tridimensional no mapa.
    }


    // ==========================================
    // 3. M�TODOS P�BLICOS DE GERENCIAMENTO DE FICHA
    // ==========================================

    /// <summary>
    /// Permite trocar a ficha de atributos ativa em tempo de execu��o (Usado pelo T�nico e pela Lentid�o).
    /// </summary>
    public void AtualizarFichaAtributos(AtributosPersonagem novaFicha)
    {
        if (novaFicha != null)
        {
            atributos = novaFicha; // Atribui a nova ficha (duplicada com buff/debuff ou restaurada original).
            Debug.Log($"[MOVIMENTO PLAYER] Ficha de atributos atualizada! Nova velocidade base: {atributos.velocidadeMovimento} | % Speed: {atributos.porcentagemSpeed}%");
        }
    }


    // ==========================================
    // 4. L�GICA DE MOVIMENTO E F�SICA
    // ==========================================

    void AplicarGravidade() // Trata o c�lculo da for�a da gravidade e estado tocando o ch�o.
    {
        if (characterController.isGrounded && velocidadeVertical < 0) // SE o her�i estiver encostando firme no solo.
        {
            velocidadeVertical = -2f; // Fixa a acelera��o vertical em um valor baixo negativo para prender o her�i ao ch�o.
        }
        else // SEN�O (se estiver no ar caindo ou pulando).
        {
            velocidadeVertical += gravidade * Time.deltaTime; // Acumula a atra��o da gravidade no tempo.
        }
    }

    void SolicitarPulo() // Fun��o respons�vel por calcular e aplicar a for�a do pulo.
    {
        if (characterController.isGrounded) // SE o personagem estiver pisando no ch�o.
        {
            // L� a for�a do pulo direto da ficha de atributos (ou usa a vari�vel do Inspector como fallback)
            float forcaPuloFinal = (atributos != null) ? atributos.forcaPulo : forcaPulo;
            velocidadeVertical = Mathf.Sqrt(forcaPuloFinal * -2f * gravidade); // Aplica a f�rmula matem�tica de acelera��o do pulo.
        }
    }

    void Correr() // Determina a velocidade de deslocamento horizontal.
    {
        float velocidadeBase = 5f; // Valor padr�o de fallback.

        if (atributos != null)
        {
            // Calcula a velocidade somando o valor base com a porcentagemSpeed configurada na ficha
            velocidadeBase = atributos.velocidadeMovimento + (atributos.velocidadeMovimento * (atributos.porcentagemSpeed / 100f));
        }

        bool botaoCorrerPressionado = acoesPlay.MovimentoPlayer.Correr.IsPressed(); // Verifica se a tecla de corrida est� pressionada.

        if (botaoCorrerPressionado) // SE estiver correndo.
        {
            velocidadeAtual = velocidadeBase * 2f; // Dobra a velocidade base.
        }
        else // SEN�O (caminhada normal).
        {
            velocidadeAtual = velocidadeBase; // Aplica a velocidade base normal.
        }
    }

    
    void Andar()
    {
        Vector2 leituraInput =
            acoesPlay.MovimentoPlayer.Andar.ReadValue<Vector2>();

        // Calcula a direção relativa à orientação atual do personagem.
        Vector3 direcao =
            (transform.right * leituraInput.x) +
            (transform.forward * leituraInput.y);

        // Evita movimento diagonal mais rápido.
        if (direcao.magnitude > 1f)
            direcao.Normalize();

        // Calcula o movimento horizontal.
        Vector3 movimentoHorizontal = direcao * velocidadeAtual;

        // Adiciona a velocidade vertical da gravidade e do pulo.
        Vector3 movimentoFinal = new Vector3(
            movimentoHorizontal.x,
            velocidadeVertical,
            movimentoHorizontal.z
        );

        // Move o personagem respeitando as colisões.
        characterController.Move(movimentoFinal * Time.deltaTime);

        // Identifica se existe entrada de movimento.
        bool andando = leituraInput.sqrMagnitude > 0.01f;

        // Atualiza a animação.
        if (acoesPlay.MovimentoPlayer.Andar.triggered)
        {
            playerAnimation.SetBool("isWalking", andando);
        }
    }

    public void AplicarLentidao(float porcentagemReducao, float duracao)
    {
        //StartCoroutine(RotinaAplicarLentidao(porcentagemReducao, duracao));
    }

   /* private IEnumerator RotinaAplicarLentidao(float porcentagemReducao, float duracao)
    {
        AtributosPersonagem fichaOriginal = atributos;
        if (fichaOriginal == null) yield break;

        // Cria a duplicata em mem�ria
        AtributosPersonagem fichaDebuffada = Instantiate(fichaOriginal);
        fichaDebuffada.velocidadeMovimento = fichaOriginal.velocidadeMovimento * (1f - porcentagemReducao);

        // Aplica a nova ficha com velocidade reduzida
        atributos = fichaDebuffada;
        Debug.Log($"[MOVIMENTO PLAYER] Sofreu lentid�o! Velocidade reduzida para: {atributos.velocidadeMovimento}");

        // Aguarda a dura��o do efeito
        yield return new WaitForSeconds(duracao);

        // Restaura a ficha original e destr�i a tempor�ria
        atributos = fichaOriginal;
        if (fichaDebuffada != null)
        {
            Destroy(fichaDebuffada);
        }

        Debug.Log("[MOVIMENTO PLAYER] Efeito de lentid�o acabou. Velocidade restaurada.");
    }
   */
}