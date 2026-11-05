using UnityEngine;
using Photon.Pun;

public class Bala : MonoBehaviour
{
    private float dano;
    private TipoDano tipoDano;
    private GameObject donoDaArma;
    private string nomeArma;
    private int viewIDAtirador;
    private bool souOAtirador;

    public void Inicializar(float dano, TipoDano tipo, GameObject dono, string nome)
    {
        this.dano = dano;
        this.tipoDano = tipo;
        this.donoDaArma = dono;
        this.nomeArma = nome;

        if (dono != null)
        {
            PhotonView pvDono = dono.GetComponentInParent<PhotonView>();
            if (pvDono != null)
            {
                this.viewIDAtirador = pvDono.ViewID;
                this.souOAtirador = pvDono.IsMine;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Ignores de segurança por hierarquia
        if (donoDaArma != null && other.transform.root == donoDaArma.transform.root)
            return;

        // 2. Busca o sistema de vida no objeto ou nos pais
        Sistemadevida vidaAlvo = other.GetComponentInParent<Sistemadevida>();

        if (vidaAlvo != null && vidaAlvo.photonView != null)
        {
            // 🛑 TRAVA DE AUTO-DANO: Se o ViewID do alvo for O MESMO de quem atirou, ignora!
            if (vidaAlvo.photonView.ViewID == viewIDAtirador)
                return;

            // Apenas a autoridade do tiro (quem disparou) envia o RPC de dano
            if (souOAtirador)
            {
                vidaAlvo.photonView.RPC(nameof(Sistemadevida.ReceberDanoRPC), RpcTarget.All, dano, viewIDAtirador);
            }

            gameObject.SetActive(false);
            return;
        }

        // Se colidir com paredes/cenário (objetos sem vida que não sejam triggers)
        if (!other.isTrigger)
        {
            gameObject.SetActive(false);
        }
    }
}