using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // Importa as bibliotecas principais do Photon
using Photon.Realtime;

// 1. A classe agora herda de MonoBehaviourPun e implementa IPunObservable para sincronizar variáveis
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
    [Tooltip("Tempo acumulado pelo Time 1")]
    public float progressoTime1 = 0f;

    [Tooltip("Tempo acumulado pelo Time 2")]
    public float progressoTime2 = 0f;

    public int timeTentandoDominar = 0;
    public bool emDisputa = false;

    // Lista controlada EXCLUSIVAMENTE pelo Master Client
    private List<Sistemadevida> jogadoresNaArea = new List<Sistemadevida>();

    private void Update()
    {
        // 2. APENAS O MASTER CLIENT processa a lógica, o tempo e as colisões.
        // Isso evita que o lag (latência) de diferentes jogadores cause dessincronização.
        if (PhotonNetwork.IsMasterClient)
        {
            // Limpa jogadores mortos ou destruídos
            jogadoresNaArea.RemoveAll(jogador => jogador == null || jogador.vidaAtual <= 0);

            if (jogadoresNaArea.Count > 0)
            {
                ProcessarCaptura();
            }
            else
            {
                // Se a área está vazia, o Master Client reseta a zona
                timeTentandoDominar = 0;
                emDisputa = false;
                timeDonoDoTerritorio = 0;
                progressoTime1 = 0f;
                progressoTime2 = 0f;
            }
        }

        // 3. TODOS OS JOGADORES (inclusive os clientes) rodam a atualização da UI,
        // baseando-se nos valores que o Master Client calculou e sincronizou.
        AtualizarInterface();
    }

    private void ProcessarCaptura()
    {
        int timeEncontrado = -1;
        bool conflitoDetectado = false;

        // Varre a lista inteira para ver se há mais de um time diferente
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

        // Se está em disputa, ninguém ganha progresso
        if (emDisputa)
        {
            timeTentandoDominar = 0;
            return;
        }

        timeTentandoDominar = timeEncontrado;

        // Se o time atual já for o dono do território, ignora
        if (timeEncontrado == timeDonoDoTerritorio) return;

        // Soma o progresso apenas para o time isolado correto
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

    // 4. Os Triggers também só importam para o Master Client, que cuida da lista.
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

    // 5. O MÉTODO DE SINCRONIZAÇÃO: Transmite os dados matemáticos continuamente na rede.
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // O Master Client "Escreve" os dados oficiais na rede.
            stream.SendNext(timeDonoDoTerritorio);
            stream.SendNext(progressoTime1);
            stream.SendNext(progressoTime2);
            stream.SendNext(timeTentandoDominar);
            stream.SendNext(emDisputa);
        }
        else
        {
            // Os outros jogadores "Leem" os dados da rede e atualizam suas variáveis locais.
            timeDonoDoTerritorio = (int)stream.ReceiveNext();
            progressoTime1 = (float)stream.ReceiveNext();
            progressoTime2 = (float)stream.ReceiveNext();
            timeTentandoDominar = (int)stream.ReceiveNext();
            emDisputa = (bool)stream.ReceiveNext();
        }
    }
}