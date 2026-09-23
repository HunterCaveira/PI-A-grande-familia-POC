using UnityEngine; // Importa as ferramentas principais do motor da Unity.
using UnityEngine.InputSystem; // Importa o novo Input System da Unity para leitura de teclas.

public class GerenciadorDeEquipamentos : MonoBehaviour // Declara a classe do Gerenciador de Equipamentos.
{
    // ==========================================
    // 1. CONFIGURAÇÕES VISUAIS E DE TEMPO
    // ==========================================

    [Header("Configurações Visuais das Mãos")]
    [SerializeField] private Transform pontoMaos; // Objeto pai que contém as malhas 3D das mãos/armas (Filhos 0, 1, 2, 3...).

    [Header("Configurações de Tempo")]
    [SerializeField] private float tempoAnimacaoTroca = 0.5f; // Intervalo de tempo mínimo em segundos entre trocas de slots.
    private float tempoLiberadoParaTrocar = 0f; // Armazena o carimbo de tempo (Time.time) liberado para a próxima troca.

    [Header("Controle Interno")]
    private int indiceEquipamentoAtual = 0; // Armazena o slot atualmente ativo nas mãos (0 = Arma Principal).
    private int proximoIndicePendente = -1; // Armazena a intenção de troca agendada no buffer (-1 = Nenhuma troca pendente).

    // Array que guardará as referências dos scripts de habilidade do personagem que implementam a interface IHabilidade
    private ISkills[] habilidadesDoPersonagem;


    // ==========================================
    // 2. CICLO DE VIDA DA UNITY
    // ==========================================

    void Awake() // Executado ao carregar a cena/componente na memória.
    {
        // Busca automaticamente todos os scripts do mesmo GameObject que implementam a interface IHabilidade
        habilidadesDoPersonagem = GetComponents<ISkills>();
    }

    void Update() // Executado uma vez a cada frame do jogo.
    {
        // 1. Leitura das teclas do teclado e agendamento na fila do buffer
        if (Keyboard.current != null)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 1; // Tecla Q -> Habilidade Slot 1 (1º script no herói)
            }
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 2; // Tecla E -> Habilidade Slot 2 (2º script no herói)
            }
            else if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 3; // Tecla F -> Habilidade Slot 3 (3º script no herói / Tônico)
            }
        }

        // 2. Executa a troca se o relógio da animação liberou E há alguma intenção de troca pendente no buffer
        if (Time.time >= tempoLiberadoParaTrocar && proximoIndicePendente != -1)
        {
            TrocarEquipamento(proximoIndicePendente); // Invoca a função pública de troca de equipamento.
            proximoIndicePendente = -1; // Limpa o agendamento do buffer após executar.
        }
    }


    // ==========================================
    // 3. TROCA DE EQUIPAMENTOS E HABILIDADES
    // ==========================================

    public void TrocarEquipamento(int novoIndice) // Método responsável por alternar armas e disparar habilidades.
    {
        // TRAVA DE COOLDOWN: Se não for a arma principal (índice 0) e a habilidade NÃO estiver pronta, aborta a troca
        if (novoIndice != 0 && !PodeTrocarParaHabilidade(novoIndice))
        {
            return;
        }

        // Se pressionar a mesma tecla da habilidade que já está ativa na mão, alterna de volta para a Arma Principal (0)
        if (novoIndice == indiceEquipamentoAtual)
        {
            novoIndice = 0;
        }

        // Cancela projeções de OUTRAS habilidades antes de alternar, preservando o slot que está sendo ativado
        CancelarOutrasHabilidades(novoIndice);

        // Atualiza a visibilidade das malhas 3D filhas em 'pontoMaos' (Slot 0, 1, 2, 3...)
        if (pontoMaos != null)
        {
            for (int i = 0; i < pontoMaos.childCount; i++)
            {
                pontoMaos.GetChild(i).gameObject.SetActive(i == novoIndice);
            }
        }

        // Dispara o código de ativação da habilidade escolhida
        AtivarHabilidadePorSlot(novoIndice);

        // Atualiza a variável do item ativo nas mãos e grava o cooldown do tempo de animação de troca
        indiceEquipamentoAtual = novoIndice;
        tempoLiberadoParaTrocar = Time.time + tempoAnimacaoTroca;
    }


    // ==========================================
    // 4. MÉTODOS AUXILIARES E VALIDAÇÕES
    // ==========================================

    private bool PodeTrocarParaHabilidade(int slotIndex) // Consulta se o script do slot pode ser ativado.
    {
        if (habilidadesDoPersonagem == null) return false;

        // Converte o slot do usuário (1, 2, 3) para o índice do array de C# (0, 1, 2)
        int arrayIndex = slotIndex - 1;
        if (arrayIndex >= 0 && arrayIndex < habilidadesDoPersonagem.Length)
        {
            return habilidadesDoPersonagem[arrayIndex].EstaPronta(); // Retorna o resultado da interface EstaPronta()
        }

        return true;
    }

    private void CancelarOutrasHabilidades(int slotSendoAtivado) // Cancela apenas as habilidades diferentes da selecionada.
    {
        if (habilidadesDoPersonagem == null) return;

        int indexManter = slotSendoAtivado - 1; // Converte para o índice do array.

        for (int i = 0; i < habilidadesDoPersonagem.Length; i++)
        {
            // Executa o cancelamento apenas nas habilidades diferentes do slot que está sendo ativado agora
            if (i != indexManter)
            {
                habilidadesDoPersonagem[i].CancelarHabilidade();
            }
        }
    }

    private void AtivarHabilidadePorSlot(int slotIndex) // Dispara o método AtivarHabilidade() da interface.
    {
        if (habilidadesDoPersonagem == null) return;

        int arrayIndex = slotIndex - 1; // Converte o slot (1, 2, 3) para o array (0, 1, 2).
        if (arrayIndex >= 0 && arrayIndex < habilidadesDoPersonagem.Length)
        {
            habilidadesDoPersonagem[arrayIndex].AtivarHabilidade(); // Invoca o método da interface do slot selecionado.
        }
    }
}