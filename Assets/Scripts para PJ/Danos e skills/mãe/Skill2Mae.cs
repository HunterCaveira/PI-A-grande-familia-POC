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
    [SerializeField] private ZonaDeComando zonaDeComandoRef; // Arraste a zona aqui
    [SerializeField] private float duracaoEfeito = 3.5f;     // Quanto tempo dura o efeito no inimigo

    private Sistemadevida meuSistemaDeVida;

    void Awake()
    {
        meuSistemaDeVida = GetComponent<Sistemadevida>();
    }

    // --- Implementação da Interface IasUI ---

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

        // Atualiza o cooldown
        proximoUsoPermitido = Time.time + tempoRecarga;

        if (zonaDeComandoRef == null)
        {
            Debug.LogWarning("[Skill 2] Zona de Comando não foi atribuída no Inspector!");
            return;
        }

        // Descobre o TimeID do jogador que disparou a skill
        int meuTimeID = meuSistemaDeVida != null ? meuSistemaDeVida.TimeID : 1;

        // Pega os inimigos detectados na zona pela ZonaDeComando
        var inimigosAfetados = zonaDeComandoRef.ObterInimigosNaZona(meuTimeID);

        // Envia o sinal para cada inimigo perder o movimento, iluminar e gerenciar por enum
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
        // Caso precise cancelar algo ao trocar de item/arma
    }
}