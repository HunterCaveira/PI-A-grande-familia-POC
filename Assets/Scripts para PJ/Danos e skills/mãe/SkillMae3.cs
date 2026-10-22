using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillMae3 : MonoBehaviour, IasUI
{
    // ==========================================
    // 1. CONFIGURAÇÃO DO SLOT E UI
    // ==========================================

    [Header("Configuração do Slot e UI")]
    [SerializeField] private int slotIndex = 3; // Slot 3 (Tecla F)
    public int SlotIndex => slotIndex;

    [SerializeField] private Sprite fotoPronta;
    public Sprite FotoPronta => fotoPronta;

    [SerializeField] private Sprite fotoRecarregando;
    public Sprite FotoRecarregando => fotoRecarregando;

    public float TempoProximouso { get; set; } = 0f;


    // ==========================================
    // 2. CONFIGURAÇÕES DE MIRA E ALCANCE
    // ==========================================

    [Header("Configurações de Mira e Alcance")]
    [SerializeField] private float alcanceDoTiro = 100f;
    [SerializeField] private LayerMask mascaraDeTiro;
    [Tooltip("Arraste a câmera do jogador aqui (se vazio, pegará Camera.main automaticamente)")]
    [SerializeField] private Camera playerCamera;


    // ==========================================
    // 3. TEMPOS E CONTROLES (WIND-UP E COOLDOWN)
    // ==========================================

    [Header("Tempo de Espera e Cooldown")]
    [SerializeField] private float tempoDeEsperaDisparo = 0.5f;
    [SerializeField] private float tempoCooldown = 5.0f;

    private bool emUsoOuAnimacao = false;


    // ==========================================
    // 4. REFERÊNCIAS
    // ==========================================

    [Header("Referências")]
    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;
    [SerializeField] private Transform pontoDisparoNaMao;
    [SerializeField] private GameObject projetilPreCriado;


    // ==========================================
    // 5. CICLO DE VIDA DA UNITY
    // ==========================================

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (gerenciadorDeEquipamentos == null)
            gerenciadorDeEquipamentos = GetComponentInParent<GerenciadorDeEquipamentos>();
    }

    private void Start()
    {
        if (projetilPreCriado != null)
        {
            projetilPreCriado.SetActive(false);
        }

        if (pontoDisparoNaMao != null)
        {
            pontoDisparoNaMao.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (pontoDisparoNaMao != null && pontoDisparoNaMao.gameObject.activeSelf && !emUsoOuAnimacao)
            {
                if (EstaPronta())
                {
                    StartCoroutine(RotinaFaseDeEsperaDisparo());
                }
            }
        }
    }

    private void OnDisable()
    {
        CancelarHabilidadeForcado();
    }


    // ==========================================
    // 6. MÉTODOS DA INTERFACE (IasUI)
    // ==========================================

    public bool EstaPronta()
    {
        return Time.time >= TempoProximouso && !emUsoOuAnimacao;
    }

    public bool PodeUsar()
    {
        return EstaPronta();
    }

    public void AtivarHabilidade()
    {
        if (!EstaPronta())
        {
            Debug.LogWarning($"[SKILL 3] Habilidade em recarga! Disponível em: {TempoProximouso - Time.time:F1}s");
            return;
        }

        if (pontoDisparoNaMao != null)
        {
            pontoDisparoNaMao.gameObject.SetActive(true);
        }

        Debug.Log("[SKILL 3] Ponto de disparo ativado pela tecla F! Clique com o mouse para atirar.");
    }

    public void CancelarHabilidade()
    {
        CancelarHabilidadeForcado();
    }

    private void CancelarHabilidadeForcado()
    {
        StopAllCoroutines();
        emUsoOuAnimacao = false;

        if (pontoDisparoNaMao != null)
        {
            pontoDisparoNaMao.gameObject.SetActive(false);
        }
    }


    // ==========================================
    // 7. CORROTINA DE DISPARO (WIND-UP)
    // ==========================================

    private IEnumerator RotinaFaseDeEsperaDisparo()
    {
        emUsoOuAnimacao = true;
        Debug.Log("[SKILL 3] Preparando disparo...");

        yield return new WaitForSeconds(tempoDeEsperaDisparo);

        if (pontoDisparoNaMao != null && pontoDisparoNaMao.gameObject.activeSelf)
        {
            DispararComPrecisao();
        }

        TempoProximouso = Time.time + tempoCooldown;
        emUsoOuAnimacao = false;
    }


    // ==========================================
    // 8. LÓGICA DE MIRA E LANÇAMENTO
    // ==========================================

    private void DispararComPrecisao()
    {
        if (projetilPreCriado == null || pontoDisparoNaMao == null) return;

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
        {
            Debug.LogError("[SKILL 3] Câmera não encontrada para direcionar o tiro!");
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 pontoAcerto;
        if (Physics.Raycast(ray, out RaycastHit acerto, alcanceDoTiro, mascaraDeTiro, QueryTriggerInteraction.Ignore))
        {
            pontoAcerto = acerto.point;
        }
        else
        {
            pontoAcerto = ray.GetPoint(alcanceDoTiro);
        }

        Vector3 direcaoDoTiro = (pontoAcerto - pontoDisparoNaMao.position).normalized;

        if (Vector3.Distance(pontoDisparoNaMao.position, pontoAcerto) < 1f)
        {
            direcaoDoTiro = playerCamera.transform.forward;
        }

        if (projetilPreCriado.activeInHierarchy)
        {
            projetilPreCriado.SetActive(false);
        }

        projetilPreCriado.transform.position = pontoDisparoNaMao.position;
        projetilPreCriado.transform.rotation = Quaternion.LookRotation(direcaoDoTiro);

        // Busca o Sistemadevida no próprio objeto ou no objeto pai (clone principal)
        if (projetilPreCriado.TryGetComponent<ProjetilLentidao>(out ProjetilLentidao scriptProjetil))
        {
            int meuTimeID = -1;
            Sistemadevida minhaVida = GetComponentInParent<Sistemadevida>();

            if (minhaVida != null)
            {
                meuTimeID = minhaVida.TimeID;
            }

            scriptProjetil.Inicializar(meuTimeID, gameObject);
        }

        projetilPreCriado.SetActive(true);
        pontoDisparoNaMao.gameObject.SetActive(false);

        Debug.Log("[SKILL 3] Projétil disparado com sucesso!");
    }
}