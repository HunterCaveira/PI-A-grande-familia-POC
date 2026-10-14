using System.Collections.Generic; // Permite o uso de listas de dados C#.
using UnityEngine; // Importa os componentes principais da Unity.
using UnityEngine.UI; // Importa suporte a elementos clássicos de UI.
using TMPro; // Importa suporte para componentes TextMeshPro.
using UnityEngine.SceneManagement; // Importa o gerenciador de troca de Cenas.
using Photon.Pun; // Importa o ecossistema principal do Photon PUN 2.
using Photon.Realtime; // Importa manipuladores de salas e estados de rede do Photon.

// A classe herda de MonoBehaviourPunCallbacks para escutar respostas do servidor Photon.
public class GerenciadorHUDServer : MonoBehaviourPunCallbacks
{
    // ==========================================
    // 1. MAPEAMENTO DE ELEMENTOS DA UI
    // ==========================================

    [Header("Campos de Entrada (UI)")]
    [SerializeField] private TMP_InputField campoNickname; // InputField onde o jogador digita o apelido.
    [SerializeField] private TextMeshProUGUI textoAvisoErro; // Texto usado para exibir alertas e erros.

    [Header("Painéis da Cena")]
    [SerializeField] private GameObject painelTelaDoServer; // Painel com botões 'Criar Server' e 'Entrar Server'.
    [SerializeField] private GameObject painelAcharServer; // Painel contendo a lista de salas abertas.


    // ==========================================
    // 2. INICIALIZAÇÃO E PRESERVAÇÃO DE NICKNAME
    // ==========================================

    private void Start()
    {
        // Garante a sincronização automática de cenas para a sala inteira no Photon.
        PhotonNetwork.AutomaticallySyncScene = true;

        // Se o jogador já havia preenchido um Nickname no Photon anteriormente, mantém visível no InputField.
        if (campoNickname != null && !string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            campoNickname.text = PhotonNetwork.NickName; // Restaura o Nickname salvo no campo de texto.
        }

        // Garante a conexão inicial com a nuvem do Photon caso ainda não esteja conectado.
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings(); // Conecta utilizando as credenciais do PhotonServerSettings.
            ExibirAviso("Conectando ao servidor Photon...");
        }
        else if (PhotonNetwork.InLobby)
        {
            ExibirAviso(""); // Limpa avisos de carregamento se já estiver no Lobby.
        }
    }


    // ==========================================
    // 3. CALLBACKS NATIVOS DO PHOTON (REDE)
    // ==========================================

    /// <summary>
    /// Callback executado quando a conexão física com o Servidor Master é concluída.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        Debug.Log("[HUD SERVER] Conectado ao Servidor Master com sucesso!");

        // Entra no Lobby padrão se ainda não estiver no processo de entrada ou já dentro dele.
        if (!PhotonNetwork.InLobby && PhotonNetwork.NetworkClientState != ClientState.JoiningLobby)
        {
            PhotonNetwork.JoinLobby(); // Solicita entrada no Lobby para receber atualizações de salas.
        }
    }

    /// <summary>
    /// Callback executado quando a entrada no Lobby é confirmada pela rede.
    /// </summary>
    public override void OnJoinedLobby()
    {
        Debug.Log("[HUD SERVER] Entrou no Lobby com sucesso e está pronto!");
        ExibirAviso(""); // Limpa alertas da interface.
    }


    // ==========================================
    // 4. MÉTODOS AUXILIARES E VALIDAÇÕES
    // ==========================================

    /// <summary>
    /// Altera a mensagem do texto de aviso na tela de forma protegida contra exceções nulas.
    /// </summary>
    private void ExibirAviso(string mensagem)
    {
        if (textoAvisoErro != null)
        {
            textoAvisoErro.text = mensagem; // Atribui o texto informado.
            textoAvisoErro.gameObject.SetActive(!string.IsNullOrEmpty(mensagem)); // Exibe apenas se não for vazio.
        }
    }

    /// <summary>
    /// Valida se o Nickname obrigatório foi preenchido e se o cliente está pronto na rede.
    /// </summary>
    private bool ValidarNicknameEConexao()
    {
        // 1. Valida se o campo de entrada do Nickname está em branco ou nulo.
        if (campoNickname == null || string.IsNullOrWhiteSpace(campoNickname.text))
        {
            ExibirAviso("Digite seu nome de Familiar antes de continuar!"); // Exibe alerta em vermelho na UI.
            Debug.LogWarning("[HUD SERVER] Tentativa de prosseguir sem preencher o Nickname!");
            return false; // Interrompe o fluxo da ação.
        }

        // 2. Valida se a conexão com os servidores do Photon está ativa.
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            ExibirAviso("Aguarde a conexão com o servidor Photon...");
            return false; // Bloqueia a ação até concluir a conexão.
        }

        // Persiste o apelido tratado no perfil Photon do jogador local para ser reutilizado em todas as telas.
        PhotonNetwork.NickName = campoNickname.text.Trim();
        ExibirAviso(""); // Limpa os avisos da tela.
        return true; // Libera o avanço da ação.
    }


    // ==========================================
    // 5. AÇÕES PÚBLICAS DOS BOTÕES DA UI
    // ==========================================

    /// <summary>
    /// Ação do botão "Criar Server".
    /// </summary>
    public void ClicarCriarServer()
    {
        Debug.Log("[HUD SERVER] Botão 'Criar Server' clicado!");

        if (!ValidarNicknameEConexao()) return; // Aborta caso o Nickname seja inválido.

        // Avança para a cena de criação da sala.
        SceneManager.LoadScene("Criar Server");
    }

    /// <summary>
    /// Ação do botão "Server" / "Entrar Server".
    /// </summary>
    public void ClicarEntrarServer()
    {
        Debug.Log("[HUD SERVER] Botão 'Entrar Server' clicado!");

        if (!ValidarNicknameEConexao()) return; // Aborta caso o Nickname seja inválido.

        // Transiciona a visualização para o painel de busca de servidores (Achar Server).
        if (painelTelaDoServer != null) painelTelaDoServer.SetActive(false);
        if (painelAcharServer != null) painelAcharServer.SetActive(true);
    }

    /// <summary>
    /// Ação do botão "Voltar" dentro do painel da lista de servidores.
    /// </summary>
    public void ClicarVoltarParaTelaServer()
    {
        // Retorna a visualização para os botões iniciais.
        if (painelAcharServer != null) painelAcharServer.SetActive(false);
        if (painelTelaDoServer != null) painelTelaDoServer.SetActive(true);
    }

    /// <summary>
    /// Ação do botão "Voltar" principal para o menu do jogo.
    /// </summary>
    public void ClicarVoltarMenu()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect(); // Encerra a conexão ativamente ao sair.
        }

        SceneManager.LoadScene("HUD do jogo"); // Retorna para o Menu Principal.
    }
}