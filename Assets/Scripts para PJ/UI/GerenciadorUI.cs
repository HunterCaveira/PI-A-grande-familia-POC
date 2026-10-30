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

    // Dicionário que mapeia o número do Slot (1, 2, 3) para a sua respetiva UI
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
        // Se o boneco ainda não nasceu na rede, tenta procurá-lo periodicamente de forma leve
        if (jogador == null)
        {
            if (Time.time >= proximaBusca)
            {
                TentarAcharJogadorLocal();
                proximaBusca = Time.time + 0.5f; // Tenta a cada meio segundo
            }
            return;
        }

        // Atualiza a cor do texto da munição em tempo real
        AcabouMunição();

        // Atualiza o visual das habilidades apenas quando houver mudança de estado (Pronta <-> Recarregando)
        foreach (var par in slotsMap)
        {
            SlotUIInfo slotInfo = par.Value;

            if (slotInfo.Habilidade == null || slotInfo.ImagemUI == null) continue;

            // Utiliza o método EstaPronta() para respeitar tanto o tempo quanto as travas de animação/uso
            bool estaPronta = slotInfo.Habilidade.EstaPronta();

            if (estaPronta != slotInfo.UltimoEstadoPronto)
            {
                slotInfo.UltimoEstadoPronto = estaPronta;
                // Troca a imagem da UI para a foto pronta ou escurecida (recarregando)
                slotInfo.ImagemUI.sprite = estaPronta ? slotInfo.Habilidade.FotoPronta : slotInfo.Habilidade.FotoRecarregando;
            }
        }
    }

    // Método autônomo de busca para garantir que a UI se conecte ao herói correto sem erros
    private void TentarAcharJogadorLocal()
    {
        // Pega todos os objetos que possuem o componente PhotonView na cena
        PhotonView[] viewsNaCena = Object.FindObjectsByType<PhotonView>(FindObjectsSortMode.None);

        foreach (var view in viewsNaCena)
        {
            // PROCURA O DONO: Filtra apenas o boneco que pertence ao SEU computador (IsMine) 
            // e que seja o objeto raiz "Pai" do herói.
            if (view.IsMine && view.gameObject.name.Contains("Pai"))
            {
                AtribuirJogadorLocal(view.gameObject);
                break;
            }
        }
    }

    // Método principal chamado para vincular a interface ao herói instanciado na rede
    public void AtribuirJogadorLocal(GameObject jogadorLocal)
    {
        jogador = jogadorLocal;

        if (jogador == null) return;

        // MULTIPLAYER SEGURO: Procura a arma e as habilidades APENAS dentro do corpo do seu próprio personagem
        pistolaRef = jogador.GetComponentInChildren<tiroPistola>();

        // Se a referência da munição no tiroPistola estiver vazia, tenta encontrá-la no componente TextMeshPro do hud se necessário
        if (pistolaRef != null && pistolaRef.ammunitionDisplay == null && meuTexto != null)
        {
            pistolaRef.ammunitionDisplay = meuTexto;
        }

        // PEGA automaticamente todas as habilidades que estão no Jogador local (Parede, Cinto, Tônico)
        IasUI[] habilidadesDoJogador = jogador.GetComponentsInChildren<IasUI>();

        // Associa cada habilidade encontrada ao seu respetivo slot baseado no SlotIndex dela
        foreach (var hab in habilidadesDoJogador)
        {
            if (slotsMap.ContainsKey(hab.SlotIndex))
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

        if (pistolaRef.balaZerada == true)
        {
            meuTexto.color = Color.blue; // Fica azul se estiver zerada/recarregando
        }
        else
        {
            // Se ainda tem bala, pinta de branco
            meuTexto.color = Color.white;
        }
    }
}