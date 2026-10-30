// Importa apenas as bibliotecas essenciais da Unity
using UnityEngine;
using System.Collections.Generic;

public class BalaPool : MonoBehaviour
{
    [Header("Configurações do Pool")]
    [Tooltip("O prefab da bala. NÃO precisa ter PhotonView, pois o RPC da pistola já sincroniza o tiro.")]
    [SerializeField] private GameObject balaPrefab;

    [Tooltip("Quantidade de balas criadas no início para evitar travamentos (ex: tamanho do carregador)")]
    [SerializeField] private int quantidadeInicial = 30;

    [Header("Lista de Balas")]
    // Lista que guarda todas as balas criadas para reaproveitamento
    [SerializeField] private List<GameObject> listaDeBalas;

    // Awake é chamado assim que o objeto nasce na cena
    private void Awake()
    {
        InicializarPool();
    }

    // Prepara a piscina de balas antes de o jogador dar o primeiro tiro
    private void InicializarPool()
    {
        listaDeBalas = new List<GameObject>();

        for (int i = 0; i < quantidadeInicial; i++)
        {
            CriarNovaBala();
        }
    }

    // Método isolado para criar uma bala (usado no início e caso a piscina esvazie)
    private GameObject CriarNovaBala()
    {
        // 🌐 ARQUITETURA MULTIPLAYER: Usa Instantiate normal (local).
        // Como o RPC do tiroPistola roda em todos os clientes, cada computador puxa uma bala local ao mesmo tempo.
        // O "this.transform" já define o parentesco diretamente na criação, economizando processamento.
        GameObject novaBala = Instantiate(balaPrefab, this.transform);

        // Desativa a bala na cena até que ela seja disparada
        novaBala.SetActive(false);

        // Adiciona a bala pronta na lista
        listaDeBalas.Add(novaBala);

        return novaBala;
    }

    // Método chamado pelo tiroPistola para pegar uma bala pronta
    public GameObject GetitemFromPool()
    {
        // Procura a primeira bala que estiver invisível (desativada) na cena
        for (int i = 0; i < listaDeBalas.Count; i++)
        {
            if (listaDeBalas[i].activeInHierarchy == false)
            {
                return listaDeBalas[i];
            }
        }

        // Se o jogador atirar muito rápido e todas as 30 balas estiverem no ar ao mesmo tempo,
        // o sistema cria uma bala extra de emergência e devolve para o cano da arma.
        return CriarNovaBala();
    }
}