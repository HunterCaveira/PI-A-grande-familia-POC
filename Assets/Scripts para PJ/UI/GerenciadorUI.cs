using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GerenciadorUI : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Não precisa arrastar nada! O SetupJogadorMultiplayer vai preencher isso automaticamente.")]
    [SerializeField] private GameObject jogador;

    [Header("Imagens da UI na Tela")]
    [SerializeField] private Image imagemSlotQ; // Slot 1
    [SerializeField] private Image imagemSlotE; // Slot 2
    [SerializeField] private Image imagemSlotF; // Slot 3
    [SerializeField] public TextMeshProUGUI meuTexto;
    [SerializeField] private tiroPistola pistolaRef;

    // Estrutura interna para controlar o estado de cada slot
    private class SlotUIInfo
    {
        public IasUI Habilidade;
        public Image ImagemUI;
        public bool UltimoEstadoPronto = true;
    }

    private Dictionary<int, SlotUIInfo> slotsMap = new Dictionary<int, SlotUIInfo>();

    void Start()
    {
        // 1. Mapeia os slots fixos da UI (Mas ainda não vincula ao jogador, pois ele pode não ter nascido ainda)
        slotsMap[1] = new SlotUIInfo { ImagemUI = imagemSlotQ }; // 1 = Q
        slotsMap[2] = new SlotUIInfo { ImagemUI = imagemSlotE }; // 2 = E
        slotsMap[3] = new SlotUIInfo { ImagemUI = imagemSlotF }; // 3 = F
    }

    // =================================================================================
    // ESTE É O MÉTODO QUE FALTAVA PARA RESOLVER O ERRO VERMELHO!
    // Ele é chamado automaticamente pelo SetupJogadorMultiplayer quando você nasce na rede.
    // =================================================================================
    public void AtribuirJogadorLocal(GameObject jogadorLocal)
    {
        jogador = jogadorLocal;

        if (jogador == null) return;

        // MULTIPLAYER SEGURO: Procura a arma APENAS dentro do corpo do seu próprio personagem
        pistolaRef = jogador.GetComponentInChildren<tiroPistola>();

        // PEGA automaticamente todas as habilidades que estão no Jogador local
        IasUI[] habilidadesDoJogador = jogador.GetComponentsInChildren<IasUI>();

        // Associa cada habilidade encontrada ao seu respectivo slot baseado no SlotIndex dela
        foreach (var hab in habilidadesDoJogador)
        {
            if (slotsMap.ContainsKey(hab.SlotIndex))
            {
                slotsMap[hab.SlotIndex].Habilidade = hab;
                Debug.Log($"<color=yellow>[GerenciadorUI] Habilidade encontrada e encaixada no Slot {hab.SlotIndex}!</color>");
            }
        }
    }

    void Update()
    {
        // Se o boneco ainda não nasceu na rede, não tenta atualizar a UI para evitar erros
        if (jogador == null) return;

        // Atualiza a cor do texto da munição em tempo real
        AcabouMunição();

        // Atualiza o visual das habilidades apenas quando houver mudança de estado (Pronta <-> Recarregando)
        foreach (var par in slotsMap)
        {
            SlotUIInfo slotInfo = par.Value;

            if (slotInfo.Habilidade == null || slotInfo.ImagemUI == null) continue;

            // Utiliza o método EstaPronta() para respeitar tanto o tempo quanto as travas de animação/uso
            bool estaPronta = slotInfo.Habilidade.EstaPronta();

            if (estaPronta != slotInfo.UltimoEstadoPronto)
            {
                slotInfo.UltimoEstadoPronto = estaPronta;
                // Troca para a foto certa (Pronta ou Recarregando) da própria interface
                slotInfo.ImagemUI.sprite = estaPronta ? slotInfo.Habilidade.FotoPronta : slotInfo.Habilidade.FotoRecarregando;
            }
        }
    }

    public void AcabouMunição()
    {
        if (pistolaRef == null || meuTexto == null) return;

        if (pistolaRef.balaZerada == true)
        {
            meuTexto.color = Color.blue;
        }
        else
        {
            // Se ainda tem bala, pinta de branco
            meuTexto.color = Color.white;
        }
    }
}