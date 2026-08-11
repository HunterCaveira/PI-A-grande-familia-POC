using System.Collections.Generic;
using UnityEngine;

public class Dominacao : MonoBehaviour
{
    [Header("Configurações do Território")]
    public int timeDonoDoTerritorio = 0;
    public float tempoNecessario = 5f;

    [Header("Interface (UI)")]
    public GameObject uiTime1;
    public GameObject uiTime2;
    public GameObject uiConflito;

    [Header("Progresso Salvo (Tempos Independentes)")]
    [Tooltip("Tempo acumulado pelo Time 1")]
    public float progressoTime1 = 0f;

    [Tooltip("Tempo acumulado pelo Time 2")]
    public float progressoTime2 = 0f;

    // Mostra quem está ativamente na área somando pontos agora (útil para a UI)
    public int timeTentandoDominar = 0;
    public bool emDisputa = false;

    private List<Sistemadevida> jogadoresNaArea = new List<Sistemadevida>();

    private void Update()
    {
        // Limpa jogadores mortos ou destruídos
        // IMPORTANTE: Troque "vidaAtual <= 0" pela sua variável real de morte
        jogadoresNaArea.RemoveAll(jogador => jogador == null || jogador.vidaAtual <= 0);

        if (jogadoresNaArea.Count > 0)
        {
            ProcessarCaptura();
        }
        else
        {
            // Se não tem ninguém na área, ninguém está tentando dominar
            timeTentandoDominar = 0;
            emDisputa = false;
        }

        AtualizarInterface();
    }

    private void ProcessarCaptura()
    {
        int timeEncontrado = -1;
        emDisputa = false;

        // Verifica quem está na área
        foreach (Sistemadevida jogador in jogadoresNaArea)
        {
            if (timeEncontrado == -1)
            {
                timeEncontrado = jogador.TimeID;
            }
            else if (jogador.TimeID != timeEncontrado)
            {
                emDisputa = true;
                break;
            }
        }

        if (emDisputa) return; // Se tem conflito, pausa a contagem

        // Atualiza quem está dominando no momento (para a UI)
        timeTentandoDominar = timeEncontrado;

        // Se o time já for o dono, não faz nada
        if (timeEncontrado == timeDonoDoTerritorio) return;

        // AQUI ESTÁ A MÁGICA DOS TEMPOS INDEPENDENTES:
        // Verifica qual time está na área e soma APENAS no tempo dele
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

    // Função separada para organizar o que acontece quando alguém ganha
    private void DominarTerritorio(int novoDono)
    {
        timeDonoDoTerritorio = novoDono;

        // Quando um time domina a área, zeramos o progresso de ambos
        // para que a próxima disputa comece do zero.
        progressoTime1 = 0f;
        progressoTime2 = 0f;

        timeTentandoDominar = 0;

        Debug.Log("Vitória! Território dominado pelo time: " + timeDonoDoTerritorio);
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
        Sistemadevida scriptDoJogador = quemEntrou.GetComponent<Sistemadevida>();

        if (scriptDoJogador != null && scriptDoJogador.vidaAtual > 0 && !jogadoresNaArea.Contains(scriptDoJogador))
        {
            jogadoresNaArea.Add(scriptDoJogador);
        }
    }

    private void OnTriggerExit(Collider quemSaiu)
    {
        Sistemadevida scriptDoJogador = quemSaiu.GetComponent<Sistemadevida>();
        if (scriptDoJogador != null && jogadoresNaArea.Contains(scriptDoJogador))
        {
            jogadoresNaArea.Remove(scriptDoJogador);
        }
    }
}   