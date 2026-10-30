using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class GerenciadorSelecaoHeroi : MonoBehaviourPunCallbacks
{
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
    [SerializeField] private List<SlotJogadorUI> slotsTime1 = new List<SlotJogadorUI>();

    [Header("Slots do Time 2 (Máximo 5)")]
    [SerializeField] private List<SlotJogadorUI> slotsTime2 = new List<SlotJogadorUI>();

    [Header("Fichas de Atributos dos Personagens")]
    [SerializeField] private List<AtributosPersonagem> listaFichasHerois = new List<AtributosPersonagem>();

    [Header("Controles e Botões da Sala")]
    [SerializeField] private Button botaoPlayMaster;
    [SerializeField] private TextMeshProUGUI textoAvisoStatus;

    // Chaves de rede
    private const string CHAVE_TIME = "TimeJogador";
    private const string CHAVE_HEROI = "HeroiIndice";
    private const string CHAVE_PREFAB = "NomePrefabBase"; // Guarda o nome do Prefab para o GerenciadorDeSpawn instanciar

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        LimparTodosOsSlotsUI();
        ExibirInformacoesDaSala();

        // Se o jogador local ainda não definiu um time, aloca-o no Time 1 por padrão.
        if (PhotonNetwork.InRoom && ObterTimeDoJogador(PhotonNetwork.LocalPlayer) == 0)
        {
            SetarTimeLocal(1);
        }

        // Se o jogador entrou e ainda não escolheu herói, define o primeiro da lista como padrão (ou "Pai" da pasta Resources)
        if (PhotonNetwork.InRoom && ObterHeroiDoJogador(PhotonNetwork.LocalPlayer) == -1)
        {
            if (listaFichasHerois.Count > 0)
            {
                ClicarSelecionarHeroi(0);
            }
            else
            {
                // Fallback seguro caso a lista esteja vazia: define diretamente o "Pai" da pasta Resources
                DefinirPrefabPadraoNaRede("Pai");
            }
        }

        AtualizarTodaAInterfaceRede();
    }

    private void ExibirInformacoesDaSala()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            if (textoNomeDaSala != null) textoNomeDaSala.text = $"SALA: {PhotonNetwork.CurrentRoom.Name}";

            if (textoPublicaOuPrivada != null)
            {
                bool ehPrivada = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Senha");
                string tipo = ehPrivada ? "SALA PRIVADA" : "SALA PÚBLICA";
                textoPublicaOuPrivada.text = tipo;
            }
        }
    }

    public void ClicarEntrarTime1() => TentarMudarDeTime(1);
    public void ClicarEntrarTime2() => TentarMudarDeTime(2);

    public void ClicarSelecionarHeroi(int indiceHeroi)
    {
        if (indiceHeroi < 0 || indiceHeroi >= listaFichasHerois.Count) return;

        // BUSCA O NOME DO PREFAB DIRETO DA FICHA (Ou define "Pai" como base padrão da pasta Resources)
        string nomeDoPrefab = "Pai";
        if (listaFichasHerois[indiceHeroi] != null && listaFichasHerois[indiceHeroi].prefabPersonagem != null)
        {
            nomeDoPrefab = listaFichasHerois[indiceHeroi].prefabPersonagem.name;
        }

        Hashtable propriedadeHeroi = new Hashtable
        {
            { CHAVE_HEROI, indiceHeroi },
            { CHAVE_PREFAB, nomeDoPrefab } // Envia o nome do prefab da pasta Resources para a nuvem do Photon
        };

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

    public void ClicarIniciarPartidaMaster()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Garante que se algum jogador porventura entrou sem selecionar propriedades, ele herda o padrão "Pai" da pasta Resources antes de carregar a cena
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.ContainsKey(CHAVE_PREFAB))
            {
                Hashtable propPadrao = new Hashtable { { CHAVE_PREFAB, "Pai" } };
                p.SetCustomProperties(propPadrao);
            }
        }

        Debug.Log("[SELEÇÃO] Dono da sala iniciou a partida. Carregando cena 'Main Park'...");
        //PhotonNetwork.LoadLevel("Main Park");
        SceneManager.LoadScene("Main Park");
    }

    private void TentarMudarDeTime(int numeroTimeDesejado)
    {
        if (ObterTimeDoJogador(PhotonNetwork.LocalPlayer) == numeroTimeDesejado)
        {
            ExibirAviso($"Você já está no Time {numeroTimeDesejado}!");
            return;
        }

        int jogadoresNoTime = ContarJogadoresNoTime(numeroTimeDesejado);

        if (jogadoresNoTime >= 5)
        {
            ExibirAviso($"O Time {numeroTimeDesejado} está cheio! (Máximo 5 jogadores)");
            return;
        }

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
        return 0;
    }

    private int ObterHeroiDoJogador(Player p)
    {
        if (p.CustomProperties.TryGetValue(CHAVE_HEROI, out object heroiObj)) return (int)heroiObj;
        return -1;
    }

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

    private void AtualizarTodaAInterfaceRede()
    {
        LimparTodosOsSlotsUI();

        int indexSlotTime1 = 0;
        int indexSlotTime2 = 0;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int timeDoJogador = ObterTimeDoJogador(p);
            int indiceHeroi = ObterHeroiDoJogador(p);

            string nickBase = string.IsNullOrEmpty(p.NickName) ? "Jogador" : p.NickName;
            string nickFormatado = p.IsMasterClient ? $"<color=#FFD700>{nickBase} (LÍDER)</color>" : nickBase;

            if (timeDoJogador == 1 && indexSlotTime1 < slotsTime1.Count)
            {
                PreencherSlotUI(slotsTime1[indexSlotTime1], nickFormatado, indiceHeroi);
                indexSlotTime1++;
            }
            else if (timeDoJogador == 2 && indexSlotTime2 < slotsTime2.Count)
            {
                PreencherSlotUI(slotsTime2[indexSlotTime2], nickFormatado, indiceHeroi);
                indexSlotTime2++;
            }
        }
        AtualizarBotaoPlayMaster();
    }

    private void PreencherSlotUI(SlotJogadorUI slot, string nicknameFormatado, int indiceHeroi)
    {
        if (slot.textoNickname != null)
        {
            slot.textoNickname.text = nicknameFormatado;
            slot.textoNickname.gameObject.SetActive(true);
        }

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

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) => AtualizarTodaAInterfaceRede();
    public override void OnPlayerEnteredRoom(Player newPlayer) => AtualizarTodaAInterfaceRede();
    public override void OnPlayerLeftRoom(Player otherPlayer) => AtualizarTodaAInterfaceRede();
    public override void OnMasterClientSwitched(Player newMasterClient) => AtualizarTodaAInterfaceRede();
}