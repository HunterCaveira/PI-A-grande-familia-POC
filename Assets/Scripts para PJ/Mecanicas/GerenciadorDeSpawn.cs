using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GerenciadorDeSpawn : MonoBehaviourPunCallbacks
{
    [Header("Pontos de Nascimento (Automáticos)")]
    [SerializeField] private List<Transform> pontosSpawnTime1 = new List<Transform>();
    [SerializeField] private List<Transform> pontosSpawnTime2 = new List<Transform>();

    private static bool jaInstanciouNestaSessao = false;

    private void Awake()
    {
        jaInstanciouNestaSessao = false;
    }

    private void Start()
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        BuscarPontosDeSpawnAutomaticos();

        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            InstanciarMeuPersonagem();
        }
    }

    public override void OnJoinedRoom()
    {
        InstanciarMeuPersonagem();
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

    private void InstanciarMeuPersonagem()
    {
        int meuTime = 1; // Padrão

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("TimeJogador", out object timeObj))
        {
            meuTime = (int)timeObj;
        }
        else
        {
            // FALLBACK PARA TESTES: Se a propriedade não existir (teste direto no Main Park),
            // o dono da sala (MasterClient) fica no Time 1 e os convidados (Builds) vão para o Time 2.
            if (!PhotonNetwork.IsMasterClient)
            {
                meuTime = 2;
            }
        }

        // MODIFICAÇÃO: Força sempre o uso do prefab base "Pai" que está na pasta Resources para ambos os times
        string prefabParaSpawnar = "Pai";

        Transform pontoDeSpawn = this.transform;
        if (meuTime == 1 && pontosSpawnTime1.Count > 0)
        {
            pontoDeSpawn = pontosSpawnTime1[Random.Range(0, pontosSpawnTime1.Count)];
        }
        else if (meuTime == 2 && pontosSpawnTime2.Count > 0)
        {
            pontoDeSpawn = pontosSpawnTime2[Random.Range(0, pontosSpawnTime2.Count)];
        }

        GameObject meuPersonagem = PhotonNetwork.Instantiate(
            prefabParaSpawnar,
            pontoDeSpawn.position,
            pontoDeSpawn.rotation
        );

        if (meuPersonagem != null && meuPersonagem.TryGetComponent<Sistemadevida>(out Sistemadevida sistemaVida))
        {
            sistemaVida.TimeID = meuTime;
        }

        Debug.Log($"<color=green>[SPAWN] SUCESSO! Meu personagem '{prefabParaSpawnar}' foi instanciado para o Time {meuTime}!</color>");
    }
}