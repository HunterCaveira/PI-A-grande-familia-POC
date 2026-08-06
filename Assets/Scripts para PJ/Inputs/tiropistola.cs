using UnityEngine;
using TMPro;


public class tiropistola : MonoBehaviour
{
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

    private void OnEnable()
    {
        acoesPlayer.MovimentoPlayer.Atirar.Enable();
        acoesPlayer.MovimentoPlayer.Recarregar.Enable();
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

        if (preparadoParaAtirar && atirando && !recarregando && balasSobrando <= 0)
            Reload();

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

    if (Physics.Raycast(ray, out acerto))
        pontoAcerto = acerto.point;
    else
        pontoAcerto = ray.GetPoint(75);

    Vector3 direçãoSemEspalhar = pontoAcerto - pontoAtaque.position;

    for (int i = 0; i < tiroPorClick; i++)
    {
        if (balasSobrando <= 0) break;

        float x = Random.Range(-Espalhar, Espalhar);
        float y = Random.Range(-Espalhar, Espalhar);

        Vector3 direçãoComEspalhar = direçãoSemEspalhar + new Vector3(x, y, 0);



        // 1. Pega a bala do Pool ao invés de Instanciar
        GameObject balaAtual = meuPoolDeBalas.GetitemFromPool();

        // Define a posição e a direção da bala recém-pega do pool
        balaAtual.transform.position = pontoAtaque.position;
        balaAtual.transform.forward = direçãoComEspalhar.normalized;

        // Ativa o item na cena
        balaAtual.SetActive(true);

        // 2. PASSAGEM DE DADOS: Configura os dados de dano na bala!
        if (balaAtual.TryGetComponent<Bala>(out Bala scriptBala))
        {
            scriptBala.Inicializar(danoPorTiro, tipoDeDano, donoDaArma, nomeDaArma);
        }

        // 3. Aplica forças físicas
        Rigidbody rb = balaAtual.GetComponent<Rigidbody>();
        if (rb != null)
        {
                // IMPORTANTE: Zera a velocidade anterior para que o tiro não saia com a física acumulada do uso anterior no pool
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
}