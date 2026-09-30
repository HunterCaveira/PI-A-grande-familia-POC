using System.Collections; // Importa suporte a Corrotinas (IEnumerator e WaitForSeconds).
using UnityEngine; // Importa as ferramentas principais do motor da Unity.

public class HabilidadeTonico : MonoBehaviour, IasUI // Implementa IasUI (que herda de IHabilidade)
{
    [Header("Configuração do Slot e UI")]
    [SerializeField] private int slotIndex = 3;
    public int SlotIndex => slotIndex;

    // Propriedades exigidas pela interface IasUI para o HUD/UI
    [SerializeField] private Sprite fotoPronta;
    public Sprite FotoPronta => fotoPronta;

    [SerializeField] private Sprite fotoRecarregando;
    public Sprite FotoRecarregando => fotoRecarregando;

    // Propriedade corrigida para atender a interface IasUI permitindo leitura e escrita
    public float TempoProximouso { get; set; } = 0f;


    // ==========================================
    // 2. CONFIGURAÇÕES DOS BUFFS
    // ==========================================

    [Header("Configurações do Buff")]

    [SerializeField]
    private float velocidadeComBuff = 20f; // Altera o valor diretamente na ficha temporária do herói (Ex: de 5 para 20).

    [SerializeField]
    [Range(0f, 1f)]
    private float porcentagemReducaoDano = 0.3f; // Desconto de 30% no dano recebido no Sistemadevida.

    [SerializeField]
    private float duracaoDoEfeito = 6.0f; // Tempo de permanência do efeito bufado em segundos.


    // ==========================================
    // 3. TEMPOS E CONTROLES
    // ==========================================

    [Header("Tempo de Animação e Cooldown")]

    [SerializeField] private float tempoAnimacaoConsumo = 1.0f; // Tempo com o tônico na mão para simular o consumo.
    [SerializeField] private float tempoCooldown = 10.0f; // Tempo de recarga após o fim do efeito.

    private bool emUsoOuAnimacao = false; // Trava booleana de execução interna.


    // ==========================================
    // 4. REFERÊNCIAS E FICHAS DE ATRIBUTOS
    // ==========================================

    [Header("Controle De Referência")]

    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos; // Troca de slots de armas/habilidades.
    [SerializeField] private Sistemadevida sistemaDeVida; // Gerenciador de vida do jogador.
    [SerializeField] private AtributosPersonagem atributosOriginal; // Ficha de atributos original (Asset em disco).
    [SerializeField] private GameObject prefabTonicoMao; // Objeto 3D do frasco anexado à mão.

    private AtributosPersonagem atributosBuffados; // Instância temporária em memória gerada via Instantiate.


    // ==========================================
    // 5. CICLO DE VIDA DA UNITY
    // ==========================================

    private void Awake() // Método de inicialização ao carregar o componente na cena.
    {
        if (gerenciadorDeEquipamentos == null) gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
        if (sistemaDeVida == null) sistemaDeVida = GetComponent<Sistemadevida>();
    }

    private void OnDisable() // Executado automaticamente se o herói for desativado ou morrer.
    {
        CancelarHabilidadeForcado(); // Se o herói for desativado ou morrer, resgata a ficha original de emergência.
    }


    // ==========================================
    // 6. MÉTODOS DA INTERFACE (IasUI / IHabilidade)
    // ==========================================

    public bool EstaPronta() // Consulta se a habilidade está fora de recarga e pronta para uso.
    {
        return Time.time >= TempoProximouso && !emUsoOuAnimacao;
    }

    // ADICIONADO PARA ATENDER À CONTRATAÇÃO DA INTERFACE IHABILIDADE:
    public bool PodeUsar()
    {
        return EstaPronta();
    }

    public void AtivarHabilidade() // Método invocado pelo GerenciadorDeEquipamentos assim que a tecla correspondente é pressionada.
    {
        Debug.Log("1. Tentando ativar habilidade"); // <--- ETAPA 1

        if (!EstaPronta()) // SE a habilidade estiver em cooldown ou execução.
        {
            Debug.LogWarning($"[TÔNICO] Habilidade em recarga/uso! Disponível apenas em: {TempoProximouso - Time.time:F1}s");
            if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0); // Garante o retorno à arma principal.
            return;
        }

