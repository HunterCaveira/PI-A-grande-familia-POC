using System.Collections;
using UnityEngine;

public class GerenciadorRespawn : MonoBehaviour
{
    [SerializeField] protected tiroPistola Tiropistola;

    [Header("Prefabs dos Personagens (Offline / Singleplayer)")]
    [SerializeField] private GameObject prefabPersonagem1;
    [SerializeField] private GameObject prefabPersonagem2;

    [Header("Configurações de Respawn")]
    [SerializeField] private Transform[] pontosDeSpawn;
    [SerializeField] private float tempoParaRenascer = 3f;

    // Referência do jogador ativo instanciado na cena
    private GameObject jogadorAtivo;

    private void Start()
    {
        if (pontosDeSpawn == null || pontosDeSpawn.Length == 0)
        {
            Debug.LogError("[GerenciadorRespawn] Nenhum ponto de spawn atribuído no Inspector!");
            return;
        }

        // 1. Lê a escolha do personagem salva pelo Menu via PlayerPrefs
        int idPersonagem = PlayerPrefs.GetInt("PersonagemEscolhido", 1);

        // 2. Instancia o jogador local na cena
        SpawnarPersonagemInicial(idPersonagem);

        if (Tiropistola != null)
        {
            Tiropistola.gameObject.SendMessage("Reload", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void SpawnarPersonagemInicial(int id)
    {
        Transform spawnEscolhido = pontosDeSpawn[Random.Range(0, pontosDeSpawn.Length)];
        GameObject prefabParaCriar = (id == 1) ? prefabPersonagem1 : prefabPersonagem2;

        if (prefabParaCriar != null)
        {
            // Instancia o objeto localmente sem usar PhotonNetwork.Instantiate
            jogadorAtivo = Instantiate(prefabParaCriar, spawnEscolhido.position, spawnEscolhido.rotation);
        }
        else
        {
            Debug.LogError($"[GerenciadorRespawn] O Prefab do Personagem {id} não foi atribuído no Inspector!");
        }
    }

    public void ProcessarMorte(GameObject jogador)
    {
        StartCoroutine(RotinaRespawn(jogador));
    }

    private IEnumerator RotinaRespawn(GameObject jogador)
    {
        // 1. Desativa o GameObject do jogador durante o tempo de respawn
        jogador.SetActive(false);

        // 2. Aguarda a contagem de tempo
        yield return new WaitForSeconds(tempoParaRenascer);

        // 3. Sorteia um novo ponto de spawn e reposiciona o jogador
        if (pontosDeSpawn.Length > 0)
        {
            Transform spawnEscolhido = pontosDeSpawn[Random.Range(0, pontosDeSpawn.Length)];
            jogador.transform.position = spawnEscolhido.position;
            jogador.transform.rotation = spawnEscolhido.rotation;
        }

        // 4. Reativa o jogador e restaura os atributos de vida
        jogador.SetActive(true);

        if (jogador.TryGetComponent<Sistemadevida>(out Sistemadevida vida))
        {
            vida.Renascer();
        }
    }
}