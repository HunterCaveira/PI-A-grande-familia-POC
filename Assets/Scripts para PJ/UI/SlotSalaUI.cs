using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class SlotSalaUI : MonoBehaviour

{

    [Header("Elementos Visuais do Slot")]

    [SerializeField] private TextMeshProUGUI textoNomeSala; // Onde vai aparecer o nome da sala.

    [SerializeField] private TextMeshProUGUI textoTipoSala; // Onde vai aparecer se é Pública ou Privada.

    [SerializeField] private TextMeshProUGUI textoContagem; // Onde vai aparecer "5/10" ou "Sala Cheia".

    [SerializeField] private Button botaoEntrar; // O botão invisível ou visível que o jogador clica para entrar.



    /// <summary>

    /// Configura os textos do slot baseado nas informações fornecidas pelo Photon e define a ação de clique.

    /// </summary>

    public void ConfigurarSlot(RoomInfo infoSala, GerenciadorHUDServer gerenciador)

    {

        // 1. Define o nome da sala na UI

        textoNomeSala.text = infoSala.Name;



        // 2. Verifica o tipo da sala (se possui a propriedade "Senha" registrada no Lobby, é Privada)

        bool ehPrivada = infoSala.CustomProperties.ContainsKey("Senha");

        textoTipoSala.text = ehPrivada ? "Privada" : "Pública";

        // Altera a cor do texto para dar destaque (Laranja para Privada, Verde para Pública)

        textoTipoSala.color = ehPrivada ? new Color(1f, 0.5f, 0f) : Color.green;



        // 3. Verifica se a sala está cheia

        bool salaCheia = infoSala.PlayerCount >= infoSala.MaxPlayers;



        if (salaCheia)

        {

            // Se estiver cheia, exibe texto vermelho e desativa a interatividade do botão

            textoContagem.text = "<color=red>Sala Cheia</color>";

            botaoEntrar.interactable = false;

        }

        else

        {

            // Se houver vaga, exibe a contagem normalmente e deixa o botão clicável

            textoContagem.text = $"{infoSala.PlayerCount}/{infoSala.MaxPlayers}";

            botaoEntrar.interactable = true;

        }



        // 4. Configura a ação de clique do botão

        botaoEntrar.onClick.RemoveAllListeners(); // Remove ações antigas (caso o objeto tenha sido reciclado)



        // Adiciona um evento que chama o Gerenciador enviando as informações Desta Sala específica

        botaoEntrar.onClick.AddListener(() => gerenciador.TentarEntrarNaSalaSelecionada(infoSala));

    }

}