        // Dispara a corrotina de consumo direto ao pressionar a tecla
        StartCoroutine(RotinaConsumirEAplicarTonico());
    }

    public void CancelarHabilidade() // Chamado pelo Gerenciador ao trocar de equipamento.
    {
        // Se já consumiu a bebida e o efeito está rodando no temporizador, NÃO cancela a corrotina!
        if (emUsoOuAnimacao)
        {
            if (prefabTonicoMao != null) prefabTonicoMao.SetActive(false); // Apenas esconde a malha visual da mão.
            return; // Permite que o tempo do buff continue contando até expirar!
        }

        CancelarHabilidadeForcado();
    }

    private void CancelarHabilidadeForcado() // Interrupção de emergência (Morte/Desativação)
    {
        StopAllCoroutines();
        RemoverBuffs(); // Devolve a ficha original
        emUsoOuAnimacao = false;

        if (prefabTonicoMao != null) prefabTonicoMao.SetActive(false);
    }


    // ==========================================
    // 7. CORROTINA DO TÔNICO
    // ==========================================

    private IEnumerator RotinaConsumirEAplicarTonico() // Corrotina que executa o consumo direto do tônico.
    {
        emUsoOuAnimacao = true; // Seta a trava de uso para evitar ativações duplas.

        if (prefabTonicoMao != null) prefabTonicoMao.SetActive(true); // Exibe o objeto 3D do frasco na mão do herói.

        Debug.Log("2. Bebendo tônico... Aguardando animação."); // <--- ETAPA 2

        // FASE 1: TEMPO DE CONSUMO (ANIMAÇÃO DE BEBER)
        yield return new WaitForSeconds(tempoAnimacaoConsumo);

        // FASE 2: APLICAÇÃO DOS BUFFS (GERA E INJETA A FICHA TEMPORÁRIA DUPLICADA)
        AplicarBuffs();

        if (prefabTonicoMao != null) prefabTonicoMao.SetActive(false); // Oculta o tônico da mão após beber.
        if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0); // Retorna a mão para a arma principal (Slot 0).

        // FASE 3: DURAÇÃO DO EFEITO
        yield return new WaitForSeconds(duracaoDoEfeito);

        // FASE 4: REMOÇÃO DOS BUFFS E RESTAURAÇÃO DA FICHA ORIGINAL
        RemoverBuffs(); // Executado após a duração do efeito finalizar!

        TempoProximouso = Time.time + tempoCooldown; // Define o próximo segundo do relógio para liberação.
        emUsoOuAnimacao = false; // Libera a trava de execução da habilidade.

        Debug.Log($"4. [TÔNICO] Efeito finalizado. Habilidade entrou em Cooldown de {tempoCooldown}s."); // <--- ETAPA 4
    }


    // ==========================================
    // 8. APLICAÇÃO E REMOÇÃO DE STATUS (SISTEMA DE FICHA DUPLICADA)
    // ==========================================

    private void AplicarBuffs()
    {
        if (atributosOriginal != null)
        {
            // Se houver alguma ficha duplicada anterior pendente na memória, destrói antes
            if (atributosBuffados != null) Destroy(atributosBuffados);

            // Cria uma duplicata em memória da ficha original para não alterar o arquivo no disco
            atributosBuffados = Instantiate(atributosOriginal);
            atributosBuffados.velocidadeMovimento = velocidadeComBuff; // Injeta a velocidade com buff (Ex: 20).

            Debug.Log("3. Ficha clonada. Velocidade configurada para: " + atributosBuffados.velocidadeMovimento); // <--- ETAPA 3

            // Atualiza o script de MovimentodoPlayer com a nova ficha temporária
            MovimentodoPlayer movimento = GetComponent<MovimentodoPlayer>();
            if (movimento != null)
            {
                movimento.AtualizarFichaAtributos(atributosBuffados);
            }

            // Atualiza o script de Sistemadevida com a nova ficha temporária
            if (sistemaDeVida != null)
            {
                sistemaDeVida.AtualizarFichaAtributos(atributosBuffados);
                sistemaDeVida.PorcentagemReducaoDano = porcentagemReducaoDano; // Aplica a redução de dano (Ex: 30%).
            }
        }
        else
        {
            Debug.LogError("[TÔNICO] O campo 'Atributos Original' está vazio no Inspetor!");
        }
    }

    private void RemoverBuffs()
    {
        // Devolve a ficha original intacta para o MovimentodoPlayer
        MovimentodoPlayer movimento = GetComponent<MovimentodoPlayer>();
        if (movimento != null && atributosOriginal != null)
        {
            movimento.AtualizarFichaAtributos(atributosOriginal);
        }

        // Devolve a ficha original intacta para o Sistemadevida e reseta a redução de dano
        if (sistemaDeVida != null && atributosOriginal != null)
        {
            sistemaDeVida.AtualizarFichaAtributos(atributosOriginal);
            sistemaDeVida.PorcentagemReducaoDano = 0f;
        }

        // Limpa a ficha temporária da memória RAM
        if (atributosBuffados != null)
        {
            Destroy(atributosBuffados);
            atributosBuffados = null;
        }
    }
}