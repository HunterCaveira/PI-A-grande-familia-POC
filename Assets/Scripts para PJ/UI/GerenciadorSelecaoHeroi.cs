using System; // Importa bibliotecas essenciais de estruturas C#.
using System.Collections.Generic; // Permite o uso de Listas (List<T>) em C#.
using UnityEngine; // Importa componentes nativos da Unity.
using UnityEngine.UI; // Importa componentes de interface de usuário (Image).
using TMPro; // Importa suporte a TextMeshPro para textos de interface.

public class GerenciadorSelecaoHeroi : MonoBehaviour
{
    // Struct auxiliar para organizar a referência de cada slot de jogador na Hierarchy
    [Serializable]
    public struct SlotJogadorUI
    {
        public TextMeshProUGUI textoNickname; // Texto do Nickname (ex: Jogador 0)
        public Image imagemIconeHeroi;         // Imagem do ícone do Familiar Selecionado
    }

    // ==========================================
    // 1. CAMPOS DE UI DOS TIMES
    // ==========================================

    [Header("Slots do Time 1")]
    [SerializeField] private List<SlotJogadorUI> slotsTime1 = new List<SlotJogadorUI>();

    [Header("Slots do Time 2")]
    [SerializeField] private List<SlotJogadorUI> slotsTime2 = new List<SlotJogadorUI>();

    [Header("Personagens Disponíveis")]
    [SerializeField] private List<AtributosPersonagem> listaAtributosHerois = new List<AtributosPersonagem>();


    // ==========================================
    // 2. INICIALIZAÇÃO DA INTERFACE
    // ==========================================

    private void Start()
    {
        // Ao iniciar a tela, reseta e apaga todos os slots dos dois times
        ResetarTodosOsSlots();

        // SIMULAÇÃO LOCAL: Configura o jogador atual no primeiro slot do Time 1
        ConfigurarJogadorEntrou(1, 0, "SeuNickname");
    }


    // ==========================================
    // 3. MÉTODOS DE CONTROLE DE INTERFACE
    // ==========================================

    /// <summary>
    /// Desativa o texto e o ícone de todos os slots de ambos os times.
    /// </summary>
    public void ResetarTodosOsSlots()
    {
        LimparListaSlots(slotsTime1);
        LimparListaSlots(slotsTime2);
    }

    private void LimparListaSlots(List<SlotJogadorUI> listaSlots)
    {
        foreach (var slot in listaSlots)
        {
            if (slot.textoNickname != null)
            {
                slot.textoNickname.text = "";
                slot.textoNickname.gameObject.SetActive(false); // Apaga o texto do nickname
            }

            if (slot.imagemIconeHeroi != null)
            {
                slot.imagemIconeHeroi.sprite = null;
                slot.imagemIconeHeroi.gameObject.SetActive(false); // Apaga o ícone do personagem
            }
        }
    }

    /// <summary>
    /// Faz o nickname do jogador aparecer quando ele entra na sala no time correto.
    /// </summary>
    public void ConfigurarJogadorEntrou(int numeroTime, int indiceSlot, string nickname)
    {
        List<SlotJogadorUI> timeAlvo = (numeroTime == 1) ? slotsTime1 : slotsTime2;

        if (indiceSlot >= 0 && indiceSlot < timeAlvo.Count)
        {
            SlotJogadorUI slot = timeAlvo[indiceSlot];

            if (slot.textoNickname != null)
            {
                slot.textoNickname.text = nickname;
                slot.textoNickname.gameObject.SetActive(true); // Exibe o nickname na tela
            }
        }
    }

    /// <summary>
    /// Atualiza o ícone do herói quando o jogador clica para selecionar seu personagem.
    /// </summary>
    public void SeleccionarHeroiSlot(int numeroTime, int indiceSlot, AtributosPersonagem heroi)
    {
        List<SlotJogadorUI> timeAlvo = (numeroTime == 1) ? slotsTime1 : slotsTime2;

        if (indiceSlot >= 0 && indiceSlot < timeAlvo.Count && heroi != null)
        {
            SlotJogadorUI slot = timeAlvo[indiceSlot];

            if (slot.imagemIconeHeroi != null)
            {
                slot.imagemIconeHeroi.sprite = heroi.iconePersonagem;
                slot.imagemIconeHeroi.gameObject.SetActive(true); // Exibe o ícone do personagem escolhido
            }
        }
    }
}