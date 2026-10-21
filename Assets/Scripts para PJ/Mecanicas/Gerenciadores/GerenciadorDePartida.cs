using UnityEngine;
using TMPro;
using Photon.Pun;

public class GerenciadorDePartida : MonoBehaviourPun, IPunObservable
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

        // Apenas o Master Client calcula a pontuação oficial
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
                photonView.RPC("FinalizarPartidaRPC", RpcTarget.All, tempoTotalTime1, tempoTotalTime2);
            }
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

    [PunRPC]
    private void FinalizarPartidaRPC(float pontosFinalTime1, float pontosFinalTime2)
    {
        partidaAcabou = true;
        tempoRestante = 0f;
        tempoTotalTime1 = pontosFinalTime1;
        tempoTotalTime2 = pontosFinalTime2;

        AtualizarRelogiosNaTela();

        // Determina o time do jogador local através do CustomProperties
        int meuTime = 1;
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("TimeJogador"))
        {
            meuTime = (int)PhotonNetwork.LocalPlayer.CustomProperties["TimeJogador"];
        }

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
}