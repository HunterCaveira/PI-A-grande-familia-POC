using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GerenciadorDeSpawn : MonoBehaviourPunCallbacks
{
    [Header("Pontos de Nascimento (Automáticos)")]
    [SerializeField] private List<Transform> pontosSpawnTime1 = new List<Transform>();
    [SerializeField] private List<Transform> pontosSpawnTime2 = new List<Transform>();

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
        // ==========================================
        // 🛡️ VERIFICAÇÃO RIGOROSA DO PHOTON
        // Pergunta à rede se EU (Jogador Local) já criei o meu herói nesta cena.
        // ==========================================
        foreach (PhotonView view in Object.FindObjectsByType<PhotonView>(FindObjectsSortMode.None))
        {
            if (view.IsMine)
            {
                // Se já existe um PhotonView meu na cena, bloqueia novas criações com segurança
                Debug.Log($"<color=orange>[SPAWN] O meu herói ({view.gameObject.name}) já está ativo na cena. Nenhuma nova criação necessária.</color>");
                return;
            }
        }

        // 1. Descobre de qual Time o jogador local faz parte
        int meuTime = 1;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("TimeJogador", out object timeObj))
        {
            meuTime = (int)timeObj;
        }

        // 2. Lê qual foi o Herói escolhido na Tela de Seleção
        string prefabParaSpawnar = "Pai";
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("NomePrefabBase", out object prefabObj))
        {
            prefabParaSpawnar = (string)prefabObj;
        }

        // Se for do Time 2, adiciona " 1" ao nome do prefab (ex: "Pai 1")
        if (meuTime == 2)
        {
            prefabParaSpawnar += " 1";
        }

        // 3. Define a posição de nascimento correta baseada no time
        Transform pontoDeSpawn = this.transform;
        if (meuTime == 1 && pontosSpawnTime1.Count > 0)
        {
            pontoDeSpawn = pontosSpawnTime1[Random.Range(0, pontosSpawnTime1.Count)];
        }
        else if (meuTime == 2 && pontosSpawnTime2.Count > 0)
        {
            pontoDeSpawn = pontosSpawnTime2[Random.Range(0, pontosSpawnTime2.Count)];
        }

        // 4. Cria o seu boneco na rede de forma limpa e isolada
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