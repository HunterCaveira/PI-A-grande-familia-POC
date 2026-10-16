using UnityEngine; // Importa as classes e estruturas fundamentais da Unity.
using UnityEngine.UI; // Importa componentes basicos de UI da Unity.
using TMPro; // Importa suporte a textos e campos de entrada TextMeshPro.
using UnityEngine.SceneManagement; // Importa o gerenciador de troca de Cenas.
using Photon.Pun; // Importa o ecossistema principal do Photon PUN 2.
using Photon.Realtime; // Importa opcoes de configuracao de salas e tipos de rede Photon.
using ExitGames.Client.Photon; // Importa a Hashtable personalizada do Photon para senhas.

// A classe herda de MonoBehaviourPunCallbacks para receber respostas de eventos da rede Photon.
public class GerenciadorCriarServer : MonoBehaviourPunCallbacks
{
    // ==========================================
    // 1. MAPEAMENTO DE ELEMENTOS DA UI
    // ==========================================

    [Header("Exibição do Jogador")]
    [SerializeField] private TextMeshProUGUI textoNicknameJogador; // Texto no topo que exibe o Nickname do jogador local.

    [Header("Configuração de Sala Pública")]
    [SerializeField] private TMP_InputField campoNomeSalaPublica; // InputField OBRIGATÓRIO do nome da Sala Pública.

    [Header("Configuração de Sala Privada")]
    [SerializeField] private TMP_InputField campoNomeSalaPrivada; // InputField OBRIGATÓRIO do nome da Sala Privada.
    [SerializeField] private TMP_InputField campoSenhaSalaPrivada; // InputField OBRIGATÓRIO da senha da Sala Privada.

    [Header("Textos de Mensagens de Alerta")]
    [SerializeField] private TextMeshProUGUI textoAvisoStatus; // Componente de texto vermelho para mensagens de erro/alerta.


    // ==========================================
    // 2. INICIALIZAÇÃO
    // ==========================================

    private void Start()
    {
        // Garante a sincronizacao automatica da troca de cenas para toda a sala no Photon.
        PhotonNetwork.AutomaticallySyncScene = true;

        // Atualiza a interface exibindo o Nickname do jogador.
        ExibirNicknameDoJogador();

        // Limpa qualquer mensagem de erro residual ao abrir a tela.
        ExibirAviso("");
    }

    /// <summary>
    /// Lê o Nickname do jogador no PhotonNetwork e exibe no texto no topo da interface.
    /// </summary>
    private void ExibirNicknameDoJogador()
    {
        if (textoNicknameJogador != null)
        {
            string nick = string.IsNullOrEmpty(PhotonNetwork.NickName) ? "Familiar" : PhotonNetwork.NickName;
            textoNicknameJogador.text = $"{nick}"; // Atualiza o texto no topo.
        }
    }


    // ==========================================
    // 3. MÉTODOS AUXILIARES DE MENSAGEM
    // ==========================================

    /// <summary>
    /// Exibe mensagens de erro em destaque na tela de forma protegida contra referencias nulas.
    /// </summary>
    private void ExibirAviso(string mensagem)
    {
        if (textoAvisoStatus != null)
        {
            textoAvisoStatus.text = mensagem; // Define a nova string de aviso.
            textoAvisoStatus.gameObject.SetActive(!string.IsNullOrEmpty(mensagem)); // Exibe o texto apenas se houver conteudo.
        }
    }


    // ==========================================
    // 4. MÉTODOS PÚBLICOS DOS BOTÕES
    // ==========================================

    /// <summary>
    /// Ação do botão "Criar Sala Publica". Exige estritamente o preenchimento do Nome da Sala.
    /// </summary>
    public void ClicarCriarSalaPublica()
    {
        Debug.Log("[CRIAR SERVER] Validando criação de Sala Pública...");

        // CHECAGEM DE SEGURANÇA: Se o campo estiver vazio, bloqueia imediatamente.
        if (campoNomeSalaPublica == null || string.IsNullOrWhiteSpace(campoNomeSalaPublica.text))
        {
            ExibirAviso("ERRO: Digite o nome da Sala Pública antes de criá-la!"); // Exibe aviso vermelho.
            Debug.LogWarning("[CRIAR SERVER] Bloqueado: Nome da Sala Pública em branco!");
            return; // ABORTA A EXECUÇÃO E NÃO CRIA A SALA.
        }

        string nomeSala = campoNomeSalaPublica.text.Trim(); // Formata o texto tirando espacos extras.

        // Parametros de configuracao da Sala Publica.
        RoomOptions opcoesPublicas = new RoomOptions
        {
            MaxPlayers = 10,  // Suporta ate 10 jogadores (5v5).
            IsVisible = true, // Visivel publicamente na lista do Lobby.
            IsOpen = true     // Aberta para conexoes.
        };

        ExibirAviso("Criando sala pública...");
        PhotonNetwork.CreateRoom(nomeSala, opcoesPublicas); // Envia o pedido de criacao ao Photon.
    }

