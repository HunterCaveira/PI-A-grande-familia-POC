using UnityEngine;
using TMPro;
using Photon.Pun; // Importa a biblioteca do Photon

public class tiroPistola : MonoBehaviourPun // Herda de MonoBehaviourPun para usar rede
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
    [Tooltip("Pode ser arrastado no Inspector ou deixado vazio para busca automática na cena")]
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

        // BUSCA AUTOMÁTICA DO POOL DE BALAS (Funciona em todos os clientes)
        if (meuPoolDeBalas == null)
        {
            meuPoolDeBalas = Object.FindFirstObjectByType<BalaPool>();

            if (meuPoolDeBalas == null)
            {
                Debug.LogWarning($"[tiroPistola] Nenhum objeto do tipo 'BalaPool' foi encontrado na cena!");
            }
        }
    }

    private void OnEnable()
    {
        // 🔒 Bloqueia a ativação do teclado se o boneco não for seu
        if (photonView != null && !photonView.IsMine) return;

        acoesPlayer.MovimentoPlayer.Atirar.Enable();
        acoesPlayer.MovimentoPlayer.Recarregar.Enable();
        balasSobrando = tamanhoCarregador;
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
        // 🔒 Trava absoluta: Impede que a tela de um jogador processe o Update das armas dos outros
        if (photonView != null && !photonView.IsMine) return;

        MyInput();

        // Atualiza a UI apenas se o tiroPorClick for maior que 0 para evitar erros matemáticos
        if (ammunitionDisplay != null && tiroPorClick > 0)
        {
            ammunitionDisplay.SetText(balasSobrando / tiroPorClick + " / " + tamanhoCarregador / tiroPorClick);
        }
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
        if (meuPoolDeBalas == null)
        {
            Debug.LogError("[tiroPistola] Impossível atirar: 'meuPoolDeBalas' não foi encontrado na cena!");
            return;
        }

        preparadoParaAtirar = false;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 pontoAcerto;

        if (Physics.Raycast(ray, out RaycastHit acerto, alcanceDoTiro, mascaraDeTiro, QueryTriggerInteraction.Ignore))
            pontoAcerto = acerto.point;
        else
            pontoAcerto = ray.GetPoint(alcanceDoTiro);

        Vector3 direçãoBase = (pontoAcerto - pontoAtaque.position).normalized;

        for (int i = 0; i < tiroPorClick; i++)
        {
            if (balasSobrando <= 0) break;

            balaZerada = (tamanhoCarregador <= 0);

            float x = Random.Range(-espalhamentoHorizontal, espalhamentoHorizontal) * 0.05f;
            float y = Random.Range(-espalhamentoVertical, espalhamentoVertical) * 0.05f;

            Vector3 vetorEspalhamento = (playerCamera.transform.right * x) + (playerCamera.transform.up * y);
            Vector3 direçãoFinal = (direçãoBase + vetorEspalhamento).normalized;

            // 🌐 MULTIPLAYER: Envia o comando de atirar para todos os jogadores na sala
            photonView.RPC("RPC_AtirarSincronizado", RpcTarget.All, pontoAtaque.position, direçãoFinal, pontoAcerto);

            balasSobrando--;
        }

        Invoke(nameof(ResetShot), tempoEntreAtirar);
    }

    // ===============================================================
    // 🌐 FUNÇÃO DE REDE: Todos os computadores executam esta função 
    // e puxam uma bala do seu próprio Pool local ao mesmo tempo!
    // ===============================================================
    [PunRPC]
    private void RPC_AtirarSincronizado(Vector3 origemTiro, Vector3 direcaoTiro, Vector3 pontoAcertoLinha)
    {
        // 1. Desenha o rastro do tiro na tela (opcional)
        if (lineRenderer != null)
            StartCoroutine(DesenharRaio(origemTiro, pontoAcertoLinha));

        // 2. Faz o MuzzleFlash
        if (muzzleFlash != null)
            Instantiate(muzzleFlash, origemTiro, Quaternion.identity, pontoAtaque);

        // 3. Pega a bala no Pool local de cada jogador
        if (meuPoolDeBalas == null) return;

        GameObject balaAtual = meuPoolDeBalas.GetitemFromPool();

        if (balaAtual != null)
        {
            balaAtual.transform.position = origemTiro;
            balaAtual.transform.forward = direcaoTiro;
            balaAtual.SetActive(true);

            if (balaAtual.TryGetComponent<Bala>(out Bala scriptBala))
                scriptBala.Inicializar(danoPorTiro, tipoDeDano, donoDaArma, nomeDaArma);

            if (balaAtual.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(direcaoTiro * forçaTiro, ForceMode.Impulse);

                if (forçaCima > 0f)
                    rb.AddForce(Vector3.up * forçaCima, ForceMode.Impulse);
            }
        }
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