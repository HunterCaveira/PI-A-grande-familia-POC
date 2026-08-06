using System; // Importante para usar o Action!
using UnityEngine;

public class Sistemadevida : MonoBehaviour
{
    [Header("Configurações do Herói")]
    [SerializeField] private AtributosPersonagem dadosDoHeroi;

    [Header("PvP e Partida")]
    [SerializeField] private int timeID = 1;

    [SerializeField] private float vidaAtual;

    public int TimeID => timeID;

    // EVENTO: Envia (vidaAtual, vidaMaxima) para quem estiver ouvindo
    public event Action<float, float> OnVidaMudou;

    private void Awake()
    {
        if (dadosDoHeroi != null)
        {
            vidaAtual = dadosDoHeroi.vidaMaxima;
        }
    }

    private void Start()
    {
        // Dispara o evento no início para a UI começar com o valor correto
        NotificarMudancaDeVida();
    }

    public void ReceberDano(InformacaoDano dano)
    {
        if (vidaAtual <= 0) return;

        // Fogo amigo
        if (dano.atacante != null && dano.atacante.TryGetComponent<Sistemadevida>(out Sistemadevida atacanteVida))
        {
            if (atacanteVida.TimeID == this.timeID && dano.atacante != this.gameObject)
            {
                return;
            }
        }

        // Aplica o dano
        vidaAtual -= dano.quantidade;
        vidaAtual = Mathf.Max(vidaAtual, 0f);

        // AVISA A UI QUE A VIDA MUDOU!
        NotificarMudancaDeVida();

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
            NotificarMudancaDeVida(); // Avisa a UI no respawn
        }
    }

    private void NotificarMudancaDeVida()
    {
        // O ?.Invoke garante que só dispara se houver algum script escutando esse evento
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