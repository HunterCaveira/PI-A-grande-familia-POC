// Importa as bibliotecas base e de UI da Unity
using UnityEngine;
using UnityEngine.UI;
// Importa a biblioteca do Photon para reconhecer de quem é o boneco
using Photon.Pun;

public class BarraDeVidaUI : MonoBehaviour
{
    [Header("Componentes de UI")]
    // O Slider visual da Unity que representa a barra enchendo/esvaziando
    [SerializeField] private Slider _sliderVida;

    // Referência ao sistema de vida do jogador local
    private Sistemadevida sistemaDeVida;

    // Variáveis de controle para não fritar a CPU procurando o boneco a cada frame
    private float tempoEntreBuscas = 0.5f;
    private float proximaBusca = 0f;

    private void Update()
    {
        // Se o boneco for destruído (morrer) ou ainda não tiver dado respawn
        if (sistemaDeVida == null)
        {
            // MUDANÇA CRÍTICA: Só executa a busca pesada a cada 0.5 segundos, em vez de 60x por segundo
            if (Time.time >= proximaBusca)
            {
                BuscarSistemaDeVidaLocal();
                proximaBusca = Time.time + tempoEntreBuscas;
            }
        }
    }

    private void BuscarSistemaDeVidaLocal()
    {
        // Vasculha o mapa inteiro atrás de todos os Sistemas de Vida que estão ativos
        Sistemadevida[] sistemasNaCena = Object.FindObjectsByType<Sistemadevida>(FindObjectsSortMode.None);

        foreach (Sistemadevida sistema in sistemasNaCena)
        {
            PhotonView pv = sistema.GetComponent<PhotonView>();

            // TRAVA DE REDE: Confirma se este sistema de vida pertence ao SEU boneco
            // Ignora a vida dos inimigos e dos aliados, focando apenas na sua própria tela
            if (pv != null && pv.IsMine)
            {
                sistemaDeVida = sistema;

                // Conecta o ouvido da UI à boca do Sistema de Vida.
                // Sempre que o boneco gritar "Tomei dano!", a UI escuta e atualiza a barra automaticamente.
                sistemaDeVida.OnVidaMudou += AtualizarBarra;

                // NOTA: Para a barra não começar vazia, o seu Sistemadevida.cs deve 
                // disparar o evento OnVidaMudou?.Invoke(vidaAtual, vidaMaxima) dentro do método Start() dele!

                Debug.Log("<color=green>[UI] Barra de vida conectada ao jogador local!</color>");

                // Achou o seu boneco, interrompe a busca para poupar processamento
                break;
            }
        }
    }

    // Chamado automaticamente se o Canvas ou a tela for desativada
    private void OnDisable()
    {
        // Segurança contra Memory Leaks (Vazamento de Memória)
        // Desconecta a UI do sistema de vida antes de ser destruída
        if (sistemaDeVida != null)
        {
            sistemaDeVida.OnVidaMudou -= AtualizarBarra;
            sistemaDeVida = null;
        }
    }

    // Função que é ativada instantaneamente pelo evento OnVidaMudou
    private void AtualizarBarra(float vidaAtual, float vidaMaxima)
    {
        if (_sliderVida != null && vidaMaxima > 0)
        {
            // Ajusta o limite máximo do Slider (ex: 200 de vida)
            _sliderVida.maxValue = vidaMaxima;
            // Preenche o Slider até ao valor atual (ex: 150)
            _sliderVida.value = vidaAtual;
        }
    }
}