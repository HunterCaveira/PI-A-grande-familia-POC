using System.Collections; // Importa o namespace necessário para utilizar Corrotinas (IEnumerator e WaitForSeconds).
using UnityEngine; // Importa a biblioteca principal do motor da Unity para acessar MonoBehaviour, GameObject, Time, etc.

public class HabilidadeTonico : MonoBehaviour, IHabilidade // Declara a classe pública HabilidadeTonico herdando de MonoBehaviour e implementando o contrato da interface IHabilidade.
{
    // ==========================================
    // 1. CONFIGURAÇÕES DA HABILIDADE E BUFFS
    // ==========================================

    [Header("Configurações do Buff de Tônico")] // Organiza visualmente os campos desta seção no Inspector da Unity.

    [SerializeField] // Exibe o campo no Inspector da Unity mantendo a variável encapsulada como privada.
    private float multiplicadorVelocidadeExtra = 1.5f; // Armazena o fator multiplicador de velocidade (1.5f representa um aumento de +50% sobre a velocidade normal).

    [SerializeField] // Exibe o campo no Inspector.
    [Range(0f, 1f)] // Cria uma barra deslizante no Inspector restringindo o valor entre 0.0 (0% de redução) e 1.0 (100% de mitigação).
    private float porcentagemReducaoDano = 0.3f; // Armazena a taxa de dano mitigado no Sistemadevida (0.3f representa 30% de desconto no dano sofrido).

    [SerializeField] // Exibe o campo no Inspector.
    private float duracaoDoEfeito = 6.0f; // Armazena o tempo total em segundos que o jogador permanecerá com velocidade extra e dano reduzido.


    // ==========================================
    // 2. TEMPOS, TEMPORIZADORES E ANIMAÇÃO
    // ==========================================

    [Header("Tempo de Animação e Cooldown")] // Seção de controle de tempos e recarga no Inspector.

    [SerializeField] // Exibe o campo no Inspector.
    private float tempoAnimacaoConsumo = 1.0f; // Armazena o tempo em segundos que o herói leva consumindo o frasco antes dos efeitos serem ativados (reserva para animação futura).

    [SerializeField] // Exibe o campo no Inspector.
    private float tempoCooldown = 10.0f; // Armazena o tempo em segundos de espera (recarga) necessário após o fim do efeito para poder reusar a habilidade.

    private float tempoProximoUso = 0f; // Guarda o segundo exato do relógio do jogo (Time.time) em que a habilidade ficará disponível novamente.
    private bool emUsoOuAnimacao = false; // Flag booleana que indica se o jogador está bebendo o tônico ou sob o efeito ativo do buff no momento.


    // ==========================================
    // 3. REFERÊNCIAS DE COMPONENTES E OBJETOS
    // ==========================================

    [Header("Controle De Referência")] // Seção de conexões de scripts e objetos visuais no Inspector.

    [SerializeField] // Exibe o campo no Inspector para atribuição manual ou automática.
    private GerenciadorDeEquipamentos gerenciadorDeEquipamentos; // Armazena a referência do script responsável por alternar armas e objetos visuais das mãos.

    [SerializeField] // Exibe o campo no Inspector.
    private Sistemadevida sistemaDeVida; // Armazena a referência do script de vida do jogador para alterar a taxa de redução de dano.

    [SerializeField] // [CORREÇÃO DO ERRO]: Declarada a variável da ficha de atributos do herói que estava faltando no seu script!
    private AtributosPersonagem atributosHeroi; // Armazena a referência ao ScriptableObject de atributos do personagem.

    [SerializeField] // Exibe o campo no Inspector.
    private GameObject prefabTonicoMao; // Armazena o GameObject do frasco/cilindro do tônico anexado à estrutura da mão do herói.


    // ==========================================
    // 4. MÉTODOS DO CICLO DE VIDA DA UNITY
    // ==========================================

    private void Awake() // Método executado automaticamente pela Unity quando a instância do script é carregada na memória da cena.
    {
        gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>(); // Procura e atribui o componente GerenciadorDeEquipamentos anexado ao mesmo GameObject.
        sistemaDeVida = GetComponent<Sistemadevida>(); // Procura e atribui o componente Sistemadevida anexado ao mesmo GameObject.
    }

    private void OnDisable() // [CORREÇÃO DE SEGURANÇA]: Executado automaticamente pela Unity se o jogador morrer ou for desativado.
    {
        CancelarHabilidade(); // Garante a limpeza completa de Corrotinas e a restauração dos atributos originais para não quebrar o jogo no Respawn.
    }


    // ==========================================
    // 5. MÉTODOS CONTRATADOS PELA INTERFACE IHABILIDADE
    // ==========================================

    public bool EstaPronta() // Pergunta pública estipulada pela interface para consultar se a habilidade pode ser ativada.
    {
        // Retorna verdadeiro (true) SE o relógio atual do jogo (Time.time) for maior ou igual ao tempoProximoUso E a habilidade NÃO estiver sendo executada no momento.
        return Time.time >= tempoProximoUso && !emUsoOuAnimacao;
    }

