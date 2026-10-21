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

    private void Awake()
    {
        if (Instancia == null)
            Instancia = this;
        else
            Destroy(gameObject);
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
            {
                pontosSpawnTime2.Add(obj.transform);
            }
            else if (obj.name.Contains("Ponto de Respawn"))
            {
                pontosSpawnTime1.Add(obj.transform);
            }
        }
    }

    public void SolicitarRespawn(int timeID, string nomePrefabHeroi)
    {
        StartCoroutine(RotinaRespawn(timeID, nomePrefabHeroi));
    }

    private IEnumerator RotinaRespawn(int timeID, string nomePrefabHeroi)
    {
        yield return new WaitForSeconds(tempoParaRespawn);

        // MODIFICAÇÃO: Ignora o nome recebido e utiliza obrigatoriamente "Pai" da pasta Resources
        string prefabFinal = "Pai";

        Transform pontoEscolhido = ObterPontoDeSpawn(timeID);
        if (pontoEscolhido == null)
        {
            pontoEscolhido = this.transform;
        }

        GameObject novoBoneco = PhotonNetwork.Instantiate(prefabFinal, pontoEscolhido.position, pontoEscolhido.rotation);

        if (novoBoneco != null && novoBoneco.TryGetComponent<Sistemadevida>(out Sistemadevida vida))
        {
            vida.TimeID = timeID;
        }

        Debug.Log($"<color=cyan>[RESPAWN] Jogador renasceu com sucesso como '{prefabFinal}' no Time {timeID}!</color>");
    }

    private Transform ObterPontoDeSpawn(int timeID)
    {
        List<Transform> listaPontos = (timeID == 1) ? pontosSpawnTime1 : pontosSpawnTime2;

        if (listaPontos == null || listaPontos.Count == 0) return null;

        int indiceAleatorio = Random.Range(0, listaPontos.Count);
        return listaPontos[indiceAleatorio];
    }
}