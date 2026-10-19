using System; // Importa estruturas base de dados do C# como [Serializable].
using System.Collections.Generic; // Importa a manipulação de Listas genéricas (List<T>).
using UnityEngine; // Importa as ferramentas e componentes nativos da Unity.
using UnityEngine.UI; // Importa suporte aos componentes gráficos de UI (Button, Image).
using TMPro; // Importa o suporte avançado a textos do TextMeshPro.
using UnityEngine.SceneManagement; // Importa o gerenciador de carregamento de Cenas.
using Photon.Pun; // Importa os métodos e componentes principais do Photon PUN 2.
using Photon.Realtime; // Importa as classes e callbacks de jogadores e salas do Photon.
using ExitGames.Client.Photon; // Importa a estrutura Hashtable do Photon para sincronização em rede.

// A classe herda de MonoBehaviourPunCallbacks para escutar atualizações de rede em tempo real.
public class GerenciadorSelecaoHeroi : MonoBehaviourPunCallbacks
{
    // Estrutura auxiliar serializável para organizar os elementos visuais dos slots de cada time no Inspector
    [Serializable]
    public struct SlotJogadorUI
    {
        public TextMeshProUGUI textoNickname; // Referência do componente de texto do Nickname do slot.
        public Image imagemIconeHeroi;         // Referência do componente de imagem do ícone do Familiar.
    }

    // ==========================================
    // 1. COMPONENTES E REFERÊNCIAS DA UI
    // ==========================================

    [Header("Informações da Sala Atual")]
    [SerializeField] private TextMeshProUGUI textoPublicaOuPrivada; // Componente de texto para exibir 'SALA PÚBLICA' ou 'SALA PRIVADA'.
    [SerializeField] private TextMeshProUGUI textoNomeDaSala;        // Componente de texto para exibir o Nome da Sala.

    [Header("Slots do Time 1 (Máximo 5)")]
    [SerializeField] private List<SlotJogadorUI> slotsTime1 = new List<SlotJogadorUI>(); // Lista dos 5 slots do Time 1.

    [Header("Slots do Time 2 (Máximo 5)")]
    [SerializeField] private List<SlotJogadorUI> slotsTime2 = new List<SlotJogadorUI>(); // Lista dos 5 slots do Time 2.

    [Header("Fichas de Atributos dos Personagens")]
    [SerializeField] private List<AtributosPersonagem> listaFichasHerois = new List<AtributosPersonagem>(); // Fichas dos heróis.

    [Header("Controles e Botões da Sala")]
    [SerializeField] private Button botaoPlayMaster; // Botão de Play exclusivo para o Dono da Sala (Master Client).
    [SerializeField] private TextMeshProUGUI textoAvisoStatus; // Componente de texto para mensagens de alerta na interface.

    // Chaves de propriedades customizadas para salvar dados no Photon
    private const string CHAVE_TIME = "TimeJogador"; // Valor numérico do time: 1 ou 2.
    private const string CHAVE_HEROI = "HeroiIndice"; // Valor numérico do índice da ficha do herói escolhido.


    // ==========================================
    // 2. INICIALIZAÇÃO DA CENA
    // ==========================================

    private void Start()
    {
        // Define que todos os jogadores conectados na sala trocarão de cena simultaneamente quando o Master carregar o nível.
        PhotonNetwork.AutomaticallySyncScene = true;

        // Limpa visualmente todos os nomes e ícones da tela.
        LimparTodosOsSlotsUI();

        // Exibe o Nome e se a Sala é Pública ou Privada nos componentes do topo.
        ExibirInformacoesDaSala();

        // Se o jogador local ainda não definiu um time nas propriedades de rede, aloca-o no Time 1 por padrão.
        if (PhotonNetwork.InRoom && ObterTimeDoJogador(PhotonNetwork.LocalPlayer) == 0)
        {
            SetarTimeLocal(1);
        }

        // Atualiza e desenha toda a interface para refletir os jogadores conectados na sala.
        AtualizarTodaAInterfaceRede();
    }


    // ==========================================
    // 3. EXIBIÇÃO DE DADOS DA SALA E DO MASTER
    // ==========================================

    /// <summary>
    /// Lê as informações da sala do Photon e atualiza os textos da interface.
    /// </summary>
    private void ExibirInformacoesDaSala()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            if (textoNomeDaSala != null)
            {
                textoNomeDaSala.text = $"SALA: {PhotonNetwork.CurrentRoom.Name}"; // Preenche o nome da sala.
            }

