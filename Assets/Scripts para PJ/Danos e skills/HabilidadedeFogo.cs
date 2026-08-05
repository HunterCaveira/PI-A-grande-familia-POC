using UnityEngine;

public class HabilidadeDeFogo : MonoBehaviour
{
    [Header("Configurações da Habilidade")]
    [SerializeField] private float danoBase = 35f;
    [SerializeField] private string biribinha = "Bola de Fogo";

    private GameObject lancador;

    public void Inicializar(GameObject donoDaHabilidade)
    {
        lancador = donoDaHabilidade;
    
    }
    private void OnTriggerEnter(Collider other)
    { 
    
        if (other.TryGetComponent<Sistemadevida>(out Sistemadevida vidaAtingida))
        {

            GameObject atacanteReal = (lancador != null) ? lancador : this.gameObject;

            InformacaoDano danoDeFogo = new InformacaoDano(
                danoBase,
                TipoDano.Fogo,
                atacanteReal,
                biribinha              
                );

            vidaAtingida.ReceberDano(danoDeFogo);

        }
        Destroy( this.gameObject );
        
    
    }

    
}
