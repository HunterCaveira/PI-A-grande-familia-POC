using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GerenciadorDeSpawn : MonoBehaviourPunCallbacks
{
    [Header("Pontos de Nascimento (Automáticos)")]
    [SerializeField] private List<Transform> pontosSpawnTime1 = new List<Transform>();
    [SerializeField] private List<Transform> pontosSpawnTime2 = new List<Transform>();

    private const string PrefabJogador = "Pai"; // Prefab dentro de Assets/Resources

    // Campo de instância: cada vez que a cena carrega nasce um novo gerenciador com valor false.
    private bool jaInstanciou = false;

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

        GameObject[] todosObjetos = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (GameObject obj in todosObjetos)
        {
            if (obj.name.Contains("Ponto de Respawn2"))
                pontosSpawnTime2.Add(obj.transform);
            else if (obj.name.Contains("Ponto de Respawn"))
                pontosSpawnTime1.Add(obj.transform);
        }
    }

    private void InstanciarMeuPersonagem()
    {
        // Evita criar dois personagens (Start + OnJoinedRoom)
        if (jaInstanciou) return;
        jaInstanciou = true;

        // Padrão para testes: MasterClient = Time 1, convidado = Time 2
        int meuTime = PhotonNetwork.IsMasterClient ? 1 : 2;

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("TimeJogador", out object timeObj))
        {
            meuTime = Convert.ToInt32(timeObj);
        }

        Transform pontoDeSpawn = this.transform;
        List<Transform> lista = (meuTime == 1) ? pontosSpawnTime1 : pontosSpawnTime2;
        if (lista.Count > 0)
            pontoDeSpawn = lista[UnityEngine.Random.Range(0, lista.Count)];

        // O TimeID viaja em InstantiationData e é lido pelo OnPhotonInstantiate
        // do Sistemadevida em TODOS os clientes.
        PhotonNetwork.Instantiate(
            PrefabJogador,
            pontoDeSpawn.position,
            pontoDeSpawn.rotation,
            0,
            new object[] { meuTime }
        );

        Debug.Log($"<color=green>[SPAWN] Personagem '{PrefabJogador}' instanciado para o Time {meuTime} em {pontoDeSpawn.name}</color>");
    }
}