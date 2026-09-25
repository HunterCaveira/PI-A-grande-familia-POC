using System.Collections;
using UnityEngine;

public class GerenciadorRespawn : MonoBehaviour
{
    [SerializeField] protected tiropistola  Tiropistola; 
    [Header("Configurações de Respawn")]
    [SerializeField] private Transform[] pontosDeSpawn;
    [SerializeField] private float tempoParaRenascer = 3f;

    private void Start()
    {
        if(pontosDeSpawn == null || pontosDeSpawn.Length == 0)
        {
            Debug.LogError("[GerenciadorRespawn] Nenhum ponto de spawn atribuído no Inspector!");
            return;
        }


       Tiropistola.gameObject.SendMessage("Reload");
    }

    public void ProcessarMorte(GameObject jogador)
    {
        StartCoroutine(RotinaRespawn(jogador));
    }

    private IEnumerator RotinaRespawn(GameObject jogador)
    {
        // 1. Oculta e desativa o jogador durante a contagem
        jogador.SetActive(false);

        // 2. Aguarda o tempo configurado
        yield return new WaitForSeconds(tempoParaRenascer);

        // 3. Seleciona um ponto de spawn aleatório da lista
        if (pontosDeSpawn.Length > 0)
        {
            Transform spawnEscolhido = pontosDeSpawn[Random.Range(0, pontosDeSpawn.Length)];
            jogador.transform.position = spawnEscolhido.position;
            jogador.transform.rotation = spawnEscolhido.rotation;
        }

        // 4. Reativa o GameObject e restaura a vida
        jogador.SetActive(true);

        if (jogador.TryGetComponent<Sistemadevida>(out Sistemadevida vida))
        {
            vida.Renascer();
           

        }
    }
}