using Photon.Pun;
using TMPro;
using UnityEngine; // Importa as ferramentas principais do motor da Unity.
using UnityEngine.SceneManagement; // Importa a biblioteca responsável por gerenciar e carregar Cenas do projeto.

public class GerenciadorDeTelasUI : MonoBehaviour // Declara a classe pública GerenciadorDeTelasUI herdando de MonoBehaviour.
{
    // ==========================================
    // 1. CONFIGURAÇÕES DE TRANSIÇÃO
    // ==========================================

    [Header("Configurações de Navegação")] // Organiza visualmente a variável no painel Inspector da Unity.

    [SerializeField]
    private string nomeCenaSelecao = "SelecaoPersonagem"; // Nome exato do arquivo da cena de Seleção contido na pasta Assets/Scenes.
    [Header("Exibição do Jogador Local")]

    [SerializeField] private TextMeshProUGUI textoNicknameJogador; // Referência do texto onde será exibido o Nickname.

    private void Start()
    {
        ExibirNicknameDoJogador(); // Atualiza a interface com o nome digitado na HUD Server.
      
    }

    private void ExibirNicknameDoJogador()
    {
        if (textoNicknameJogador != null)
        {
            // Lê o Nickname gravado no PhotonNetwork (ou exibe um nome padrão se testado diretamente)
            string nick = string.IsNullOrEmpty(PhotonNetwork.NickName) ? "Jogador Local" : PhotonNetwork.NickName;
            textoNicknameJogador.text = $"NICK: {nick}";
        }
    }

    // ==========================================
    // 2. MÉTODOS PÚBLICOS DE NAVEGAÇÃO
    // ==========================================

    /// <summary>
    /// Método invocado quando o jogador clica no botão JOGAR.
    /// Carrega a cena de Seleção de Personagem.
    /// </summary>
    public void ClicarBotaoJogar() // Declara o método público associado ao evento OnClick do botão Jogar.
    {
        if (!string.IsNullOrEmpty(nomeCenaSelecao)) // SE o nome da cena tiver sido preenchido e não estiver vazio.
        {
            SceneManager.LoadScene(nomeCenaSelecao); // Solicita ao motor da Unity para carregar e trocar para a cena especificada.
        }
        else // SENÃO (se a variável de texto estiver vazia no Inspector).
        {
            Debug.LogError("[MENU UI ERRO] O nome da cena de Seleção de Personagem não foi configurado no Inspector!"); // Exibe alerta no Console.
        }
    }
}