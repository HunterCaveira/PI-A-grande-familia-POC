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
        if (other.TryGetComponent<ReceptorDeImpacto>(out ReceptorDeImpacto receptor))
        {
            Vector3 direcao = Vector3.zero;

            if (usarOrigemDoJogador)
            {
                // Opção 1: Direção calculada a partir do centro do Jogador (Afastamento radial limpo)
                Transform jogadorTransform = transform.root;
                direcao = (other.transform.position - jogadorTransform.position);
            }
            else
            {
                // Opção 2: Direção calculada dinamicamente com base em onde a Hitbox interceptou o inimigo
                Vector3 pontoDeImpacto = other.ClosestPoint(transform.position);
                direcao = (other.transform.position - pontoDeImpacto);
            }

            // Normaliza e limpa o eixo Y para o personagem não flutuar
            direcao.y = 0;
            direcao.Normalize();

            // Se por algum motivo a direção zerar, usa a frente do jogador como segurança
            if (direcao == Vector3.zero)
            {
                direcao = transform.root.forward;
            }

            // Dispara o impulso no seu ReceptorDeImpacto
            receptor.ReceberImpulso(direcao, forcaEmpurrao);
        }
    }
}