using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Respawn : MonoBehaviourPunCallbacks
{
    public static Respawn Instancia { get; private set; }

    [Header("Pontos de Spawn no Mapa (Automáticos)")]
    [SerializeField] private List<Transform> pontosSpawnTime1 = new List<Transform>();
    [SerializeField] private List<Transform> pontosSpawnTime2 = new List<Transform>();

    [Header("Regras de Partida")]
    [SerializeField] private float tempoParaRespawn = 5f;

    private const string PrefabJogador = "Pai"; // Prefab dentro de Assets/Resources

    private void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
        }
        else
        {
            // Destroy(this) remove só o componente duplicado, sem apagar o GameObject inteiro
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
    }

    private void Start()
    {
        BuscarPontosDeSpawnAutomaticos();
    }

    private void BuscarPontosDeSpawnAutomaticos()
    {
        pontosSpawnTime1.Clear();
        pontosSpawnTime2.Clear();

        GameObject[] todosObjetos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (GameObject obj in todosObjetos)
        {
            if (obj.name.Contains("Ponto de Respawn2"))
                pontosSpawnTime2.Add(obj.transform);
            else if (obj.name.Contains("Ponto de Respawn"))
                pontosSpawnTime1.Add(obj.transform);
        }
    }

    // Deve ser chamado apenas no cliente DONO do personagem que morreu.
    // O parâmetro nomePrefabHeroi é mantido por compatibilidade; sempre usa "Pai".
    public void SolicitarRespawn(int timeID, string nomePrefabHeroi = PrefabJogador)
    {
        StartCoroutine(RotinaRespawn(timeID));
    }

    private IEnumerator RotinaRespawn(int timeID)
    {
        yield return new WaitForSeconds(tempoParaRespawn);

        // Se o jogador saiu da sala durante a espera, não recria
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[RESPAWN] Cancelado: jogador não está mais na sala.");
            yield break;
        }

        Transform ponto = ObterPontoDeSpawn(timeID);
        if (ponto == null) ponto = this.transform;

        // O TimeID viaja em InstantiationData e é lido pelo OnPhotonInstantiate
        // do Sistemadevida em TODOS os clientes (não precisa setar depois).
        PhotonNetwork.Instantiate(
            PrefabJogador,
            ponto.position,
            ponto.rotation,
            0,
            new object[] { timeID }
        );

        Debug.Log($"<color=cyan>[RESPAWN] Jogador renasceu como '{PrefabJogador}' no Time {timeID}!</color>");
    }

    private Transform ObterPontoDeSpawn(int timeID)
    {
        List<Transform> lista = (timeID == 1) ? pontosSpawnTime1 : pontosSpawnTime2;
        if (lista == null || lista.Count == 0) return null;
        return lista[Random.Range(0, lista.Count)];
    }
}