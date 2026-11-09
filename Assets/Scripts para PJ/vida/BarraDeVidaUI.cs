using UnityEngine;
using UnityEngine.UI;

public class BarraDeVidaUI : MonoBehaviour
{
    [Header("Componentes de UI")]
    [SerializeField] private Slider _sliderVida; // A imagem da barra (Fill)

    [Header("Referência")]
    [SerializeField] private Sistemadevida sistemaDeVida;

    private void OnEnable()
    {
        // Se inscreve no evento de mudança de vida
        if (sistemaDeVida != null)
        {
            sistemaDeVida.OnVidaMudou += AtualizarBarra;
        }
    }

    private void OnDisable()
    {
        // Cancela a inscrição quando o objeto for desativado para evitar vazamento de memória
        if (sistemaDeVida != null)
        {
            sistemaDeVida.OnVidaMudou -= AtualizarBarra;
        }
    }

    // Este método é executado automaticamente quando o evento 'OnVidaMudou' é disparado
    private void AtualizarBarra(float vidaAtual, float vidaMaxima)
    {
        if (_sliderVida != null && vidaMaxima > 0)
        {
            // O fillAmount da Unity aceita valores entre 0.0 e 1.0 (ex: 50 / 100 = 0.5)
           _sliderVida.maxValue = vidaMaxima; // ISSO AQUI É NO START OU NO ON ENABLE
            _sliderVida.value = vidaAtual;
        }
    }
}