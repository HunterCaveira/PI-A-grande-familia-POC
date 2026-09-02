using UnityEngine;

public class habilidadeMãe1 : MonoBehaviour
{
    [Header("Configurações do Ataque")]
    public float forcaDoImpulso = 10f;
    public float dano = 20f;

    [Tooltip("De quem é esse ataque? 1 = Jogador, 2 = Inimigo")]
    public int meuTimeID = 1;

    // Só roda quando a hitbox for ativada e encostar em alguém
    private void OnTriggerEnter(Collider outro)
    {
        // 1. O objeto tem sistema de vida?
        if (outro.TryGetComponent<Sistemadevida>(out Sistemadevida vidaAlvo))
        {
            // 2. IDENTIFICAÇÃO DE ID (Aliado vs Inimigo)
            // Se o meu time é 1 (Jogador) e o alvo for 2 (Inimigo) -> Ataca!
            // Você também pode fazer: if (vidaAlvo.TimeID != meuTimeID) para ser universal
            if (vidaAlvo.TimeID != meuTimeID)
            {
                // A) Aplica o Dano
                InformacaoDano info = new InformacaoDano
                {
                    quantidade = this.dano,
                    atacante = this.gameObject
                };
                vidaAlvo.ReceberDano(info);

                // B) Aplica o Impulso no CharacterController
                if (outro.TryGetComponent<ReceptorDeImpacto>(out ReceptorDeImpacto receptor))
                {
                    Vector3 direcao = outro.transform.position - transform.position;
                    direcao.y = 0; // Mantém no chão
                    receptor.ReceberImpulso(direcao.normalized, forcaDoImpulso);
                }

                Debug.Log($"Acertou um alvo do Time {vidaAlvo.TimeID}!");
            }
        }
    }
}