using UnityEngine;

[CreateAssetMenu(fileName = "NovoAtributo", menuName = "HeroShooter/Atributos do Heroi")]
public class AtributosPersonagem : ScriptableObject
{
    [Header("Atributos de Vida")]
    public float vidaMaxima = 100f;

    [Header("Atributos de Movimento")]
    public float velocidadeMovimento = 10f;
    public float forcaPulo = 2f;
    

}