            if (textoPublicaOuPrivada != null)
            {
                string tipo = PhotonNetwork.CurrentRoom.IsVisible ? "SALA PÚBLICA" : "SALA PRIVADA"; // Determina o tipo.
                textoPublicaOuPrivada.text = tipo; // Exibe o tipo de sala.
            }
        }
    }


    // ==========================================
    // 4. MÉTODOS PÚBLICOS DOS BOTÕES DA UI
    // ==========================================

    /// <summary>
    /// Ação invocado ao clicar no botão 'Time 1'.
    /// </summary>
    public void ClicarEntrarTime1()
    {
        TentarMudarDeTime(1);
    }

    /// <summary>
    /// Ação invocado ao clicar no botão 'Time 2'.
    /// </summary>
    public void ClicarEntrarTime2()
    {
        TentarMudarDeTime(2);
    }

    /// <summary>
    /// Ação invocado ao clicar na foto de um herói no centro da tela.
    /// </summary>
    public void ClicarSelecionarHeroi(int indiceHeroi)
    {
        if (indiceHeroi < 0 || indiceHeroi >= listaFichasHerois.Count) return;

        // Grava o índice do herói escolhido no perfil do jogador local e transmite para a sala Photon.
        Hashtable propriedadeHeroi = new Hashtable
        {
            { CHAVE_HEROI, indiceHeroi }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(propriedadeHeroi); // Sincroniza via rede.
        ExibirAviso($"Familiar '{listaFichasHerois[indiceHeroi].nomePersonagem}' selecionado!");
    }

    /// <summary>
    /// Ação do botão 'Play'. Exclusivo para o Dono da Sala (Master Client).
    /// </summary>
    public void ClicarIniciarPartidaMaster()
    {
        if (!PhotonNetwork.IsMasterClient) return; // Segurança contra chamadas não autorizadas.

        Debug.Log("[SELEÇÃO] Dono da sala iniciou a partida. Carregando cena 'Main Park'...");

        // Carrega a cena 3D 'Main Park' para todos os jogadores na sala simultaneamente.
        PhotonNetwork.LoadLevel("Main Park");
    }


    // ==========================================
    // 5. LÓGICA DE ALOCATION E DADOS DE REDE
    // ==========================================

    /// <summary>
    /// Solicita a mudança de time validando se o time desejado possui vagas (máximo 5).
    /// </summary>
    private void TentarMudarDeTime(int numeroTimeDesejado)
    {
        // Se o jogador já estiver no time selecionado, cancela a operação.
        if (ObterTimeDoJogador(PhotonNetwork.LocalPlayer) == numeroTimeDesejado)
        {
            ExibirAviso($"Você já está no Time {numeroTimeDesejado}!");
            return;
        }

        // Conta quantos participantes já estão no time desejado.
        int jogadoresNoTime = ContarJogadoresNoTime(numeroTimeDesejado);

        // BLOQUEIO DE VAGAS: Impede a troca se o time já tiver 5 participantes.
        if (jogadoresNoTime >= 5)
        {
            ExibirAviso($"O Time {numeroTimeDesejado} está cheio! (Máximo 5 jogadores)");
            Debug.LogWarning($"[SELEÇÃO] Troca para o Time {numeroTimeDesejado} bloqueada: Limite de 5 atingido!");
            return; // Bloqueia a ação.
        }

        // Aplica e transmite o novo time do jogador local.
        SetarTimeLocal(numeroTimeDesejado);
        ExibirAviso($"Você mudou para o Time {numeroTimeDesejado}!");
    }

    private void SetarTimeLocal(int numeroTime)
    {
        Hashtable propriedadeTime = new Hashtable
        {
            { CHAVE_TIME, numeroTime }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(propriedadeTime); // Grava no Photon.
    }

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

    private int ObterTimeDoJogador(Player p)
    {
        if (p.CustomProperties.TryGetValue(CHAVE_TIME, out object timeObj))
        {
            return (int)timeObj;
        }
        return 0; // 0 indica sem time configurado
    }

    private int ObterHeroiDoJogador(Player p)
    {
        if (p.CustomProperties.TryGetValue(CHAVE_HEROI, out object heroiObj))
        {
            return (int)heroiObj;
        }
        return -1; // -1 indica nenhum herói escolhido
    }


    // ==========================================
    // 6. ATUALIZAÇÃO E RENDERIZAÇÃO DA UI
    // ==========================================

    /// <summary>
    /// Limpa e oculta visualmente todos os slots dos dois times.
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
    /// Percorre todos os jogadores da sala, formata seus nicknames (destacando o Dono da Sala com cor) e os posiciona.
    /// </summary>
    private void AtualizarTodaAInterfaceRede()
    {
        LimparTodosOsSlotsUI(); // Reseta a tela antes de redesenhar.

        int indexSlotTime1 = 0; // Índice visual do próximo slot livre no Time 1.
        int indexSlotTime2 = 0; // Índice visual do próximo slot livre no Time 2.

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int timeDoJogador = ObterTimeDoJogador(p);
            int indiceHeroi = ObterHeroiDoJogador(p);

            // FORMATANDO O NICKNAME E COR DO DONO DA SALA (MASTER CLIENT)
            string nickBase = string.IsNullOrEmpty(p.NickName) ? "Jogador" : p.NickName;
            string nickFormatado = nickBase;

            // Se o jogador for o Dono da Sala (Master Client), aplica a cor Dourada/Amarela via RichText e tag de Líder
            if (p.IsMasterClient)
            {
                nickFormatado = $"<color=#FFD700>★ {nickBase} (LÍDER)</color>";
            }

            // Posiciona o jogador na coluna do Time 1
            if (timeDoJogador == 1 && indexSlotTime1 < slotsTime1.Count)
            {
                PreencherSlotUI(slotsTime1[indexSlotTime1], nickFormatado, indiceHeroi);
                indexSlotTime1++;
            }
            // Posiciona o jogador na coluna do Time 2
            else if (timeDoJogador == 2 && indexSlotTime2 < slotsTime2.Count)
            {
                PreencherSlotUI(slotsTime2[indexSlotTime2], nickFormatado, indiceHeroi);
                indexSlotTime2++;
            }
        }

        // Atualiza a visibilidade do botão Play
        AtualizarBotaoPlayMaster();
    }

    /// <summary>
    /// Preenche e ativa um slot individual da interface com o Nickname e a foto do Herói.
    /// </summary>
    private void PreencherSlotUI(SlotJogadorUI slot, string nicknameFormatado, int indiceHeroi)
    {
        if (slot.textoNickname != null)
        {
            slot.textoNickname.text = nicknameFormatado; // Exibe o nickname (formatado com cor para o líder).
            slot.textoNickname.gameObject.SetActive(true); // Exibe o texto na UI.
        }

        if (slot.imagemIconeHeroi != null)
        {
            if (indiceHeroi >= 0 && indiceHeroi < listaFichasHerois.Count && listaFichasHerois[indiceHeroi] != null)
            {
                slot.imagemIconeHeroi.sprite = listaFichasHerois[indiceHeroi].iconePersonagem;
                slot.imagemIconeHeroi.gameObject.SetActive(true); // Exibe a foto do personagem escolhido.
            }
            else
            {
                slot.imagemIconeHeroi.gameObject.SetActive(false); // Oculta se não escolheu herói.
            }
        }
    }

    /// <summary>
    /// Exibe o botão de Play e permite interatividade EXCLUSIVAMENTE para o Dono da Sala.
    /// </summary>
    private void AtualizarBotaoPlayMaster()
    {
        if (botaoPlayMaster != null)
        {
            bool ehDono = PhotonNetwork.IsMasterClient;
            botaoPlayMaster.gameObject.SetActive(ehDono); // Esconde botão se não for o Dono da Sala.
            botaoPlayMaster.interactable = ehDono; // Habilita o clique se for o Dono da Sala.
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
    /// Disparado automaticamente quando qualquer jogador atualiza suas propriedades de time/herói na rede.
    /// </summary>
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        AtualizarTodaAInterfaceRede(); // Redesenha a tela instantaneamente para todos os jogadores na sala.
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        AtualizarTodaAInterfaceRede(); // Atualiza a tela quando um novo jogador entra.
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        AtualizarTodaAInterfaceRede(); // Atualiza a tela quando um jogador sai.
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        AtualizarTodaAInterfaceRede(); // Transfere a cor de líder e o botão Play para o novo Dono da Sala.
    }
}