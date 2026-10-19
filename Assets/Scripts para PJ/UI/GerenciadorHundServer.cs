using System.Collections.Generic; // Permite o uso de Dicionários e Listas.
using UnityEngine; // Importa a engine Unity.
using UnityEngine.UI; // Importa a UI nativa.
using TMPro; // Importa os textos TextMeshPro.
using UnityEngine.SceneManagement; // Importa controle de cenas.
using Photon.Pun; // Importa ferramentas do Photon.
using Photon.Realtime; // Importa classes de conexão do Photon.

public class GerenciadorHUDServer : MonoBehaviourPunCallbacks
{
    // ==========================================
    // 1. MAPEAMENTO DA INTERFACE BÁSICA
    // ==========================================
    [Header("Campos de Entrada (UI)")]
    [SerializeField] private TMP_InputField campoNickname; // Onde o jogador digita o apelido
    [SerializeField] private TextMeshProUGUI textoAvisoErro; // Texto para erros gerais

    [Header("Painéis Principais")]
    [SerializeField] private GameObject painelTelaDoServer; // Tela inicial (Criar/Entrar)
    [SerializeField] private GameObject painelAcharServer; // Painel da Lista de Salas

    // ==========================================
    // 2. ELEMENTOS DA LISTA DE SALAS
    // ==========================================
    [Header("Gerenciador da Lista de Salas")]
    [SerializeField] private Transform containerListaSalas; // O objeto que vai organizar as salas (Content do ScrollView ou LayoutGroup)
    [SerializeField] private GameObject prefabSlotSala; // O prefab que possui o script SlotSalaUI

    // ==========================================
    // 3. ELEMENTOS DO PAINEL DE SENHA
    // ==========================================
    [Header("Painel de Senha (Sala Privada)")]
    [SerializeField] private GameObject painelPedirSenha; // Painel popup de senha
    [SerializeField] private TMP_InputField campoSenhaDigitada; // Input onde o jogador tenta adivinhar a senha
    [SerializeField] private TextMeshProUGUI textoAvisoSenha; // Texto de erro de senha incorreta

    // Variáveis de controle interno para a sala que o jogador clicou
    private Dictionary<string, RoomInfo> cacheDeSalas = new Dictionary<string, RoomInfo>();
    private string salaSelecionadaAtual;
    private string senhaRealDaSalaSelecionada;

    // ==========================================
    // INICIALIZAÇÃO
    // ==========================================
    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        // Esconde o painel de senha ao iniciar
        if (painelPedirSenha != null) painelPedirSenha.SetActive(false);

