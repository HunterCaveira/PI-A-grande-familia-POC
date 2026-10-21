using System.Collections;
using UnityEngine;
using Photon.Pun; // Importa a API do Photon

public class HabilidadeTonico : MonoBehaviourPun, IasUI
{
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
        if (gerenciadorDeEquipamentos == null) gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
        if (sistemaDeVida == null) sistemaDeVida = GetComponent<Sistemadevida>();
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
        if (!photonView.IsMine) return;

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
            photonView.RPC("SincronizarVisualTonicoRPC", RpcTarget.All, false);
            return;
        }

        CancelarHabilidadeForcado();
    }

    private void CancelarHabilidadeForcado()
    {
        StopAllCoroutines();
        RemoverBuffs();
        emUsoOuAnimacao = false;

        if (photonView != null && photonView.IsMine)
        {
            photonView.RPC("SincronizarVisualTonicoRPC", RpcTarget.All, false);
        }
    }


    // ==========================================
    // 7. CORROTINA DO TÔNICO
    // ==========================================

    private IEnumerator RotinaConsumirEAplicarTonico()
    {
        emUsoOuAnimacao = true;

        // MULTIPLAYER: Liga o tônico para todos na rede
        photonView.RPC("SincronizarVisualTonicoRPC", RpcTarget.All, true);

        // FASE 1: TEMPO DE CONSUMO (ANIMAÇÃO DE BEBER)
        yield return new WaitForSeconds(tempoAnimacaoConsumo);

        // FASE 2: APLICAÇÃO DOS BUFFS (FICHA TEMPORÁRIA LOCAL)
        AplicarBuffs();

        // MULTIPLAYER: Desliga o tônico na mão para todos na rede
        photonView.RPC("SincronizarVisualTonicoRPC", RpcTarget.All, false);

        if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0);

        // FASE 3: DURAÇÃO DO EFEITO
        yield return new WaitForSeconds(duracaoDoEfeito);

        // FASE 4: REMOÇÃO DOS BUFFS
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

            MovimentodoPlayer movimento = GetComponent<MovimentodoPlayer>();
            if (movimento != null)
            {
                movimento.AtualizarFichaAtributos(atributosBuffados);
            }

            if (sistemaDeVida != null)
            {
                sistemaDeVida.AtualizarFichaAtributos(atributosBuffados);
                sistemaDeVida.PorcentagemReducaoDano = porcentagemReducaoDano;
            }
        }
    }

    private void RemoverBuffs()
    {
        MovimentodoPlayer movimento = GetComponent<MovimentodoPlayer>();
        if (movimento != null && atributosOriginal != null)
        {
            movimento.AtualizarFichaAtributos(atributosOriginal);
        }

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


    // ==========================================
    // 9. EVENTOS RPC DO PHOTON
    // ==========================================

    [PunRPC]
    public void SincronizarVisualTonicoRPC(bool visivel)
    {
        if (prefabTonicoMao != null)
        {
            prefabTonicoMao.SetActive(visivel);
        }
    }
}