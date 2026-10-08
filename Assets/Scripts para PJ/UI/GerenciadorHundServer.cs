using UnityEngine; // Importa as ferramentas e componentes fundamentais da Unity.
using UnityEngine.UI; // importa componentes clássicos de UI da UNity (Button, Image, etc.).
using TMPro; //Importa a biblioteca TextMeshPro para leitura de campos de texto (TMP_InputField).
using UnityEngine.SceneManagement; // Importa a biblioteca de gerenciamento e transição de Cenas.
using Photon.Pun; // Importa as ferramentas principais do ecossistema Photon PUN 2.
public class GerenciadorHundServer : MonoBehaviour // HErda de MonoBehaviourPunCallbacks para receber eventos da rede Photon
{
    // ==========================================
    // 1. COMPONENTES E REFERÊNCIAS DA UI
    // ==========================================

    [Header("Campos de Entrada (UI)")]
    [SerializeField] private TMP_InputField campoNickname; // Referência do InputField onde o jogador digita o seu apelido.
    [SerializeField] private TextMeshProUGUI textoAvisoErro; // Texto opcional para exibir mensagens de aviso/erro na tela.

    [Header("Painéis do Servidor")]
    [SerializeField] private GameObject painelSelecaoServer; // Painel contendo os botões "Criar Server" e "Entrar Server".
    [SerializeField] private GameObject painelListaServidores; // Painel que exibe a lista de salas existentes no jogo.

    // ==========================================
    // 2. CICLO DE VIDA E CONEXÃO INICIAL
    // ==========================================


    private void Start()
    {
        // Tenta conectar apenas se houver configurações válidas do Photon configuradas no projeto
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.NickName = campoNickname.text.Trim();
        }
    }


    // ==========================================
    // 3. MÉTODOS DE VALIDAÇÃO DE NICKNAME
    // ==========================================

    /// <summary>
    /// Valida se o jogador digitou um Nickname válido antes de prosseguir.
    /// </summary>
    private bool ValidarNickname()
    {
        if (campoNickname != null && !string.IsNullOrWhiteSpace(campoNickname.text))
        {
            // Salva o apelido no PhotonNetwork para ser sincronizado com todos os outros jogadores da sala
            PhotonNetwork.NickName = campoNickname.text.Trim();

            if (textoAvisoErro != null) textoAvisoErro.text = ""; // Limpa avisos de erro na interface.
            return true; // Retorna verdadeiro indicando que o nome é válido.
                       
        }

        if (textoAvisoErro != null)
        {
            textoAvisoErro.text = "Digite um Nickname válido antes de continuar!";
        
        }

        Debug.LogWarning("[HUD SERVER] O Nickname válido não foi preenchudo!");
        return false;
    }

    // ==========================================
    // 4. MÉTODOS PÚBLICOS DE BOTÕES
    // ==========================================

    /// <summary>
    /// Ação invocado ao clicar no botão "Criar Server".
    /// </summary>

    public void ClicarCriarServer()
    {
        if (!ValidarNickname()) return; //Aborta a execução se o Nickname não for válido.

        // Avança para a cena de Seleção de Personagem onde a sala será criada ou configurada
        SceneManager.LoadScene("TelaSelecaoPersonagem");
    
    }

    /// <summary>
    /// Ação invocado ao clicar no botão "Voltar".
    /// </summary>
    
    public void ClicarVoltarMenu()
    {

        SceneManager.LoadScene("HUD do jogo"); //Retorna para a cena do Menu Principal.
    }
}
