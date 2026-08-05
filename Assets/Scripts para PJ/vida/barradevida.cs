using UnityEngine;
using UnityEngine.UI;

public class barradevida : MonoBehaviour
{

    [Header("Componetes de UI")]
    [SerializeField] private Image imagemPreenchimentoBarra;

    [Header("Referência")]
    [SerializeField] private Sistemadevida sistemaDeVida;

    private void OnEnable()
    {
        if (sistemaDeVida != null)
        {

            sistemaDeVida.OnVidaMudou += AtualizarBarra;

        }
    }

    private void OnDisable()
    {
        if (sistemaDeVida != null)
        {

            sistemaDeVida.OnVidaMudou -= AtualizarBarra;
        }
        
    }

    private void AtualizarBarra(float vidaAtual, float vidaMaxima)
    {

        if (imagemPreenchimentoBarra != null && vidaMaxima > 0)
        {

            imagemPreenchimentoBarra.fillAmount = vidaAtual / vidaMaxima;


        }
    }

}
