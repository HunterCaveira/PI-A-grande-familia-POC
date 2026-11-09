using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GerenciadorUI : MonoBehaviour
{
    [Header("Imagens da UI na Tela")]
    [SerializeField] private Image imagemSlotQ; // Slot 1
    [SerializeField] private Image imagemSlotE; // Slot 2
    [SerializeField] private Image imagemSlotF; // Slot 3
    [SerializeField] public TextMeshProUGUI meuTexto;

    private GameObject jogadorLocal;
    private tiroPistola pistolaRef;

    private class SlotUIInfo
    {
        public IasUI Habilidade;
        public Image ImagemUI;
        public bool UltimoEstadoPronto = true;
    }

    private Dictionary<int, SlotUIInfo> slotsMap = new Dictionary<int, SlotUIInfo>();
    private bool uiConfigurada = false;

    private void Start()
    {
        slotsMap[1] = new SlotUIInfo { ImagemUI = imagemSlotQ }; // 1 = Q
        slotsMap[2] = new SlotUIInfo { ImagemUI = imagemSlotE }; // 2 = E
        slotsMap[3] = new SlotUIInfo { ImagemUI = imagemSlotF }; // 3 = F
    }

    private void Update()
    {
        // Se o jogador ainda não foi configurado ou se o clone atual foi desativado (ex: durante respawn), rebinda a UI
        if (!uiConfigurada || jogadorLocal == null || !jogadorLocal.activeInHierarchy)
        {
            BuscarEConfigurarJogadorLocal();
            return;
        }

        AcabouMunição();

        foreach (var par in slotsMap)
        {
            SlotUIInfo slotInfo = par.Value;

            if (slotInfo.Habilidade == null || slotInfo.ImagemUI == null) continue;

            bool estaPronta = slotInfo.Habilidade.EstaPronta();

            if (estaPronta != slotInfo.UltimoEstadoPronto)
            {
                slotInfo.UltimoEstadoPronto = estaPronta;
                slotInfo.ImagemUI.sprite = estaPronta ? slotInfo.Habilidade.FotoPronta : slotInfo.Habilidade.FotoRecarregando;
            }
        }
    }

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