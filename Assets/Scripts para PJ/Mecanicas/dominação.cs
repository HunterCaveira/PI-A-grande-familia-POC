using UnityEngine;

public class dominação : MonoBehaviour
{
    public int timeDonoDoTerritorio = 0; // 0 significa que é Neutro
    public float progressoDeDominacao = 0f;
    public float tempoNecessario = 5f; // Precisa ficar 5 segundos para dominar

    // Essa função detecta quem está pisando na área
    private void OnTriggerStay(Collider quemEntrou)
    {
        // AQUI ESTÁ A MÁGICA: 
        // O território "pega" o componente StatusDoJogador de quem pisou na área
         Sistemadevida scriptDoJogador = quemEntrou.GetComponent<Sistemadevida>();

        // Verifica se quem entrou realmente é um jogador (se ele tem o script)
        if (scriptDoJogador != null)
        {
            // Puxando apenas a variável IDtime do outro script!
            int idDoInvasor = scriptDoJogador.TimeID;

            // Se o time do invasor for diferente do time dono do território, ele começa a dominar
            if (idDoInvasor != timeDonoDoTerritorio)
            {
                // Aumenta o progresso baseado no tempo que passa
                progressoDeDominacao += Time.deltaTime;

                // Se o progresso chegar ao tempo necessário, o território é conquistado
                if (progressoDeDominacao >= tempoNecessario)
                {
                    timeDonoDoTerritorio = idDoInvasor; // O território muda de dono
                    progressoDeDominacao = 0f; // Zera o progresso para a próxima disputa

                    Debug.Log("Território dominado pelo time: " + timeDonoDoTerritorio);
                }
            }
        }
    }

    // Se o jogador sair da área antes de dominar, o progresso zera
    private void OnTriggerExit(Collider quemSaiu)
    {
        Sistemadevida scriptDoJogador = quemSaiu.GetComponent<Sistemadevida>();

        if (scriptDoJogador != null)
        {
            progressoDeDominacao = 0f;
        }
    }
}

















        