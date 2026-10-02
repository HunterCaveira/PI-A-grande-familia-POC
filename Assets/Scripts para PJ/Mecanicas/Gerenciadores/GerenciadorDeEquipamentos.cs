using UnityEngine;
using UnityEngine.InputSystem;

public class GerenciadorDeEquipamentos : MonoBehaviour
{
    [Header("Configurações Visuais das Mãos")]
    [SerializeField] private Transform pontoMaos;

    [Header("Configurações de Tempo")]
    [SerializeField] private float tempoAnimacaoTroca = 0.5f;
    private float tempoLiberadoParaTrocar = 0f;

    [Header("Controle Interno")]
    private int indiceEquipamentoAtual = 0;
    private int proximoIndicePendente = -1;

    // Guarda os scripts de habilidade do personagem que usam ISkills
    private IasUI[] habilidadesDoPersonagem;

    void Awake()
    {
        // Busca automaticamente todos os scripts de habilidade no mesmo personagem
        habilidadesDoPersonagem = GetComponents<IasUI>();
    }

    void Update()
    {
        // 1. Leitura das teclas e agendamento na fila (Buffer)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 1; // Tecla Q -> Habilidade Slot 1
            }
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 2; // Tecla E -> Habilidade Slot 2
            }
            else if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 3; // Tecla F -> Habilidade Slot 3
            }
        }

        // 2. Executa a troca se o relógio liberou e há algo pendente no buffer
        if (Time.time >= tempoLiberadoParaTrocar && proximoIndicePendente != -1)
        {
            TrocarEquipamento(proximoIndicePendente);
            proximoIndicePendente = -1; // Limpa o agendamento
        }
    }

    public void TrocarEquipamento(int novoIndice)
    {
        // TRAVA DE COOLDOWN: Se não for a arma principal (índice 0) e a habilidade NÃO estiver pronta, cancela a troca
        if (novoIndice != 0 && !PodeTrocarParaHabilidade(novoIndice))
        {
            return;
        }

        // Cancela projeções de outras habilidades antes de alternar
        CancelarTodasAsHabilidades();

        // Se pressionar a mesma tecla da habilidade ativa, alterna de volta para a Arma Principal (0)
        if (novoIndice == indiceEquipamentoAtual)
        {
            novoIndice = 0;
        }

        // Atualiza as malhas 3D filhas em 'pontoMaos' (Slot 0, 1, 2, 3...)
        if (pontoMaos != null)
        {
            for (int i = 0; i < pontoMaos.childCount; i++)
            {
                pontoMaos.GetChild(i).gameObject.SetActive(i == novoIndice);
            }
        }

        // Dispara o código de ativação da habilidade escolhida
        AtivarHabilidadePorSlot(novoIndice);

        // Atualiza a variável do item ativo e grava a trava de tempo da animação
        indiceEquipamentoAtual = novoIndice;
        tempoLiberadoParaTrocar = Time.time + tempoAnimacaoTroca;
    }

    private bool PodeTrocarParaHabilidade(int slotIndex)
    {
        if (habilidadesDoPersonagem == null) return true;

        foreach (var hab in habilidadesDoPersonagem)
        {
            if (hab.SlotIndex == slotIndex)
            {
                return hab.PodeUsar(); // CORRIGIDO: Usa PodeUsar() do contrato ISkills
            }
        }

        return true;
    }

    private void CancelarTodasAsHabilidades()
    {
        if (habilidadesDoPersonagem == null) return;

        foreach (var hab in habilidadesDoPersonagem)
        {
            hab.CancelarHabilidade();
        }
    }

    private void AtivarHabilidadePorSlot(int slotIndex)
    {
        if (habilidadesDoPersonagem == null) return;

        foreach (var hab in habilidadesDoPersonagem)
        {
            if (hab.SlotIndex == slotIndex)
            {
                hab.AtivarHabilidade();
                break;
            }
        }
    }
}