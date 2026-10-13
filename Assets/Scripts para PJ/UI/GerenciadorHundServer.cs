using System.Collections.Generic; // Permite trabalhar com listas dinamicas (List<RoomInfo>).
using UnityEngine; // Importa os componentes e classes base do ecossistema Unity.
using UnityEngine.UI; // Importa suporte aos elementos de interface padrao (Button, Image).
using TMPro; // Importa o suporte completo para TextMeshPro.
using UnityEngine.SceneManagement; // Importa o gerenciador de carregamento de Cenas.
using Photon.Pun; // Importa os recursos principais do Photon PUN 2.
using Photon.Realtime; // Importa manipuladores de salas e eventos do Photon.

// A classe herda de MonoBehaviourPunCallbacks para escutar atualizacoes de rede e do Lobby.
public class GerenciadorHUDServer : MonoBehaviourPunCallbacks
{
    // ==========================================
    // 1. COMPONENTES DE INTERFACE GRÁFICA (UI)
    // ==========================================

    [Header("Campos de Entrada (UI)")]
    [SerializeField] private TMP_InputField campoNickname; // InputField onde o jogador digita seu apelido de Familiar.
    [SerializeField] private TextMeshProUGUI textoAvisoErro; // Texto usado para exibir alertas e status.

    [Header("Painéis de Navegação")]
    [SerializeField] private GameObject painelTelaDoServer; // Painel contendo os botoes 'Criar Server' e 'Entrar Server'.
    [SerializeField] private GameObject painelAcharServer; // Painel contendo a lista visual de servidores abertos.


    // ==========================================
    // 2. INICIALIZAÇÃO DA CONEXÃO DE REDE
    // ==========================================

    private void Start()
    {
        // Garante que todos os jogadores na mesma sala troquem de cena juntos.
        PhotonNetwork.AutomaticallySyncScene = true;

        // Se ainda nao estiver conectado ao servidor Photon, estabelece a conexao inicial.
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            ExibirAviso("Conectando ao servidor...");
        }
        else if (PhotonNetwork.InLobby)
        {
            ExibirAviso(""); // Limpa avisos se ja estiver pronto no Lobby.
        }
    }


    // ==========================================
    // 3. CALLBACKS NATIVOS DO PHOTON (REDE)
    // ==========================================

    /// <summary>
    /// Callback acionado quando a conexao fisica com o Servidor Master e estabelecida.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        Debug.Log("[HUD SERVER] Conectado ao Servidor Master do Photon!");

        // Entra no Lobby principal apenas se nao estiver no processo ou ja dentro dele.
        if (!PhotonNetwork.InLobby && PhotonNetwork.NetworkClientState != ClientState.JoiningLobby)
        {
            PhotonNetwork.JoinLobby(); // Garante a entrada no Lobby para receber atualizacoes de salas.
        }
    }

    /// <summary>
    /// Callback acionado quando a entrada no Lobby padrao e confirmada.
    /// </summary>
    public override void OnJoinedLobby()
    {
        Debug.Log("[HUD SERVER] Entrou no Lobby com sucesso!");
        ExibirAviso(""); // Limpa mensagens de carregamento.
    }


    // ==========================================
    // 4. MÉTODOS AUXILIARES E VALIDAÇÕES
    // ==========================================

    /// <summary>
    /// Atualiza o componente de mensagem de aviso de forma segura.
    /// </summary>
    private void ExibirAviso(string mensagem)
    {
        if (textoAvisoErro != null)
        {
            textoAvisoErro.text = mensagem;
            textoAvisoErro.gameObject.SetActive(!string.IsNullOrEmpty(mensagem));
        }
    }

    /// <summary>
    /// Valida se o Nickname obrigatorio foi preenchido e se a rede esta conectada.
    /// </summary>
    private bool ValidarNicknameEConexao()
    {
        // 1. Verifica se o campo do Nickname esta em branco ou nulo.
        if (campoNickname == null || string.IsNullOrWhiteSpace(campoNickname.text))
        {
            ExibirAviso("Digite seu nome de Familiar antes de continuar!");
            Debug.LogWarning("[HUD SERVER] Clique bloqueado: Nickname obrigatorio nao preenchido!");
            return false;
        }

        // 2. Verifica se a conexao com a nuvem do Photon esta pronta.
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            ExibirAviso("Aguarde a conexão com o servidor Photon...");
            return false;
        }

        // Salva o nome validado na rede Photon.
        PhotonNetwork.NickName = campoNickname.text.Trim();
        ExibirAviso("");
        return true;
    }


    // ==========================================
    // 5. AÇÕES PÚBLICAS DOS BOTÕES DA TELA
    // ==========================================

    /// <summary>
    /// Ação invocado ao clicar no botão "Criar Server".
    /// </summary>
    public void ClicarCriarServer()
    {
        Debug.Log("[HUD SERVER] Botao 'Criar Server' clicado!");

        if (!ValidarNicknameEConexao()) return; // Aborta se a validacao do Nickname falhar.

        // Avanca para a cena de configuracao de criacao da sala.
        SceneManager.LoadScene("Criar Server");
    }

    /// <summary>
    /// Ação invocado ao clicar no botão "Entrar em um Server" / "Server".
    /// </summary>
    public void ClicarEntrarServer()
    {
        Debug.Log("[HUD SERVER] Botao 'Entrar Server' clicado!");

        if (!ValidarNicknameEConexao()) return; // Aborta se a validacao do Nickname falhar.

        // Alterna a visualizacao para a tela de lista de servidores (Achar Server).
        if (painelTelaDoServer != null) painelTelaDoServer.SetActive(false);
        if (painelAcharServer != null) painelAcharServer.SetActive(true);
    }

    /// <summary>
    /// Ação do botão "Voltar" dentro do painel da lista de servidores.
    /// </summary>
    public void ClicarVoltarParaTelaServer()
    {
        // Retorna para a tela de selecao inicial (Criar Server / Entrar Server).
        if (painelAcharServer != null) painelAcharServer.SetActive(false);
        if (painelTelaDoServer != null) painelTelaDoServer.SetActive(true);
    }

    /// <summary>
    /// Ação invocado ao clicar no botão "Voltar" principal para o menu do jogo.
    /// </summary>
    public void ClicarVoltarMenu()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect(); // Desconecta da rede com seguranca.
        }

        SceneManager.LoadScene("HUD do jogo"); // Retorna para o Menu Principal.
    }
}