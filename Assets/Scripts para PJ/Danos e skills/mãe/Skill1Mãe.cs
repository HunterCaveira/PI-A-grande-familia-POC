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
    [SerializeField] private GameObject objetoIntermediario; // O objeto com o Collider/Trigger de ataque

    private void Awake()
    {
        // Garante que a hitbox começa desativada ao iniciar o jogo
        if (objetoIntermediario != null)
        {
            objetoIntermediario.SetActive(false);
        }
    }

    // --- Implementação dos Contratos de Interface ---

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

        // Atualiza o tempo de recarga
        proximoUsoPermitido = Time.time + tempoRecarga;

        // Ativa o Intermediário (Hitbox)
        if (objetoIntermediario != null)
        {
            objetoIntermediario.SetActive(true);

            // Exemplo: Desativa a hitbox automaticamente após 0.3 segundos (golpe rápido)
            CancelInvoke(nameof(DesativarHitbox));
            Invoke(nameof(DesativarHitbox), 0.3f);
        }

        Debug.Log("[Skill 1] Habilidade ativada com sucesso!");
    }

    public void CancelarHabilidade()
    {
        // Chamado pelo Gerenciador de Equipamentos se o jogador cancelar/trocar de ação
        DesativarHitbox();
    }

    private void DesativarHitbox()
    {
        if (objetoIntermediario != null)
        {
            objetoIntermediario.SetActive(false);
        }
    }
}