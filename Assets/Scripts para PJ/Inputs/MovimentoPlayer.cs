using UnityEngine;
using Photon.Pun; // Adicionado para Multiplayer

[RequireComponent(typeof(CharacterController))]
public class MovimentodoPlayer : MonoBehaviourPun // Herda de MonoBehaviourPun
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

        // MULTIPLAYER: Se for um clone da rede, desativa a física local para evitar colisões fantasmas e sai
        if (!photonView.IsMine)
        {
            characterController.enabled = false;
            return;
        }

        acoesPlay.MovimentoPlayer.Pular.started += ctx => SolicitarPulo();
    }

    private void OnEnable()
    {
        if (photonView != null && !photonView.IsMine) return; // Trava Multiplayer
        acoesPlay.MovimentoPlayer.Enable();
    }

    private void OnDisable()
    {
        acoesPlay.MovimentoPlayer.Disable();
    }

    void Update()
    {
        if (!photonView.IsMine) return; // MULTIPLAYER: Apenas o dono se move

        AplicarGravidade();
        Correr();
        Andar();
    }

    public void AtualizarFichaAtributos(AtributosPersonagem novaFicha)
    {
        if (novaFicha != null)
        {
            atributos = novaFicha;
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

        if (acoesPlay.MovimentoPlayer.Correr.IsPressed())
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
        if (!photonView.IsMine) return; // Apenas o dono gerencia sua própria lentidão localmente
        StartCoroutine(RotinaAplicarLentidao(porcentagemReducao, duracao));
    }

    private System.Collections.IEnumerator RotinaAplicarLentidao(float porcentagemReducao, float duracao)
    {
        AtributosPersonagem fichaOriginal = atributos;
        if (fichaOriginal == null) yield break;

        AtributosPersonagem fichaDebuffada = Instantiate(fichaOriginal);
        fichaDebuffada.velocidadeMovimento = fichaOriginal.velocidadeMovimento * (1f - porcentagemReducao);

        atributos = fichaDebuffada;

        yield return new WaitForSeconds(duracao);

        atributos = fichaOriginal;
        if (fichaDebuffada != null) Destroy(fichaDebuffada);
    }
}