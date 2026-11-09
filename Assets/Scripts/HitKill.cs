using UnityEngine;
using Photon.Pun;

public class HitKill : MonoBehaviour
{
    private float dano = 200000f;
    private int viewIDAtirador;

    private void OnTriggerEnter(Collider other)
    {
        Sistemadevida vidaalvo = other.GetComponentInParent<Sistemadevida>();
        vidaalvo.photonView.RPC(nameof(Sistemadevida.ReceberDanoRPC), RpcTarget.All, dano, viewIDAtirador);
    }
}
