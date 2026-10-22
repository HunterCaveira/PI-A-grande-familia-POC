using System.Collections;
using UnityEngine;

public class HabilidadeTonico : MonoBehaviour, IasUI
{
    // ==========================================
    // 1. CONFIGURAÇÃO DE SLOT E UI
    // ==========================================

    [Header("Configuração do Slot e UI")]
    [SerializeField] private int slotIndex = 3;
    public int SlotIndex => slotIndex;

    [SerializeField] private Sprite fotoPronta;
    public Sprite FotoPronta => fotoPronta;

    [SerializeField] private Sprite fotoRecarregando;
    public Sprite FotoRecarregando => fotoRecarregando;

    public float TempoProximouso { get; set; } = 0f;


    // ==========================================
    // 2. CONFIGURAÇÕES DOS BUFFS
    // ==========================================

    [Header("Configurações do Buff")]
    [SerializeField] private float velocidadeComBuff = 20f;

    [SerializeField]
    [Range(0f, 1f)]
    private float porcentagemReducaoDano = 0.3f;

    [SerializeField] private float duracaoDoEfeito = 6.0f;


    // ==========================================
    // 3. TEMPOS E CONTROLES
    // ==========================================

    [Header("Tempo de Animação e Cooldown")]
    [SerializeField] private float tempoAnimacaoConsumo = 1.0f;
    [SerializeField] private float tempoCooldown = 10.0f;

    private bool emUsoOuAnimacao = false;


    // ==========================================
    // 4. REFERÊNCIAS E FICHAS DE ATRIBUTOS
    // ==========================================

    [Header("Controle De Referência")]
    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;
    [SerializeField] private Sistemadevida sistemaDeVida;
    [SerializeField] private AtributosPersonagem atributosOriginal;
    [SerializeField] private GameObject prefabTonicoMao;

    private AtributosPersonagem atributosBuffados;


    // ==========================================
    // 5. CICLO DE VIDA DA UNITY
    // ==========================================

    private void Awake()
    {
        if (gerenciadorDeEquipamentos == null)
            gerenciadorDeEquipamentos = GetComponentInParent<GerenciadorDeEquipamentos>();

        if (sistemaDeVida == null)
            sistemaDeVida = GetComponentInParent<Sistemadevida>();
    }

    private void OnDisable()
    {
        CancelarHabilidadeForcado();
    }


    // ==========================================
    // 6. MÉTODOS DA INTERFACE (IasUI / IHabilidade)
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
            if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0);
            return;
        }

        StartCoroutine(RotinaConsumirEAplicarTonico());
    }

    public void CancelarHabilidade()
    {
        if (emUsoOuAnimacao)
        {
            SincronizarVisualTonico(false);
            return;
        }

        CancelarHabilidadeForcado();
    }

    private void CancelarHabilidadeForcado()
    {
        StopAllCoroutines();
        RemoverBuffs();
        emUsoOuAnimacao = false;
        SincronizarVisualTonico(false);
    }


    // ==========================================
    // 7. CORROTINA DO TÔNICO
    // ==========================================

    private IEnumerator RotinaConsumirEAplicarTonico()
    {
        emUsoOuAnimacao = true;

        SincronizarVisualTonico(true);

        yield return new WaitForSeconds(tempoAnimacaoConsumo);

        AplicarBuffs();

        SincronizarVisualTonico(false);

        if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0);

        yield return new WaitForSeconds(duracaoDoEfeito);

        RemoverBuffs();

        TempoProximouso = Time.time + tempoCooldown;
        emUsoOuAnimacao = false;
    }


    // ==========================================
    // 8. APLICAÇÃO E REMOÇÃO DE STATUS
    // ==========================================

    private void AplicarBuffs()
    {
        if (atributosOriginal != null)
        {
            if (atributosBuffados != null) Destroy(atributosBuffados);

            atributosBuffados = Instantiate(atributosOriginal);
            atributosBuffados.velocidadeMovimento = velocidadeComBuff;

            MovimentodoPlayer movimento = GetComponentInParent<MovimentodoPlayer>();
            if (movimento != null)
            {
                movimento.AtualizarFichaAtributos(atributosBuffados);
            }

            if (sistemaDeVida == null) sistemaDeVida = GetComponentInParent<Sistemadevida>();
            if (sistemaDeVida != null)
            {
                sistemaDeVida.AtualizarFichaAtributos(atributosBuffados);
                sistemaDeVida.PorcentagemReducaoDano = porcentagemReducaoDano;
            }
        }
    }

    private void RemoverBuffs()
    {
        MovimentodoPlayer movimento = GetComponentInParent<MovimentodoPlayer>();
        if (movimento != null && atributosOriginal != null)
        {
            movimento.AtualizarFichaAtributos(atributosOriginal);
        }

        if (sistemaDeVida == null) sistemaDeVida = GetComponentInParent<Sistemadevida>();
        if (sistemaDeVida != null && atributosOriginal != null)
        {
            sistemaDeVida.AtualizarFichaAtributos(atributosOriginal);
            sistemaDeVida.PorcentagemReducaoDano = 0f;
        }

        if (atributosBuffados != null)
        {
            Destroy(atributosBuffados);
            atributosBuffados = null;
        }
    }

    private void SincronizarVisualTonico(bool visivel)
    {
        if (prefabTonicoMao != null)
        {
            prefabTonicoMao.SetActive(visivel);
        }
    }
}