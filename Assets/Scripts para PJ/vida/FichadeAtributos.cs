using UnityEngine; // Importa as ferramentas principais da Unity para usar ScriptableObject e atributos do Inspector.

[CreateAssetMenu(fileName = "NovoAtributo", menuName = "HeroShooter/Atributos do Heroi")] // Permite criar o arquivo de ficha diretamente pelo menu de contexto na aba Project.
public class AtributosPersonagem : ScriptableObject // Declara a classe pública AtributosPersonagem herdando de ScriptableObject.
{
    // ==========================================
    // 1. ATRIBUTOS DE VIDA E COMBATE
    // ==========================================

    [Header("Atributos de Vida")] // Agrupa visualmente esta seção no painel Inspector da Unity.

    public float vidaMaxima = 100f; // Armazena a quantidade máxima de pontos de vida base do personagem.


    // ==========================================
    // 2. ATRIBUTOS DE MOVIMENTO BASE
    // ==========================================

    [Header("Atributos de Movimento")] // Agrupa visualmente esta seção no painel Inspector.

    public float velocidadeMovimento = 10f; // Armazena a velocidade base de caminhada do personagem em m/s.

    public float forcaPulo = 2f; // Armazena o valor da força/altura aplicada ao pulo do herói.

    public float porcentagemSpeed; // Modificador percentual de velocidade opcional mantido na ficha.
}