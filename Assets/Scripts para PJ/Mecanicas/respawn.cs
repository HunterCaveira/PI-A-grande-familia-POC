using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawn : MonoBehaviour
{
    public static Respawn Instancia { get; private set; }

    [Header("Pontos de Spawn no Mapa")]
    [SerializeField] private List<Transform> pontosSpawnTime1;
    [SerializeField] private List<Transform> pontosSpawnTime2;

    [Header("Regras de Partida")]
    [SerializeField] private float tempoParaRespawn = 5f;

    private void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void SolicitarRespawn(GameObject bonecoAntigo)
    {
        StartCoroutine(RotinaRespawn(bonecoAntigo));
    }

    private IEnumerator RotinaRespawn(GameObject bonecoAntigo)
    {
        // 1. Desativa o herói na cena durante o tempo de respawn
        if (bonecoAntigo != null)
        {
            bonecoAntigo.SetActive(false);
        }

        // 2. Aguarda a contagem regressiva
        yield return new WaitForSeconds(tempoParaRespawn);

        // 3. Lê a escolha do time/personagem salva no PlayerPrefs
        int timeID = PlayerPrefs.GetInt("PersonagemEscolhido", 1);

        // 4. Seleciona um ponto de spawn aleatório
        Transform pontoEscolhido = ObterPontoDeSpawn(timeID);

        // 5. Reposiciona o clone e o reativa
        if (bonecoAntigo != null)
        {
            if (pontoEscolhido != null)
            {
                bonecoAntigo.transform.position = pontoEscolhido.position;
                bonecoAntigo.transform.rotation = pontoEscolhido.rotation;
            }

            bonecoAntigo.SetActive(true);

            if (bonecoAntigo.TryGetComponent<Sistemadevida>(out Sistemadevida vida))
            {
                vida.Renascer();
            }
        }

        Debug.Log($"[RESPAWN] Jogador reposicionado com sucesso! Time {timeID}");
    }

    private Transform ObterPontoDeSpawn(int timeID)
    {
        List<Transform> listaPontos = (timeID == 1) ? pontosSpawnTime1 : pontosSpawnTime2;

        if (listaPontos == null || listaPontos.Count == 0) return null;

        int indiceAleatorio = Random.Range(0, listaPontos.Count);
        return listaPontos[indiceAleatorio];
    }
}