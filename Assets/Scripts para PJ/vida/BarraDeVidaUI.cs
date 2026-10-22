using UnityEngine;
using UnityEngine.UI;
using Photon.Pun; // Necessário para acessar a rede

public class BarraDeVidaUI : MonoBehaviour
{
    [Header("Componentes de UI")]
    [SerializeField] private Slider _sliderVida;

    private Sistemadevida sistemaDeVida;
    private bool jogadorConectado = false;

    private void Update()
    {
        // Se o boneco for destruído (morrer) ou ainda não tiver nascido, tenta buscar novamente
        if (sistemaDeVida == null)
        {
            jogadorConectado = false;
            BuscarSistemaDeVidaLocal();
        }
    }

    private void BuscarSistemaDeVidaLocal()
    {
        // Encontra todos os sistemas de vida ativos na cena (Unity 6)
        Sistemadevida[] sistemasNaCena = Object.FindObjectsByType<Sistemadevida>(FindObjectsSortMode.None);

        foreach (Sistemadevida sistema in sistemasNaCena)
        {
            PhotonView pv = sistema.GetComponent<PhotonView>();

            // Verifica se o boneco encontrado pertence a este computador
            if (pv != null && pv.IsMine)
            {
                sistemaDeVida = sistema;
                sistemaDeVida.OnVidaMudou += AtualizarBarra;
                jogadorConectado = true;

                Debug.Log("<color=green>[UI] Barra de vida conectada ao jogador local!</color>");
                break;
            }
        }
    }

    private void OnDisable()
    {
        // Cancela a inscrição quando a UI for desativada para evitar vazamento de memória
        if (sistemaDeVida != null)
        {
            sistemaDeVida.OnVidaMudou -= AtualizarBarra;
            jogadorConectado = false;
        }
    }

    private void AtualizarBarra(float vidaAtual, float vidaMaxima)
    {
        if (_sliderVida != null && vidaMaxima > 0)
        {
            _sliderVida.maxValue = vidaMaxima;
            _sliderVida.value = vidaAtual;
        }
    }
}