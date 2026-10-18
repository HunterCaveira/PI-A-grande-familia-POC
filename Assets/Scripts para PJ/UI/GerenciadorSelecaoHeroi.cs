using System; // Permite o uso de atributos nativos C# como [Serializable].
using System.Collections.Generic; // Permite o uso de listas dinâmicas (List<T>).
using UnityEngine; // Importa os componentes e funções nativas da Unity.
using UnityEngine.UI; // Importa suporte aos elementos clássicos de UI (Button, Image).
using TMPro; // Importa o suporte avançado para elementos de texto TextMeshPro.
using UnityEngine.SceneManagement; // Importa o gerenciador para carregamento e transição de Cenas.
using Photon.Pun; // Importa o ecossistema principal de rede Photon PUN 2.
using Photon.Realtime; // Importa manipuladores de jogadores e salas em tempo real do Photon.
using ExitGames.Client.Photon; // Importa a estrutura Hashtable do Photon para sincronização na rede.

// A classe herda de MonoBehaviourPunCallbacks para escutar automaticamente eventos de rede do Photon.
public class GerenciadorSelecaoHeroi : MonoBehaviourPunCallbacks
{
    // Estrutura auxiliar para mapear no Inspector os elementos visuais de cada slot de jogador
    [Serializable]
    public struct SlotJogadorUI
    {
        public TextMeshProUGUI textoNickname; // Referência do texto do nickname no slot.
        public Image imagemIconeHeroi;         // Referência da imagem do ícone do personagem no slot.
    }

    // ==========================================
    // 1. MAPEAMENTO DA INTERFACE E RECURSOS (UI)
    // ==========================================

    [Header("Informações da Sala Atual")]
    [SerializeField] private TextMeshProUGUI textoPublicaOuPrivada; // Exibe 'SALA PÚBLICA' ou 'SALA PRIVADA'.
    [SerializeField] private TextMeshProUGUI textoNomeDaSala;        // Exibe o Nome da Sala criada.

    [Header("Slots de UI do Time 1 (Máximo 5)")]
    [SerializeField] private List<SlotJogadorUI> slotsTime1 = new List<SlotJogadorUI>(); // 5 slots visuais do Time 1.

    [Header("Slots de UI do Time 2 (Máximo 5)")]
    [SerializeField] private List<SlotJogadorUI> slotsTime2 = new List<SlotJogadorUI>(); // 5 slots visuais do Time 2.

    [Header("Fichas de Atributos dos Personagens")]
    [SerializeField] private List<AtributosPersonagem> listaFichasHerois = new List<AtributosPersonagem>(); // Fichas dos heróis.

    [Header("Controles e Botões da Sala")]
    [SerializeField] private Button botaoPlayMaster; // Botão 'Play' acessível apenas para o Dono da Sala (Master Client).
    [SerializeField] private TextMeshProUGUI textoAvisoStatus; // Texto para alertas visuais e mensagens de erro.

    // Constantes de rede para salvar e recuperar as propriedades dos jogadores no Photon
    private const string CHAVE_TIME = "TimeJogador"; // Guarda o número do time (1 ou 2).
    private const string CHAVE_HEROI = "HeroiIndice"; // Guarda o índice da ficha do herói escolhido.


    // ==========================================
    // 2. INICIALIZAÇÃO DA CENA
    // ==========================================

    private void Start()
    {
        // Garante que o Master Client mude a cena de todos na sala simultaneamente ao iniciar o jogo.
        PhotonNetwork.AutomaticallySyncScene = true;

        // Limpa e oculta visualmente todos os slots da tela.
        LimparTodosOsSlotsUI();

        // Atualiza os textos com o Nome e Tipo da Sala atual (Pública/Privada).
        ExibirInformacoesDaSala();

        // Configura a exibição do botão Play apenas para o Dono da Sala.
        AtualizarBotaoPlayMaster();

        // Ao entrar na sala, se o jogador ainda não escolheu um time, aloca-o no Time 1 por padrão.
        if (PhotonNetwork.InRoom && ObterTimeDoJogador(PhotonNetwork.LocalPlayer) == 0)
        {
            TentarEntrarNoTime(1);
        }

        // Desenha a interface com os jogadores já conectados na sala.
        AtualizarTodaAInterfaceRede();
    }


    // ==========================================
    // 3. MÉTODOS DE EXIBIÇÃO DE DADOS DA SALA
    // ==========================================

