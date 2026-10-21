using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(CharacterController))]
public class MovimentodoPlayer : MonoBehaviourPun
{
    private CharacterController characterController;
    private AcoesPlayer acoesPlay;

    [Header("Configurações de Movimento")]
    [SerializeField] private float velocidade;
    [SerializeField] private float gravidade = -9.81f;
    [SerializeField] private float forcaPulo = 2f;
    [SerializeField] private float velocidadeVertical;
    private float velocidadeAtual;

    [Header("Ficha de Atributos")]
    [SerializeField] private AtributosPersonagem atributos;

    public AtributosPersonagem AtributosAtuais => atributos;

    private void Awake()
    {
        acoesPlay = new AcoesPlayer();
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (photonView != null && !photonView.IsMine) return;

        acoesPlay.MovimentoPlayer.Pular.started += ctx => SolicitarPulo();
    }

    private void OnEnable()
    {
        if (photonView == null || photonView.IsMine)
        {
            acoesPlay?.MovimentoPlayer.Enable();
        }
    }

    private void OnDisable()
    {
        acoesPlay?.MovimentoPlayer.Disable();
    }

    void Update()
    {
        if (photonView != null && !photonView.IsMine) return;

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

    void AplicarGravidade()
    {
        if (characterController.isGrounded && velocidadeVertical < 0)
        {
            velocidadeVertical = -2f;
        }
        else
        {
            velocidadeVertical += gravidade * Time.deltaTime;
        }
    }

    void SolicitarPulo()
    {
        if (photonView != null && !photonView.IsMine) return;

        if (characterController.isGrounded)
        {
            float forcaPuloFinal = (atributos != null) ? atributos.forcaPulo : forcaPulo;
            velocidadeVertical = Mathf.Sqrt(forcaPuloFinal * -2f * gravidade);
        }
    }

    void Correr()
    {
        float velocidadeBase = 5f;

        if (atributos != null)
        {
            velocidadeBase = atributos.velocidadeMovimento + (atributos.velocidadeMovimento * (atributos.porcentagemSpeed / 100f));
        }

        bool botaoCorrerPressionado = acoesPlay.MovimentoPlayer.Correr.IsPressed();

        if (botaoCorrerPressionado)
        {
            velocidadeAtual = velocidadeBase * 2f;
        }
        else
        {
            velocidadeAtual = velocidadeBase;
        }
    }

    void Andar()
    {
        Vector2 leituraInput = acoesPlay.MovimentoPlayer.Andar.ReadValue<Vector2>();

        Vector3 direcao = (transform.right * leituraInput.x) + (transform.forward * leituraInput.y);
        Vector3 movimentoHorizontal = direcao * velocidadeAtual;
        Vector3 movimentoFinal = new Vector3(movimentoHorizontal.x, velocidadeVertical, movimentoHorizontal.z);

        characterController.Move(movimentoFinal * Time.deltaTime);
    }

    public void AplicarLentidao(float porcentagemReducao, float duracao)
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
        {
            Destroy(fichaDebuffada);
        }

        Debug.Log("[MOVIMENTO PLAYER] Efeito de lentidão acabou. Velocidade restaurada.");
    }
}