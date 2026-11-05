// Importa apenas as bibliotecas essenciais da Unity
using System.Collections.Generic;
using UnityEngine;

public class BalaPool : MonoBehaviour
{
    [Header("Configurações do Pool")]
    [Tooltip("O prefab da bala. NÃO deve ter PhotonView nem PhotonTransformView.")]
    [SerializeField] private GameObject balaPrefab;

    [Tooltip("Quantidade de balas criadas no início para evitar travamentos (ex: tamanho do carregador)")]
    [SerializeField] private int quantidadeInicial = 30;

    [Header("Lista de Balas")]
    // Lista que guarda todas as balas criadas para reaproveitamento
    [SerializeField] private List<GameObject> listaDeBalas;

    private void Awake()
    {
        InicializarPool();
    }

    private void InicializarPool()
    {
        listaDeBalas = new List<GameObject>();

        for (int i = 0; i < quantidadeInicial; i++)
        {
            CriarNovaBala();
        }
    }

    private GameObject CriarNovaBala()
    {
        // 🌐 ARQUITETURA MULTIPLAYER (RPC): Usa Instantiate local do Unity.
        // Como o RPC do tiro roda em cada computador, cada cliente ativa sua própria bala local.
        GameObject novaBala = Instantiate(balaPrefab, transform);

        // Desativa a bala na cena até que ela seja disparada
        novaBala.SetActive(false);

        // Adiciona a bala pronta na lista
        listaDeBalas.Add(novaBala);

        return novaBala;
    }

    public GameObject GetitemFromPool()
    {
        // Procura a primeira bala que estiver invisível (desativada) na cena
        for (int i = 0; i < listaDeBalas.Count; i++)
        {
            if (!listaDeBalas[i].activeInHierarchy)
            {
                return listaDeBalas[i];
            }
        }

        // Cria uma bala extra caso o pool esvazie
        return CriarNovaBala();
    }
}