    /// <summary>
    /// Lê as informações da sala do Photon e exibe o Nome e o Tipo de Sala.
    /// </summary>
    private void ExibirInformacoesDaSala()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            if (textoNomeDaSala != null)
            {
                textoNomeDaSala.text = $"SALA: {PhotonNetwork.CurrentRoom.Name}"; // Exibe o nome da sala.
            }

            if (textoPublicaOuPrivada != null)
            {
                string tipoSala = PhotonNetwork.CurrentRoom.IsVisible ? "SALA PÚBLICA" : "SALA PRIVADA";
                textoPublicaOuPrivada.text = tipoSala; // Exibe o tipo de sala.
            }
        }
    }


    // ==========================================
    // 4. MÉTODOS PÚBLICOS DE SELEÇÃO DE TIME E HERÓI (UI)
    // ==========================================

    /// <summary>
    /// Ação executada ao clicar no botão 'Time 1'. Permite a escolha/troca para o Time 1.
    /// </summary>
    public void ClicarEntrarTime1()
    {
        TentarEntrarNoTime(1); // Executa o fluxo de troca para o Time 1.
    }

    /// <summary>
    /// Ação executada ao clicar no botão 'Time 2'. Permite a escolha/troca para o Time 2.
    /// </summary>
    public void ClicarEntrarTime2()
    {
        TentarEntrarNoTime(2); // Executa o fluxo de troca para o Time 2.
    }

    /// <summary>
    /// Ação executada ao clicar no ícone de um herói (ex: Pai = 0, Mãe = 1...).
    /// </summary>
    public void ClicarSelecionarHeroi(int indiceHeroi)
    {
        if (indiceHeroi < 0 || indiceHeroi >= listaFichasHerois.Count) return;

        // Grava a escolha do personagem no perfil de rede do jogador local.
        Hashtable propriedadeHeroi = new Hashtable
        {
            { CHAVE_HEROI, indiceHeroi }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(propriedadeHeroi); // Sincroniza com a sala.
        ExibirAviso($"Personagem '{listaFichasHerois[indiceHeroi].nomePersonagem}' selecionado!");
    }

    /// <summary>
    /// Ação do botão 'Play'. Acessível apenas para o Dono da Sala.
    /// </summary>
    public void ClicarIniciarPartidaMaster()
    {
        if (!PhotonNetwork.IsMasterClient) return; // Segurança extra.

        Debug.Log("[SELEÇÃO] Master iniciou o jogo! Carregando a cena 'Main Park'...");
        PhotonNetwork.LoadLevel("Main Park"); // Carrega a cena 3D do jogo para todos.
    }


    // ==========================================
    // 5. LÓGICA DE ALOCAÇÃO E TROCA DE TIME
    // ==========================================

    /// <summary>
    /// Realiza a escolha/troca de time do jogador local respeitando o limite de 5 vagas por time.
    /// </summary>
    private void TentarEntrarNoTime(int numeroTimeDesejado)
    {
        // Se o jogador já estiver no time clicado, não faz nada.
        if (ObterTimeDoJogador(PhotonNetwork.LocalPlayer) == numeroTimeDesejado)
        {
            ExibirAviso($"Você já está no Time {numeroTimeDesejado}!");
            return;
        }

        // Conta quantos jogadores já estão no time selecionado.
        int jogadoresNoTime = ContarJogadoresNoTime(numeroTimeDesejado);

        // BLOQUEIO DE LIMITE DE VAGAS: Se o time desejado já tiver 5 jogadores, impede a entrada.
        if (jogadoresNoTime >= 5)
        {
            ExibirAviso($"O Time {numeroTimeDesejado} está cheio! (Máximo 5 jogadores)");
            Debug.LogWarning($"[SELEÇÃO] Troca bloqueada: O Time {numeroTimeDesejado} atingiu o limite de 5 jogadores.");
            return; // Aborta a troca de time.
        }

        // Cria a Hashtable com o novo número do time (1 ou 2).
        Hashtable propriedadeTime = new Hashtable
        {
            { CHAVE_TIME, numeroTimeDesejado }
        };

        // Salva e transmite a mudança de time para toda a sala Photon em tempo real.
        PhotonNetwork.LocalPlayer.SetCustomProperties(propriedadeTime);

        ExibirAviso($"Você mudou para o Time {numeroTimeDesejado}!");
    }

    /// <summary>
    /// Conta a quantidade de jogadores alocados em um determinado time.
    /// </summary>
    private int ContarJogadoresNoTime(int numeroTime)
    {
        int contador = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (ObterTimeDoJogador(p) == numeroTime)
            {
                contador++;
            }
        }
        return contador;
    }

    /// <summary>
    /// Lê a propriedade de rede do jogador e retorna o número do seu time (0 = Nenhum, 1 = Time 1, 2 = Time 2).
    /// </summary>
    private int ObterTimeDoJogador(Player p)
    {
        if (p.CustomProperties.TryGetValue(CHAVE_TIME, out object timeObj))
        {
            return (int)timeObj;
        }
        return 0; // Retorna 0 se ainda não escolheu time
    }

    /// <summary>
    /// Lê a propriedade de rede do jogador e retorna o índice do seu herói escolhido.
    /// </summary>
    private int ObterHeroiDoJogador(Player p)
    {
        if (p.CustomProperties.TryGetValue(CHAVE_HEROI, out object heroiObj))
        {
            return (int)heroiObj;
        }
        return -1; // Retorna -1 se ainda não selecionou herói
    }


    // ==========================================
    // 6. ATUALIZAÇÃO VISUAL DA INTERFACE (UI)
    // ==========================================

    /// <summary>
    /// Reseta e esconde visualmente todos os slots dos dois times.
    /// </summary>
    private void LimparTodosOsSlotsUI()
    {
        LimparListaSlot(slotsTime1);
        LimparListaSlot(slotsTime2);
    }

    private void LimparListaSlot(List<SlotJogadorUI> listaSlots)
    {
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

    /// <summary>
    /// Percorre todos os jogadores da sala e os aloca visualmente nas colunas dos seus respetivos times.
    /// </summary>
    private void AtualizarTodaAInterfaceRede()
    {
        LimparTodosOsSlotsUI(); // Limpa a tela antes de redesenhar.

        int indexSlotTime1 = 0; // Índice visual do próximo slot livre no Time 1.
        int indexSlotTime2 = 0; // Índice visual do próximo slot livre no Time 2.

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int timeDoJogador = ObterTimeDoJogador(p);
            int indiceHeroi = ObterHeroiDoJogador(p);
            string nick = string.IsNullOrEmpty(p.NickName) ? "Jogador" : p.NickName;

            // Aloca o jogador na coluna do Time 1
            if (timeDoJogador == 1 && indexSlotTime1 < slotsTime1.Count)
            {
                PreencherSlotUI(slotsTime1[indexSlotTime1], nick, indiceHeroi);
                indexSlotTime1++;
            }
            // Aloca o jogador na coluna do Time 2
            else if (timeDoJogador == 2 && indexSlotTime2 < slotsTime2.Count)
            {
                PreencherSlotUI(slotsTime2[indexSlotTime2], nick, indiceHeroi);
                indexSlotTime2++;
            }
        }

        AtualizarBotaoPlayMaster();
    }

    /// <summary>
    /// Preenche e ativa um slot específico de UI com o nickname e o ícone do herói.
    /// </summary>
    private void PreencherSlotUI(SlotJogadorUI slot, string nickname, int indiceHeroi)
    {
        if (slot.textoNickname != null)
        {
            slot.textoNickname.text = nickname;
            slot.textoNickname.gameObject.SetActive(true); // Exibe o nickname na UI.
        }

        if (slot.imagemIconeHeroi != null)
        {
            if (indiceHeroi >= 0 && indiceHeroi < listaFichasHerois.Count && listaFichasHerois[indiceHeroi] != null)
            {
                slot.imagemIconeHeroi.sprite = listaFichasHerois[indiceHeroi].iconePersonagem;
                slot.imagemIconeHeroi.gameObject.SetActive(true); // Exibe o ícone do personagem na UI.
            }
            else
            {
                slot.imagemIconeHeroi.gameObject.SetActive(false); // Esconde a imagem se não escolheu herói.
            }
        }
    }

    private void AtualizarBotaoPlayMaster()
    {
        if (botaoPlayMaster != null)
        {
            botaoPlayMaster.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            botaoPlayMaster.interactable = PhotonNetwork.IsMasterClient;
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
    // 7. CALLBACKS AUTOMÁTICOS DO PHOTON (REDE)
    // ==========================================

    /// <summary>
    /// Disparado automaticamente quando um jogador escolhe um time ou herói (muda de CustomProperties).
    /// </summary>
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        AtualizarTodaAInterfaceRede(); // Redesenha a tela instantaneamente para todos os jogadores na sala.
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        AtualizarTodaAInterfaceRede();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        AtualizarTodaAInterfaceRede();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        AtualizarBotaoPlayMaster();
    }
}