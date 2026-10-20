using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Respawn : MonoBehaviourPunCallbacks
{
    // Singleton simples para ser acessado facilmente de qualquer lugar
    public static Respawn Instancia { get; private set; }

    [Header("Pontos de Spawn no Mapa")]
    [SerializeField] private List<Transform> pontosSpawnTime1;
    [SerializeField] private List<Transform> pontosSpawnTime2;

    [Header("Regras de Partida")]
    [SerializeField] private float tempoParaRespawn = 5f;

    private void Awake()
    {
        if (Instancia == null)
            Instancia = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Método chamado quando alguém morre. Deve ser executado apenas pelo dono do boneco ou via PhotonView.
    /// </summary>
    public void SolicitarRespawn(int timeID, string nomePrefabHeroi)
    {
        StartCoroutine(RotinaRespawn(timeID, nomePrefabHeroi));
    }

    private IEnumerator RotinaRespawn(int timeID, string nomePrefabHeroi)
    {
        // 1. Aguarda o tempo de respawn enquanto o jogador está morto/observando
        yield return new WaitForSeconds(tempoParaRespawn);

        // 2. Escolhe o ponto de spawn correto baseado no time
        Transform pontoEscolhido = ObterPontoDeSpawn(timeID);

        if (pontoEscolhido == null)
        {
            pontoEscolhido = this.transform; // Fallback se a lista estiver vazia
        }

        // 3. Instancia um novo personagem limpo na rede através do Photon
        // O prefab renasce na posição e rotação exata da base do seu time
        GameObject novoBoneco = PhotonNetwork.Instantiate(nomePrefabHeroi, pontoEscolhido.position, pontoEscolhido.rotation);

        // 4. Garante que o novo boneco recebe o TimeID correto na ficha
        if (novoBoneco.TryGetComponent<Sistemadevida>(out Sistemadevida vida))
        {
            vida.TimeID = timeID;
        }

        Debug.Log($"[RESPAWN] Jogador renasceu com sucesso no Time {timeID}!");
    }

    private Transform ObterPontoDeSpawn(int timeID)
    {
        List<Transform> listaPontos = (timeID == 1) ? pontosSpawnTime1 : pontosSpawnTime2;

        if (listaPontos == null || listaPontos.Count == 0) return null;

        // Escolhe um ponto aleatório dentro do conjunto de pontos do time
        int indiceAleatorio = Random.Range(0, listaPontos.Count);
        return listaPontos[indiceAleatorio];
    }
}