// Importa bibliotecas básicas de C# e Unity
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // Necessário para trocar de cena (carregar o menu)
// Importa as bibliotecas do Photon para gerir a rede e a sala multiplayer
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

// Herda de MonoBehaviourPunCallbacks para podermos ouvir eventos do servidor (como entrar/sair da sala)
public class GerenciadorSelecaoHeroi : MonoBehaviourPunCallbacks
{
    // Estrutura que guarda a referência visual de cada jogador na tela (Nome e Foto)
    [Serializable]
    public struct SlotJogadorUI
    {
        public TextMeshProUGUI textoNickname;
        public Image imagemIconeHeroi;
    }

    [Header("Informações da Sala Atual")]
    [SerializeField] private TextMeshProUGUI textoPublicaOuPrivada;
    [SerializeField] private TextMeshProUGUI textoNomeDaSala;

    [Header("Slots do Time 1 (Máximo 5)")]
    // Lista com os espaços visuais do Time Azul na tela
    [SerializeField] private List<SlotJogadorUI> slotsTime1 = new List<SlotJogadorUI>();

    [Header("Slots do Time 2 (Máximo 5)")]
    // Lista com os espaços visuais do Time Vermelho na tela
    [SerializeField] private List<SlotJogadorUI> slotsTime2 = new List<SlotJogadorUI>();

    [Header("Fichas de Atributos dos Personagens")]
    // Lista com todos os heróis disponíveis para escolha (para puxar a foto e o nome do prefab)
    [SerializeField] private List<AtributosPersonagem> listaFichasHerois = new List<AtributosPersonagem>();

    [Header("Controles e Botões da Sala")]
    [SerializeField] private Button botaoPlayMaster;
    [SerializeField] private TextMeshProUGUI textoAvisoStatus;

    // MUDANÇA: Variável para você definir no Inspector para qual cena o botão de voltar vai
    [Header("Configurações de Cena")]
    [Tooltip("Nome exato da cena do menu principal. Pela sua imagem, é 'HUD Server'")]
    [SerializeField] private string nomeCenaMenu = "HUD Server";

    // Chaves de rede usadas para guardar informações na nuvem do Photon para cada jogador
    private const string CHAVE_TIME = "TimeJogador";
    private const string CHAVE_HEROI = "HeroiIndice";
    private const string CHAVE_PREFAB = "NomePrefabBase"; // Guarda o nome do Prefab para o GerenciadorDeSpawn instanciar

    private void Start()
    {
        // Garante que quando o líder carregar a cena do jogo, todos os outros jogadores carreguem junto automaticamente
        PhotonNetwork.AutomaticallySyncScene = true;

        // Limpa a tela antes de preencher com os jogadores que já estão na sala
        LimparTodosOsSlotsUI();
        ExibirInformacoesDaSala();

        // Se o jogador local ainda não definiu um time, aloca-o no Time 1 por padrão
        if (PhotonNetwork.InRoom && ObterTimeDoJogador(PhotonNetwork.LocalPlayer) == 0)
        {
            SetarTimeLocal(1);
        }

        // Se o jogador entrou e ainda não escolheu herói, define o primeiro da lista como padrão
        if (PhotonNetwork.InRoom && ObterHeroiDoJogador(PhotonNetwork.LocalPlayer) == -1)
        {
            if (listaFichasHerois.Count > 0)
            {
                ClicarSelecionarHeroi(0); // Seleciona o primeiro herói da lista (índice 0)
            }
            else
            {
                // Fallback seguro caso a lista esteja vazia: define diretamente o "Pai" da pasta Resources
                DefinirPrefabPadraoNaRede("Pai");
            }
        }

        // Atualiza a tela para mostrar todos os jogadores nos seus times
        AtualizarTodaAInterfaceRede();
    }

