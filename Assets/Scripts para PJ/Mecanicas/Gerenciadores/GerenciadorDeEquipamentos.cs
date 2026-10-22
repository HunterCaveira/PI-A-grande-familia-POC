using UnityEngine;
using UnityEngine.InputSystem;

public class GerenciadorDeEquipamentos : MonoBehaviour
{
    [Header("Configurações Visuais das Mãos")]
    [SerializeField] private Transform pontoMaos;

    [Header("Configurações de Tempo")]
    [SerializeField] private float tempoAnimacaoTroca = 0.5f;
    [Tooltip("Tempo em segundos sem uso para voltar automaticamente à arma principal (Slot 0).")]
    [SerializeField] private float tempoInatividadeVoltarArma = 5.0f;

    private float tempoLiberadoParaTrocar = 0f;
    private float cronometroInatividade = 0f;

    [Header("Controle Interno")]
    private int indiceEquipamentoAtual = 0;
    private int proximoIndicePendente = -1;

    private IasUI[] habilidadesDoPersonagem;

    private void Awake()
    {
        habilidadesDoPersonagem = GetComponents<IasUI>();
        if (habilidadesDoPersonagem == null || habilidadesDoPersonagem.Length == 0)
        {
            habilidadesDoPersonagem = GetComponentsInChildren<IasUI>(true);
        }
    }

    private void Update()
    {
        // 1. Leitura do Teclado (Atalhos Q, E, F)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 1;
            }
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 2;
            }
            else if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 3;
            }
        }

        // 2. Processa troca pendente de equipamento
        if (Time.time >= tempoLiberadoParaTrocar && proximoIndicePendente != -1)
        {
            TrocarEquipamento(proximoIndicePendente);
            proximoIndicePendente = -1;
        }

        // 3. Cronômetro de Inatividade (Auto-retorno para Slot 0 após 5s)
        ControalrInatividadeEquipamento();
    }

    private void ControalrInatividadeEquipamento()
    {
        // Se estiver usando qualquer item/habilidade diferente da arma principal (Slot 0)
        if (indiceEquipamentoAtual != 0)
        {
            cronometroInatividade += Time.deltaTime;

            if (cronometroInatividade >= tempoInatividadeVoltarArma)
            {
                Debug.Log($"[GerenciadorDeEquipamentos] Inatividade de {tempoInatividadeVoltarArma}s atingida. Voltando para a arma principal.");
                TrocarEquipamento(0);
            }
        }
        else
        {
            cronometroInatividade = 0f;
        }
    }

    public void TrocarEquipamento(int novoIndice)
    {
        if (novoIndice == indiceEquipamentoAtual)
        {
            novoIndice = 0;
        }

        if (novoIndice != 0 && !PodeTrocarParaHabilidade(novoIndice))
        {
            return;
        }

        CancelarTodasAsHabilidades();

        // Atualiza a malha visual 3D das mãos no clone
        SincronizarTrocaEquipamento(novoIndice);

        AtivarHabilidadePorSlot(novoIndice);

        indiceEquipamentoAtual = novoIndice;
        tempoLiberadoParaTrocar = Time.time + tempoAnimacaoTroca;

        // Reseta o cronômetro sempre que trocar de equipamento
        cronometroInatividade = 0f;
    }

    /// <summary>
    /// Permite que habilidades estendam/resetem a contagem ao interagir (ex: ao mirar/movimentar a habilidade)
    /// </summary>
    public void ResetarCronometroInatividade()
    {
        cronometroInatividade = 0f;
    }

    private void SincronizarTrocaEquipamento(int novoIndice)
    {
        if (pontoMaos != null)
        {
            for (int i = 0; i < pontoMaos.childCount; i++)
            {
                pontoMaos.GetChild(i).gameObject.SetActive(i == novoIndice);
            }
        }
    }

    private bool PodeTrocarParaHabilidade(int slotIndex)
    {
        if (habilidadesDoPersonagem == null) return true;

        foreach (var hab in habilidadesDoPersonagem)
        {
            if (hab.SlotIndex == slotIndex)
            {
                return hab.PodeUsar();
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