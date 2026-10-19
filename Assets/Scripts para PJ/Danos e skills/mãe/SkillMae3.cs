using System.Collections; // Importa suporte a Corrotinas (IEnumerator e WaitForSeconds).
using UnityEngine; // Importa as ferramentas principais do motor da Unity.
using UnityEngine.InputSystem; // Importa o novo Input System.

public class SkillMae3 : MonoBehaviour, IasUI // Implementa a interface IasUI para o HUD/UI
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
    // 2. CONFIGURAÇÕES DE MIRA E DISPARO
    // ==========================================

    [Header("Configurações de Mira e Alcance")]
    [SerializeField] private float alcanceDoTiro = 100f;
    [SerializeField] private LayerMask mascaraDeTiro;
    [Tooltip("Arraste a câmera que orbita o personagem aqui (ou deixe vazio para pegar a Camera.main)")]
    [SerializeField] private Camera playerCamera;


    // ==========================================
    // 3. TEMPOS E CONTROLES (WIND-UP E COOLDOWN)
    // ==========================================

    [Header("Tempo de Espera e Cooldown")]
    [SerializeField] private float tempoDeEsperaDisparo = 0.5f; // Tempo preparando o disparo antes de lançar o projétil.
    [SerializeField] private float tempoCooldown = 5.0f; // Tempo de recarga após o uso.

    private bool emUsoOuAnimacao = false; // Trava booleana de execução interna.


    // ==========================================
    // 4. REFERÊNCIAS
    // ==========================================

    [Header("Referências")]
    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;
    [SerializeField] private Transform pontoDisparoNaMao; // Ponto na mão ativado pela tecla F.
    [SerializeField] private GameObject projetilPreCriado; // O projétil de lentidão na cena.


    // ==========================================
    // 5. CICLO DE VIDA DA UNITY
    // ==========================================

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (gerenciadorDeEquipamentos == null)
            gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
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
        // O clique do mouse só dispara se o ponto de disparo estiver ativo na mão e a habilidade não estiver em wind-up
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

    public void AtivarHabilidade() // Chamado pelo GerenciadorDeEquipamentos ao apertar F.
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
        Debug.Log("[SKILL 3] Iniciando fase de preparação do disparo...");

        // Aguarda o tempo de wind-up configurado
        yield return new WaitForSeconds(tempoDeEsperaDisparo);

        // Verifica se o ponto de disparo ainda continua ativo após a espera
        if (pontoDisparoNaMao != null && pontoDisparoNaMao.gameObject.activeSelf)
        {
            DispararComPrecisao();
        }
        else
        {
            Debug.LogWarning("[SKILL 3] Disparo cancelado: O ponto de disparo foi desativado durante a espera!");
        }

        TempoProximouso = Time.time + tempoCooldown; // Define o cooldown
        emUsoOuAnimacao = false;
    }


    // ==========================================
    // 8. LÓGICA DE MIRA E LANÇAMENTO DO PROJÉTIL
    // ==========================================

    private void DispararComPrecisao()
    {
        if (projetilPreCriado == null || pontoDisparoNaMao == null) return;

        // 1. O Ray sai do centro da tela (mira/crosshair) da câmera
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

        // 2. Calcula a direção exata do cano da mão até o ponto mirado pela câmera
        Vector3 direcaoDoTiro = (pontoAcerto - pontoDisparoNaMao.position).normalized;

        if (Vector3.Distance(pontoDisparoNaMao.position, pontoAcerto) < 1f)
        {
            direcaoDoTiro = playerCamera.transform.forward;
        }

        // 3. Configura e ativa o projétil de lentidão
        if (projetilPreCriado.activeInHierarchy)
        {
            projetilPreCriado.SetActive(false);
        }

        projetilPreCriado.transform.position = pontoDisparoNaMao.position;
        projetilPreCriado.transform.rotation = Quaternion.LookRotation(direcaoDoTiro);

        // Passa o TimeID para o projétil evitar fogo amigo (se houver o script ProjetilLentidao)
        if (projetilPreCriado.TryGetComponent<ProjetilLentidao>(out ProjetilLentidao scriptProjetil))
        {
            int meuTimeID = -1;
            if (TryGetComponent<Sistemadevida>(out Sistemadevida minhaVida))
            {
                meuTimeID = minhaVida.TimeID;
            }
            scriptProjetil.Inicializar(meuTimeID, this.gameObject);
        }

        projetilPreCriado.SetActive(true);

        // 4. Desativa o ponto de disparo na mão imediatamente após o tiro
        pontoDisparoNaMao.gameObject.SetActive(false);

        Debug.Log("[SKILL 3] Projétil de lentidão disparado com sucesso!");
    }
}