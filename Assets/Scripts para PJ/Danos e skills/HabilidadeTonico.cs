using System.Collections; // Importa suporte a Corrotinas.
using UnityEngine; // Importa ferramentas do motor da Unity.
using UnityEngine.InputSystem; // Importa o novo Input System.

public class HabilidadeTonico : MonoBehaviour, IHabilidade // Script da habilidade do Tônico.
{
    // ==========================================
    // 1. CONFIGURAÇÕES DOS BUFFS
    // ==========================================

    [Header("Configurações do Buff")]

    [SerializeField]
    private float velocidadeComBuff = 20f; // Altera o valor diretamente na ficha do herói de 5 para 20.

    [SerializeField]
    [Range(0f, 1f)]
    private float porcentagemReducaoDano = 0.3f; // Desconto de 30% no dano recebido no Sistemadevida.

    [SerializeField]
    private float duracaoDoEfeito = 6.0f; // Tempo de permanência do efeito bufado em segundos.


    // ==========================================
    // 2. TEMPOS E CONTROLES
    // ==========================================

    [Header("Tempo de Animação e Cooldown")]

    [SerializeField] private float tempoAnimacaoConsumo = 1.0f; // Tempo com o tônico na mão simular consumo.
    [SerializeField] private float tempoCooldown = 10.0f; // Tempo de recarga.

    private float tempoProximoUso = 0f; // Temporizador interno de recarga.
    private bool emUsoOuAnimacao = false; // Trava de execução.
    private bool aguardandoCliqueConsumo = false; // Trava de espera do clique do mouse.
    private float velocidadeOriginalSalva; // Guarda a velocidade base original (5) para restaurar no fim.


    // ==========================================
    // 3. REFERÊNCIAS
    // ==========================================

    [Header("Controle De Referência")]

    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos; // Troca de slots de armas/habilidades.
    [SerializeField] private Sistemadevida sistemaDeVida; // Gerenciador de vida do jogador.
    [SerializeField] private AtributosPersonagem atributosHeroi; // Ficha de atributos (ScriptableObject).
    [SerializeField] private GameObject prefabTonicoMao; // Objeto 3D do frasco na mão.


    // ==========================================
    // 4. CICLO DE VIDA
    // ==========================================

    private void Awake()
    {
        if (gerenciadorDeEquipamentos == null) gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
        if (sistemaDeVida == null) sistemaDeVida = GetComponent<Sistemadevida>();
    }

    private void Update()
    {
        // Se o tônico estiver na mão e o jogador clicar com o Botão Esquerdo do Mouse
        if (aguardandoCliqueConsumo && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            aguardandoCliqueConsumo = false; // Remove a espera.
            StartCoroutine(RotinaConsumirEAplicarTonico()); // Inicia o consumo e aplicação dos buffs.
        }
    }

    private void OnDisable()
    {
        CancelarHabilidade(); // Restaura parâmetros caso o herói morra ou seja desativado.
    }


    // ==========================================
    // 5. INTERFACE IHABILIDADE
    // ==========================================

    public bool EstaPronta()
    {
        return Time.time >= tempoProximoUso && !emUsoOuAnimacao && !aguardandoCliqueConsumo;
    }

    public void AtivarHabilidade()
    {
        if (!EstaPronta())
        {
            if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0);
            return;
        }

        if (prefabTonicoMao != null) prefabTonicoMao.SetActive(true); // Exibe o frasco na mão.
        aguardandoCliqueConsumo = true; // Prontidão para o clique do mouse.
    }

    public void CancelarHabilidade()
    {
        StopAllCoroutines();
        RemoverBuffs();
        emUsoOuAnimacao = false;
        aguardandoCliqueConsumo = false;

        if (prefabTonicoMao != null) prefabTonicoMao.SetActive(false);
    }


    // ==========================================
    // 6. CORROTINA DO TÔNICO
    // ==========================================

    private IEnumerator RotinaConsumirEAplicarTonico()
    {
        emUsoOuAnimacao = true;

        // FASE 1: TEMPO DE CONSUMO
        yield return new WaitForSeconds(tempoAnimacaoConsumo);

        // FASE 2: APLICAÇÃO DOS BUFFS
        AplicarBuffs();

        if (prefabTonicoMao != null) prefabTonicoMao.SetActive(false);
        if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0); // Devolve mão para arma.

        // FASE 3: DURAÇÃO DO EFEITO
        yield return new WaitForSeconds(duracaoDoEfeito);

        // FASE 4: REMOÇÃO E RECARGA
        RemoverBuffs();
        tempoProximoUso = Time.time + tempoCooldown;
        emUsoOuAnimacao = false;
    }


    // ==========================================
    // 7. APLICAÇÃO E REMOÇÃO DE STATUS
    // ==========================================

    private void AplicarBuffs()
    {
        if (atributosHeroi != null)
        {
            velocidadeOriginalSalva = atributosHeroi.velocidadeMovimento; // Salva o valor original (5).
            atributosHeroi.velocidadeMovimento = velocidadeComBuff; // Altera diretamente na ficha para 20!
        }

        if (sistemaDeVida != null)
        {
            sistemaDeVida.PorcentagemReducaoDano = porcentagemReducaoDano; // Injeta os 30% de redução no Sistemadevida.
        }
    }

    private void RemoverBuffs()
    {
        if (atributosHeroi != null && velocidadeOriginalSalva > 0f)
        {
            atributosHeroi.velocidadeMovimento = velocidadeOriginalSalva; // Devolve o valor 5 para a ficha.
        }

        if (sistemaDeVida != null)
        {
            sistemaDeVida.PorcentagemReducaoDano = 0f; // Reseta a mitigação para 0%.
        }
    }
}