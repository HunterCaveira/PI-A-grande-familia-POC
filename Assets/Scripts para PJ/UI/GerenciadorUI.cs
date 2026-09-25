using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GerenciadorUI : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private GameObject jogador;

    [Header("Imagens da UI (Ordem: Slot Q, E, F)")]
    [SerializeField] private Image imagemSlotQ;
    [SerializeField] private Image imagemSlotE;
    [SerializeField] private Image imagemSlotF;

    private class SlotUIInfo
    {
        public ISkills Habilidade;
        public Image ImagemUI;
        public bool UltimoEstadoPronto = true;
    }

    private Dictionary<int, SlotUIInfo> slotsMap = new Dictionary<int, SlotUIInfo>();

    void Start()
    {
        if (jogador == null) return;

        ISkills[] habilidades = jogador.GetComponents<ISkills>();

        slotsMap[1] = new SlotUIInfo { ImagemUI = imagemSlotQ };
        slotsMap[2] = new SlotUIInfo { ImagemUI = imagemSlotE };
        slotsMap[3] = new SlotUIInfo { ImagemUI = imagemSlotF };

        foreach (var hab in habilidades)
        {
            if (slotsMap.ContainsKey(hab.SlotIndex))
            {
                slotsMap[hab.SlotIndex].Habilidade = hab;
            }
        }
    }

    void Update()
    {
        foreach (var par in slotsMap)
        {
            SlotUIInfo slotInfo = par.Value;

            if (slotInfo.Habilidade == null || slotInfo.ImagemUI == null) continue;

            bool estaPronta = Time.time >= slotInfo.Habilidade.TempoProximouso;

            if (estaPronta != slotInfo.UltimoEstadoPronto)
            {
                slotInfo.UltimoEstadoPronto = estaPronta;
                slotInfo.ImagemUI.sprite = estaPronta ? slotInfo.Habilidade.FotoPronta : slotInfo.Habilidade.FotoRecarregando;
            }
        }
    }
}