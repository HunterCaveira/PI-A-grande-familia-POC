// Importa as coleções para uso de Coroutines (cronômetros de espera)
using System.Collections;
// Importa a biblioteca base da Unity
using UnityEngine;
// Importa a biblioteca do Photon para instanciar objetos na rede multiplayer
using Photon.Pun;

public class GerenciadorRespawn : MonoBehaviourPun
{
    [Header("Configurações de Respawn")]
    [Tooltip("Lista com os pontos espalhados pelo mapa onde os jogadores podem renascer")]
    [SerializeField] private Transform[] pontosDeSpawn;

    [Tooltip("Quantos segundos o jogador fica aguardando antes de renascer")]
    [SerializeField] private float tempoParaRenascer = 3f;

    [Tooltip("Nome exato do prefab do herói (ex: 'Pai') que está guardado dentro da pasta 'Resources'")]
    [SerializeField] private string nomePrefabHeroi = "Pai";

    // Instância global estática (Singleton) para permitir que outros scripts encontrem o gerenciador facilmente
    public static GerenciadorRespawn Instancia;

    private void Awake()
    {
        // Configura o Singleton do gerenciador de respawn
        if (Instancia == null)
            Instancia = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Validação de segurança para garantir que configurou os pontos de spawn no mapa
        if (pontosDeSpawn == null || pontosDeSpawn.Length == 0)
        {
            Debug.LogError("[GerenciadorRespawn] Nenhum ponto de spawn atribuído no Inspector da cena!");
        }
    }

    // ==========================================
    // MÉTODOS DE PROCESSAMENTO DE MORTE
    // ==========================================

    // Sobrecarga 1: Chamada diretamente pelo Sistema de Vida moderno
    public void ProcessarMorte(int timeID)
    {
        StartCoroutine(RotinaRespawnRede(timeID));
    }

    // Sobrecarga 2: Mantida por compatibilidade caso algum script antigo passe o GameObject do jogador
    public void ProcessarMorte(GameObject jogador)
    {
        int timeID = 1; // Valor padrão caso não encontre a ficha

        // Tenta descobrir a qual time o jogador pertencia antes de morrer
        if (jogador != null && jogador.TryGetComponent<Sistemadevida>(out Sistemadevida vida))
        {
            timeID = vida.TimeID;
        }

        StartCoroutine(RotinaRespawnRede(timeID));
    }

    // A Corrotina principal que gerencia o tempo de espera e o renascimento na rede
    private IEnumerator RotinaRespawnRede(int timeID)
    {
        // 1. Aguarda os segundos configurados (tempo de tela preta / spectate)
        yield return new WaitForSeconds(tempoParaRenascer);

        // 2. Valida se existem pontos de spawn disponíveis no mapa
        if (pontosDeSpawn == null || pontosDeSpawn.Length == 0)
        {
            Debug.LogError("[GerenciadorRespawn] Impossível renascer: A lista de pontos de spawn está vazia!");
            yield break;
        }

        // 3. Sorteia aleatoriamente um dos pontos de spawn cadastrados na cena
        Transform spawnEscolhido = pontosDeSpawn[Random.Range(0, pontosDeSpawn.Length)];

        // 4. Prepara os dados extras (InstantiationData) para enviar o TimeID pela rede ao recriar o boneco
        object[] dadosSpawn = new object[] { timeID };

        // 5. MULTIPLAYER: Recria o personagem do jogador do zero na rede usando PhotonNetwork.Instantiate.
        // Nota: O prefab especificado em 'nomePrefabHeroi' DEVE estar dentro de uma pasta chamada "Resources".
        if (PhotonNetwork.IsConnected)
        {
            GameObject novoHeroi = PhotonNetwork.Instantiate(nomePrefabHeroi, spawnEscolhido.position, spawnEscolhido.rotation, 0, dadosSpawn);
            Debug.Log($"<color=green>[GerenciadorRespawn] Herói renascido com sucesso na rede na posição {spawnEscolhido.position}!</color>");
        }
        else
        {
            Debug.LogWarning("[GerenciadorRespawn] Aviso: Photon não está conectado na rede.");
        }
    }
}