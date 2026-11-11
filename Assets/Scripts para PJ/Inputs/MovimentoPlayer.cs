using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(CharacterController))]
public class MovimentodoPlayer : MonoBehaviourPun
{
    private CharacterController characterController;
    private AcoesPlayer acoesPlay;
    private bool inputAtivo = false; // true só no dono do personagem

    [SerializeField] Animator animator;

    [Header("Configurações de Movimento")]
    [SerializeField] private float velocidade;
    [SerializeField] private float gravidade = -9.81f;
    [SerializeField] private float forcaPulo = 2f;
    [SerializeField] private float velocidadeVertical;
    private float velocidadeAtual;

    [Header("Ficha de Atributos")]
    [SerializeField] private AtributosPersonagem atributos;

    public AtributosPersonagem AtributosAtuais => atributos;

    private bool SouDono => photonView == null || photonView.IsMine;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        acoesPlay = new AcoesPlayer();
    }

    private void Start()
    {
        // No Start o Photon já definiu ViewID e dono, então IsMine é confiável.
        if (!SouDono) return;

        acoesPlay.MovimentoPlayer.Pular.started += OnPular;
        acoesPlay.MovimentoPlayer.Enable();
        inputAtivo = true;
    }

    private void OnEnable()
    {
        // Só reativa o input se já foi confirmado que sou o dono
        if (inputAtivo)
            acoesPlay?.MovimentoPlayer.Enable();
    }

    private void OnDisable()
    {
        acoesPlay?.MovimentoPlayer.Disable();
    }

    private void OnDestroy()
    {
        if (acoesPlay == null) return;

        if (inputAtivo)
            acoesPlay.MovimentoPlayer.Pular.started -= OnPular;

        acoesPlay.Dispose();
    }

    private void OnPular(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        SolicitarPulo();
    }

    private void Update()
    {
        if (!SouDono || !inputAtivo) return;

        AplicarGravidade();
        Correr();
        Andar();
    }

    public void AtualizarFichaAtributos(AtributosPersonagem novaFicha)
    {
        if (novaFicha != null)
        {
            atributos = novaFicha;
            Debug.Log($"[MOVIMENTO PLAYER] Ficha de atributos atualizada! Nova velocidade base: {atributos.velocidadeMovimento} | % Speed: {atributos.porcentagemSpeed}%");
        }
    }

    private void AplicarGravidade()
    {
        if (characterController.isGrounded && velocidadeVertical < 0)
            velocidadeVertical = -2f;
        else
            velocidadeVertical += gravidade * Time.deltaTime;
    }

    private void SolicitarPulo()
    {
        if (!SouDono) return;

        if (characterController.isGrounded)
        {
            float forcaPuloFinal = (atributos != null) ? atributos.forcaPulo : forcaPulo;
            velocidadeVertical = Mathf.Sqrt(forcaPuloFinal * -2f * gravidade);
        }
    }

    private void Correr()
    {
        float velocidadeBase = 5f;

        if (atributos != null)
            velocidadeBase = atributos.velocidadeMovimento + (atributos.velocidadeMovimento * (atributos.porcentagemSpeed / 100f));

        bool botaoCorrerPressionado = acoesPlay.MovimentoPlayer.Correr.IsPressed();
        velocidadeAtual = botaoCorrerPressionado ? velocidadeBase * 2f : velocidadeBase;
    }

    private void Andar()
    {
        Vector2 leituraInput = acoesPlay.MovimentoPlayer.Andar.ReadValue<Vector2>();

        Vector3 direcao = (transform.right * leituraInput.x) + (transform.forward * leituraInput.y);
        Vector3 movimentoHorizontal = direcao * velocidadeAtual;
        Vector3 movimentoFinal = new Vector3(movimentoHorizontal.x, velocidadeVertical, movimentoHorizontal.z);

        characterController.Move(movimentoFinal * Time.deltaTime);

        if (movimentoHorizontal != Vector3.zero)
        {
            animator.SetFloat("horizontal", 1f);
        }
        else
        {
            animator.SetFloat("horizontal", 0f);
        }
    }

    // ==========================================
    // LENTIDÃO (efeito precisa rodar no DONO do personagem)
    // ==========================================

    public void AplicarLentidao(float porcentagemReducao, float duracao)
    {
        if (SouDono)
            StartCoroutine(RotinaAplicarLentidao(porcentagemReducao, duracao));
        else
            photonView.RPC(nameof(RPC_AplicarLentidao), photonView.Owner, porcentagemReducao, duracao);
    }

    [PunRPC]
    private void RPC_AplicarLentidao(float porcentagemReducao, float duracao)
    {
        StartCoroutine(RotinaAplicarLentidao(porcentagemReducao, duracao));
    }

    private System.Collections.IEnumerator RotinaAplicarLentidao(float porcentagemReducao, float duracao)
    {
        AtributosPersonagem fichaOriginal = atributos;
        if (fichaOriginal == null) yield break;

        AtributosPersonagem fichaDebuffada = Instantiate(fichaOriginal);
        fichaDebuffada.velocidadeMovimento = fichaOriginal.velocidadeMovimento * (1f - porcentagemReducao);

        atributos = fichaDebuffada;
        Debug.Log($"[MOVIMENTO PLAYER] Sofreu lentidão! Velocidade reduzida para: {atributos.velocidadeMovimento}");

        yield return new WaitForSeconds(duracao);

        atributos = fichaOriginal;
        if (fichaDebuffada != null)
            Destroy(fichaDebuffada);

        Debug.Log("[MOVIMENTO PLAYER] Efeito de lentidão acabou. Velocidade restaurada.");
    }
}