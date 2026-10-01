using System.Collections; // Importa suporte a Corrotinas (IEnumerator e WaitForSeconds).
using UnityEngine; // Importa as ferramentas principais do motor da Unity.

public class HabilidadeTonico : MonoBehaviour, IHabilidade // Declara a classe pública HabilidadeTonico implementando a interface IHabilidade.
{
    // ==========================================
    // 1. CONFIGURAÇÕES DOS BUFFS
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
    // 2. TEMPOS E CONTROLES
    // ==========================================

    [Header("Tempo de Animação e Cooldown")]

    [SerializeField] private float tempoAnimacaoConsumo = 1.0f; // Tempo com o tônico na mão para simular o consumo.
    [SerializeField] private float tempoCooldown = 10.0f; // Tempo de recarga após o fim do efeito.

    private float tempoProximoUso = 0f; // Temporizador interno de recarga (Time.time).
    private bool emUsoOuAnimacao = false; // Trava booleana de execução interna.


    // ==========================================
    // 3. REFERÊNCIAS E FICHAS DE ATRIBUTOS
    // ==========================================

    [Header("Controle De Referência")]

    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos; // Troca de slots de armas/habilidades.
    [SerializeField] private Sistemadevida sistemaDeVida; // Gerenciador de vida do jogador.
    [SerializeField] private AtributosPersonagem atributosOriginal; // Ficha de atributos original (Asset em disco).
    [SerializeField] private GameObject prefabTonicoMao; // Objeto 3D do frasco anexado à mão.

    private AtributosPersonagem atributosBuffados; // Instância temporária em memória gerada via Instantiate.


    // ==========================================
    // 4. CICLO DE VIDA DA UNITY
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
    // 5. MÉTODOS DA INTERFACE IHABILIDADE
    // ==========================================

    public bool EstaPronta() // Consulta se a habilidade está fora de recarga e pronta para uso.
    {
        return Time.time >= tempoProximoUso && !emUsoOuAnimacao;
    }

    public void AtivarHabilidade() // Método invocado pelo GerenciadorDeEquipamentos assim que a tecla (F) é pressionada.
    {
        //Debug.Log("[TÔNICO] Tecla 'F' pressionada! Verificando prontidão da habilidade...");

        if (!EstaPronta()) // SE a habilidade estiver em cooldown ou execução.
        {
            Debug.LogWarning($"[TÔNICO] Habilidade em recarga/uso! Disponível apenas em: {tempoProximoUso - Time.time:F1}s");
            if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0); // Garante o retorno à arma principal.
            return;
        }

        // Dispara a corrotina de consumo direto ao pressionar a tecla 'F'
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
    // 6. CORROTINA DO TÔNICO
    // ==========================================

    private IEnumerator RotinaConsumirEAplicarTonico() // Corrotina que executa o consumo direto do tônico.
    {
        emUsoOuAnimacao = true; // Seta a trava de uso para evitar ativações duplas.

        if (prefabTonicoMao != null) prefabTonicoMao.SetActive(true); // Exibe o objeto 3D do frasco na mão do herói.

        //Debug.Log($"[TÔNICO] Bebendo o tônico... Aguardando {tempoAnimacaoConsumo}s de tempo de consumo.");

        // FASE 1: TEMPO DE CONSUMO (ANIMAÇÃO DE BEBER)
        yield return new WaitForSeconds(tempoAnimacaoConsumo);

        // LOG DE CONFIRMAÇÃO DE CONSUMO
       // Debug.Log("<color=green>[TÔNICO LOG] O Tônico foi consumido com sucesso!</color>");

        // FASE 2: APLICAÇÃO DOS BUFFS (GERA E INJETA A FICHA TEMPORÁRIA DUPLICADA)
        AplicarBuffs();

        if (prefabTonicoMao != null) prefabTonicoMao.SetActive(false); // Oculta o tônico da mão após beber.
        if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0); // Retorna a mão para a arma principal (Slot 0).

        // FASE 3: DURAÇÃO DO EFEITO
        //Debug.Log($"[TÔNICO] Buffs ativos! Duração do efeito: {duracaoDoEfeito} segundos.");
        yield return new WaitForSeconds(duracaoDoEfeito);

        // FASE 4: REMOÇÃO DOS BUFFS E RESTAURAÇÃO DA FICHA ORIGINAL
        RemoverBuffs(); // Executado após a duração do efeito finalizar!

        tempoProximoUso = Time.time + tempoCooldown; // Define o próximo segundo do relógio para liberação.
        emUsoOuAnimacao = false; // Libera a trava de execução da habilidade.

        Debug.Log($"[TÔNICO] Efeito finalizado. Habilidade entrou em Cooldown de {tempoCooldown}s.");
    }


    // ==========================================
    // 7. APLICAÇÃO E REMOÇÃO DE STATUS (SISTEMA DE FICHA DUPLICADA)
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

            //Debug.Log($"<color=cyan>[TÔNICO BUFF] Ficha temporária ativada com velocidade: {atributosBuffados.velocidadeMovimento}</color>");
        }
        //else
        //{
           // Debug.LogError("[TÔNICO ERRO] A referência 'atributosOriginal' NÃO foi atribuída no Inspector do Tônico!");
        //}
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

      //  Debug.Log($"<color=yellow>[TÔNICO RESET] Ficha original restaurada no herói com velocidade base: {atributosOriginal.velocidadeMovimento}</color>");

        // Limpa a ficha temporária da memória RAM
        if (atributosBuffados != null)
        {
            Destroy(atributosBuffados);
            atributosBuffados = null;
        }
    }
}