    private void ExibirInformacoesDaSala()
    {
        // Se o jogador estiver numa sala válida
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            // Escreve o nome da sala no topo da tela
            if (textoNomeDaSala != null) textoNomeDaSala.text = $"SALA: {PhotonNetwork.CurrentRoom.Name}";

            // Verifica se a sala tem senha e avisa na tela se é Pública ou Privada
            if (textoPublicaOuPrivada != null)
            {
                bool ehPrivada = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Senha");
                string tipo = ehPrivada ? "SALA PRIVADA" : "SALA PÚBLICA";
                textoPublicaOuPrivada.text = tipo;
            }
        }
    }

    // Métodos chamados pelos botões na tela para entrar num time específico
    public void ClicarEntrarTime1() => TentarMudarDeTime(1);
    public void ClicarEntrarTime2() => TentarMudarDeTime(2);

    // Chamado quando o jogador clica na foto de um herói na tela
    public void ClicarSelecionarHeroi(int indiceHeroi)
    {
        // Proteção contra erros de índice
        if (indiceHeroi < 0 || indiceHeroi >= listaFichasHerois.Count) return;

        // BUSCA O NOME DO PREFAB DIRETO DA FICHA (Ou define "Pai" como base padrão da pasta Resources)
        string nomeDoPrefab = "Pai";
        if (listaFichasHerois[indiceHeroi] != null && listaFichasHerois[indiceHeroi].prefabPersonagem != null)
        {
            nomeDoPrefab = listaFichasHerois[indiceHeroi].prefabPersonagem.name;
        }

        // Cria o pacote de dados (Hashtable) para enviar à nuvem do Photon
        Hashtable propriedadeHeroi = new Hashtable
        {
            { CHAVE_HEROI, indiceHeroi },
            { CHAVE_PREFAB, nomeDoPrefab } // Envia o nome do prefab da pasta Resources para a nuvem
        };

        // Salva a escolha do herói na rede (os outros jogadores serão avisados via callback)
        PhotonNetwork.LocalPlayer.SetCustomProperties(propriedadeHeroi);
        ExibirAviso($"Personagem '{nomeDoPrefab}' selecionado!");
    }

    private void DefinirPrefabPadraoNaRede(string nomePrefab)
    {
        Hashtable propriedadeHeroi = new Hashtable
        {
            { CHAVE_HEROI, 0 },
            { CHAVE_PREFAB, nomePrefab }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(propriedadeHeroi);
    }

    // Chamado pelo botão "Jogar" (apenas visível para o líder da sala)
    public void ClicarIniciarPartidaMaster()
    {
        // Se quem clicou não for o dono da sala, ignora
        if (!PhotonNetwork.IsMasterClient) return;

        // Garante que se algum jogador porventura entrou sem selecionar propriedades, ele herda o padrão "Pai"
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.ContainsKey(CHAVE_PREFAB))
            {
                Hashtable propPadrao = new Hashtable { { CHAVE_PREFAB, "Pai" } };
                p.SetCustomProperties(propPadrao);
            }
        }

        Debug.Log("[SELEÇÃO] Dono da sala iniciou a partida. Carregando cena 'Main Park'...");

        // O SceneManager muda a cena do líder, e o AutomaticallySyncScene puxa os outros jogadores junto
        SceneManager.LoadScene("Main Park");
    }

    // ==========================================
    // MÉTODO PARA O BOTÃO DE VOLTAR (NOVO)
    // ==========================================
    // Associe este método ao evento OnClick() do botão "Voltar" na TelaSelecaoPersonagem
    public void ClicarVoltarMenu()
    {
        ExibirAviso("Saindo da sala...");

        // Se o jogador estiver conectado numa sala, pede ao Photon para sair
        if (PhotonNetwork.InRoom)
        {
            // O comando LeaveRoom avisa o servidor que você saiu. 
            // Quando a saída for concluída, o servidor dispara a função OnLeftRoom() logo abaixo.
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            // Se já não estiver na sala por algum erro de rede, força o carregamento da cena do menu direto
            SceneManager.LoadScene(nomeCenaMenu);
        }
    }

    // Callback automático do Photon que é chamado assim que a saída da sala é concluída com sucesso
    public override void OnLeftRoom()
    {
        Debug.Log("[SELEÇÃO] Saiu da sala com sucesso. Voltando ao Menu.");
        // Carrega a cena definida (HUD Server)
        SceneManager.LoadScene(nomeCenaMenu);
    }

    private void TentarMudarDeTime(int numeroTimeDesejado)
    {
        // Se o jogador já está no time que clicou, avisa e ignora
        if (ObterTimeDoJogador(PhotonNetwork.LocalPlayer) == numeroTimeDesejado)
        {
            ExibirAviso($"Você já está no Time {numeroTimeDesejado}!");
            return;
        }

        // Conta quantos jogadores já estão no time desejado
        int jogadoresNoTime = ContarJogadoresNoTime(numeroTimeDesejado);

        // Se o time já tiver 5 pessoas, bloqueia a entrada
        if (jogadoresNoTime >= 5)
        {
            ExibirAviso($"O Time {numeroTimeDesejado} está cheio! (Máximo 5 jogadores)");
            return;
        }

        // Salva a mudança de time na rede
        SetarTimeLocal(numeroTimeDesejado);
        ExibirAviso($"Você mudou para o Time {numeroTimeDesejado}!");
    }

    private void SetarTimeLocal(int numeroTime)
    {
        Hashtable propriedadeTime = new Hashtable { { CHAVE_TIME, numeroTime } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(propriedadeTime);
    }

    private int ContarJogadoresNoTime(int numeroTime)
    {
        int contador = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (ObterTimeDoJogador(p) == numeroTime) contador++;
        }
        return contador;
    }

    private int ObterTimeDoJogador(Player p)
    {
        if (p.CustomProperties.TryGetValue(CHAVE_TIME, out object timeObj)) return (int)timeObj;
        return 0; // 0 significa que não tem time
    }

    private int ObterHeroiDoJogador(Player p)
    {
        if (p.CustomProperties.TryGetValue(CHAVE_HEROI, out object heroiObj)) return (int)heroiObj;
        return -1; // -1 significa que não escolheu herói
    }

    private void LimparTodosOsSlotsUI()
    {
        LimparListaSlot(slotsTime1);
        LimparListaSlot(slotsTime2);
    }

    private void LimparListaSlot(List<SlotJogadorUI> listaSlots)
    {
        // Esconde todos os textos e fotos dos slots para preparar uma atualização limpa
        foreach (var slot in listaSlots)
        {
            if (slot.textoNickname != null)
            {
                slot.textoNickname.text = "";
                slot.textoNickname.gameObject.SetActive(false);
            }
            if (slot.imagemIconeHeroi != null)
            {
                slot.imagemIconeHeroi.sprite = null;
                slot.imagemIconeHeroi.gameObject.SetActive(false);
            }
        }
    }

    private void AtualizarTodaAInterfaceRede()
    {
        LimparTodosOsSlotsUI();

        int indexSlotTime1 = 0;
        int indexSlotTime2 = 0;

        // Passa por cada jogador conectado na sala
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int timeDoJogador = ObterTimeDoJogador(p);
            int indiceHeroi = ObterHeroiDoJogador(p);

            // Formata o nome do jogador (coloca amarelo se for o dono da sala)
            string nickBase = string.IsNullOrEmpty(p.NickName) ? "Jogador" : p.NickName;
            string nickFormatado = p.IsMasterClient ? $"<color=#FFD700>{nickBase} (LÍDER)</color>" : nickBase;

            // Se for do Time 1 e houver espaço na lista da tela, preenche
            if (timeDoJogador == 1 && indexSlotTime1 < slotsTime1.Count)
            {
                PreencherSlotUI(slotsTime1[indexSlotTime1], nickFormatado, indiceHeroi);
                indexSlotTime1++;
            }
            // Se for do Time 2 e houver espaço na lista da tela, preenche
            else if (timeDoJogador == 2 && indexSlotTime2 < slotsTime2.Count)
            {
                PreencherSlotUI(slotsTime2[indexSlotTime2], nickFormatado, indiceHeroi);
                indexSlotTime2++;
            }
        }

        // Verifica se quem está a jogar é o líder para mostrar ou esconder o botão de iniciar
        AtualizarBotaoPlayMaster();
    }

    private void PreencherSlotUI(SlotJogadorUI slot, string nicknameFormatado, int indiceHeroi)
    {
        // Liga o texto e coloca o nome do jogador
        if (slot.textoNickname != null)
        {
            slot.textoNickname.text = nicknameFormatado;
            slot.textoNickname.gameObject.SetActive(true);
        }

        // Puxa a foto correspondente à ficha escolhida e liga a imagem
        if (slot.imagemIconeHeroi != null)
        {
            if (indiceHeroi >= 0 && indiceHeroi < listaFichasHerois.Count && listaFichasHerois[indiceHeroi] != null)
            {
                slot.imagemIconeHeroi.sprite = listaFichasHerois[indiceHeroi].iconePersonagem;
                slot.imagemIconeHeroi.gameObject.SetActive(true);
            }
            else
            {
                slot.imagemIconeHeroi.gameObject.SetActive(false);
            }
        }
    }

    private void AtualizarBotaoPlayMaster()
    {
        if (botaoPlayMaster != null)
        {
            bool ehDono = PhotonNetwork.IsMasterClient;
            botaoPlayMaster.gameObject.SetActive(ehDono);
            botaoPlayMaster.interactable = ehDono;
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

    // ==========================================
    // CALLBACKS DO PHOTON (Gatilhos de Rede)
    // ==========================================
    // Sempre que alguém trocar de herói ou time, a tela atualiza
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) => AtualizarTodaAInterfaceRede();
    // Sempre que alguém entrar na sala, a tela atualiza
    public override void OnPlayerEnteredRoom(Player newPlayer) => AtualizarTodaAInterfaceRede();
    // Sempre que alguém sair (fechar o jogo ou usar o botão Voltar), a tela atualiza
    public override void OnPlayerLeftRoom(Player otherPlayer) => AtualizarTodaAInterfaceRede();
    // Se o líder sair e o jogo escolher um novo líder, a tela atualiza (para passar a coroa)
    public override void OnMasterClientSwitched(Player newMasterClient) => AtualizarTodaAInterfaceRede();
}