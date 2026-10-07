using UnityEngine;

public class SkillUmHitbox : MonoBehaviour
{
    [Header("Atributos do Ataque")]
    [SerializeField] private float quantidadeDano = 25f;
    [SerializeField] private float forcaEmpurrao = 10f;

    [Header("Configuração de Direção")]
    [Tooltip("Se marcado, empurra a partir do centro do Jogador. Se desmarcado, empurra a partir do ponto exato onde a Hitbox tocou.")]
    [SerializeField] private bool usarOrigemDoJogador = true;

    private void OnTriggerEnter(Collider other) // Use OnTriggerEnter2D se o projeto for 2D
    {
        // Evita que o jogador tome dano da própria skill
        if (other.gameObject == transform.root.gameObject) return;

        // 1. Aplica o Dano utilizando o seu Sistemadevida
        if (other.TryGetComponent<Sistemadevida>(out Sistemadevida vidaAlvo))
        {
            InformacaoDano infoDano = new InformacaoDano
            {
                quantidade = quantidadeDano,
                atacante = transform.root.gameObject
            };

            vidaAlvo.ReceberDano(infoDano);
        }

        // 2. Aplica o Empurrão Adaptativo
        // 2. Aplica o Empurrão chamando o EnemyController unificado
        if (other.TryGetComponent<EnemyController>(out EnemyController inimigoCtrl))
        {
            Vector3 direcao = (other.transform.position - transform.root.position).normalized;
            direcao.y = 0; // Mantém no chão
            direcao.Normalize();

            float forcaEmpurrao = 10f;
            float duracaoEmpurrao = 0.2f;

            inimigoCtrl.ReceberEmpurrao(direcao, forcaEmpurrao, duracaoEmpurrao);
        }
    }
}