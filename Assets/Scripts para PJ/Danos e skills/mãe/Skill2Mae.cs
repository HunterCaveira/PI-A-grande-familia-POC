using UnityEngine;

public class SkillDois : MonoBehaviour, IasUI
{
    [Header("Configuração do Slot (IasUI)")]
    [SerializeField] private int slotIndex = 2; // 2 = Tecla E
    public int SlotIndex => slotIndex;

    [Header("Configuração de Cooldown")]
    [SerializeField] private float tempoRecarga = 6f;
    private float proximoUsoPermitido = 0f;
    public float TempoProximouso => proximoUsoPermitido;

    [Header("Imagens da UI (IasUI)")]
    [SerializeField] private Sprite fotoPronta;
    [SerializeField] private Sprite fotoRecarregando;
    public Sprite FotoPronta => fotoPronta;
    public Sprite FotoRecarregando => fotoRecarregando;

    [Header("Referências da Skill")]
    [SerializeField] private ZonaDeComando zonaDeComandoRef;
    [SerializeField] private float duracaoEfeito = 3.5f;

    private Sistemadevida meuSistemaDeVida;

    private void Awake()
    {
        meuSistemaDeVida = GetComponentInParent<Sistemadevida>();
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

        if (zonaDeComandoRef == null)
        {
            Debug.LogWarning("[Skill 2] Zona de Comando não foi atribuída no Inspector!");
            return;
        }

        if (meuSistemaDeVida == null)
        {
            meuSistemaDeVida = GetComponentInParent<Sistemadevida>();
        }

        int meuTimeID = meuSistemaDeVida != null ? meuSistemaDeVida.TimeID : 1;

        var inimigosAfetados = zonaDeComandoRef.ObterInimigosNaZona(meuTimeID);

        foreach (var inimigo in inimigosAfetados)
        {
            if (inimigo.TryGetComponent<EnemyController>(out var controladorEstado))
            {
                controladorEstado.AplicarEfeitoPrisao(duracaoEfeito);
            }
        }

        Debug.Log($"[Skill 2] Ativada! {inimigosAfetados.Count} inimigos presos na zona.");
    }

    public void CancelarHabilidade()
    {
        // Limpeza/cancelamento caso necessário
    }
}