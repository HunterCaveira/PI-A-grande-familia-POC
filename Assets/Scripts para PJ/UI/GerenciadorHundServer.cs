using UnityEngine; // Importa as ferramentas base do ecossistema Unity.
using UnityEngine.UI; // Importa componentes de interface tradicional.
using TMPro; // Importa bibliotecas para manipulacao de texto TextMeshPro.
using UnityEngine.SceneManagement; // Importa o gerenciador de transicao de cenas.
using Photon.Pun; // Importa os recursos principais do Photon PUN 2.
using Photon.Realtime; // Importa estruturas de rede em tempo real do Photon.

// A classe herda de MonoBehaviourPunCallbacks para escutar respostas da rede Photon.
public class GerenciadorHUDServer : MonoBehaviourPunCallbacks
{
    // ==========================================
    // 1. CAMPOS DE UI MAPEADOS NO INSPECTOR
    // ==========================================

    [Header("Campos de Entrada (UI)")]
    [SerializeField] private TMP_InputField campoNickname; // InputField onde o jogador digita o apelido do Familiar.
    [SerializeField] private TMP_InputField campoNomeSala; // InputField opcional para nome personalizado de sala.
    [SerializeField] private TextMeshProUGUI textoAvisoErro; // Texto vermelho usado para feedbacks de erro na UI.


    // ==========================================
    // 2. INICIALIZAÇÃO DA CONEXÃO DE REDE
    // ==========================================

