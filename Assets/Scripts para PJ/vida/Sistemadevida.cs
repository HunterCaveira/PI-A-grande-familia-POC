using System; // Importante para usar o Action!
using UnityEngine;

public class Sistemadevida : MonoBehaviour
{
    [Header("Configurações do Herói")]
    [SerializeField] private AtributosPersonagem dadosDoHeroi;

    [Header("PvP e Partida")]
    [SerializeField] private int timeID = 1;

    [SerializeField] public float vidaAtual;

    public int TimeID => timeID;

    // [ACRÉSCIMO DA ETAPA 2]: Propriedade para aplicar a porcentagem de redução de dano do Tônico (Ex: 0.3f = 30% de desconto)
    public float PorcentagemReducaoDano { get; set; } = 0f;

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

        // [ACRÉSCIMO DA ETAPA 2]: Calcula o dano final aplicando a porcentagem de redução de dano
        float danoFinal = dano.quantidade * (1f - PorcentagemReducaoDano);

        // Aplica o dano com a mitigação calculada
        vidaAtual -= danoFinal;
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
            PorcentagemReducaoDano = 0f; // Reseta a redução de dano caso o player estivesse bufado ao morrer
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

        // Procura o gerenciador na cena e envia o objeto para a rotina de respawn
        GerenciadorRespawn gerenciador = FindFirstObjectByType<GerenciadorRespawn>();

        if (gerenciador != null)
        {
            gerenciador.ProcessarMorte(this.gameObject);
        }
        else
        {
            Debug.LogWarning("Nenhum GerenciadorRespawn encontrado na cena!");
        }
    }
}