    /// <summary>
    /// Ação do botão "Criar Sala Privada". Exige estritamente o preenchimento de Nome E Senha.
    /// </summary>
    public void ClicarCriarSalaPrivada()
    {
        Debug.Log("[CRIAR SERVER] Validando criação de Sala Privada...");

        // CHECAGEM DE SEGURANÇA 1: Valida se o nome da sala privada foi preenchido.
        if (campoNomeSalaPrivada == null || string.IsNullOrWhiteSpace(campoNomeSalaPrivada.text))
        {
            ExibirAviso("ERRO: Digite o nome da Sala Privada antes de criá-la!"); // Exibe aviso vermelho.
            Debug.LogWarning("[CRIAR SERVER] Bloqueado: Nome da Sala Privada em branco!");
            return; // ABORTA A EXECUÇÃO.
        }

        // CHECAGEM DE SEGURANÇA 2: Valida se a senha da sala privada foi preenchida.
        if (campoSenhaSalaPrivada == null || string.IsNullOrWhiteSpace(campoSenhaSalaPrivada.text))
        {
            ExibirAviso("ERRO: Digite uma senha para a Sala Privada!"); // Exibe aviso vermelho.
            Debug.LogWarning("[CRIAR SERVER] Bloqueado: Senha da Sala Privada em branco!");
            return; // ABORTA A EXECUÇÃO.
        }

        string nomeSala = campoNomeSalaPrivada.text.Trim();
        string senhaSala = campoSenhaSalaPrivada.text.Trim();

        // Tabela de propriedades customizadas para armazenar a senha no Photon.
        Hashtable propriedadesCustomizadas = new Hashtable
        {
            { "Senha", senhaSala } // Grava a senha da sala privada.
        };

        // Parametros de configuracao da Sala Privada.
        RoomOptions opcoesPrivadas = new RoomOptions
        {
            MaxPlayers = 10, // Suporta ate 10 jogadores.
            IsVisible = false, // Oculta da lista publica geral para privacidade.
            IsOpen = true, // Aberta para quem souber o nome e a senha.
            CustomRoomProperties = propriedadesCustomizadas, // Anexa as propriedades customizadas.
            CustomRoomPropertiesForLobby = new string[] { "Senha" } // Expoe a senha para comparacao.
        };

        ExibirAviso("Criando sala privada...");
        PhotonNetwork.CreateRoom(nomeSala, opcoesPrivadas); // Envia o pedido de criacao ao Photon.
    }

    /// <summary>
    /// Ação do botão "Volta" para retornar à cena HUD Server.
    /// </summary>
    public void ClicarVoltarHUDServer()
    {
        SceneManager.LoadScene("HUD Server");
    }


    // ==========================================
    // 5. CALLBACKS E RESPOSTAS DO PHOTON
    // ==========================================

    /// <summary>
    /// Callback executado quando a sala e criada e o jogador entra nela com sucesso.
    /// </summary>
    public override void OnJoinedRoom()
    {
        Debug.Log($"[PHOTON] Criou e entrou na sala '{PhotonNetwork.CurrentRoom.Name}' com sucesso!");

        // Transiciona a sala inteira para a Tela de Selecao de Personagem.
        PhotonNetwork.LoadLevel("TelaSelecaoPersonagem");
    }

    /// <summary>
    /// Callback executado caso falhe ao criar a sala (ex: nome ja existente no servidor).
    /// </summary>
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ExibirAviso("ERRO: O nome desta sala já está em uso! Escolha outro nome.");
        Debug.LogError($"[PHOTON] Erro na criação de sala: {message}");
    }
}