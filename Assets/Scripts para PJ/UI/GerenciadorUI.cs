// Importa as coleções de listas, namespaces da Unity, componentes de UI e TextMeshPro
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// Importa o Photon para reconhecer a autoridade de rede do jogador local
using Photon.Pun;

public class GerenciadorUI : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Não precisa arrastar nada! O script procura o seu jogador local automaticamente na cena.")]
    [SerializeField] private GameObject jogador;

    [Header("Imagens da UI na Tela")]
    [SerializeField] private Image imagemSlotQ; // Slot 1 (Parede)
    [SerializeField] private Image imagemSlotE; // Slot 2 (Cinto)
    [SerializeField] private Image imagemSlotF; // Slot 3 (Tônico)
    [SerializeField] public TextMeshProUGUI meuTexto; // Texto da munição na tela
    [SerializeField] private tiroPistola pistolaRef; // Referência à arma do jogador

    // Estrutura interna para controlar o estado visual de cada slot da HUD
    private class SlotUIInfo
    {
        public IasUI Habilidade;
        public Image ImagemUI;
        public bool UltimoEstadoPronto = true;
    }

    // Dicionário que mapeia o número do Slot (1, 2, 3) para a sua respectiva UI
    private Dictionary<int, SlotUIInfo> slotsMap = new Dictionary<int, SlotUIInfo>();

    // Variáveis de controle para varredura otimizada
    private float proximaBusca = 0f;

    void Start()
    {
        // 1. Mapeia os slots fixos da HUD na tela
        slotsMap[1] = new SlotUIInfo { ImagemUI = imagemSlotQ }; // 1 = Q
        slotsMap[2] = new SlotUIInfo { ImagemUI = imagemSlotE }; // 2 = E
        slotsMap[3] = new SlotUIInfo { ImagemUI = imagemSlotF }; // 3 = F

        // Tenta encontrar o jogador local logo no início
        TentarAcharJogadorLocal();
    }

    void Update()
    {
        // Se o boneco ainda não nasceu na rede ou foi destruído (morte/respawn)
        if (jogador == null)
        {
            if (Time.time >= proximaBusca)
            {
                LimparReferenciasJogador(); // Limpa resíduos de instâncias destruídas
                TentarAcharJogadorLocal();
                proximaBusca = Time.time + 0.5f; // Tenta a cada meio segundo
            }
            return;
        }

        // Atualiza a cor do texto da munição em tempo real
        AcabouMunição();

        // Atualiza o visual das habilidades apenas quando houver mudança de estado
        foreach (var par in slotsMap)
        {
            SlotUIInfo slotInfo = par.Value;

            // Validação de segurança contra objetos destruídos da Unity
            if (slotInfo.ImagemUI == null || slotInfo.Habilidade == null || (slotInfo.Habilidade is Object obj && obj == null))
                continue;

            // Utiliza o método EstaPronta() para respeitar tanto o tempo quanto as travas
            bool estaPronta = slotInfo.Habilidade.EstaPronta();

            if (estaPronta != slotInfo.UltimoEstadoPronto)
            {
                slotInfo.UltimoEstadoPronto = estaPronta;

                Sprite novoSprite = estaPronta ? slotInfo.Habilidade.FotoPronta : slotInfo.Habilidade.FotoRecarregando;
                if (novoSprite != null)
                {
                    slotInfo.ImagemUI.sprite = novoSprite;
                }
            }
        }
    }

    // Limpa referências armazenadas caso o jogador morra ou saia da partida
    private void LimparReferenciasJogador()
    {
        pistolaRef = null;
        foreach (var par in slotsMap)
        {
            par.Value.Habilidade = null;
            par.Value.UltimoEstadoPronto = true;
        }
    }

    // Método autônomo de busca para garantir que a UI se conecte ao herói correto sem erros
    private void TentarAcharJogadorLocal()
    {
        // Pega todos os objetos que possuem o componente PhotonView na cena
        PhotonView[] viewsNaCena = Object.FindObjectsByType<PhotonView>(FindObjectsSortMode.None);

        foreach (var view in viewsNaCena)
        {
            // PROCURA O DONO: Filtra apenas o boneco do jogador local (IsMine) e raiz ("Pai")
            if (view != null && view.IsMine && view.gameObject.name.Contains("Pai"))
            {
                AtribuirJogadorLocal(view.gameObject);
                break;
            }
        }
    }

    // Método principal chamado para vincular a interface ao herói instanciado na rede
    public void AtribuirJogadorLocal(GameObject jogadorLocal)
    {
        if (jogadorLocal == null) return;

        LimparReferenciasJogador(); // Garante um vínculo limpo sem restos do personagem antigo
        jogador = jogadorLocal;

        // MULTIPLAYER SEGURO: Procura a arma e as habilidades APENAS no seu próprio personagem
        pistolaRef = jogador.GetComponentInChildren<tiroPistola>();

        // Associa o componente de texto da munição caso esteja nulo no script da arma
        if (pistolaRef != null && pistolaRef.ammunitionDisplay == null && meuTexto != null)
        {
            pistolaRef.ammunitionDisplay = meuTexto;
        }

        // PEGA automaticamente todas as habilidades no Jogador local (Parede, Cinto, Tônico)
        IasUI[] habilidadesDoJogador = jogador.GetComponentsInChildren<IasUI>();

        // Associa cada habilidade encontrada ao seu respectivo slot baseado no SlotIndex
        foreach (var hab in habilidadesDoJogador)
        {
            if (hab != null && slotsMap.ContainsKey(hab.SlotIndex))
            {
                slotsMap[hab.SlotIndex].Habilidade = hab;
                Debug.Log($"<color=yellow>[GerenciadorUI] Habilidade encontrada e encaixada no Slot {hab.SlotIndex}!</color>");
            }
        }
    }

    // Gerencia a cor do texto da munição na tela
    public void AcabouMunição()
    {
        if (pistolaRef == null || meuTexto == null) return;

        if (pistolaRef.balaZerada)
        {
            meuTexto.color = Color.blue; // Fica azul se estiver zerada/recarregando
        }
        else
        {
            meuTexto.color = Color.red; // Se ainda tem bala, pinta de branco
        }
    }
}