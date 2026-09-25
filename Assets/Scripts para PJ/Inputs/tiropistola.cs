using UnityEngine;
using TMPro;

public class tiroPistola : MonoBehaviour
{
    [Header("Configurações do Raycast")]
    [SerializeField] private float alcanceDoTiro = 100f;
    [SerializeField] private float espalhamentoHorizontal = 0.5f;
    [SerializeField] private float espalhamentoVertical = 0.5f;
    public LayerMask mascaraDeTiro;

    [Header("Visual do Tiro")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float duracaoLinha = 0.05f;

    [Header("Pool de Balas")]
    public BalaPool meuPoolDeBalas;

    [Header("Dano da Arma")]
    [SerializeField] private float danoPorTiro = 20f;
    [SerializeField] private TipoDano tipoDeDano = TipoDano.Fisico;
    [SerializeField] private string nomeDaArma = "Pistola";

    [Header("Projétil")]
    public float forçaTiro;
    [Tooltip("Força extra para cima (arco da bala). Deixe 0 para tiro reto seguindo a mira")]
    public float forçaCima;

    [Header("Status da Arma")]
    public float tempoEntreAtirar, tempoRecarga;
    public int tamanhoCarregador, tiroPorClick;
    public bool permitirSegurarBotão;

    [Header("Referências")]
    [Tooltip("Arraste a câmera que orbita o personagem aqui (ou deixe vazio para pegar a Camera.main)")]
    public Camera playerCamera;
    public Transform pontoAtaque;
    public GameObject muzzleFlash;
    public TextMeshProUGUI ammunitionDisplay;

    protected int balasSobrando;
    private AcoesPlayer acoesPlayer;
    private bool atirando, preparadoParaAtirar, recarregando;
    private GameObject donoDaArma;
    public bool balaZerada;

    private void Awake()
    {
        acoesPlayer = new AcoesPlayer();
        balasSobrando = tamanhoCarregador;
        preparadoParaAtirar = true;
        donoDaArma = transform.root.gameObject;

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

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

    private void Update()
    {
        MyInput();

        if (ammunitionDisplay != null)
            ammunitionDisplay.SetText(balasSobrando / tiroPorClick + " / " + tamanhoCarregador / tiroPorClick);
    }

    private void MyInput()
    {
        if (permitirSegurarBotão)
            atirando = acoesPlayer.MovimentoPlayer.Atirar.IsPressed();
        else
            atirando = acoesPlayer.MovimentoPlayer.Atirar.WasPressedThisFrame();

        if (acoesPlayer.MovimentoPlayer.Recarregar.WasPressedThisFrame() && balasSobrando < tamanhoCarregador && !recarregando)
            Reload();

        if (preparadoParaAtirar && atirando && !recarregando && balasSobrando > 0)
            Atirar();
    }

    private void Atirar()
    {
        preparadoParaAtirar = false;

        // O Ray sai do centro da tela (mira/crosshair) da câmera em terceira pessoa
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 pontoAcerto;
        if (Physics.Raycast(ray, out RaycastHit acerto, alcanceDoTiro, mascaraDeTiro, QueryTriggerInteraction.Ignore))
            pontoAcerto = acerto.point;
        else
            pontoAcerto = ray.GetPoint(alcanceDoTiro);

        if (lineRenderer != null)
            StartCoroutine(DesenharRaio(pontoAtaque.position, pontoAcerto));

        // Direção do cano da arma até o ponto que a mira da câmera está apontando
        Vector3 direçãoBase = (pontoAcerto - pontoAtaque.position).normalized;

        for (int i = 0; i < tiroPorClick; i++)
        {
            if (balasSobrando <= 0)
            {
                break;
            }
            if (tamanhoCarregador <= 0)
            {
                balaZerada = true;
            }
            else
            {
                balaZerada = false;
            }
            // Reduzimos o impacto do espalhamento multiplicando por um fator menor (ex: 0.05f)
            // Assim, valores como 0.5 no Inspetor viram um desvio sutil e realista
            float x = Random.Range(-espalhamentoHorizontal, espalhamentoHorizontal) * 0.05f;
            float y = Random.Range(-espalhamentoVertical, espalhamentoVertical) * 0.05f;

            Vector3 vetorEspalhamento = (playerCamera.transform.right * x) + (playerCamera.transform.up * y);
            Vector3 direçãoFinal = (direçãoBase + vetorEspalhamento).normalized;

            GameObject balaAtual = meuPoolDeBalas.GetitemFromPool();
            balaAtual.transform.position = pontoAtaque.position;
            balaAtual.transform.forward = direçãoFinal;
            balaAtual.SetActive(true);

            if (balaAtual.TryGetComponent<Bala>(out Bala scriptBala))
                scriptBala.Inicializar(danoPorTiro, tipoDeDano, donoDaArma, nomeDaArma);

            if (balaAtual.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                rb.AddForce(direçãoFinal * forçaTiro, ForceMode.Impulse);

                if (forçaCima > 0f)
                    rb.AddForce(Vector3.up * forçaCima, ForceMode.Impulse);
            }

            balasSobrando--;
        }

        if (muzzleFlash != null)
            Instantiate(muzzleFlash, pontoAtaque.position, Quaternion.identity, pontoAtaque);

        Invoke(nameof(ResetShot), tempoEntreAtirar);
    }

    private void ResetShot() => preparadoParaAtirar = true;

    private void Reload()
    {
        recarregando = true;
        Invoke(nameof(TerminarRecarga), tempoRecarga);
    }

    private void TerminarRecarga()
    {
        balasSobrando = tamanhoCarregador;
        recarregando = false;
    }

    

    private System.Collections.IEnumerator DesenharRaio(Vector3 inicio, Vector3 fim)
    {
        lineRenderer.SetPosition(0, inicio);
        lineRenderer.SetPosition(1, fim);
        lineRenderer.enabled = true;
        yield return new WaitForSeconds(duracaoLinha);
        lineRenderer.enabled = false;
    }
}