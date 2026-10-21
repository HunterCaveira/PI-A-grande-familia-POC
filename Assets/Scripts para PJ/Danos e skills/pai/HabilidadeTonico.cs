using System.Collections;
using UnityEngine;
using Photon.Pun;

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

    [Header("Configurações do Buff")]
    [SerializeField] private float velocidadeComBuff = 20f;
    [SerializeField][Range(0f, 1f)] private float porcentagemReducaoDano = 0.3f;
    [SerializeField] private float duracaoDoEfeito = 6.0f;

    [Header("Tempo de Animação e Cooldown")]
    [SerializeField] private float tempoAnimacaoConsumo = 1.0f;
    [SerializeField] private float tempoCooldown = 10.0f;

    private bool emUsoOuAnimacao = false;

    [Header("Controle De Referência")]
    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;
    [SerializeField] private Sistemadevida sistemaDeVida;
    [SerializeField] private AtributosPersonagem atributosOriginal;
    [SerializeField] private GameObject prefabTonicoMao;

    private AtributosPersonagem atributosBuffados;

    private void Awake()
    {
        if (gerenciadorDeEquipamentos == null) gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
        if (sistemaDeVida == null) sistemaDeVida = GetComponent<Sistemadevida>();
    }

    private void OnDisable()
    {
        if (photonView != null && !photonView.IsMine) return;
        CancelarHabilidadeForcado();
    }

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
        if (photonView != null && !photonView.IsMine) return;

        if (!EstaPronta())
        {
            if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0);
            return;
        }

        StartCoroutine(RotinaConsumirEAplicarTonico());
    }

    public void CancelarHabilidade()
    {
        if (photonView != null && !photonView.IsMine) return;

        if (emUsoOuAnimacao)
        {
            photonView.RPC("RPC_AlternarFrasco", RpcTarget.All, false);
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
            photonView.RPC("RPC_AlternarFrasco", RpcTarget.All, false);
        }
    }

    private IEnumerator RotinaConsumirEAplicarTonico()
    {
        emUsoOuAnimacao = true;

        photonView.RPC("RPC_AlternarFrasco", RpcTarget.All, true);

        yield return new WaitForSeconds(tempoAnimacaoConsumo);

        AplicarBuffs();

        photonView.RPC("RPC_AlternarFrasco", RpcTarget.All, false);
        if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0);

        yield return new WaitForSeconds(duracaoDoEfeito);

        RemoverBuffs();

        TempoProximouso = Time.time + tempoCooldown;
        emUsoOuAnimacao = false;
    }

    [PunRPC]
    private void RPC_AlternarFrasco(bool mostrar)
    {
        if (prefabTonicoMao != null)
        {
            prefabTonicoMao.SetActive(mostrar);
        }
    }

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
        else
        {
            Debug.LogError("[TÔNICO] O campo 'Atributos Original' está vazio no Inspetor!");
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
}