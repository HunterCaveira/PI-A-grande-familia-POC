using System.Collections;
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
    public float forçaTiro = 50f;
    [Tooltip("Força extra para cima (arco da bala). Deixe 0 para tiro reto seguindo a mira")]
    public float forçaCima = 0f;

    [Header("Status da Arma")]
    public float tempoEntreAtirar = 0.2f, tempoRecarga = 1.5f;
    public int tamanhoCarregador = 12, tiroPorClick = 1;
    public bool permitirSegurarBotão = false;

    [Header("Referências")]
    [Tooltip("Arraste a câmera que orbita o personagem aqui (ou deixe vazio para pegar a Camera.main)")]
    public Camera playerCamera;
    public Transform pontoAtaque;
    public GameObject muzzleFlash;
    public TextMeshProUGUI ammunitionDisplay;

    public bool balaZerada;
    protected int balasSobrando;

    private AcoesPlayer acoesPlayer;
    private bool atirando, preparadoParaAtirar, recarregando;
    private GameObject donoDaArma;

    private void Awake()
    {
        acoesPlayer = new AcoesPlayer();
    }

    private void OnEnable()
    {
        // 1. Garante que as entradas do Input System sejam ativadas
        if (acoesPlayer == null) acoesPlayer = new AcoesPlayer();
        acoesPlayer.MovimentoPlayer.Atirar.Enable();
        acoesPlayer.MovimentoPlayer.Recarregar.Enable();

        // 2. Atualiza a referência do clone como dono oficial da arma
        donoDaArma = transform.root.gameObject;

        // 3. Reseta o estado da munição e recarga
        balasSobrando = tamanhoCarregador;
        preparadoParaAtirar = true;
        recarregando = false;
        balaZerada = false;

        // 4. Se o ponto de ataque não estiver configurado, usa a própria posição
        if (pontoAtaque == null) pontoAtaque = transform;

        // 5. Busca automática de referências da cena caso tenham se perdido na clonagem
        VerificarEConectarReferenciasDaCena();
    }

    private void OnDisable()
    {
        if (acoesPlayer != null)
        {
            acoesPlayer.MovimentoPlayer.Atirar.Disable();
            acoesPlayer.MovimentoPlayer.Recarregar.Disable();
        }
    }

    private void Update()
    {
        MyInput();

        // Atualiza o contador de munição se houver UI conectada
        if (ammunitionDisplay != null)
        {
            ammunitionDisplay.SetText((balasSobrando / tiroPorClick) + " / " + (tamanhoCarregador / tiroPorClick));
        }

        // Atualiza a flag de munição zerada
        balaZerada = (balasSobrando <= 0);
    }

    private void VerificarEConectarReferenciasDaCena()
    {
        // Garante que a câmera principal seja encontrada
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Busca o Pool de Balas na cena caso a referência do prefab tenha ficado nula no clone
        if (meuPoolDeBalas == null)
        {
            meuPoolDeBalas = FindFirstObjectByType<BalaPool>();
        }
    }

    private void MyInput()
    {
        if (permitirSegurarBotão)
            atirando = acoesPlayer.MovimentoPlayer.Atirar.IsPressed();
        else
            atirando = acoesPlayer.MovimentoPlayer.Atirar.WasPressedThisFrame();

        if (acoesPlayer.MovimentoPlayer.Recarregar.WasPressedThisFrame() && balasSobrando < tamanhoCarregador && !recarregando)
        {
            Reload();
        }

        if (preparadoParaAtirar && atirando && !recarregando && balasSobrando > 0)
        {
            Atirar();
        }
    }

    private void Atirar()
    {
        preparadoParaAtirar = false;

        // Re-verifica referências essenciais antes de disparar
        VerificarEConectarReferenciasDaCena();

        if (playerCamera == null || meuPoolDeBalas == null)
        {
            Debug.LogWarning("[tiroPistola] Não é possível atirar: Câmera ou BalaPool não encontrados no clone!");
            preparadoParaAtirar = true;
            return;
        }

        // O Ray sai do centro da tela (mira) da câmera
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 pontoAcerto;
        if (Physics.Raycast(ray, out RaycastHit acerto, alcanceDoTiro, mascaraDeTiro, QueryTriggerInteraction.Ignore))
            pontoAcerto = acerto.point;
        else
            pontoAcerto = ray.GetPoint(alcanceDoTiro);

        if (lineRenderer != null)
            StartCoroutine(DesenharRaio(pontoAtaque.position, pontoAcerto));

        Vector3 direçãoBase = (pontoAcerto - pontoAtaque.position).normalized;

        for (int i = 0; i < tiroPorClick; i++)
        {
            if (balasSobrando <= 0) break;

            float x = Random.Range(-espalhamentoHorizontal, espalhamentoHorizontal) * 0.05f;
            float y = Random.Range(-espalhamentoVertical, espalhamentoVertical) * 0.05f;

            Vector3 vetorEspalhamento = (playerCamera.transform.right * x) + (playerCamera.transform.up * y);
            Vector3 direçãoFinal = (direçãoBase + vetorEspalhamento).normalized;

            GameObject balaAtual = meuPoolDeBalas.GetitemFromPool();

            if (balaAtual != null)
            {
                balaAtual.transform.position = pontoAtaque.position;
                balaAtual.transform.forward = direçãoFinal;
                balaAtual.SetActive(true);

                if (balaAtual.TryGetComponent<Bala>(out Bala scriptBala))
                {
                    scriptBala.Inicializar(danoPorTiro, tipoDeDano, donoDaArma, nomeDaArma);
                }

                if (balaAtual.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    rb.AddForce(direçãoFinal * forçaTiro, ForceMode.Impulse);

                    if (forçaCima > 0f)
                        rb.AddForce(Vector3.up * forçaCima, ForceMode.Impulse);
                }
            }

            balasSobrando--;
        }

        if (muzzleFlash != null)
        {
            Instantiate(muzzleFlash, pontoAtaque.position, Quaternion.identity, pontoAtaque);
        }

        Invoke(nameof(ResetShot), tempoEntreAtirar);
    }

    private void ResetShot() => preparadoParaAtirar = true;

    public void Reload()
    {
        if (recarregando) return;
        recarregando = true;
        Invoke(nameof(TerminarRecarga), tempoRecarga);
    }

    private void TerminarRecarga()
    {
        balasSobrando = tamanhoCarregador;
        recarregando = false;
        balaZerada = false;
    }

    private IEnumerator DesenharRaio(Vector3 inicio, Vector3 fim)
    {
        lineRenderer.SetPosition(0, inicio);
        lineRenderer.SetPosition(1, fim);
        lineRenderer.enabled = true;
        yield return new WaitForSeconds(duracaoLinha);
        lineRenderer.enabled = false;
    }
}