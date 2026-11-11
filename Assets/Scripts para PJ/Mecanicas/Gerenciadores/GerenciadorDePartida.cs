// Importa bibliotecas básicas da Unity, TextMeshPro, Photon para multiplayer e Collections para Corrotinas
using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Collections;

// A classe herda de MonoBehaviourPun e implementa IPunObservable para sincronizar dados em tempo real pela rede
public class GerenciadorDePartida : MonoBehaviourPun, IPunObservable
{
    [Header("Interface (UI)")]
    [Tooltip("Arraste o objeto de Texto (TextMeshPro) do cronômetro da partida aqui")]
    public TextMeshProUGUI textoCronometro;
    [Tooltip("Texto da UI que exibe a porcentagem do Time Roxo")]
    public TextMeshProUGUI dominaçãoRoxo;
    [Tooltip("Texto da UI que exibe a porcentagem do Time Amarelo")]
    public TextMeshProUGUI dominaçãoAmarelo;

    [Header("Telas de Fim de Jogo")]
    [Tooltip("Tela exibida se o Time 1 vencer")]
    public GameObject imagemVitoriaTime1;
    [Tooltip("Tela exibida se o Time 2 vencer")]
    public GameObject imagemVitoriaTime2;
    [Tooltip("Tela exibida em caso de empate")]
    public GameObject imagemEmpate;

    [Header("Troca de Cena")]
    [Tooltip("Nome exato da cena que será carregada (deve estar adicionada em Build Settings)")]
    public string nomeDaProximaCena = "MenuInicial";
    [Tooltip("Tempo em segundos de espera com a tela de vitória visível antes de mudar de cena")]
    public float tempoDeEsperaTrocaCena = 5f;

    [Header("Configurações da Partida")]
    [Tooltip("Duração total da partida em segundos (ex: 300s = 5 minutos)")]
    public float tempoDePartida = 300f;
    private float tempoRestante;
    private bool partidaAcabou = false;

    [Header("Pontuação Acumulada")]
    public float tempoTotalTime1 = 0f;
    public float tempoTotalTime2 = 0f;

    [Header("Conexão com a Área")]
    [Tooltip("Referência ao script de Dominação do mapa (Pode ser deixado vazio para busca automática)")]
    public Dominacao areaDeDominacao;

    // Start é chamado no primeiro frame da cena
    private void Start()
    {
        // Garante que todas as imagens de vitória/derrota comecem desativadas (invisíveis)
        if (imagemVitoriaTime1 != null) imagemVitoriaTime1.SetActive(false);
        if (imagemVitoriaTime2 != null) imagemVitoriaTime2.SetActive(false);
        if (imagemEmpate != null) imagemEmpate.SetActive(false);

        // MUDANÇA DE MELHORIA: Se esqueceu de arrastar no Inspector, procura o objeto de dominação sozinho na cena
        if (areaDeDominacao == null)
        {
            areaDeDominacao = Object.FindFirstObjectByType<Dominacao>();
            if (areaDeDominacao == null)
            {
                Debug.LogWarning("[GerenciadorDePartida] Nenhum script 'Dominacao' foi encontrado na cena!");
            }
        }

        // Apenas o líder da sala (Master Client) inicializa os valores oficiais do tempo e placar
        if (PhotonNetwork.IsMasterClient)
        {
            tempoRestante = tempoDePartida;
            tempoTotalTime1 = 0f;
            tempoTotalTime2 = 0f;
        }
    }

