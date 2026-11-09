using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GerenciadorDeSpawn : MonoBehaviourPunCallbacks
{
    public static GerenciadorDeSpawn Instancia { get; private set; }

    [Header("Pontos de Nascimento (Automáticos)")]
    [SerializeField] private List<Transform> pontosSpawnTime1 = new List<Transform>();
    [SerializeField] private List<Transform> pontosSpawnTime2 = new List<Transform>();

    private const string PrefabJogador = "Pai";
    private bool jaInstanciou = false;

    private void Awake()
    {
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);
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

        GameObject[] todosObjetos = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (GameObject obj in todosObjetos)
        {
            if (obj.name.Contains("Ponto de Respawn2"))
                pontosSpawnTime2.Add(obj.transform);
            else if (obj.name.Contains("Ponto de Respawn"))
                pontosSpawnTime1.Add(obj.transform);
        }
    }

    public void InstanciarMeuPersonagem()
    {
        if (jaInstanciou) return;
        jaInstanciou = true;

        int meuTime = PhotonNetwork.IsMasterClient ? 1 : 2;

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("TimeJogador", out object timeObj))
        {
            meuTime = Convert.ToInt32(timeObj);
        }

        Transform pontoDeSpawn = ObterPontoDeSpawn(meuTime);

        PhotonNetwork.Instantiate(
            PrefabJogador,
            pontoDeSpawn.position,
            pontoDeSpawn.rotation,
            0,
            new object[] { meuTime }
        );
    }

    // 🌐 OPÇÃO 1: Aguarda o tempo e reativa a mesma instância do jogador no novo ponto de spawn
    public void SolicitarRespawnReaproveitado(Sistemadevida sistemaVida, int timeID, float tempoEspera = 3f)
    {
        StartCoroutine(RotinaRespawnReaproveitado(sistemaVida, timeID, tempoEspera));
    }

    private IEnumerator RotinaRespawnReaproveitado(Sistemadevida sistemaVida, int timeID, float tempo)
    {
        yield return new WaitForSeconds(tempo);

        if (sistemaVida != null)
        {
            Transform pontoDeSpawn = ObterPontoDeSpawn(timeID);
            sistemaVida.Renascer(pontoDeSpawn.position, pontoDeSpawn.rotation);
        }
    }

    private Transform ObterPontoDeSpawn(int timeID)
    {
        List<Transform> lista = (timeID == 1) ? pontosSpawnTime1 : pontosSpawnTime2;
        if (lista.Count > 0)
            return lista[UnityEngine.Random.Range(0, lista.Count)];

        return transform;
    }
}