    public void AtivarHabilidade() // Método de acionamento contratado pela interface, chamado pelo GerenciadorDeEquipamentos quando a tecla (F) é pressionada.
    {
        if (!EstaPronta()) // SE a checagem do método EstaPronta() retornar falso (habilidade em cooldown ou em execução).
        {
            if (gerenciadorDeEquipamentos != null) // SE a referência do GerenciadorDeEquipamentos for válida.
            {
                gerenciadorDeEquipamentos.TrocarEquipamento(0); // Garante a permanência ou o retorno da mão para a Arma Principal (Slot 0).
            }
            return; // Aborta a execução do restante da função de ativação.
        }

        // Inicia a execução da Corrotina que controlará toda a sequência temporal de consumo, buffs e recarga
        StartCoroutine(RotinaConsumirEAplicarTonico());
    }

    public void CancelarHabilidade() // Método público contratado pela interface para interromper e limpar a habilidade caso o jogador sofra um cancelamento ou desequipe.
    {
        StopAllCoroutines(); // Interrompe imediatamente qualquer Corrotina ativa associada a este script.
        RemoverBuffs(); // Invoca o método auxiliar para restaurar os modificadores de velocidade e defesa aos seus valores originais.
        emUsoOuAnimacao = false; // Reseta a flag booleana de estado de uso para falso.

        if (prefabTonicoMao != null) // SE a referência do objeto visual do tônico na mão for válida.
        {
            prefabTonicoMao.SetActive(false); // Oculta o modelo do frasco desativando o seu GameObject na cena.
        }
    }


    // ==========================================
    // 6. CORROTINA DO FLUXO DO TÔNICO
    // ==========================================

    private IEnumerator RotinaConsumirEAplicarTonico() // Método do tipo Corrotina que gerencia as fases sequenciais assíncronas do tônico.
    {
        emUsoOuAnimacao = true; // Define a flag para verdadeiro, travando novas chamadas enquanto esta rotina estiver em andamento.

        if (prefabTonicoMao != null) // SE a referência do frasco do tônico existir.
        {
            prefabTonicoMao.SetActive(true); // Exibe o objeto 3D do frasco na mão do personagem durante a fase de beber.
        }

        // ----------------------------------------------------
        // FASE 1: TEMPO DE CONSUMO / ANIMAÇÃO FUTURA
        // ----------------------------------------------------
        // Ponto de integração futura: Aqui dispararemos o Trigger da animação do personagem (ex: animator.SetTrigger("BeberTonico"))
        yield return new WaitForSeconds(tempoAnimacaoConsumo); // Pausa a execução da Corrotina pelo tempo estipulado em segundos para simular a animação de consumo.

        // ----------------------------------------------------
        // FASE 2: APLICAÇÃO DOS BUFFS
        // ----------------------------------------------------
        AplicarBuffs(); // Invoca o método interno que injeta os valores de velocidade extra e mitigação de dano nos respectivos componentes.

        if (prefabTonicoMao != null) // SE o objeto do frasco estiver atribuído.
        {
            prefabTonicoMao.SetActive(false); // Oculta o modelo do frasco da mão do personagem após finalizar a bebida.
        }

        if (gerenciadorDeEquipamentos != null) // SE a referência do GerenciadorDeEquipamentos for válida.
        {
            gerenciadorDeEquipamentos.TrocarEquipamento(0); // Retorna automaticamente a malha da mão do herói para a Arma Principal (Slot 0).
        }

        // ----------------------------------------------------
        // FASE 3: DURAÇÃO DO EFEITO NO CORPO DO HERÓI
        // ----------------------------------------------------
        yield return new WaitForSeconds(duracaoDoEfeito); // Pausa a Corrotina mantendo o herói bufado durante os segundos configurados em duracaoDoEfeito.

        // ----------------------------------------------------
        // FASE 4: REMOÇÃO DOS BUFFS E CÁLCULO DE COOLDOWN
        // ----------------------------------------------------
        RemoverBuffs(); // Invoca o método de limpeza para retornar os status de velocidade e defesa do personagem ao padrão.
        tempoProximoUso = Time.time + tempoCooldown; // Grava o segundo futuro do relógio em que a habilidade se tornará utilizável somando o tempoCooldown.
        emUsoOuAnimacao = false; // Desmarca a trava booleana liberando o estado interno do script.
    }


    // ==========================================
    // 7. MÉTODOS AUXILIARES DE APLICAÇÃO DE STATUS
    // ==========================================

    private void AplicarBuffs() // Método privado auxiliar para aplicar os modificadores no herói.
    {
        if (atributosHeroi != null) // SE a referência do ScriptableObject de atributos do herói for válida.
        {
            // Altera o multiplicador diretamente na ficha de atributos (ex: 1.5f para +50% de velocidade)
            atributosHeroi.MultiplicadorVelocidade = multiplicadorVelocidadeExtra;
        }

        if (sistemaDeVida != null) // SE a referência do script de vida for válida.
        {
            sistemaDeVida.PorcentagemReducaoDano = porcentagemReducaoDano; // Injeta a taxa de mitigação de dano (ex: 0.3f para 30%).
        }
    }

    private void RemoverBuffs() // Método privado auxiliar para restaurar os parâmetros normais.
    {
        if (atributosHeroi != null) // SE a referência dos atributos for válida.
        {
            // Retorna o multiplicador da ficha de atributos para o valor neutro padrão (1f = 100%)
            atributosHeroi.MultiplicadorVelocidade = 1f;
        }

        if (sistemaDeVida != null) // SE a referência do script de vida for válida.
        {
            sistemaDeVida.PorcentagemReducaoDano = 0f; // Retorna a taxa de mitigação para zero (0%).
        }
    }
}