using UnityEngine;
using TMPro; // Biblioteca obrigatória para mexer com TextMeshPro

public class GerenciadorDePartida : MonoBehaviour
{
    [Header("Interface (UI)")]
    [Tooltip("Arraste o objeto de Texto (TextMeshPro) do Canvas aqui")]
    public TextMeshProUGUI textoCronometro;
    public TextMeshProUGUI dominaçãoRoxo;
    public TextMeshProUGUI dominaçãoAmarelo;

    [Header("Telas de Fim de Jogo")]
    [Tooltip("Arraste as imagens/telas do Canvas que vão aparecer no fim da partida")]
    public GameObject imagemVitoriaTime1;
    public GameObject imagemVitoriaTime2; // Pode ser a imagem de "Derrota" caso o jogador seja o Time 1
    public GameObject imagemEmpate;

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

        // Garante que todas as imagens comecem desativadas (invisíveis)
        if (imagemVitoriaTime1 != null) imagemVitoriaTime1.SetActive(false);
        if (imagemVitoriaTime2 != null) imagemVitoriaTime2.SetActive(false);
        if (imagemEmpate != null) imagemEmpate.SetActive(false);
    }

    private void Update()
    {
        if (partidaAcabou) return;

        // 1. Cronômetro rodando para trás
        tempoRestante -= Time.deltaTime;

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

        // 3. Atualiza os textos da tela com os tempos novos
        AtualizarRelogiosNaTela();

        // 4. Fim de partida
        if (tempoRestante <= 0)
        {
            FinalizarPartida();
        }
    }

    // Gerencia a atualização de todos os textos de tempo da interface
    private void AtualizarRelogiosNaTela()
    {
        if (textoCronometro != null)
        {
            textoCronometro.text = FormatarTempo(tempoRestante);
        }

        if (dominaçãoRoxo != null)
        {
            dominaçãoRoxo.text = string.Format($"{Mathf.Round(tempoTotalTime1)} %");
        }

        if (dominaçãoAmarelo != null)
        {
            dominaçãoAmarelo.text = string.Format($"{Mathf.Round(tempoTotalTime2)} %");
        }
    }

    // Função que transforma segundos em formato de relógio (Minutos:Segundos)
    private string FormatarTempo(float tempoEmSegundos)
    {
        int minutos = Mathf.FloorToInt(tempoEmSegundos / 60);
        int segundos = Mathf.FloorToInt(tempoEmSegundos % 60);
        return string.Format("{0:00}:{1:00}", minutos, segundos);

        //return string.Format($"{Mathf.Round(tempoEmSegundos)} %");
    }
   

    private void FinalizarPartida()
    {
        partidaAcabou = true;
        tempoRestante = 0f;
        AtualizarRelogiosNaTela(); // Crava o relógio no 00:00 visualmente e atualiza pontuação final

        Debug.Log($"Fim! Time 1: {tempoTotalTime1:F1}s | Time 2: {tempoTotalTime2:F1}s");

        // Compara os tempos e exibe a imagem correspondente
        if (tempoTotalTime1 > tempoTotalTime2)
        {
            Debug.Log("Time 1 Venceu!");
            if (imagemVitoriaTime1 != null) imagemVitoriaTime1.SetActive(true);
        }
        else if (tempoTotalTime2 > tempoTotalTime1)
        {
            Debug.Log("Time 2 Venceu!");
            if (imagemVitoriaTime2 != null) imagemVitoriaTime2.SetActive(true);
        }
        else
        {
            Debug.Log("Empate!");
            if (imagemEmpate != null) imagemEmpate.SetActive(true);
        }
    }
}