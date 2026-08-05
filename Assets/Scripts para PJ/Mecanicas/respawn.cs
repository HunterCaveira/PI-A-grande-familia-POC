using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GerenciadorRespawn : MonoBehaviour
{
    // Singleton simples para ser acessado facilmente de qualquer lugar
    public static GerenciadorRespawn Instancia { get; private set; }

    [Header("Pontos de Spawn no Mapa")]
    [SerializeField] private List<Transform> pontosSpawnTime1;
    [SerializeField] private List<Transform> pontosSpawnTime2;

    [Header("Regras de Partida")]
    [SerializeField] private float tempoParaRespawn = 5f;

    private void Awake()
    {
        if (Instancia == null)
            Instancia = this;
        else
            Destroy(gameObject);
    }

    // Método chamado quando alguém morre
    public void SolicitarRespawn(GameObject jogador, int timeID)
    {
        StartCoroutine(RotinaRespawn(jogador, timeID));
    }

    private IEnumerator RotinaRespawn(GameObject jogador, int timeID)
    {
        // 1. Oculta/Desativa o jogador enquanto espera
        jogador.SetActive(false);

        // 2. Aguarda o tempo de respawn
        yield return new WaitForSeconds(tempoParaRespawn);

        // 3. Escolhe o ponto de spawn correto baseado no time
        Transform pontoEscolhido = ObterPontoDeSpawn(timeID);

        // 4. Reposiciona o jogador no local do time dele
        if (pontoEscolhido != null)
        {
            jogador.transform.position = pontoEscolhido.position;
            jogador.transform.rotation = pontoEscolhido.rotation;
        }

        // 5. Reativa o jogador
        jogador.SetActive(true);

        // 6. Restaura a vida do jogador chamando um método de reset no SistemaDeVida
        if (jogador.TryGetComponent<Sistemadevida>(out Sistemadevida vida))
        {
            vida.Renascer();
        }
    }

    private Transform ObterPontoDeSpawn(int timeID)
    {
        List<Transform> listaPontos = (timeID == 1) ? pontosSpawnTime1 : pontosSpawnTime2;

        if (listaPontos.Count == 0) return null;

        // Escolhe um ponto aleatório dentro do conjunto de pontos do time
        int indiceAleatorio = Random.Range(0, listaPontos.Count);
        return listaPontos[indiceAleatorio];
    }
}