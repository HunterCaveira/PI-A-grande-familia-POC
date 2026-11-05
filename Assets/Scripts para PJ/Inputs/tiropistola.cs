using System.Collections;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class tiroPistola : MonoBehaviourPun
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
    [Tooltip("Força extra para cima (arco da bala). Deixe 0 para tiro reto")]
    public float forçaCima = 0f;

    [Header("Status da Arma")]
    public float tempoEntreAtirar = 0.2f, tempoRecarga = 1.5f;
    public int tamanhoCarregador = 12, tiroPorClick = 1;
    public bool permitirSegurarBotão = false;

    [Header("Referências")]
    [Tooltip("Nome exato do GameObject da Câmera na cena para a Opção 1")]
    [SerializeField] private string nomeDaCamera = "Main Camera";
    public Camera playerCamera;
    public Transform pontoAtaque;
    public GameObject muzzleFlash;
    public TextMeshProUGUI ammunitionDisplay;

    public bool balaZerada;
    protected int balasSobrando;

    private AcoesPlayer acoesPlayer;
    private bool atirando, preparadoParaAtirar, recarregando;
    private GameObject donoDaArma;
    private PhotonView pvRaiz;

    private void Awake()
    {
        acoesPlayer = new AcoesPlayer();
        pvRaiz = transform.root.GetComponent<PhotonView>();
        donoDaArma = transform.root.gameObject; // Garante a referência do objeto pai do jogador
    }

    private void OnEnable()
    {
        if (pvRaiz != null && !pvRaiz.IsMine) return;

        if (acoesPlayer == null) acoesPlayer = new AcoesPlayer();
        acoesPlayer.MovimentoPlayer.Atirar.Enable();
        acoesPlayer.MovimentoPlayer.Recarregar.Enable();

        donoDaArma = transform.root.gameObject;

        balasSobrando = tamanhoCarregador;
        preparadoParaAtirar = true;
        recarregando = false;
        balaZerada = false;

        if (pontoAtaque == null) pontoAtaque = transform;

        VerificarEConectarReferenciasDaCena();
    }

    private void OnDisable()
    {
        if (pvRaiz != null && !pvRaiz.IsMine) return;

        if (acoesPlayer != null)
        {
            acoesPlayer.MovimentoPlayer.Atirar.Disable();
            acoesPlayer.MovimentoPlayer.Recarregar.Disable();
        }
    }

    private void Update()
    {
        if (pvRaiz != null && !pvRaiz.IsMine) return;

        MyInput();

        if (ammunitionDisplay != null)
        {
            ammunitionDisplay.SetText((balasSobrando / tiroPorClick) + " / " + (tamanhoCarregador / tiroPorClick));
        }

        balaZerada = (balasSobrando <= 0);
    }

    // 🎯 OPÇÃO 1 IMPLEMENTADA: Busca o GameObject da Câmera pelo nome exato na cena
    private void VerificarEConectarReferenciasDaCena()
    {
        if (playerCamera == null)
        {
            GameObject camObj = GameObject.Find("Main Camera");
            if (camObj != null)
            {
                playerCamera = camObj.GetComponent<Camera>();
            }
        }

        if (meuPoolDeBalas == null)
        {
            meuPoolDeBalas = transform.root.GetComponentInChildren<BalaPool>();
            if (meuPoolDeBalas == null) meuPoolDeBalas = FindFirstObjectByType<BalaPool>();
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

        VerificarEConectarReferenciasDaCena();

        if (playerCamera == null)
        {
            Debug.LogWarning($"[tiroPistola] Câmera '{nomeDaCamera}' não encontrada na cena!");
            preparadoParaAtirar = true;
            return;
        }

        // 🎯 STRAIGHT TO POINT: Direção frontal direta da Câmera
        Vector3 direçãoBase = playerCamera.transform.forward;

        Vector3 pontoAcerto;
        Ray rayVisual = new Ray(playerCamera.transform.position, direçãoBase);

        if (Physics.Raycast(rayVisual, out RaycastHit acerto, alcanceDoTiro, mascaraDeTiro, QueryTriggerInteraction.Ignore))
        {
            pontoAcerto = acerto.point;
        }
        else
        {
            pontoAcerto = rayVisual.GetPoint(alcanceDoTiro);
        }

        for (int i = 0; i < tiroPorClick; i++)
        {
            if (balasSobrando <= 0) break;

            float x = Random.Range(-espalhamentoHorizontal, espalhamentoHorizontal) * 0.05f;
            float y = Random.Range(-espalhamentoVertical, espalhamentoVertical) * 0.05f;

            Vector3 vetorEspalhamento = (playerCamera.transform.right * x) + (playerCamera.transform.up * y);
            Vector3 direçãoFinal = (direçãoBase + vetorEspalhamento).normalized;

            PhotonView targetPV = photonView != null ? photonView : pvRaiz;
            if (targetPV != null)
            {
                targetPV.RPC(nameof(RPC_ExecutarTiro), RpcTarget.All, pontoAtaque.position, direçãoFinal, pontoAcerto);
            }

            balasSobrando--;
        }

        Invoke(nameof(ResetShot), tempoEntreAtirar);
    }

    [PunRPC]
    private void RPC_ExecutarTiro(Vector3 origemTiro, Vector3 direcaoFinal, Vector3 pontoAcerto)
    {
        VerificarEConectarReferenciasDaCena();

        if (lineRenderer != null)
            StartCoroutine(DesenharRaio(origemTiro, pontoAcerto));

        if (meuPoolDeBalas != null)
        {
            GameObject balaAtual = meuPoolDeBalas.GetitemFromPool();

            if (balaAtual != null)
            {
                balaAtual.transform.position = origemTiro;
                balaAtual.transform.rotation = Quaternion.LookRotation(direcaoFinal);
                balaAtual.SetActive(true);

                if (balaAtual.TryGetComponent<Bala>(out Bala scriptBala))
                {
                    scriptBala.Inicializar(danoPorTiro, tipoDeDano, donoDaArma, nomeDaArma);
                }

                if (balaAtual.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                    rb.WakeUp();

                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    Vector3 velocidadeCalculada = (direcaoFinal * forçaTiro) + (Vector3.up * forçaCima);
                    rb.linearVelocity = velocidadeCalculada;
                }
            }
        }

        if (muzzleFlash != null)
        {
            Instantiate(muzzleFlash, origemTiro, Quaternion.identity, pontoAtaque != null ? pontoAtaque : transform);
        }
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