    private void Start()
    {
        // Garante a sincronizacao automatica da troca de cenas para todos na mesma sala.
        PhotonNetwork.AutomaticallySyncScene = true;

        // Se o cliente ainda nao estiver conectado ao servidor Photon, inicia o processo.
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings(); // Conecta utilizando as credenciais cadastradas.
            AtualizarTextoAviso("Conectando ao servidor Photon...");
        }
        else if (PhotonNetwork.InLobby)
        {
            AtualizarTextoAviso(""); // Limpa avisos se ja estiver pronto no Lobby.
        }
    }


    // ==========================================
    // 3. CALLBACKS DO PHOTON (RESPOSTAS DA REDE)
    // ==========================================

    /// <summary>
    /// Callback disparado quando a conexao inicial com o Servidor Master e concluida.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        Debug.Log("[PHOTON] Conectado ao Servidor Master!");

        // Entra no Lobby apenas se nao estiver transicionando nem dentro dele.
        if (!PhotonNetwork.InLobby && PhotonNetwork.NetworkClientState != ClientState.JoiningLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    /// <summary>
    /// Callback disparado assim que o jogador entra com sucesso no Lobby padrao.
    /// </summary>
    public override void OnJoinedLobby()
    {
        Debug.Log("[PHOTON] Entrou no Lobby com sucesso!");
        AtualizarTextoAviso(""); // Limpa qualquer aviso de carregamento.
    }


    // ==========================================
    // 4. MÉTODOS AUXILIARES E VALIDAÇÃO DE ENTRADA
    // ==========================================

    /// <summary>
    /// Altera o texto do componente de aviso de forma segura sem gerar NullReferenceException.
    /// </summary>
    private void AtualizarTextoAviso(string mensagem)
    {
        if (textoAvisoErro != null)
        {
            textoAvisoErro.text = mensagem; // Define a mensagem na tela.
            textoAvisoErro.gameObject.SetActive(!string.IsNullOrEmpty(mensagem)); // Exibe o texto apenas se houver mensagem.
        }
    }

    /// <summary>
    /// Valida se o Nickname foi preenchido e se o cliente esta pronto para operacoes na rede.
    /// </summary>
    private bool ValidarFormulario()
    {
        // 1. Checa se o campo do Nickname esta vazio ou com espacos.
        if (campoNickname == null || string.IsNullOrWhiteSpace(campoNickname.text))
        {
            Debug.LogWarning("[HUD SERVER] Clique bloqueado: Nickname nao preenchido!");
            AtualizarTextoAviso("Digite seu nome de Familiar antes de continuar!");
            return false; // Interrompe a acao.
        }

        // 2. Checa se a conexao Photon esta ativa e pronta.
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[HUD SERVER] Clique bloqueado: Photon ainda conectando...");
            AtualizarTextoAviso("Aguarde a conexao com o servidor Photon...");
            return false; // Interrompe a acao.
        }

        // Se ambas as checagens passarem, salva o nome no perfil Photon.
        PhotonNetwork.NickName = campoNickname.text.Trim();
        AtualizarTextoAviso(""); // Limpa avisos da tela.
        return true; // Libera o processamento.
    }


    // ==========================================
    // 5. AÇÕES PÚBLICAS DOS BOTÕES
    // ==========================================

    /// <summary>
    /// Acao associada ao botao 'Criar Server'.
    /// </summary>
    public void ClicarCriarServer()
    {
        Debug.Log("[HUD SERVER] Botao 'Criar Server' clicado!"); // Confirma o clique no Console.

        if (!ValidarFormulario()) return; // Aborta se a validacao falhar.

        // Define o nome da sala (usa o campo ou gera um ID aleatorio).
        string nomeSala = (campoNomeSala != null && !string.IsNullOrWhiteSpace(campoNomeSala.text))
            ? campoNomeSala.text.Trim()
            : $"Sala_{Random.Range(1000, 9999)}";

        // Define parametros de configuracao da sala Photon.
        RoomOptions opcoes = new RoomOptions
        {
            MaxPlayers = 10,  // Permite ate 10 jogadores.
            IsVisible = true, // Visivel publicamente no Lobby.
            IsOpen = true     // Permite novas conexoes.
        };

        AtualizarTextoAviso("Criando sala...");
        PhotonNetwork.CreateRoom(nomeSala, opcoes); // Solicita a criacao da sala na nuvem Photon.
    }

    /// <summary>
    /// Acao associada ao botao 'Server' / 'Entrar Server'.
    /// </summary>
    public void ClicarEntrarServer()
    {
        Debug.Log("[HUD SERVER] Botao 'Entrar Server' clicado!"); // Confirma o clique no Console.

        if (!ValidarFormulario()) return; // Aborta se a validacao falhar.

        // Se houver um nome digitado no campo de sala, busca por ele; caso contrario entra em sala aleatoria.
        if (campoNomeSala != null && !string.IsNullOrWhiteSpace(campoNomeSala.text))
        {
            AtualizarTextoAviso($"Buscando sala '{campoNomeSala.text.Trim()}'...");
            PhotonNetwork.JoinRoom(campoNomeSala.text.Trim());
        }
        else
        {
            AtualizarTextoAviso("Buscando uma sala disponivel...");
            PhotonNetwork.JoinRandomRoom();
        }
    }

    /// <summary>
    /// Acao associada ao botao 'Voltar'.
    /// </summary>
    public void ClicarVoltarMenu()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        SceneManager.LoadScene("HUD do jogo");
    }


    // ==========================================
    // 6. TRATAMENTO DE RETORNO DA REDE
    // ==========================================

    /// <summary>
    /// Callback disparado quando a entrada ou criacao de sala e concluida com sucesso.
    /// </summary>
    public override void OnJoinedRoom()
    {
        Debug.Log($"[PHOTON] Entrou na sala '{PhotonNetwork.CurrentRoom.Name}'!");

        // Carrega a cena de Selecao de Personagem via Photon para todos na sala.
        PhotonNetwork.LoadLevel("TelaSelecaoPersonagem");
    }

    /// <summary>
    /// Callback disparado se nao houver salas aleatorias abertas no momento.
    /// </summary>
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        AtualizarTextoAviso("Nenhuma sala aberta encontrada! Clique em 'Criar Server'.");
    }

    /// <summary>
    /// Callback disparado se a sala especificada pelo nome nao for encontrada.
    /// </summary>
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        AtualizarTextoAviso("Sala nao encontrada! Verifique o nome digitado.");
    }
}