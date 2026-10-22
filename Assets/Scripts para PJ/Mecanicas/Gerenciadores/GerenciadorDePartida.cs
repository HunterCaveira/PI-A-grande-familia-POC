using UnityEngine;
using TMPro;

public class GerenciadorDePartida : MonoBehaviour
{
    [Header("Interface (UI)")]
    public TextMeshProUGUI textoCronometro;
    public TextMeshProUGUI dominaçãoRoxo;
    public TextMeshProUGUI dominaçãoAmarelo;

    [Header("Telas de Fim de Jogo")]
    public GameObject imagemVitoria;
    public GameObject imagemDerrota;
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

        if (imagemVitoria != null) imagemVitoria.SetActive(false);
        if (imagemDerrota != null) imagemDerrota.SetActive(false);
        if (imagemEmpate != null) imagemEmpate.SetActive(false);
    }

    private void Update()
    {
        if (partidaAcabou) return;

        tempoRestante -= Time.deltaTime;

        // Calcula a pontuação acumulada se a área não estiver em disputa
        if (areaDeDominacao != null && !areaDeDominacao.emDisputa)
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

        // Checa condição de término da partida
        if (tempoRestante <= 0 || tempoTotalTime1 >= 100f || tempoTotalTime2 >= 100f)
        {
            FinalizarPartida(tempoTotalTime1, tempoTotalTime2);
        }

        AtualizarRelogiosNaTela();
    }

    private void AtualizarRelogiosNaTela()
    {
        if (textoCronometro != null)
        {
            textoCronometro.text = FormatarTempo(tempoRestante);
        }

        if (dominaçãoRoxo != null)
        {
            dominaçãoRoxo.text = $"{Mathf.Round(tempoTotalTime1)} %";
        }

        if (dominaçãoAmarelo != null)
        {
            dominaçãoAmarelo.text = $"{Mathf.Round(tempoTotalTime2)} %";
        }
    }

    private string FormatarTempo(float tempoEmSegundos)
    {
        int minutos = Mathf.FloorToInt(Mathf.Max(tempoEmSegundos, 0f) / 60);
        int segundos = Mathf.FloorToInt(Mathf.Max(tempoEmSegundos, 0f) % 60);
        return string.Format("{0:00}:{1:00}", minutos, segundos);
    }

    private void FinalizarPartida(float pontosFinalTime1, float pontosFinalTime2)
    {
        partidaAcabou = true;
        tempoRestante = 0f;
        tempoTotalTime1 = pontosFinalTime1;
        tempoTotalTime2 = pontosFinalTime2;

        AtualizarRelogiosNaTela();

        // Lê o time do jogador salvo via PlayerPrefs (padrão 1)
        int meuTime = PlayerPrefs.GetInt("PersonagemEscolhido", 1);

        int timeVencedor = 0;
        if (tempoTotalTime1 > tempoTotalTime2 || tempoTotalTime1 >= 100f) timeVencedor = 1;
        else if (tempoTotalTime2 > tempoTotalTime1 || tempoTotalTime2 >= 100f) timeVencedor = 2;

        if (timeVencedor == 0)
        {
            if (imagemEmpate != null) imagemEmpate.SetActive(true);
        }
        else if (timeVencedor == meuTime)
        {
            if (imagemVitoria != null) imagemVitoria.SetActive(true);
        }
        else
        {
            if (imagemDerrota != null) imagemDerrota.SetActive(true);
        }
    }
}