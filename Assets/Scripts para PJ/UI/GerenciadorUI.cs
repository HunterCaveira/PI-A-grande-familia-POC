using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI;

public class GerenciadorUI : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Arraste o objeto do seu Jogador aqui")]
    [SerializeField] private GameObject jogador;

    [Header("Imagens da UI na Tela")]
    [SerializeField] private Image imagemSlotQ; // Slot 1
    [SerializeField] private Image imagemSlotE; // Slot 2
    [SerializeField] private Image imagemSlotF; // Slot 3

    // Estrutura interna para controlar o estado de cada slot
    private class SlotUIInfo
    {
        public ISkills Habilidade;
        public Image ImagemUI;
        public bool UltimoEstadoPronto = true;
    }

    private Dictionary<int, SlotUIInfo> slotsMap = new Dictionary<int, SlotUIInfo>();

    void Start()
    {
        if (jogador == null)
        {
            Debug.LogWarning("O Jogador não foi atribuído no Gerenciador de UI!");
            return;
        }

        // 1. Mapeia os slots fixos da UI
        slotsMap[1] = new SlotUIInfo { ImagemUI = imagemSlotQ }; // 1 = Q
        slotsMap[2] = new SlotUIInfo { ImagemUI = imagemSlotE }; // 2 = E
        slotsMap[3] = new SlotUIInfo { ImagemUI = imagemSlotF }; // 3 = F

        // 2. PEGA automaticamente todas as habilidades que estão no Jogador
        ISkills[] habilidadesDoJogador = jogador.GetComponents<ISkills>();

        // 3. Associa cada habilidade encontrada ao seu respectivo slot baseado no SlotIndex dela
        foreach (var hab in habilidadesDoJogador)
        {
            if (slotsMap.ContainsKey(hab.SlotIndex))
            {
                slotsMap[hab.SlotIndex].Habilidade = hab;
                Debug.Log($"Habilidade encontrada e encaixada no Slot {hab.SlotIndex}!");
            }
        }
    }

    void Update()
    {
        // Atualiza o visual apenas quando houver mudança de estado (Pronta <-> Recarregando)
        foreach (var par in slotsMap)
        {
            SlotUIInfo slotInfo = par.Value;

            if (slotInfo.Habilidade == null || slotInfo.ImagemUI == null) continue;

            // Verifica se o tempo atual já ultrapassou o tempo de recarga
            bool estaPronta = Time.time >= slotInfo.Habilidade.TempoProximouso;

            if (estaPronta != slotInfo.UltimoEstadoPronto)
            {
                slotInfo.UltimoEstadoPronto = estaPronta;
                // Troca para a foto certa (Pronta ou Recarregando)
                slotInfo.ImagemUI.sprite = estaPronta ? slotInfo.Habilidade.FotoPronta : slotInfo.Habilidade.FotoRecarregando;
            }
        }
    }
}