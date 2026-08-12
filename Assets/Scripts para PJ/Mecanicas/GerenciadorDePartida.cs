using UnityEngine;
using TMPro; // Biblioteca obrigatória para mexer com TextMeshPro

public class GerenciadorDePartida : MonoBehaviour
{
    [Header("Interface (UI)")]
    [Tooltip("Arraste o objeto de Texto (TextMeshPro) do Canvas aqui")]
    public TextMeshProUGUI textoCronometro;

    [Header("Configurações da Partida")]
    public float tempoDePartida = 300f;
    private float tempoRestante;
    private bool partidaAcabou = false;

    [Header("Pontuação Acumulada")]
    public float tempoTotalTime1 = 0f;
    public float tempoTotalTime2 = 0f;

    [Header("Conexão com a Área")]
    public Dominacao areaDeDominacao;

    private void Start()
    {
        tempoRestante = tempoDePartida;
    }

    private void Update()
    {
        if (partidaAcabou) return;

        // 1. Cronômetro rodando para trás
        tempoRestante -= Time.deltaTime;
        AtualizarRelogioNaTela(); // <--- Chama a função de atualizar a UI

        // 2. Pontuação do território
        if (areaDeDominacao != null)
        {
            if (areaDeDominacao.timeDonoDoTerritorio == 1)
            {
                tempoTotalTime1 += Time.deltaTime;
            }
            else if (areaDeDominacao.timeDonoDoTerritorio == 2)
            {
                tempoTotalTime2 += Time.deltaTime;
            }
        }

        // 3. Fim de partida
        if (tempoRestante <= 0)
        {
            FinalizarPartida();
        }
    }

    // Função que transforma segundos em formato de relógio (Minutos:Segundos)
    private void AtualizarRelogioNaTela()
    {
        if (textoCronometro != null)
        {
            // Pega o tempo e divide matematicamente
            int minutos = Mathf.FloorToInt(tempoRestante / 60);
            int segundos = Mathf.FloorToInt(tempoRestante % 60);

            // Escreve no formato 00:00
            textoCronometro.text = string.Format("{0:00}:{1:00}", minutos, segundos);
        }
    }

    private void FinalizarPartida()
    {
        partidaAcabou = true;
        tempoRestante = 0f;
        AtualizarRelogioNaTela(); // Crava o relógio no 00:00 visualmente

        Debug.Log($"Fim! Time 1: {tempoTotalTime1:F1}s | Time 2: {tempoTotalTime2:F1}s");
    }
}