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

    // Componentes genéricos de habilidades (buscados automaticamente no personagem)
    private IHabilidade[] habilidadesDoPersonagem;

    void Awake()
    {
        // Coleta automaticamente todos os scripts do personagem que usam a interface IHabilidade
        habilidadesDoPersonagem = GetComponents<IHabilidade>();
    }

    void Update()
    {
        // 1. Captura de teclas (Buffer de Agendamento)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 1; // Slot 1: Q (Ex: Parede)
            }
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 2; // Slot 2: E (Ex: Tônico)
            }
            else if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 3; // Slot 3: F (Ex: Cinto)
            }
        }

        // 2. Execução da troca quando a trava de tempo liberar
        if (Time.time >= tempoLiberadoParaTrocar && proximoIndicePendente != -1)
        {
            TrocarEquipamento(proximoIndicePendente);
            proximoIndicePendente = -1;
        }
    }

    private void TrocarEquipamento(int novoIndice)
    {
        // A. Cancela a projeção/mira de TODAS as habilidades ativas de forma genérica
        CancelarTodasAsHabilidades();

        // B. Trata o retorno para a arma principal se pressionar a mesma tecla
        if (novoIndice == indiceEquipamentoAtual)
        {
            novoIndice = 0;
        }

        // C. Atualiza os visuais das mãos no 'pontoMaos' (se configurado no Inspector)
        if (pontoMaos != null)
        {
            for (int i = 0; i < pontoMaos.childCount; i++)
            {
                pontoMaos.GetChild(i).gameObject.SetActive(i == novoIndice);
            }
        }

        // D. Ativa a habilidade lógica correspondente (se existir no personagem)
        AtivarHabilidadePorSlot(novoIndice);

        // E. Atualiza estados e grava o delay da animação
        indiceEquipamentoAtual = novoIndice;
        tempoLiberadoParaTrocar = Time.time + tempoAnimacaoTroca;
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

        // Se o slot for 1 (Tecla Q) e houver pelo menos 1 habilidade no personagem
        if (slotIndex == 1 && habilidadesDoPersonagem.Length > 0)
        {
            habilidadesDoPersonagem[0].AtivarHabilidade();
        }
        // Se criar mais habilidades no futuro (Slot 2 = E, Slot 3 = F), elas entram aqui expandindo a lista
    }
}