using UnityEngine;

public class Sitemadevida : MonoBehaviour
{

    // Definindo a vida do Personagem
    [SerializeField] private AtributosPersonagem dadosDoHeroi;
    [SerializeField] private float vidaAtual;
  // Start is called once before the first execution of Update after the MonoBehaviour is created


    private void Awake()
    {
        vidaAtual = dadosDoHeroi.vidaMaxima;

    }
    public void ReceberDano(InformacaoDano dano)
    {
        if (dano.atacante != null)
        { 
         if (dano.atacante.CompareTag("Inimigo"))
            {

            }
        
        }
        
        

    }

    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
