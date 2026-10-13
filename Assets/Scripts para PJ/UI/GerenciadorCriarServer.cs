using UnityEngine; // Importa a estrutura fundamental da Unity.
using UnityEngine.UI; // Importa os componentes basicos de interface de usuario.
using TMPro; // Importa componentes de suporte ao TextMeshPro.
using UnityEngine.SceneManagement; // Importa o gerenciador de cenas.
using Photon.Pun; // Importa as extensoes do Photon Unity Networking.
using Photon.Realtime; // Importa classes e opcoes de sala do Photon.
using ExitGames.Client.Photon; // Importa Hashtable para propriedades customizadas do Photon.

// A classe herda de MonoBehaviourPunCallbacks para receber respostas de criacao de sala do Photon.
public class GerenciadorCriarServer : MonoBehaviourPunCallbacks
{
    // ==========================================
    // 1. MAPEAMENTO DE ELEMENTOS DA INTERFACE (UI)
    // ==========================================

    [Header("Configuração de Sala Pública")]
    [SerializeField] private TMP_InputField campoNomeSalaPublica; // Campo OBRIGATÓRIO do nome para Sala Pública.

    [Header("Configuração de Sala Privada")]
    [SerializeField] private TMP_InputField campoNomeSalaPrivada; // Campo OBRIGATÓRIO do nome para Sala Privada.
    [SerializeField] private TMP_InputField campoSenhaSalaPrivada; // Campo OBRIGATÓRIO da senha para Sala Privada.

    [Header("Texto de Feedback ao Jogador")]
    [SerializeField] private TextMeshProUGUI textoAvisoStatus; // Componente de texto para exibir erros e alertas.


    // ==========================================
    // 2. INICIALIZAÇÃO
    // ==========================================

    private void Start()
    {
        // Garante a sincronizacao de troca de ecrã/cena para a sala inteira.
        PhotonNetwork.AutomaticallySyncScene = true;

        ExibirAviso(""); // Limpa avisos residuais da tela ao carregar.
    }


    // ==========================================
    // 3. MÉTODOS PÚBLICOS DOS BOTÕES
    // ==========================================

    /// <summary>
    /// Ação do botão "Criar Sala Publica". Exige estritamente o Nome da Sala.
    /// </summary>
    public void ClicarCriarSalaPublica()
    {
        Debug.Log("[CRIAR SERVER] Validando criação de Sala Pública...");

        // VALIDAÇÃO ESTRITA: O nome da sala pública é OBRIGATÓRIO.
        if (campoNomeSalaPublica == null || string.IsNullOrWhiteSpace(campoNomeSalaPublica.text))
        {
            ExibirAviso("É OBRIGATÓRIO digitar o nome para a Sala Pública!");
            Debug.LogWarning("[CRIAR SERVER] Tentativa de criar Sala Pública sem nome!");
            return; // Interrompe a execucao e nao cria a sala.
        }

        string nomeSala = campoNomeSalaPublica.text.Trim(); // Formata o texto removendo espacos sobressalentes.

        // Parametros de configuracao da Sala Publica.
        RoomOptions opcoesPublicas = new RoomOptions
        {
            MaxPlayers = 10,  // Suporta ate 10 jogadores (5v5).
            IsVisible = true, // Visivel na lista publica do Lobby para todos os jogadores.
            IsOpen = true     // Aberta para receber novas conexoes.
        };

        ExibirAviso("Criando sala pública...");
        PhotonNetwork.CreateRoom(nomeSala, opcoesPublicas); // Solicita a criacao ao Photon.
    }

    /// <summary>
    /// Ação do botão "Criar Sala Privada". Exige estritamente Nome da Sala E Senha.
    /// </summary>
    public void ClicarCriarSalaPrivada()
    {
        Debug.Log("[CRIAR SERVER] Validando criação de Sala Privada...");

        // VALIDAÇÃO ESTRITA 1: O nome da sala privada é OBRIGATÓRIO.
        if (campoNomeSalaPrivada == null || string.IsNullOrWhiteSpace(campoNomeSalaPrivada.text))
        {
            ExibirAviso("É OBRIGATÓRIO digitar o nome para a Sala Privada!");
            Debug.LogWarning("[CRIAR SERVER] Tentativa de criar Sala Privada sem nome!");
            return; // Interrompe a execucao.
        }

        // VALIDAÇÃO ESTRITA 2: A senha da sala privada é OBRIGATÓRIA.
        if (campoSenhaSalaPrivada == null || string.IsNullOrWhiteSpace(campoSenhaSalaPrivada.text))
        {
            ExibirAviso("É OBRIGATÓRIO digitar uma senha para a Sala Privada!");
            Debug.LogWarning("[CRIAR SERVER] Tentativa de criar Sala Privada sem senha!");
            return; // Interrompe a execucao.
        }

        string nomeSala = campoNomeSalaPrivada.text.Trim();
        string senhaSala = campoSenhaSalaPrivada.text.Trim();

        // Tabela de propriedades customizadas do Photon para guardar a Senha associada a sala.
        Hashtable propriedadesCustomizadas = new Hashtable
        {
            { "Senha", senhaSala } // Associa a chave 'Senha' ao valor digitado.
        };

        // Parametros de configuracao da Sala Privada.
        RoomOptions opcoesPrivadas = new RoomOptions
        {
            MaxPlayers = 10, // Suporta ate 10 jogadores.
            IsVisible = false, // Oculta da lista publica geral para privacidade.
            IsOpen = true, // Permite entrada de quem souber o nome e a senha.
            CustomRoomProperties = propriedadesCustomizadas, // Grava a hashtable com a senha.
            CustomRoomPropertiesForLobby = new string[] { "Senha" } // Expoe a chave no Lobby para validacao.
        };

        ExibirAviso("Criando sala privada...");
        PhotonNetwork.CreateRoom(nomeSala, opcoesPrivadas); // Solicita a criacao ao Photon.
    }

    /// <summary>
    /// Ação do botão "Volta" / "Voltar" para retornar à tela de HUD Server.
    /// </summary>
    public void ClicarVoltarHUDServer()
    {
        SceneManager.LoadScene("HUD Server");
    }


    // ==========================================
    // 4. MÉTODOS AUXILIARES E RESPOSTAS DO PHOTON
    // ==========================================

    /// <summary>
    /// Exibe mensagens de alerta e erro na ecrã de forma protegida.
    /// </summary>
    private void ExibirAviso(string mensagem)
    {
        if (textoAvisoStatus != null)
        {
            textoAvisoStatus.text = mensagem;
            textoAvisoStatus.gameObject.SetActive(!string.IsNullOrEmpty(mensagem));
        }
    }

    /// <summary>
    /// Callback executado quando a sala e criada e o jogador entra nela com sucesso.
    /// </summary>
    public override void OnJoinedRoom()
    {
        Debug.Log($"[PHOTON] Criou e entrou na sala '{PhotonNetwork.CurrentRoom.Name}' com sucesso!");

        // Transiciona a sala inteira para a cena de Seleção de Personagem.
        PhotonNetwork.LoadLevel("TelaSelecaoPersonagem");
    }

    /// <summary>
    /// Callback executado caso falhe ao criar a sala (ex: nome ja existente na nuvem).
    /// </summary>
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ExibirAviso("Falha ao criar sala! O nome informado já está em uso.");
        Debug.LogError($"[PHOTON] Erro na criação de sala: {message}");
    }
}
