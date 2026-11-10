using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.SceneManagement;

public class GerenciadorDePartida : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Interface (UI)")]
    public TextMeshProUGUI textoCronometro;
    public TextMeshProUGUI dominaçãoRoxo;
    public TextMeshProUGUI dominaçãoAmarelo;

    [Header("Telas de Fim de Jogo")]
    public GameObject imagemVitoriaTime1;
    public GameObject imagemVitoriaTime2;
    public GameObject imagemEmpate;

    [Header("Configurações da Partida")]
    public float tempoDePartida = 300f;
    [Tooltip("Tempo em segundos que as telas de vitória ficam visíveis antes de voltar ao menu")]
    public float tempoEsperaFimDePartida = 5f;
    [Tooltip("Nome exato da cena do Menu/Lobby no Build Settings")]
    public string nomeCenaMenu = "MenuInicial";

    private float tempoRestante;
    private bool partidaAcabou = false;

    [Header("Pontuação Acumulada")]
    public float tempoTotalTime1 = 0f;
    public float tempoTotalTime2 = 0f;

    [Header("Conexão com a Área")]
    public Dominacao areaDeDominacao;

    private void Start()
    {
        if (imagemVitoriaTime1 != null) imagemVitoriaTime1.SetActive(false);
        if (imagemVitoriaTime2 != null) imagemVitoriaTime2.SetActive(false);
        if (imagemEmpate != null) imagemEmpate.SetActive(false);

        tempoRestante = tempoDePartida;

        if (areaDeDominacao == null)
        {
            areaDeDominacao = Object.FindFirstObjectByType<Dominacao>();
            if (areaDeDominacao == null)
            {
                Debug.LogWarning("[GerenciadorDePartida] Nenhum script 'Dominacao' foi encontrado na cena!");
            }
        }

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

        if (PhotonNetwork.IsMasterClient)
        {
            tempoRestante -= Time.deltaTime;

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

            if (tempoRestante <= 0 || tempoTotalTime1 >= 100f || tempoTotalTime2 >= 100f)
            {
                photonView.RPC(nameof(RPC_FinalizarPartida), RpcTarget.AllViaServer);
            }
        }

        AtualizarRelogiosNaTela();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(tempoRestante);
            stream.SendNext(tempoTotalTime1);
            stream.SendNext(tempoTotalTime2);
        }
        else
        {
            tempoRestante = (float)stream.ReceiveNext();
            tempoTotalTime1 = (float)stream.ReceiveNext();
            tempoTotalTime2 = (float)stream.ReceiveNext();
        }
    }

    private void AtualizarRelogiosNaTela()
    {
        float tempoVisual = Mathf.Max(0, tempoRestante);

        if (textoCronometro != null)
        {
            textoCronometro.text = FormatarTempo(tempoVisual);
        }

        if (dominaçãoRoxo != null)
        {
            float pctTime1 = Mathf.Clamp(Mathf.Round(tempoTotalTime1), 0f, 100f);
            dominaçãoRoxo.text = $"{pctTime1} %";
        }

        if (dominaçãoAmarelo != null)
        {
            float pctTime2 = Mathf.Clamp(Mathf.Round(tempoTotalTime2), 0f, 100f);
            dominaçãoAmarelo.text = $"{pctTime2} %";
        }
    }

    private string FormatarTempo(float tempoEmSegundos)
    {
        int minutos = Mathf.FloorToInt(tempoEmSegundos / 60);
        int segundos = Mathf.FloorToInt(tempoEmSegundos % 60);
        return string.Format("{0:00}:{1:00}", minutos, segundos);
    }

    // ==========================================
    // FIM DE JOGO E DESCONEXÃO
    // ==========================================
    [PunRPC]
    private void RPC_FinalizarPartida()
    {
        if (partidaAcabou) return;

        partidaAcabou = true;
        tempoRestante = 0f;
        AtualizarRelogiosNaTela();

        // 1. Exibe o painel do vencedor correspondente
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

        // 2. Inicia a contagem regressiva para fechar a sala e mandar todos ao menu
        StartCoroutine(RotinaSairDaPartida());
    }

    private IEnumerator RotinaSairDaPartida()
    {
        // Aguarda os segundos definidos para que os jogadores leiam a mensagem de vitória/derrota
        yield return new WaitForSeconds(tempoEsperaFimDePartida);

        // Se estiver conectado à sala, sai da sala de rede do Photon
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene("HUD Server");
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
    }

    // Callback executado automaticamente assim que a desconexão da sala for concluída pelo Photon
    public override void OnLeftRoom()
    {
        // Carrega a cena do Menu para o jogador
        SceneManager.LoadScene("HUD Server");
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
    }
}