        if (campoNickname != null && !string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            campoNickname.text = PhotonNetwork.NickName;
        }

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            ExibirAvisoGeral("Conectando ao servidor Photon...");
        }
        else if (PhotonNetwork.InLobby)
        {
            ExibirAvisoGeral("");
        }
    }

    public override void OnConnectedToMaster()
    {
        // Sempre que conectar ao Master, entra no Lobby para ter acesso à lista de salas
        if (!PhotonNetwork.InLobby && PhotonNetwork.NetworkClientState != ClientState.JoiningLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedLobby()
    {
        ExibirAvisoGeral(""); // Limpa avisos
        cacheDeSalas.Clear(); // Limpa a lista cacheada anterior
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        cacheDeSalas.Clear(); // Limpa as salas caso a internet caia
    }

    // ==========================================
    // RECEBIMENTO E ATUALIZAÇÃO DA LISTA DE SALAS
    // ==========================================

    /// <summary>
    /// O Photon dispara isso sozinho toda vez que uma sala é criada, destruída ou altera número de jogadores.
    /// </summary>
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // Atualiza nosso dicionário local com as alterações mais recentes
        foreach (RoomInfo info in roomList)
        {
            // Se a sala foi fechada ou ficou invisível, removemos da lista
            if (info.RemovedFromList || !info.IsVisible)
            {
                cacheDeSalas.Remove(info.Name);
            }
            else
            {
                // Adiciona ou atualiza as informações da sala
                cacheDeSalas[info.Name] = info;
            }
        }

        // Se o painel da lista estiver aberto, redesenhamos a UI
        if (painelAcharServer != null && painelAcharServer.activeSelf)
        {
            DesenharListaDeSalas();
        }
    }

    /// <summary>
    /// Destrói os objetos antigos e cria os novos slots de sala na interface.
    /// </summary>
    private void DesenharListaDeSalas()
    {
        // Se o container não estiver referenciado, ignora
        if (containerListaSalas == null || prefabSlotSala == null) return;

        // 1. Limpa todos os botões antigos da tela
        foreach (Transform filho in containerListaSalas)
        {
            Destroy(filho.gameObject);
        }

        // 2. Instancia um botão novo para cada sala registrada no Cache
        foreach (RoomInfo sala in cacheDeSalas.Values)
        {
            // Instancia o prefab como filho do Container
            GameObject novoSlotObj = Instantiate(prefabSlotSala, containerListaSalas);

            // Pega o script SlotSalaUI do prefab e configura enviando os dados da sala
            SlotSalaUI slotScript = novoSlotObj.GetComponent<SlotSalaUI>();
            if (slotScript != null)
            {
                slotScript.ConfigurarSlot(sala, this);
            }
        }
    }

    // ==========================================
    // LÓGICA DE ENTRAR NA SALA CLICADA (PÚBLICA / PRIVADA)
    // ==========================================

    /// <summary>
    /// Chamado pelo botão instanciado na lista quando o jogador clica numa sala.
    /// </summary>
    public void TentarEntrarNaSalaSelecionada(RoomInfo sala)
    {
        // Verifica se a sala clicada é privada (tem a chave "Senha" guardada)
        if (sala.CustomProperties.ContainsKey("Senha"))
        {
            // É PRIVADA! Salva os dados dela internamente e abre o painel para pedir a senha
            salaSelecionadaAtual = sala.Name;
            senhaRealDaSalaSelecionada = (string)sala.CustomProperties["Senha"];

            campoSenhaDigitada.text = ""; // Limpa tentativas anteriores
            textoAvisoSenha.text = ""; // Limpa erros de senha anteriores
            painelPedirSenha.SetActive(true); // Exibe o popup na tela
        }
        else
        {
            // É PÚBLICA! Entra direto.
            ExibirAvisoGeral("Entrando na sala pública...");
            PhotonNetwork.JoinRoom(sala.Name);
        }
    }

    /// <summary>
    /// Ação do botão "Confirmar" dentro do popup de pedir senha.
    /// </summary>
    public void ClicarConfirmarSenha()
    {
        // Verifica se o que o jogador digitou é igual à senha real da sala selecionada
        if (campoSenhaDigitada.text.Trim() == senhaRealDaSalaSelecionada)
        {
            // Senha correta!
            textoAvisoSenha.text = "<color=green>Senha Correta! Entrando...</color>";
            PhotonNetwork.JoinRoom(salaSelecionadaAtual); // Tenta entrar no Photon
        }
        else
        {
            // Senha Incorreta! Avisa e não deixa entrar.
            textoAvisoSenha.text = "<color=red>Senha Incorreta!</color>";
        }
    }

    /// <summary>
    /// Ação do botão "Cancelar" dentro do popup de pedir senha.
    /// </summary>
    public void ClicarFecharPainelSenha()
    {
        painelPedirSenha.SetActive(false); // Esconde o popup
    }


    // ==========================================
    // MÉTODOS BÁSICOS DO MENU (COMO ANTES)
    // ==========================================

    private void ExibirAvisoGeral(string mensagem)
    {
        if (textoAvisoErro != null)
        {
            textoAvisoErro.text = mensagem;
            textoAvisoErro.gameObject.SetActive(!string.IsNullOrEmpty(mensagem));
        }
    }

    private bool ValidarNickname()
    {
        if (campoNickname == null || string.IsNullOrWhiteSpace(campoNickname.text))
        {
            ExibirAvisoGeral("Digite seu nome de Familiar antes de continuar!");
            return false;
        }

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            ExibirAvisoGeral("Aguarde a conexão com o servidor...");
            return false;
        }

        PhotonNetwork.NickName = campoNickname.text.Trim();
        ExibirAvisoGeral("");
        return true;
    }

    public void ClicarCriarServer()
    {
        if (!ValidarNickname()) return;
        SceneManager.LoadScene("Criar Server");
    }

    public void ClicarEntrarServer() // Abre o painel Achar Server
    {
        if (!ValidarNickname()) return;

        painelTelaDoServer.SetActive(false);
        painelAcharServer.SetActive(true);

        // Assim que abre o painel de achar server, força o desenho das salas que já estavam cacheadas
        DesenharListaDeSalas();
    }

    public void ClicarVoltarParaTelaServer() // Fecha o Achar Server
    {
        painelAcharServer.SetActive(false);
        painelTelaDoServer.SetActive(true);
    }

    public void ClicarVoltarMenu()
    {
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
        SceneManager.LoadScene("HUD do jogo");
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("TelaSelecaoPersonagem");
    }
}