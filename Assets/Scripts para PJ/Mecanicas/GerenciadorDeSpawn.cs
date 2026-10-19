using UnityEngine; // Importa bibliotecas base da Unity
using Photon.Pun; // Importa Photon PUN
using Photon.Realtime; // Importa configurações de rede

public class GerenciadorDeSpawn : MonoBehaviour
{
    [Header("Pastas e Prefabs")]
    [Tooltip("Nomes exatos dos prefabs na pasta Resources. Ex: 0 = Pai, 1 = Mae")]
    [SerializeField] private string[] nomesPrefabsPersonagens;

    [Header("Pontos de Nascimento (Transforms)")]
    [SerializeField] private Transform[] pontosSpawnTime1; // Locais onde o Time 1 nasce
    [SerializeField] private Transform[] pontosSpawnTime2; // Locais onde o Time 2 nasce

    private void Start()
    {
        // Assim que a cena carrega, se estivermos conectados, manda criar o boneco
        if (PhotonNetwork.IsConnectedAndReady)
        {
            InstanciarMeuPersonagem();
        }
    }

    private void InstanciarMeuPersonagem()
    {
        // 1. DESCOBRE O TIME DO JOGADOR LOCAL
        int meuTime = 1; // Padrão é Time 1
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

        // 3. ESCOLHE O PONTO DE SPAWN COM BASE NO TIME
        Transform pontoDeSpawn = this.transform; // Ponto de fallback (onde o GameManager está)

        if (meuTime == 1 && pontosSpawnTime1.Length > 0)
        {
            // Pega um ponto aleatório da lista do Time 1
            int randomPos = Random.Range(0, pontosSpawnTime1.Length);
            pontoDeSpawn = pontosSpawnTime1[randomPos];
        }
        else if (meuTime == 2 && pontosSpawnTime2.Length > 0)
        {
            // Pega um ponto aleatório da lista do Time 2
            int randomPos = Random.Range(0, pontosSpawnTime2.Length);
            pontoDeSpawn = pontosSpawnTime2[randomPos];
        }

        // 4. INSTANCIA O PERSONAGEM NA REDE
        // Atenção: O prefab precisa OBRIGATORIAMENTE estar dentro de uma pasta chamada "Resources"
        string prefabParaSpawnar = nomesPrefabsPersonagens[indiceHeroi];

        PhotonNetwork.Instantiate(prefabParaSpawnar, pontoDeSpawn.position, pontoDeSpawn.rotation);

        Debug.Log($"[SPAWN] Instanciou {prefabParaSpawnar} na posição do Time {meuTime}");
    }
}