    // Update roda a cada frame do jogo
    private void Update()
    {
        // Se a partida já acabou, interrompe a execução do Update
        if (partidaAcabou) return;

        // 2. REGRA DE OURO DO MULTIPLAYER: APENAS O LÍDER DA SALA (Master Client) FAZ A MATEMÁTICA E CONTAGEM
        // Isso evita conflitos de relógio e lag desincronizado entre os jogadores.
        if (PhotonNetwork.IsMasterClient)
        {
            // O cronômetro regride baseando-se no tempo real do Unity
            tempoRestante -= Time.deltaTime;

            // Lógica de pontuação do território de dominação
            if (areaDeDominacao != null)
            {
                // Se a zona não estiver a ser disputada por dois inimigos ao mesmo tempo
                if (!areaDeDominacao.emDisputa)
                {
                    // Se o dono for o Time 1, acumula tempo para o Time 1
                    if (areaDeDominacao.timeDonoDoTerritorio == 1)
                    {
                        tempoTotalTime1 += Time.deltaTime;
                    }
                    // Se o dono for o Time 2, acumula tempo para o Time 2
                    else if (areaDeDominacao.timeDonoDoTerritorio == 2)
                    {
                        tempoTotalTime2 += Time.deltaTime;
                    }
                }
            }

            // Condições de término: O tempo acabou OU algum dos times atingiu 100% de dominação
            if (tempoRestante <= 0 || tempoTotalTime1 >= 100f || tempoTotalTime2 >= 100f)
            {
                // Manda uma ordem via RPC para todos os computadores da sala encerrarem a partida juntos
                photonView.RPC("RPC_FinalizarPartida", RpcTarget.All);
            }
        }

        // 3. TODOS os computadores atualizam as suas telas locais com base nos números sincronizados pelo Líder
        AtualizarRelogiosNaTela();
    }

    // ==========================================
    // MÉTODO OBRIGATÓRIO PARA O IPunObservable
    // Sincroniza as variáveis de tempo e pontuação de forma fluida pela rede
    // ==========================================
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // O Líder transmite os seus dados atuais para os clientes conectados
            stream.SendNext(tempoRestante);
            stream.SendNext(tempoTotalTime1);
            stream.SendNext(tempoTotalTime2);
        }
        else
        {
            // Os demais jogadores recebem e aplicam os valores enviados pelo Líder
            tempoRestante = (float)stream.ReceiveNext();
            tempoTotalTime1 = (float)stream.ReceiveNext();
            tempoTotalTime2 = (float)stream.ReceiveNext();
        }
    }

    // Gerencia a atualização visual de todos os textos na interface
    private void AtualizarRelogiosNaTela()
    {
        // Trava matemática para o relógio nunca exibir números negativos na tela
        float tempoVisual = Mathf.Max(0, tempoRestante);

        // Atualiza o texto do cronômetro principal
        if (textoCronometro != null)
        {
            textoCronometro.text = FormatarTempo(tempoVisual);
        }

        // Atualiza a barragem de porcentagem do Time Roxo
        if (dominaçãoRoxo != null)
        {
            // Arredonda o valor para cima e trava entre 0% e 100%
            float pctTime1 = Mathf.Clamp(Mathf.Round(tempoTotalTime1), 0f, 100f);
            dominaçãoRoxo.text = string.Format($"{pctTime1} %");
        }

        // Atualiza a barragem de porcentagem do Time Amarelo
        if (dominaçãoAmarelo != null)
        {
            float pctTime2 = Mathf.Clamp(Mathf.Round(tempoTotalTime2), 0f, 100f);
            dominaçãoAmarelo.text = string.Format($"{pctTime2} %");
        }
    }

    // Função utilitária que converte segundos crus em formato de cronômetro clássico (Minutos:Segundos)
    private string FormatarTempo(float tempoEmSegundos)
    {
        int minutos = Mathf.FloorToInt(tempoEmSegundos / 60);
        int segundos = Mathf.FloorToInt(tempoEmSegundos % 60);
        return string.Format("{0:00}:{1:00}", minutos, segundos);
    }

    // ==========================================
    // EVENTO DE FIM DE JOGO (Executado simultaneamente em todos os PCs via RPC)
    // ==========================================
    [PunRPC]
    private void RPC_FinalizarPartida()
    {
        if (partidaAcabou) return; // Trava para impedir que o fim de jogo dispare duas vezes por acidente

        partidaAcabou = true;
        tempoRestante = 0f;
        AtualizarRelogiosNaTela();

        Debug.Log($"Fim de Partida! Time 1: {tempoTotalTime1:F1}s | Time 2: {tempoTotalTime2:F1}s");

        // Compara os pontos acumulados e exibe o painel de vitória correspondente
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

        // Inicia a contagem regressiva para a troca de cena em todas as instâncias
        StartCoroutine(TrocarDeCenaAposEspera());
    }

    // Corrotina responsável por aguardar o tempo de exibição do resultado e trocar de cena
    private IEnumerator TrocarDeCenaAposEspera()
    {
        yield return new WaitForSeconds(tempoDeEsperaTrocaCena);
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        // Apenas o Master Client chama a troca de cena no Photon para evitar requisições redundantes
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("HUD Server");
            
        }
    }
}