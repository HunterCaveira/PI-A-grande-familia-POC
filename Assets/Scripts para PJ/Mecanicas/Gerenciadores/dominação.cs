using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Dominacao : MonoBehaviourPun
{
    [Header("Configurações do Território")]
    [Tooltip("ID do time que atualmente domina a área (0 = Nenhum, 1 = Time 1, 2 = Time 2)")]
    public int timeDonoDoTerritorio = 0;
    public float tempoNecessario = 5f;

    [Tooltip("Marque para ZERAR a dominação quando o jogador sair. Se desmarcar, ela apenas pausa.")]
    public bool zerarProgressoAoSair = true;

    [Tooltip("Marque se quiser que a interface (UI) suma completamente quando não houver ninguém na área.")]
    public bool ocultarUIAoFicarVazio = false;

    [Header("Interface (UI)")]
    public GameObject uiTime1;
    public GameObject uiTime2;
    public GameObject uiConflito;

    [Header("Progresso Salvo")]
    public float progressoTime1 = 0f;
    public float progressoTime2 = 0f;
    public int timeTentandoDominar = 0;
    public bool emDisputa = false;

    // Lista principal de jogadores únicos presentes na área no frame atual
    private List<Sistemadevida> jogadoresNaArea = new List<Sistemadevida>();

    private void FixedUpdate()
    {
        // Limpa a lista antes dos eventos de física do frame.
        // O OnTriggerStay repovoará a lista imediatamente com quem permanecer na área.
        jogadoresNaArea.Clear();
    }

    private void OnTriggerStay(Collider quemEstaDentro)
    {
        if (!quemEstaDentro.CompareTag("Player")) return;

        Sistemadevida scriptDoJogador = quemEstaDentro.GetComponentInParent<Sistemadevida>();

        if (scriptDoJogador != null && scriptDoJogador.vidaAtual > 0)
        {
            // Evita duplicatas caso o jogador possua múltiplos colliders no corpo
            if (!jogadoresNaArea.Contains(scriptDoJogador))
            {
                jogadoresNaArea.Add(scriptDoJogador);
            }
        }
    }
    private void OnTriggerExit(Collider quemSaiu)
    {
        if (!quemSaiu.CompareTag("Player")) return;
        Sistemadevida scriptDoJogador = quemSaiu.GetComponentInParent<Sistemadevida>();
        if (scriptDoJogador != null)
        {
            jogadoresNaArea.Remove(scriptDoJogador);
            timeDonoDoTerritorio = 0; // Reseta o dono do território quando um jogador sai
        }
    }

    private void Update()
    {
        // Apenas o Master Client calcula a lógica matemática da zona no multiplayer
        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.IsConnected)
        {
            if (jogadoresNaArea.Count > 0)
            {
                ProcessarCaptura();
            }
            else
            {
                timeTentandoDominar = 0;
                emDisputa = false;

                if (zerarProgressoAoSair)
                {
                    progressoTime1 = 0f;
                    progressoTime2 = 0f;
                }
            }
        }

        // Todos os clientes atualizam a UI localmente com base nos dados
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

        // Se o time presente já é o dono, reseta qualquer progresso de captura pendente
        if (timeEncontrado == timeDonoDoTerritorio)
        {
            progressoTime1 = 0f;
            progressoTime2 = 0f;
            return;
        }

        // Processa o tempo de captura
        if (timeEncontrado == 1)
        {
            progressoTime2 = 0f;
            progressoTime1 += Time.deltaTime;
            if (progressoTime1 >= tempoNecessario) DominarTerritorio(1);
        }
        else if (timeEncontrado == 2)
        {
            progressoTime1 = 0f;
            progressoTime2 += Time.deltaTime;
            if (progressoTime2 >= tempoNecessario) DominarTerritorio(2);
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
        bool mostrarTime1 = false;
        bool mostrarTime2 = false;
        bool mostrarConflito = false;

        if (jogadoresNaArea.Count == 0 && ocultarUIAoFicarVazio)
        {
            mostrarTime1 = false;
            mostrarTime2 = false;
            mostrarConflito = false;
        }
        else
        {
            if (emDisputa)
            {
                mostrarConflito = true;
            }
            else if (timeTentandoDominar != 0)
            {
                mostrarTime1 = (timeTentandoDominar == 1);
                mostrarTime2 = (timeTentandoDominar == 2);
            }
            else
            {
                mostrarTime1 = (timeDonoDoTerritorio == 1);
                mostrarTime2 = (timeDonoDoTerritorio == 2);
            }
        }

        if (uiTime1 != null && uiTime1.activeSelf != mostrarTime1) uiTime1.SetActive(mostrarTime1);
        if (uiTime2 != null && uiTime2.activeSelf != mostrarTime2) uiTime2.SetActive(mostrarTime2);
        if (uiConflito != null && uiConflito.activeSelf != mostrarConflito) uiConflito.SetActive(mostrarConflito);
    }
}