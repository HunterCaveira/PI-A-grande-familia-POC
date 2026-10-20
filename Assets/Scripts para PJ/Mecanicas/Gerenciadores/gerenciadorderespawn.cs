using System.Collections;
using UnityEngine;
using Photon.Pun;

public class GerenciadorRespawn : MonoBehaviourPun
{
    [Header("Configurações de Respawn")]
    [SerializeField] private Transform[] pontosDeSpawnTime1;
    [SerializeField] private Transform[] pontosDeSpawnTime2;
    [SerializeField] private float tempoParaRenascer = 3f;

    public void ProcessarMorte(GameObject jogador)
    {
        PhotonView pv = jogador.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            StartCoroutine(RotinaRespawn(jogador, pv));
        }
    }

    private IEnumerator RotinaRespawn(GameObject jogador, PhotonView pv)
    {
        // Oculta o herói em todos os clientes
        pv.RPC("SincronizarVisibilidadePlayerRPC", RpcTarget.All, false);

        yield return new WaitForSeconds(tempoParaRenascer);

        int timeId = 1;
        if (jogador.TryGetComponent<Sistemadevida>(out Sistemadevida vidaComp))
        {
            timeId = vidaComp.TimeID;
        }

        Transform[] listaPontos = (timeId == 1) ? pontosDeSpawnTime1 : pontosDeSpawnTime2;

        if (listaPontos != null && listaPontos.Length > 0)
        {
            Transform spawnEscolhido = listaPontos[Random.Range(0, listaPontos.Length)];

            // Desativa o CharacterController no momento da relocalização
            CharacterController controller = jogador.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            jogador.transform.position = spawnEscolhido.position;
            jogador.transform.rotation = spawnEscolhido.rotation;

            if (controller != null) controller.enabled = true;
        }

        // Torna o herói visível e restaura os atributos em todos os clientes
        pv.RPC("SincronizarVisibilidadePlayerRPC", RpcTarget.All, true);

        if (vidaComp != null)
        {
            vidaComp.Renascer();
        }

        // Recarrega a arma localmente
        tiroPistola arma = jogador.GetComponentInChildren<tiroPistola>();
        if (arma != null)
        {
            arma.SendMessage("Reload", SendMessageOptions.DontRequireReceiver);
        }
    }

    [PunRPC]
    public void SincronizarVisibilidadePlayerRPC(bool visivel)
    {
        // Alterna a visibilidade visual do GameObject
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var rend in renderers) rend.enabled = visivel;

        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var skRend in skinnedRenderers) skRend.enabled = visivel;
    }
}