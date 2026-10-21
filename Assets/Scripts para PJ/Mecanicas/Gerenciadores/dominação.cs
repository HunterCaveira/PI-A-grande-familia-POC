using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Dominacao : MonoBehaviourPun, IPunObservable
{
    [Header("Configurações do Território")]
    public int timeDonoDoTerritorio = 0;
    public float tempoNecessario = 5f;

    [Header("Interface (UI)")]
    public GameObject uiTime1;
    public GameObject uiTime2;
    public GameObject uiConflito;

    [Header("Progresso Salvo (Sincronizado na Rede)")]
    public float progressoTime1 = 0f;
    public float progressoTime2 = 0f;
    public int timeTentandoDominar = 0;
    public bool emDisputa = false;

    private List<Sistemadevida> jogadoresNaArea = new List<Sistemadevida>();

    private void Update()
    {
        // APENAS O MASTER CLIENT processa a lógica de domínio
        if (PhotonNetwork.IsMasterClient)
        {
            jogadoresNaArea.RemoveAll(jogador => jogador == null || jogador.vidaAtual <= 0);

            if (jogadoresNaArea.Count > 0)
            {
                ProcessarCaptura();
            }
            else
            {
                timeTentandoDominar = 0;
                emDisputa = false;
            }
        }

        // Atualização gráfica da interface
        AtualizarInterface();
    }

    private void ProcessarCaptura()
    {
        int timeEncontrado = -1;
        bool conflitoDetectado = false;

        foreach (Sistemadevida jogador in jogadoresNaArea)
        {
            if (timeEncontrado == -1)
            {
                timeEncontrado = jogador.TimeID;
            }
            else if (jogador.TimeID != timeEncontrado)
            {
                conflitoDetectado = true;
                break;
            }
        }

        emDisputa = conflitoDetectado;

        if (emDisputa)
        {
            timeTentandoDominar = 0;
            return;
        }

        timeTentandoDominar = timeEncontrado;

        if (timeEncontrado == timeDonoDoTerritorio) return;

        if (timeEncontrado == 1)
        {
            progressoTime1 += Time.deltaTime;

            if (progressoTime1 >= tempoNecessario)
            {
                DominarTerritorio(1);
            }
        }
        else if (timeEncontrado == 2)
        {
            progressoTime2 += Time.deltaTime;

            if (progressoTime2 >= tempoNecessario)
            {
                DominarTerritorio(2);
            }
        }
    }

    private void DominarTerritorio(int novoDono)
    {
        timeDonoDoTerritorio = novoDono;
        progressoTime1 = 0f;
        progressoTime2 = 0f;
        timeTentandoDominar = 0;

        Debug.Log("[DOMINAÇÃO] Território dominado pelo time: " + timeDonoDoTerritorio);
    }

    private void AtualizarInterface()
    {
        if (uiTime1 != null) uiTime1.SetActive(false);
        if (uiTime2 != null) uiTime2.SetActive(false);
        if (uiConflito != null) uiConflito.SetActive(false);

        if (emDisputa)
        {
            if (uiConflito != null) uiConflito.SetActive(true);
            return;
        }

        if (jogadoresNaArea.Count > 0 && timeTentandoDominar != 0)
        {
            if (timeTentandoDominar == 1 && uiTime1 != null) uiTime1.SetActive(true);
            if (timeTentandoDominar == 2 && uiTime2 != null) uiTime2.SetActive(true);
            return;
        }

        if (timeDonoDoTerritorio == 1 && uiTime1 != null) uiTime1.SetActive(true);
        if (timeDonoDoTerritorio == 2 && uiTime2 != null) uiTime2.SetActive(true);
    }

    private void OnTriggerEnter(Collider quemEntrou)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Sistemadevida scriptDoJogador = quemEntrou.GetComponent<Sistemadevida>();

        if (scriptDoJogador != null && scriptDoJogador.vidaAtual > 0 && !jogadoresNaArea.Contains(scriptDoJogador))
        {
            jogadoresNaArea.Add(scriptDoJogador);
        }
    }

    private void OnTriggerExit(Collider quemSaiu)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Sistemadevida scriptDoJogador = quemSaiu.GetComponent<Sistemadevida>();
        if (scriptDoJogador != null && jogadoresNaArea.Contains(scriptDoJogador))
        {
            jogadoresNaArea.Remove(scriptDoJogador);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(timeDonoDoTerritorio);
            stream.SendNext(progressoTime1);
            stream.SendNext(progressoTime2);
            stream.SendNext(timeTentandoDominar);
            stream.SendNext(emDisputa);
        }
        else
        {
            timeDonoDoTerritorio = (int)stream.ReceiveNext();
            progressoTime1 = (float)stream.ReceiveNext();
            progressoTime2 = (float)stream.ReceiveNext();
            timeTentandoDominar = (int)stream.ReceiveNext();
            emDisputa = (bool)stream.ReceiveNext();
        }
    }
}