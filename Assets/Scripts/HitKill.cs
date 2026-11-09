using UnityEngine;

public class HitKill : MonoBehaviour
{
    private float dano;

    private void OnCollisonEnter(Collider other)
    {
        Sistemadevida vidaalvo = other.GetComponentInParent<Sistemadevida>();
        vidaalvo.photonView.RPC
    }
}
