using UnityEngine;
using TMPro;
using Unity.VisualScripting;


public class tiropistola : MonoBehaviour
{
    [Header("Configurações do Raycast (Física e Espalhamento)")]
    [Tooltip("Até quantos metros o tiro consegue acertar o alvo? (Comprimento)")]
    [SerializeField] private float alcanceDoTiro = 100f;

    [Tooltip("Largura do espalhamento da bala (Eixo X)")]
    [SerializeField] private float espalhamentoHorizontal = 0.5f;

    [Tooltip("Altura do espalhamento da bala (Eixo Y)")]
    [SerializeField] private float espalhamentoVertical = 0.5f;

    [Header("Configurações de Mira")]
    [Tooltip("Quais camadas o Raycast pode acertar? (Ex: Cenário, Inimigos)")]
    public LayerMask mascaraDeTiro;

    [Header("Visual do Tiro")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float duracaoLinha = 0.05f;

    [Header("Pool de balas")]
    public BalaPool meuPoolDeBalas;
    [Header("Configurações de Dano da Arma")]
    [SerializeField] private float danoPorTiro = 20f;
    [SerializeField] private TipoDano tipoDeDano = TipoDano.Fisico;
    [SerializeField] private string nomeDaArma = "Pistola";

    [Header("Configurações do Projétil")]
    public GameObject bala;
    public float forçaTiro, forçaCima;

    [Header("Status da Arma")]
    public float tempoEntreAtirar, Espalhar, tempoRecarga;
    public int tamanhoCarregador, tiroPorClick;
    public bool permetirSegurarBotão;

    private int balasSobrando;
    private AcoesPlayer acoesPlayer;
    private bool atirando, preparadoParaAtirar, recarregando;

    [Header("Referências")]
    public Camera fpsCam;
    public Transform pontoAtaque;
    public GameObject muzzleFlash;
    public TextMeshProUGUI ammunitionDisplay;

    // Referência do jogador que segura a arma (para validação de PvP)
    private GameObject donoDaArma;

    private void Awake()
    {
        acoesPlayer = new AcoesPlayer();
        balasSobrando = tamanhoCarregador;
        preparadoParaAtirar = true;

        // Pega a raiz do objeto (Player) que está segurando a arma
        donoDaArma = transform.root.gameObject;
    }

    public void Update()
    {
        MyInput();

        if (ammunitionDisplay != null)
            ammunitionDisplay.SetText(balasSobrando / tiroPorClick + " / " + tamanhoCarregador / tiroPorClick);
    }
    //Debug.DrawRay(_camera.transform.position, _camera.transform.TransformDirection(Vector3.forward) , Color.red);
    private void OnEnable()
    {
        acoesPlayer.MovimentoPlayer.Atirar.Enable();
        acoesPlayer.MovimentoPlayer.Recarregar.Enable();
        balasSobrando = tamanhoCarregador;
    }

    private void OnDisable()
    {
        acoesPlayer.MovimentoPlayer.Atirar.Disable();
        acoesPlayer.MovimentoPlayer.Recarregar.Disable();
    }

    private void MyInput()
    {
        if (permetirSegurarBotão)
            atirando = acoesPlayer.MovimentoPlayer.Atirar.IsPressed();
        else
            atirando = acoesPlayer.MovimentoPlayer.Atirar.WasPressedThisFrame();

        if (acoesPlayer.MovimentoPlayer.Recarregar.WasPressedThisFrame() && balasSobrando < tamanhoCarregador && !recarregando)
            Reload();
        //auto recarga
      //  if (preparadoParaAtirar && atirando && !recarregando && balasSobrando <= 0)
        //    Reload();

        if (preparadoParaAtirar && atirando && !recarregando && balasSobrando > 0)
        {
            atirar();
        }
    }

    private void atirar()
    {
        preparadoParaAtirar = false;

        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit acerto;
        Vector3 pontoAcerto;

        // 1. AQUI APLICAMOS O ALCANCE (COMPRIMENTO/DISTÂNCIA) DO RAYCAST
        if (Physics.Raycast(ray, out acerto, alcanceDoTiro, mascaraDeTiro, QueryTriggerInteraction.Ignore))
            pontoAcerto = acerto.point;
        else
            pontoAcerto = ray.GetPoint(alcanceDoTiro); // Se não bater em nada, vai até o limite do alcance

        if (lineRenderer != null)
        {
            StartCoroutine(DesenharRaio(pontoAtaque.position, pontoAcerto));
        }

        Vector3 direçãoSemEspalhar = pontoAcerto - pontoAtaque.position;

        for (int i = 0; i < tiroPorClick; i++)
        {
            if (balasSobrando <= 0) break;

            // 2. APLICAÇÃO DO ESPALHAMENTO BASEADO NA ORIENTAÇÃO DA CÂMERA
            float x = Random.Range(-espalhamentoHorizontal, espalhamentoHorizontal);
            float y = Random.Range(-espalhamentoVertical, espalhamentoVertical);

            // Transforma o desvio local (X e Y) em direções reais relativas à câmera
            Vector3 vetorEspalhamento = (fpsCam.transform.right * x) + (fpsCam.transform.up * y);

            // Aplica o vetor de espalhamento sobre a direção original
            Vector3 direçãoComEspalhar = direçãoSemEspalhar + vetorEspalhamento;

            // ... (O resto do seu código de pegar a bala do Pool continua igualzinho aqui embaixo)
            GameObject balaAtual = meuPoolDeBalas.GetitemFromPool();
            balaAtual.transform.position = pontoAtaque.position;
            balaAtual.transform.forward = direçãoComEspalhar.normalized;
            balaAtual.SetActive(true);

            if (balaAtual.TryGetComponent<Bala>(out Bala scriptBala))
            {
                scriptBala.Inicializar(danoPorTiro, tipoDeDano, donoDaArma, nomeDaArma);
            }

            Rigidbody rb = balaAtual.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                rb.AddForce(direçãoComEspalhar.normalized * forçaTiro, ForceMode.Impulse);
                rb.AddForce(fpsCam.transform.up * forçaCima, ForceMode.Impulse);
            }

            balasSobrando--;
        }

        Invoke("ResetShot", tempoEntreAtirar);
    }

    private void ResetShot()
    {
        preparadoParaAtirar = true;
    }   

    private void Reload()
    {
        recarregando = true;
        Invoke("TerminarRecarga", tempoRecarga);
    }

    private void TerminarRecarga()
    {
        balasSobrando = tamanhoCarregador;
        recarregando = false;
    }
    //OPCIONAL VER O RAYCAST
    private System.Collections.IEnumerator DesenharRaio(Vector3 inicio, Vector3 fim)
    {
        lineRenderer.SetPosition(0, inicio);
        lineRenderer.SetPosition(1, fim);
        lineRenderer.enabled = true;

        yield return new WaitForSeconds(duracaoLinha);

        lineRenderer.enabled = false;
    }
}