using System.Runtime.CompilerServices;
using UnityEngine;

public class Sitemadevida : MonoBehaviour
{

    // Definindo a vida do Personagem
    [SerializeField] public float vidaMaxima;
    [SerializeField] private float vidaAtual;
    [SerializeField] public int danoSofrido;
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    private void Awake()
    {
        vidaAtual = vidaMaxima;

    }
    public void ReceberDano(float quantidadeDano)
    { 
       
        
        

    }

    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
