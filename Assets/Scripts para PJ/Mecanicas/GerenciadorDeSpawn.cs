using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GerenciadorDeSpawn : MonoBehaviour
{
    [Header("Pastas e Prefabs")]
    [Tooltip("Nomes exatos dos prefabs dentro da pasta Assets/Resources. Ex: 0 = Pai, 1 = Mae")]
    [SerializeField] private string[] nomesPrefabsPersonagens = new string[] { "Pai", "Mae" };

    [Header("Pontos de Nascimento (Transforms)")]
    [SerializeField] private Transform[] pontosSpawnTime1; // Locais de spawn do Time 1
    [SerializeField] private Transform[] pontosSpawnTime2; // Locais de spawn do Time 2

    void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log($"[REDE] Conectado na Sala: {PhotonNetwork.CurrentRoom.Name} | Total de Jogadores: {PhotonNetwork.CurrentRoom.PlayerCount}");
            InstanciarMeuPersonagem();
        }
        else
        {
            Debug.LogError("[REDE ERROR] O jogador não está em nenhuma sala do Photon!");
        }
    }

    private void InstanciarMeuPersonagem()
    {
        // 1. DESCOBRE O TIME DO JOGADOR LOCAL
        int meuTime = 1; // Padrão caso não esteja definido
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("TimeJogador", out object timeObj))
        {
            meuTime = (int)timeObj;
        }

        // 2. DESCOBRE QUAL HERÓI ELE ESCOLHEU
        int indiceHeroi = 0; // Padrão é 0 (Pai)
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("HeroiIndice", out object heroiObj))
        {
            indiceHeroi = (int)heroiObj;
        }

        // Seguranca: Garante que o índice não estoure os limites do array de nomes de prefabs
        if (nomesPrefabsPersonagens == null || nomesPrefabsPersonagens.Length == 0)
        {
            Debug.LogError("[SPAWN ERROR] Array 'nomesPrefabsPersonagens' está vazio no Inspector!");
            return;
        }

        indiceHeroi = Mathf.Clamp(indiceHeroi, 0, nomesPrefabsPersonagens.Length - 1);

        // 3. ESCOLHE O PONTO DE SPAWN COM BASE NO TIME
        Transform pontoDeSpawn = this.transform; // Fallback padrao

        if (meuTime == 1 && pontosSpawnTime1 != null && pontosSpawnTime1.Length > 0)
        {
            int randomPos = Random.Range(0, pontosSpawnTime1.Length);
            pontoDeSpawn = pontosSpawnTime1[randomPos];
        }
        else if (meuTime == 2 && pontosSpawnTime2 != null && pontosSpawnTime2.Length > 0)
        {
            int randomPos = Random.Range(0, pontosSpawnTime2.Length);
            pontoDeSpawn = pontosSpawnTime2[randomPos];
        }

        // 4. INSTANCIA O PERSONAGEM NA REDE
        string prefabParaSpawnar = nomesPrefabsPersonagens[indiceHeroi];

        // PhotonNetwork.Instantiate busca o prefab obrigatoriamente dentro de Assets/Resources/
        GameObject personagemInstanciado = PhotonNetwork.Instantiate(
            prefabParaSpawnar,
            pontoDeSpawn.position,
            pontoDeSpawn.rotation
        );

        Debug.Log($"<color=green>[SPAWN] Instanciou {prefabParaSpawnar} para o Time {meuTime} na posição: {pontoDeSpawn.position}</color>");
    }
}