using Unity.VisualScripting;
using UnityEngine;

public class Morrer : MonoBehaviour
{
    [SerializeField] private Sistemadevida AtributosPersonagem;

    public void OnCollisionEnter(Collision collision)
    {
        AtributosPersonagem.vidaAtual = 0;
    }
}
