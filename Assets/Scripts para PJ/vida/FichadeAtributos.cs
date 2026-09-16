using UnityEngine; // Importa a biblioteca principal do motor da Unity para utilizar ScriptableObject e atributos.

[CreateAssetMenu(fileName = "NovoAtributo", menuName = "HeroShooter/Atributos do Heroi")] // Permite criar o asset pelo menu de contexto da Unity.
public class AtributosPersonagem : ScriptableObject // Declara a classe herdando de ScriptableObject.
{
    // ==========================================
    // 1. ATRIBUTOS DE VIDA E COMBATE
    // ==========================================

    [Header("Atributos de Vida")] // Seção visual de configurações de vida no Inspector.

    public float vidaMaxima = 100f; // Armazena a quantidade máxima de vida base do personagem.


    // ==========================================
    // 2. ATRIBUTOS DE MOVIMENTO BASE
    // ==========================================

    [Header("Atributos de Movimento")] // Seção visual de configurações de movimento no Inspector.

    public float velocidadeMovimento = 10f; // Armazena a velocidade base de caminhada em m/s.

    public float forcaPulo = 2f; // Armazena a força/altura aplicada ao pulo.


    // ==========================================
    // 3. MODIFICADORES DINÂMICOS EM TEMPO DE EXECUÇÃO
    // ==========================================

    private float multiplicadorVelocidade = 1f; // Variável interna para guardar o multiplicador (Padrão = 1f / 100%).

    public float MultiplicadorVelocidade
    {
        get => multiplicadorVelocidade; // Retorna o valor atual do multiplicador.
        set => multiplicadorVelocidade = value; // Permite que habilidades externas (como o Tônico) alterem este valor.
    }

    // Propriedade de leitura que retorna o cálculo da velocidade final (Base x Multiplicador) em tempo real
    public float VelocidadeFinal => velocidadeMovimento * multiplicadorVelocidade;

    private void OnEnable() // Método executado automaticamente quando o asset é carregado na memória do jogo.
    {
        multiplicadorVelocidade = 1f; // Garante que o multiplicador inicie limpo (100%) sempre que o jogo rodar.
    }
}