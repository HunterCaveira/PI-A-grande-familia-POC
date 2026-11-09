using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GerenciadorUI : MonoBehaviour
{
<<<<<<< HEAD
=======
    [Header("Referências")]
    [Tooltip("Não precisa arrastar nada! O SetupJogadorMultiplayer vai preencher isso automaticamente.")]
    [SerializeField] private GameObject jogador;

>>>>>>> parent of a587532 (Finalizado o multiplayer)
    [Header("Imagens da UI na Tela")]
    [SerializeField] private Image imagemSlotQ; // Slot 1
    [SerializeField] private Image imagemSlotE; // Slot 2
    [SerializeField] private Image imagemSlotF; // Slot 3
    [SerializeField] public TextMeshProUGUI meuTexto;
<<<<<<< HEAD

    private GameObject jogadorLocal;
    private tiroPistola pistolaRef;

=======
    [SerializeField] private tiroPistola pistolaRef;

    // Estrutura interna para controlar o estado de cada slot
>>>>>>> parent of a587532 (Finalizado o multiplayer)
    private class SlotUIInfo
    {
        public IasUI Habilidade;
        public Image ImagemUI;
        public bool UltimoEstadoPronto = true;
    }

    private Dictionary<int, SlotUIInfo> slotsMap = new Dictionary<int, SlotUIInfo>();
    private bool uiConfigurada = false;

<<<<<<< HEAD
    private void Start()
    {
        slotsMap[1] = new SlotUIInfo { ImagemUI = imagemSlotQ }; // 1 = Q
        slotsMap[2] = new SlotUIInfo { ImagemUI = imagemSlotE }; // 2 = E
        slotsMap[3] = new SlotUIInfo { ImagemUI = imagemSlotF }; // 3 = F
=======
    void Start()
    {
        // 1. Mapeia os slots fixos da UI (Mas ainda não vincula ao jogador, pois ele pode não ter nascido ainda)
        slotsMap[1] = new SlotUIInfo { ImagemUI = imagemSlotQ }; // 1 = Q
        slotsMap[2] = new SlotUIInfo { ImagemUI = imagemSlotE }; // 2 = E
        slotsMap[3] = new SlotUIInfo { ImagemUI = imagemSlotF }; // 3 = F
    }

    // =================================================================================
    // ESTE É O MÉTODO QUE FALTAVA PARA RESOLVER O ERRO VERMELHO!
    // Ele é chamado automaticamente pelo SetupJogadorMultiplayer quando você nasce na rede.
    // =================================================================================
    public void AtribuirJogadorLocal(GameObject jogadorLocal)
    {
        jogador = jogadorLocal;

        if (jogador == null) return;

        // MULTIPLAYER SEGURO: Procura a arma APENAS dentro do corpo do seu próprio personagem
        pistolaRef = jogador.GetComponentInChildren<tiroPistola>();

        // PEGA automaticamente todas as habilidades que estão no Jogador local
        IasUI[] habilidadesDoJogador = jogador.GetComponentsInChildren<IasUI>();

        // Associa cada habilidade encontrada ao seu respectivo slot baseado no SlotIndex dela
        foreach (var hab in habilidadesDoJogador)
        {
            if (slotsMap.ContainsKey(hab.SlotIndex))
            {
                slotsMap[hab.SlotIndex].Habilidade = hab;
                Debug.Log($"<color=yellow>[GerenciadorUI] Habilidade encontrada e encaixada no Slot {hab.SlotIndex}!</color>");
            }
        }
>>>>>>> parent of a587532 (Finalizado o multiplayer)
    }

    private void Update()
    {
<<<<<<< HEAD
        // Se o jogador ainda não foi configurado ou se o clone atual foi desativado (ex: durante respawn), rebinda a UI
        if (!uiConfigurada || jogadorLocal == null || !jogadorLocal.activeInHierarchy)
        {
            BuscarEConfigurarJogadorLocal();
            return;
        }
=======
        // Se o boneco ainda não nasceu na rede, não tenta atualizar a UI para evitar erros
        if (jogador == null) return;
>>>>>>> parent of a587532 (Finalizado o multiplayer)

        AcabouMunição();

        foreach (var par in slotsMap)
        {
            SlotUIInfo slotInfo = par.Value;

            if (slotInfo.Habilidade == null || slotInfo.ImagemUI == null) continue;

            bool estaPronta = slotInfo.Habilidade.EstaPronta();

            if (estaPronta != slotInfo.UltimoEstadoPronto)
            {
                slotInfo.UltimoEstadoPronto = estaPronta;
<<<<<<< HEAD
=======
                // Troca para a foto certa (Pronta ou Recarregando) da própria interface
>>>>>>> parent of a587532 (Finalizado o multiplayer)
                slotInfo.ImagemUI.sprite = estaPronta ? slotInfo.Habilidade.FotoPronta : slotInfo.Habilidade.FotoRecarregando;
            }
        }
    }

<<<<<<< HEAD
    private void BuscarEConfigurarJogadorLocal()
    {
        // 1. Tenta obter o clone ativo diretamente pelo GerenciadorRespawn
        Gerenciadorderespawn respawn = FindFirstObjectByType<Gerenciadorderespawn>();
        if (respawn != null && respawn.CloneAtivo != null && respawn.CloneAtivo.activeInHierarchy)
        {
            VincularJogador(respawn.CloneAtivo);
            return;
        }

        // 2. Fallback: Procura qualquer jogador ativo na cena com o script MovimentodoPlayer
        MovimentodoPlayer jogadorEncontrado = FindFirstObjectByType<MovimentodoPlayer>();
        if (jogadorEncontrado != null && jogadorEncontrado.gameObject.activeInHierarchy)
        {
            VincularJogador(jogadorEncontrado.gameObject);
        }
    }

    private void VincularJogador(GameObject playerObj)
    {
        jogadorLocal = playerObj;
        pistolaRef = jogadorLocal.GetComponentInChildren<tiroPistola>(true);

        // Limpa os slots para reatribuição limpa
        foreach (var slot in slotsMap.Values)
        {
            slot.Habilidade = null;
        }

        // Procura todos os scripts IasUI no próprio clone e em seus sub-objetos
        IasUI[] habilidadesDoJogador = jogadorLocal.GetComponentsInChildren<IasUI>(true);

        foreach (var hab in habilidadesDoJogador)
        {
            if (slotsMap.ContainsKey(hab.SlotIndex))
            {
                slotsMap[hab.SlotIndex].Habilidade = hab;
                // Força atualização visual imediata no próximo quadro
                slotsMap[hab.SlotIndex].UltimoEstadoPronto = !hab.EstaPronta();
            }
        }

        uiConfigurada = true;
        Debug.Log($"[GerenciadorUI] UI vinculada com sucesso ao clone ativo: {jogadorLocal.name}");
    }

=======
>>>>>>> parent of a587532 (Finalizado o multiplayer)
    public void AcabouMunição()
    {
        if (pistolaRef == null || meuTexto == null) return;

        if (pistolaRef.balaZerada)
        {
            meuTexto.color = Color.blue;
        }
        else
        {
            meuTexto.color = Color.white;
        }
    }
}