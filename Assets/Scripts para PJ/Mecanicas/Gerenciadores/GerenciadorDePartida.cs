using UnityEngine;
using TMPro;
using Photon.Pun; // Importa o Photon para multiplayer

// 1. Mudamos para MonoBehaviourPun e adicionamos IPunObservable para sincronizar dados em tempo real
public class GerenciadorDePartida : MonoBehaviourPun, IPunObservable
{
    [Header("Interface (UI)")]
    [Tooltip("Arraste o objeto de Texto (TextMeshPro) do Canvas aqui")]
    public TextMeshProUGUI textoCronometro;
    public TextMeshProUGUI dominaçãoRoxo;
    public TextMeshProUGUI dominaçãoAmarelo;

    [Header("Telas de Fim de Jogo")]
    [Tooltip("Arraste as imagens/telas do Canvas que vão aparecer no fim da partida")]
    public GameObject imagemVitoriaTime1;
    public GameObject imagemVitoriaTime2;
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
        // Garante que todas as imagens comecem desativadas (invisíveis)
        if (imagemVitoriaTime1 != null) imagemVitoriaTime1.SetActive(false);
        if (imagemVitoriaTime2 != null) imagemVitoriaTime2.SetActive(false);
        if (imagemEmpate != null) imagemEmpate.SetActive(false);

        // Apenas o líder (dono da sala) inicializa o tempo para todos
        if (PhotonNetwork.IsMasterClient)
        {
            tempoRestante = tempoDePartida;
            tempoTotalTime1 = 0f;
            tempoTotalTime2 = 0f;
        }
    }

    private void Update()
    {
        if (partidaAcabou) return;

        // 2. APENAS O LÍDER DA SALA (Master Client) FAZ A MATEMÁTICA E CONTAGEM
        if (PhotonNetwork.IsMasterClient)
        {
            // Cronômetro rodando para trás
            tempoRestante -= Time.deltaTime;

            // Pontuação do território
            if (areaDeDominacao != null)
            {
                if (!areaDeDominacao.emDisputa)
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
            }

            // Verifica as condições de fim de partida e avisa TODOS OS JOGADORES pela rede
            if (tempoRestante <= 0 || tempoTotalTime1 >= 100f || tempoTotalTime2 >= 100f)
            {
                photonView.RPC("RPC_FinalizarPartida", RpcTarget.All);
            }
        }

        // 3. TODOS os computadores atualizam as suas telas com os números recebidos do Líder
        AtualizarRelogiosNaTela();
    }

    // ==========================================
    // MÉTODO OBRIGATÓRIO PARA O IPunObservable
    // Sincroniza as variáveis de tempo 10x por segundo pela rede
    // ==========================================
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // O Líder envia os dados dele para os outros
            stream.SendNext(tempoRestante);
            stream.SendNext(tempoTotalTime1);
            stream.SendNext(tempoTotalTime2);
        }
        else
        {
            // Os outros jogadores (clientes) recebem e copiam os dados
            tempoRestante = (float)stream.ReceiveNext();
            tempoTotalTime1 = (float)stream.ReceiveNext();
            tempoTotalTime2 = (float)stream.ReceiveNext();
        }
    }

    // Gerencia a atualização de todos os textos da interface
    private void AtualizarRelogiosNaTela()
    {
        // Proteção para o tempo não ficar negativo visualmente
        float tempoVisual = Mathf.Max(0, tempoRestante);

        if (textoCronometro != null)
        {
            textoCronometro.text = FormatarTempo(tempoVisual);
        }

        if (dominaçãoRoxo != null)
        {
            // Trava visualmente em 100% caso ultrapasse um pouco devido ao lag
            float pctTime1 = Mathf.Clamp(Mathf.Round(tempoTotalTime1), 0f, 100f);
            dominaçãoRoxo.text = string.Format($"{pctTime1} %");
        }

        if (dominaçãoAmarelo != null)
        {
            float pctTime2 = Mathf.Clamp(Mathf.Round(tempoTotalTime2), 0f, 100f);
            dominaçãoAmarelo.text = string.Format($"{pctTime2} %");
        }
    }

    // Função que transforma segundos em formato de relógio (Minutos:Segundos)
    private string FormatarTempo(float tempoEmSegundos)
    {
        int minutos = Mathf.FloorToInt(tempoEmSegundos / 60);
        int segundos = Mathf.FloorToInt(tempoEmSegundos % 60);
        return string.Format("{0:00}:{1:00}", minutos, segundos);
    }

    // ==========================================
    // EVENTO DE FIM DE JOGO (Chamado em todos os PCs ao mesmo tempo)
    // ==========================================
    [PunRPC]
    private void RPC_FinalizarPartida()
    {
        if (partidaAcabou) return; // Evita que seja chamado duas vezes

        partidaAcabou = true;
        tempoRestante = 0f;
        AtualizarRelogiosNaTela();

        Debug.Log($"Fim! Time 1: {tempoTotalTime1:F1}s | Time 2: {tempoTotalTime2:F1}s");

        // Compara os tempos e exibe a imagem correspondente para os jogadores
        if (tempoTotalTime1 > tempoTotalTime2 || tempoTotalTime1 >= 100f)
        {
            if (imagemVitoriaTime1 != null) imagemVitoriaTime1.SetActive(true);
        }
        else if (tempoTotalTime2 > tempoTotalTime1 || tempoTotalTime2 >= 100f)
        {
            if (imagemVitoriaTime2 != null) imagemVitoriaTime2.SetActive(true);
        }
        else
        {
            if (imagemEmpate != null) imagemEmpate.SetActive(true);
        }
    }
}