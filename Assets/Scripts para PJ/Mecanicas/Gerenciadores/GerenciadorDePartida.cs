using UnityEngine;
using TMPro;
<<<<<<< HEAD

public class GerenciadorDePartida : MonoBehaviour
{
    [Header("Interface (UI)")]
=======
using Photon.Pun; // Importa o Photon para multiplayer

// 1. Mudamos para MonoBehaviourPun e adicionamos IPunObservable para sincronizar dados em tempo real
public class GerenciadorDePartida : MonoBehaviourPun, IPunObservable
{
    [Header("Interface (UI)")]
    [Tooltip("Arraste o objeto de Texto (TextMeshPro) do Canvas aqui")]
>>>>>>> parent of a587532 (Finalizado o multiplayer)
    public TextMeshProUGUI textoCronometro;
    public TextMeshProUGUI dominaçãoRoxo;
    public TextMeshProUGUI dominaçãoAmarelo;

    [Header("Telas de Fim de Jogo")]
<<<<<<< HEAD
    public GameObject imagemVitoria;
    public GameObject imagemDerrota;
=======
    [Tooltip("Arraste as imagens/telas do Canvas que vão aparecer no fim da partida")]
    public GameObject imagemVitoriaTime1;
    public GameObject imagemVitoriaTime2;
>>>>>>> parent of a587532 (Finalizado o multiplayer)
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
<<<<<<< HEAD
        tempoRestante = tempoDePartida;

        if (imagemVitoria != null) imagemVitoria.SetActive(false);
        if (imagemDerrota != null) imagemDerrota.SetActive(false);
        if (imagemEmpate != null) imagemEmpate.SetActive(false);
=======
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
>>>>>>> parent of a587532 (Finalizado o multiplayer)
    }

    private void Update()
    {
        if (partidaAcabou) return;

<<<<<<< HEAD
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
=======
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

>>>>>>> parent of a587532 (Finalizado o multiplayer)
        if (textoCronometro != null)
        {
            textoCronometro.text = FormatarTempo(tempoRestante);
        }

        if (dominaçãoRoxo != null)
        {
<<<<<<< HEAD
            dominaçãoRoxo.text = $"{Mathf.Round(tempoTotalTime1)} %";
=======
            // Trava visualmente em 100% caso ultrapasse um pouco devido ao lag
            float pctTime1 = Mathf.Clamp(Mathf.Round(tempoTotalTime1), 0f, 100f);
            dominaçãoRoxo.text = string.Format($"{pctTime1} %");
>>>>>>> parent of a587532 (Finalizado o multiplayer)
        }

        if (dominaçãoAmarelo != null)
        {
            dominaçãoAmarelo.text = $"{Mathf.Round(tempoTotalTime2)} %";
        }
    }

<<<<<<< HEAD
=======
    // Função que transforma segundos em formato de relógio (Minutos:Segundos)
>>>>>>> parent of a587532 (Finalizado o multiplayer)
    private string FormatarTempo(float tempoEmSegundos)
    {
        int minutos = Mathf.FloorToInt(Mathf.Max(tempoEmSegundos, 0f) / 60);
        int segundos = Mathf.FloorToInt(Mathf.Max(tempoEmSegundos, 0f) % 60);
        return string.Format("{0:00}:{1:00}", minutos, segundos);
    }

<<<<<<< HEAD
    private void FinalizarPartida(float pontosFinalTime1, float pontosFinalTime2)
    {
=======
    // ==========================================
    // EVENTO DE FIM DE JOGO (Chamado em todos os PCs ao mesmo tempo)
    // ==========================================
    [PunRPC]
    private void RPC_FinalizarPartida()
    {
        if (partidaAcabou) return; // Evita que seja chamado duas vezes

>>>>>>> parent of a587532 (Finalizado o multiplayer)
        partidaAcabou = true;
        tempoRestante = 0f;
        tempoTotalTime1 = pontosFinalTime1;
        tempoTotalTime2 = pontosFinalTime2;

        AtualizarRelogiosNaTela();

<<<<<<< HEAD
        // Lê o time do jogador salvo via PlayerPrefs (padrão 1)
        int meuTime = PlayerPrefs.GetInt("PersonagemEscolhido", 1);

        int timeVencedor = 0;
        if (tempoTotalTime1 > tempoTotalTime2 || tempoTotalTime1 >= 100f) timeVencedor = 1;
        else if (tempoTotalTime2 > tempoTotalTime1 || tempoTotalTime2 >= 100f) timeVencedor = 2;

        if (timeVencedor == 0)
=======
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
>>>>>>> parent of a587532 (Finalizado o multiplayer)
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