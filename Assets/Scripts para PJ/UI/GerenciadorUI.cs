using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun; // Adicionado para Multiplayer

public class GerenciadorUI : MonoBehaviour
{
    [Header("Imagens da UI na Tela")]
    [SerializeField] private Image imagemSlotQ; // Slot 1
    [SerializeField] private Image imagemSlotE; // Slot 2
    [SerializeField] private Image imagemSlotF; // Slot 3
    [SerializeField] public TextMeshProUGUI meuTexto;

    // Removido o SerializeField de arma e jogador para evitar arrastar inimigos por engano
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

    void Start()
    {
        slotsMap[1] = new SlotUIInfo { ImagemUI = imagemSlotQ }; // 1 = Q
        slotsMap[2] = new SlotUIInfo { ImagemUI = imagemSlotE }; // 2 = E
        slotsMap[3] = new SlotUIInfo { ImagemUI = imagemSlotF }; // 3 = F
    }

    void Update()
    {
        // MULTIPLAYER: Fica buscando o jogador local caso ele ainda não tenha sido spawnado/configurado
        if (!uiConfigurada)
        {
            BuscarEConfigurarJogadorLocal();
            return; // Interrompe o Update até achar o jogador
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
        // Encontra todos os componentes de movimento (ou seja, todos os players no mapa)
        var todosOsJogadores = FindObjectsByType<MovimentodoPlayer>(FindObjectsSortMode.None);

        foreach (var jog in todosOsJogadores)
        {
            PhotonView pv = jog.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine) // Acha EXATAMENTE o jogador que pertence a esta máquina
            {
                jogadorLocal = jog.gameObject;
                pistolaRef = jogadorLocal.GetComponentInChildren<tiroPistola>(); // Pega a arma somente deste jogador

                IasUI[] habilidadesDoJogador = jogadorLocal.GetComponents<IasUI>();

                foreach (var hab in habilidadesDoJogador)
                {
                    if (slotsMap.ContainsKey(hab.SlotIndex))
                    {
                        slotsMap[hab.SlotIndex].Habilidade = hab;
                    }
                }

                uiConfigurada = true; // Trava a busca, configuração finalizada
                Debug.Log("UI vinculada ao Jogador Local da Rede com sucesso!");
                break;
            }
        }
    }

    public void AcabouMunição()
    {
        if (pistolaRef == null) return;

        if (pistolaRef.balaZerada == true)
        {
            meuTexto.color = Color.blue;
        }
        else
        {
            meuTexto.color = Color.white;
        }
    }
}