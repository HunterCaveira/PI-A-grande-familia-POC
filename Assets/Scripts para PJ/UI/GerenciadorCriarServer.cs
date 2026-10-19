using UnityEngine; // Importa as bibliotecas padrão da Unity.
using UnityEngine.UI; // Importa os componentes de interface padrão.
using TMPro; // Importa suporte a campos TextMeshPro.
using UnityEngine.SceneManagement; // Importa gerenciamento de Cenas.
using Photon.Pun; // Importa Photon PUN 2.
using Photon.Realtime; // Importa manipulação de salas.
using ExitGames.Client.Photon; // Importa Hashtable do Photon.

public class GerenciadorCriarServer : MonoBehaviourPunCallbacks
{
    [Header("Exibição do Jogador")]
    [SerializeField] private TextMeshProUGUI textoNicknameJogador;

    [Header("Configuração de Sala Pública")]
    [SerializeField] private TMP_InputField campoNomeSalaPublica;

    [Header("Configuração de Sala Privada")]
    [SerializeField] private TMP_InputField campoNomeSalaPrivada;
    [SerializeField] private TMP_InputField campoSenhaSalaPrivada;

    [Header("Textos de Mensagens de Alerta")]
    [SerializeField] private TextMeshProUGUI textoAvisoStatus;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        ExibirNicknameDoJogador();
        ExibirAviso("");
    }

    private void ExibirNicknameDoJogador()
    {
        if (textoNicknameJogador != null)
        {
            string nick = string.IsNullOrEmpty(PhotonNetwork.NickName) ? "Familiar" : PhotonNetwork.NickName;
            textoNicknameJogador.text = $"{nick}";
        }
    }

    private void ExibirAviso(string mensagem)
    {
        if (textoAvisoStatus != null)
        {
            textoAvisoStatus.text = mensagem;
            textoAvisoStatus.gameObject.SetActive(!string.IsNullOrEmpty(mensagem));
        }
    }

    public void ClicarCriarSalaPublica()
    {
        if (campoNomeSalaPublica == null || string.IsNullOrWhiteSpace(campoNomeSalaPublica.text))
        {
            ExibirAviso("ERRO: Digite o nome da Sala Pública!");
            return;
        }

        string nomeSala = campoNomeSalaPublica.text.Trim();
        RoomOptions opcoesPublicas = new RoomOptions
        {
            MaxPlayers = 10,
            IsVisible = true, // Visível no Lobby
            IsOpen = true
        };

        ExibirAviso("Criando sala pública...");
        PhotonNetwork.CreateRoom(nomeSala, opcoesPublicas);
    }

    public void ClicarCriarSalaPrivada()
    {
        if (campoNomeSalaPrivada == null || string.IsNullOrWhiteSpace(campoNomeSalaPrivada.text))
        {
            ExibirAviso("ERRO: Digite o nome da Sala Privada!");
            return;
        }

        if (campoSenhaSalaPrivada == null || string.IsNullOrWhiteSpace(campoSenhaSalaPrivada.text))
        {
            ExibirAviso("ERRO: Digite uma senha para a Sala Privada!");
            return;
        }

        string nomeSala = campoNomeSalaPrivada.text.Trim();
        string senhaSala = campoSenhaSalaPrivada.text.Trim();

        Hashtable propriedadesCustomizadas = new Hashtable { { "Senha", senhaSala } };

        RoomOptions opcoesPrivadas = new RoomOptions
        {
            MaxPlayers = 10,
            IsVisible = true, // ALTERADO: Agora é true para aparecer na lista do Lobby!
            IsOpen = true,
            CustomRoomProperties = propriedadesCustomizadas,
            CustomRoomPropertiesForLobby = new string[] { "Senha" } // Exposto para a lista saber que tem senha
        };

        ExibirAviso("Criando sala privada...");
        PhotonNetwork.CreateRoom(nomeSala, opcoesPrivadas);
    }

    public void ClicarVoltarHUDServer()
    {
        SceneManager.LoadScene("HUD Server");
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("TelaSelecaoPersonagem");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ExibirAviso("ERRO: O nome desta sala já está em uso!");
    }
}