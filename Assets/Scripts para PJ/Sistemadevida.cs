using System;
using UnityEngine;

public class Sistemadevida : MonoBehaviour
{
    [Header("Configurações do Herói")]
    [SerializeField] private AtributosPersonagem dadosDoHeroi;

    [Header("PvP e Partida")]
    [SerializeField] private int timeID = 1;

    [SerializeField] private float vidaAtual;

    public int TimeID => timeID;

    public event Action<float, float> OnVidaMudou;

    private void Awake()
    {
        if (dadosDoHeroi != null)
        {
            vidaAtual = dadosDoHeroi.vidaMaxima;
        }
        else
        {
            Debug.LogError($"[SistemaDeVida] A ficha de dados não foi atribuída em {gameObject.name}!");
        }
    }

    public void ReceberDano(InformacaoDano dano)
    {
        // 1. Se já estiver morto, ignora qualquer novo dano
        if (vidaAtual <= 0) return;

        // 2. Validação de Fogo Amigo (Somente se existir um atacante)
        if (dano.atacante != null)
        {
            if (dano.atacante.TryGetComponent<Sistemadevida>(out Sistemadevida atacanteVida))
            {
                // Se for do mesmo time e NÃO for você mesmo
                if (atacanteVida.TimeID == this.timeID && dano.atacante != this.gameObject)
                {
                    Debug.Log("Ataque ignorado: Fogo amigo desativado!");
                    return; // Cancela a aplicação do dano
                }
            }
        }

        // 3. Aplica a redução da vida PRIMEIRO
        vidaAtual -= dano.quantidade;
        vidaAtual = Mathf.Max(vidaAtual, 0f);

        // 4. Efeitos visuais/Lógica secundária por tipo de dano
        switch (dano.tipo)
        {
            case TipoDano.Fogo:
                // Ex: Iniciar efeito de queimadura
                break;
            case TipoDano.Veneno:
                break;
            case TipoDano.Fisico:
                break;
        }

        // 5. Exibe no log o estado ATUALIZADO da vida
        Debug.Log($"{gameObject.name} (Time {timeID}) recebeu {dano.quantidade} de dano! Vida restante: {vidaAtual}");

        // 6. Testa se morreu DEPOIS de ter reduzido a vida
        if (vidaAtual <= 0)
        {
            Morrer();
        }
    }
    public void Renascer()
    {

        if (dadosDoHeroi != null)
        {
            vidaAtual = dadosDoHeroi.vidaMaxima;
            NotificarMudancaDevida();


        }

    }

    private void NotificarMudancaDevida()
    { 
        if (dadosDoHeroi != null)
        {

            OnVidaMudou?.Invoke(vidaAtual, dadosDoHeroi.vidaMaxima);

        }
    

    }


    private void Morrer()
    {
        Debug.Log($"{gameObject.name} foi eliminado!");
    }
}