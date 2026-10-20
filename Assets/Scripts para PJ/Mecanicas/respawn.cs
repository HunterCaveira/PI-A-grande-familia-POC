using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Respawn : MonoBehaviourPunCallbacks
{
    // Singleton para ser acessado de qualquer script (ex: no momento da morte no Sistemadevida)
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
    /// Método público chamado quando o jogador local morre.
    /// </summary>
    /// <param name="bonecoAntigo">O GameObject do herói que foi destruído/eliminado.</param>
    public void SolicitarRespawn(GameObject bonecoAntigo)
    {
        StartCoroutine(RotinaRespawn(bonecoAntigo));
    }

    private IEnumerator RotinaRespawn(GameObject bonecoAntigo)
    {
        // 1. Destrói o boneco antigo na rede se ele pertencer a este cliente local
        if (bonecoAntigo != null)
        {
            PhotonView pv = bonecoAntigo.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                PhotonNetwork.Destroy(bonecoAntigo);
            }
        }

        // 2. Aguarda a contagem regressiva de respawn
        yield return new WaitForSeconds(tempoParaRespawn);

        // 3. Lê as configurações do jogador local salvas na transição (HeroiIndice e TimeJogador)
        int heroiIndice = 0;
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("HeroiIndice"))
        {
            heroiIndice = (int)PhotonNetwork.LocalPlayer.CustomProperties["HeroiIndice"];
        }

        int timeID = 1;
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("TimeJogador"))
        {
            timeID = (int)PhotonNetwork.LocalPlayer.CustomProperties["TimeJogador"];
        }

        string nomePrefabHeroi = (heroiIndice == 0) ? "Pai" : "Mae";

        // 4. Seleciona o ponto de spawn correto baseado no time
        Transform pontoEscolhido = ObterPontoDeSpawn(timeID);
        Vector3 posicaoSpawn = pontoEscolhido != null ? pontoEscolhido.position : Vector3.zero;
        Quaternion rotacaoSpawn = pontoEscolhido != null ? pontoEscolhido.rotation : Quaternion.identity;

        // 5. Instancia um novo personagem limpo na rede através da pasta Resources
        GameObject novoBoneco = PhotonNetwork.Instantiate(nomePrefabHeroi, posicaoSpawn, rotacaoSpawn);

        Debug.Log($"[RESPAWN] Jogador renasceu com sucesso! Prefab: {nomePrefabHeroi} | Time {timeID}");
    }

    private Transform ObterPontoDeSpawn(int timeID)
    {
        List<Transform> listaPontos = (timeID == 1) ? pontosSpawnTime1 : pontosSpawnTime2;

        if (listaPontos == null || listaPontos.Count == 0) return null;

        int indiceAleatorio = Random.Range(0, listaPontos.Count);
        return listaPontos[indiceAleatorio];
    }
}