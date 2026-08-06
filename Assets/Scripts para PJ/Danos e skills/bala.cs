using UnityEngine;

public class Bala : MonoBehaviour
{
    private float dano;
    private TipoDano tipoDano;
    private GameObject atirador;
    private string nomeArma;

    // Método de inicialização invocado pela arma ao disparar
    public void Inicializar(float danoBase, TipoDano tipo, GameObject dono, string nomeDaArma)
    {
        dano = danoBase;
        tipoDano = tipo;
        atirador = dono;
        nomeArma = nomeDaArma;

        // Autodestrói a bala após 5 segundos se não atingir nada (evita acúmulo na memória)
        // Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Evita colidir com o próprio atirador no momento do disparo
        if (other.gameObject == atirador) return;

        if (other.gameObject.CompareTag("Bala")) return;


        // Se atingiu alguém que possui o Sistema de Vida
        if (other.TryGetComponent<Sistemadevida>(out Sistemadevida vidaAtingida))
        {
            // Monta a struct de transporte de dano
            InformacaoDano infoDano = new InformacaoDano(
                dano,
                tipoDano,
                atirador,
                nomeArma
            );


            // Aplica o dano
            vidaAtingida.ReceberDano(infoDano);
        }

        // Destrói a bala ao colidir com qualquer superfície (Personagem, Parede, Chão)
       // Destroy(gameObject);
    }
}