using UnityEngine;

public class Skill1Mãe : MonoBehaviour, IasUI
{
    [Header("Configuração do Slot (IasUI)")]
    [SerializeField] private int slotIndex = 1; // 1 = Q, 2 = E, 3 = F
    public int SlotIndex => slotIndex;

    [Header("Configuração de Cooldown")]
    [SerializeField] private float tempoRecarga = 3f;
    private float proximoUsoPermitido = 0f;
    public float TempoProximouso => proximoUsoPermitido;

    [Header("Imagens da UI (IasUI)")]
    [SerializeField] private Sprite fotoPronta;
    [SerializeField] private Sprite fotoRecarregando;
    public Sprite FotoPronta => fotoPronta;
    public Sprite FotoRecarregando => fotoRecarregando;

    [Header("Referência da Hitbox (Intermediário)")]
    [SerializeField] private GameObject objetoIntermediario;

    private void Awake()
    {
        if (objetoIntermediario != null)
        {
            objetoIntermediario.SetActive(false);
        }
    }

    public bool EstaPronta()
    {
        return Time.time >= proximoUsoPermitido;
    }

    public bool PodeUsar()
    {
        return EstaPronta();
    }

    public void AtivarHabilidade()
    {
        if (!PodeUsar()) return;

        proximoUsoPermitido = Time.time + tempoRecarga;

        if (objetoIntermediario != null)
        {
            objetoIntermediario.SetActive(true);

            CancelInvoke(nameof(DesativarHitbox));
            Invoke(nameof(DesativarHitbox), 0.3f);
        }

        Debug.Log("[Skill 1] Habilidade ativada com sucesso!");
    }

    public void CancelarHabilidade()
    {
        DesativarHitbox();
    }

    private void DesativarHitbox()
    {
        CancelInvoke(nameof(DesativarHitbox));
        if (objetoIntermediario != null)
        {
            objetoIntermediario.SetActive(false);
        }
    }
}