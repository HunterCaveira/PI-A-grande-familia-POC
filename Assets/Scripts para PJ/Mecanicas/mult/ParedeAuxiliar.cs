using UnityEngine;
using Photon.Pun;

public class ParedeRedeAuxiliar : MonoBehaviourPun
{
    [SerializeField] private float tempoDeVida = 10f;

    private void Start()
    {
        // Apenas o criador (dono) gerencia a contagem para destruir a parede na rede
        if (photonView.IsMine)
        {
            Invoke(nameof(DestruirNaRede), tempoDeVida);
        }
    }

    [PunRPC]
    public void SincronizarEscalaParedeRPC(Vector3 novaEscala)
    {
        transform.localScale = novaEscala;
    }

    private void DestruirNaRede()
    {
        if (photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}