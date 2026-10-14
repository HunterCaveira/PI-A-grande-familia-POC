using UnityEngine; // Importa os componentes e classes nativas da Unity.
using UnityEngine.UI; // Importa os elementos base de UI da Unity.
using TMPro; // Importa suporte completo para componentes TextMeshPro.
using UnityEngine.SceneManagement; // Importa o gerenciamento de Cenas.
using Photon.Pun; // Importa as ferramentas principais do Photon PUN 2.
using Photon.Realtime; // Importa opções de salas e estados de rede Photon.
using ExitGames.Client.Photon; // Importa a estrutura Hashtable do Photon para senhas.

// A classe herda de MonoBehaviourPunCallbacks para escutar atualizações de criação de sala no Photon.
public class GerenciadorCriarServer : MonoBehaviourPunCallbacks
{
    // ==========================================
    // 1. MAPEAMENTO DE ELEMENTOS DA UI
    // ==========================================

    [Header("Exibição do Jogador")]
    [SerializeField] private TextMeshProUGUI textoNicknameJogador; // Texto no topo que exibe o Nickname do jogador.

    [Header("Configuração de Sala Pública")]
    [SerializeField] private TMP_InputField campoNomeSalaPublica; // InputField OBRIGATÓRIO do nome para Sala Pública.

    [Header("Configuração de Sala Privada")]
    [SerializeField] private TMP_InputField campoNomeSalaPrivada; // InputField OBRIGATÓRIO do nome para Sala Privada.
    [SerializeField] private TMP_InputField campoSenhaSalaPrivada; // InputField OBRIGATÓRIO da senha para Sala Privada.

    [Header("Textos de Mensagens de Alerta")]
    [SerializeField] private TextMeshProUGUI textoAvisoStatus; // Componente de texto para mensagens de erro/alerta.


    // ==========================================
    // 2. INICIALIZAÇÃO E EXIBIÇÃO DE NICKNAME
    // ==========================================

    private void Start()
    {
        // Garante que a troca de cena efetuada pelo Host seja replicada para toda a sala.
        PhotonNetwork.AutomaticallySyncScene = true;

        // Atualiza a interface exibindo o Nickname do jogador recuperado do PhotonNetwork.
        ExibirNicknameDoJogador();

        // Limpa avisos residuais de alertas na tela.
        ExibirAviso("");
    }

    /// <summary>
    /// Lê o Nickname gravado no PhotonNetwork e exibe no elemento de texto no topo da interface.
    /// </summary>
    private void ExibirNicknameDoJogador()
    {
        if (textoNicknameJogador != null)
        {
            string nick = string.IsNullOrEmpty(PhotonNetwork.NickName) ? "Familiar" : PhotonNetwork.NickName;
            textoNicknameJogador.text = $"{nick}"; // Atribui o texto formatado no topo.
        }
    }


    // ==========================================
    // 3. MÉTODOS PÚBLICOS DE CRIAÇÃO DE SALAS
    // ==========================================

    /// <summary>
    /// Ação do botão "Criar Sala Publica". Exige obrigatoriamente o Nome da Sala.
    /// </summary>
    public void ClicarCriarSalaPublica()
    {
        Debug.Log("[CRIAR SERVER] Validando criação de Sala Pública...");

        // MENSAGEM DE ALERTA 1: Valida se o nome da sala pública foi preenchido.
        if (campoNomeSalaPublica == null || string.IsNullOrWhiteSpace(campoNomeSalaPublica.text))
        {
            ExibirAviso("ATENÇÃO: É OBRIGATÓRIO digitar um nome para a Sala Pública!");
            Debug.LogWarning("[CRIAR SERVER] Criação bloqueada: Nome da Sala Pública vazio!");
            return; // Bloqueia a execução e não cria a sala.
        }

        string nomeSala = campoNomeSalaPublica.text.Trim(); // Formata o texto.

        // Define as regras para a criação da Sala Pública no Photon.
        RoomOptions opcoesPublicas = new RoomOptions
        {
            MaxPlayers = 10,  // Permite até 10 jogadores (5v5).
            IsVisible = true, // Torna a sala pública e listável para todos no Lobby.
            IsOpen = true     // Permite novas conexões.
        };

        ExibirAviso("Criando sala pública...");
        PhotonNetwork.CreateRoom(nomeSala, opcoesPublicas); // Envia o pedido de criação ao Photon.
    }

