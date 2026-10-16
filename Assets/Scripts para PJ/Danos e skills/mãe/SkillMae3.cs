using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillMae3 : MonoBehaviour, IasUI
{
    [Header("Configurações do Slot (IasUI)")]
    [SerializeField] private int slotIndex = 3; // Slot 3 (Tecla F)
    [SerializeField] private float tempoCooldown = 3f;
    private float proximoUsoPermitido = 0f;

    [Header("Configurações de Mira (Baseado na Pistola)")]
    [SerializeField] private float alcanceDoTiro = 100f;
    [SerializeField] private LayerMask mascaraDeTiro;
    [Tooltip("Arraste a câmera que orbita o personagem aqui (ou deixe vazio para pegar a Camera.main)")]
    [SerializeField] private Camera playerCamera;

    [Header("Configurações da Fase de Espera (Wind-up)")]
    [Tooltip("Tempo em segundos que o personagem fica 'preparando' o disparo antes de lançar o projétil.")]
    [SerializeField] private float tempoDeEsperaDisparo = 0.5f;
    private bool estaCarregando = false;

    [Header("Imagens da UI (Ícones)")]
    [SerializeField] private Sprite fotoPronta;
    [SerializeField] private Sprite fotoRecarregando;

    [Header("Referências de Posição")]
    [SerializeField] private Transform pontoDisparoNaMao;

    [Header("Referência do Projétil da Cena")]
    [SerializeField] private GameObject projetilPreCriado;

    [Header("Referência ao Gerenciador")]
    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;

    public int SlotIndex => slotIndex;
    public float TempoProximouso => proximoUsoPermitido;
    public Sprite FotoPronta => fotoPronta;
    public Sprite FotoRecarregando => fotoRecarregando;

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (gerenciadorDeEquipamentos == null)
            gerenciadorDeEquipamentos = FindFirstObjectByType<GerenciadorDeEquipamentos>();
    }

    void Start()
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

    void Update()
    {
        // O clique do mouse só dispara se o ponto de disparo estiver ativo na mão
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (pontoDisparoNaMao != null && pontoDisparoNaMao.gameObject.activeSelf)
            {
                TentarDisparar();
            }
        }
    }

    /// <summary>
    /// Chamado pelo GerenciadorDeEquipamentos ao pressionar a tecla F.
    /// Ativa o ponto de disparo na mão.
    /// </summary>
    public void AtivarHabilidade()
    {
        if (!PodeUsar() || estaCarregando)
        {
            Debug.Log("[SkillMae3] Habilidade em Cooldown ou já está preparando o disparo!");
            return;
        }

        if (pontoDisparoNaMao != null)
        {
            pontoDisparoNaMao.gameObject.SetActive(true);
        }

        Debug.Log("[SkillMae3] Ponto de disparo ativado! Clique com o mouse para atirar.");
    }

    private void TentarDisparar()
    {
        if (estaCarregando || !PodeUsar()) return;

        StartCoroutine(FaseDeEsperaDisparoRoutine());
        proximoUsoPermitido = Time.time + tempoCooldown;
    }

    private IEnumerator FaseDeEsperaDisparoRoutine()
    {
        estaCarregando = true;
        Debug.Log("[SkillMae3] Iniciando fase de espera/preparação...");

        yield return new WaitForSeconds(tempoDeEsperaDisparo);

        if (pontoDisparoNaMao != null && pontoDisparoNaMao.gameObject.activeSelf)
        {
            DispararComPrecisao();
        }
        else
        {
            Debug.LogWarning("[SkillMae3] Disparo cancelado: O ponto de disparo foi desativado durante a espera!");
        }

        estaCarregando = false;
    }

    public void CancelarHabilidade()
    {
        if (estaCarregando)
        {
            StopAllCoroutines();
            estaCarregando = false;
        }

        if (pontoDisparoNaMao != null)
        {
            pontoDisparoNaMao.gameObject.SetActive(false);
        }

        Debug.Log("[SkillMae3] Habilidade cancelada.");
    }

    public bool PodeUsar()
    {
        return Time.time >= proximoUsoPermitido && !estaCarregando;
    }

    public bool EstaPronta()
    {
        return Time.time >= proximoUsoPermitido && !estaCarregando;
    }

    public bool EstaAtiva()
    {
        return (projetilPreCriado != null && projetilPreCriado.activeInHierarchy) || estaCarregando;
    }

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

        // Evita divisão por zero caso o alvo esteja colado no cano
        if (Vector3.Distance(pontoDisparoNaMao.position, pontoAcerto) < 1f)
        {
            direcaoDoTiro = playerCamera.transform.forward;
        }

        // 3. Posiciona e ativa o projétil
        if (projetilPreCriado.activeInHierarchy)
        {
            projetilPreCriado.SetActive(false);
        }

        projetilPreCriado.transform.position = pontoDisparoNaMao.position;
        projetilPreCriado.transform.rotation = Quaternion.LookRotation(direcaoDoTiro);
        projetilPreCriado.SetActive(true);

        // 4. Desativa o ponto de disparo na mão imediatamente após atirar
        pontoDisparoNaMao.gameObject.SetActive(false);

        Debug.Log("[SkillMae3] Projétil disparado com cálculo de mira preciso!");
    }
}