    /// <summary>
    /// Ação do botão "Criar Sala Privada". Exige obrigatoriamente Nome E Senha.
    /// </summary>
    public void ClicarCriarSalaPrivada()
    {
        Debug.Log("[CRIAR SERVER] Validando criação de Sala Privada...");

        // MENSAGEM DE ALERTA 2: Valida se o nome da sala privada foi preenchido.
        if (campoNomeSalaPrivada == null || string.IsNullOrWhiteSpace(campoNomeSalaPrivada.text))
        {
            ExibirAviso("ATENÇÃO: É OBRIGATÓRIO digitar um nome para a Sala Privada!");
            Debug.LogWarning("[CRIAR SERVER] Criação bloqueada: Nome da Sala Privada vazio!");
            return; // Bloqueia a execução.
        }

        // MENSAGEM DE ALERTA 3: Valida se a senha da sala privada foi preenchida.
        if (campoSenhaSalaPrivada == null || string.IsNullOrWhiteSpace(campoSenhaSalaPrivada.text))
        {
            ExibirAviso("ATENÇÃO: É OBRIGATÓRIO digitar uma senha para a Sala Privada!");
            Debug.LogWarning("[CRIAR SERVER] Criação bloqueada: Senha da Sala Privada vazia!");
            return; // Bloqueia a execução.
        }

        string nomeSala = campoNomeSalaPrivada.text.Trim();
        string senhaSala = campoSenhaSalaPrivada.text.Trim();

        // Cria a Hashtable do Photon para armazenar a senha de acesso da sala privada.
        Hashtable propriedadesCustomizadas = new Hashtable
        {
            { "Senha", senhaSala } // Grava a chave 'Senha'.
        };

        // Define as regras para a criação da Sala Privada no Photon.
        RoomOptions opcoesPrivadas = new RoomOptions
        {
            MaxPlayers = 10, // Permite até 10 jogadores.
            IsVisible = false, // Oculta da lista pública geral de salas para garantir privacidade.
            IsOpen = true, // Aberta para quem souber o nome e a senha.
            CustomRoomProperties = propriedadesCustomizadas, // Anexa as propriedades customizadas.
            CustomRoomPropertiesForLobby = new string[] { "Senha" } // Torna a senha acessível na busca.
        };

        ExibirAviso("Criando sala privada...");
        PhotonNetwork.CreateRoom(nomeSala, opcoesPrivadas); // Envia o pedido de criação ao Photon.
    }

    /// <summary>
    /// Ação do botão "Volta" para retornar à cena HUD Server.
    /// </summary>
    public void ClicarVoltarHUDServer()
    {
        SceneManager.LoadScene("HUD Server");
    }


    // ==========================================
    // 4. MÉTODOS AUXILIARES E RESPOSTAS DA REDE
    // ==========================================

    /// <summary>
    /// Exibe mensagens de alerta e erros na interface de forma protegida.
    /// </summary>
    private void ExibirAviso(string mensagem)
    {
        if (textoAvisoStatus != null)
        {
            textoAvisoStatus.text = mensagem; // Define a nova string de aviso.
            textoAvisoStatus.gameObject.SetActive(!string.IsNullOrEmpty(mensagem)); // Exibe o texto apenas se houver conteúdo.
        }
    }

    /// <summary>
    /// Callback executado quando a sala é criada e o jogador entra nela com sucesso.
    /// </summary>
    public override void OnJoinedRoom()
    {
        Debug.Log($"[PHOTON] Criou e entrou na sala '{PhotonNetwork.CurrentRoom.Name}' com sucesso!");

        // Transiciona todos os jogadores conectados na sala para a cena de Seleção de Personagem.
        PhotonNetwork.LoadLevel("TelaSelecaoPersonagem");
    }

    /// <summary>
    /// Callback executado caso falhe ao criar a sala (ex: nome já existente na nuvem).
    /// </summary>
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ExibirAviso("Falha ao criar sala! O nome informado já está em uso.");
        Debug.LogError($"[PHOTON] Erro na criação de sala: {message}");
    }
}