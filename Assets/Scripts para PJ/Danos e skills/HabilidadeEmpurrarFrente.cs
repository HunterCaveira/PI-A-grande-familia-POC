using UnityEngine;

public class HabilidadeEmpurrarFrente : MonoBehaviour
{
    public float forcaDoImpulso = 10f;
    public float dano = 20f;

    private AcoesPlayer acoesPlayer;
    private Sistemadevida meuSistemaVida; // Guarda a vida/time de quem usa a habilidade

    [Header("Configurações do Ataque (Quadrado)")]
    public Vector3 tamanhoDaCaixa = new Vector3(2f, 2f, 2f);
    public float distanciaNaFrente = 1.5f;

    void Awake()
    {
        acoesPlayer = new AcoesPlayer();
        meuSistemaVida = GetComponent<Sistemadevida>(); // Pega o Sistemadevida do próprio atacante
    }

    void OnEnable()
    {
        acoesPlayer.Enable();
    }

    void OnDisable()
    {
        acoesPlayer.Disable();
    }

    void Update()
    {
        // Lembre-se de trocar "HabilidadeDireita" pelo nome exato no seu Input Action System
        if (acoesPlayer != null && acoesPlayer.MovimentoPlayer.Habilidade1.triggered)
        {
            Empurrar();
        }
    }

    void Empurrar()
    {
        Vector3 centroDaCaixa = transform.position + (transform.forward * distanciaNaFrente);
        Collider[] coisasAcertadas = Physics.OverlapBox(centroDaCaixa, tamanhoDaCaixa / 2f, transform.rotation);

        foreach (Collider objeto in coisasAcertadas)
        {
            // Ignora o próprio jogador
            if (objeto.gameObject == this.gameObject) continue;

            // 1. Busca o Sistemadevida no objeto em vez de checar Tag
            if (objeto.TryGetComponent<Sistemadevida>(out Sistemadevida vidaAlvo))
            {
                // 2. Valida o Time (Só ataca se for de um TimeID diferente)
                if (meuSistemaVida == null || vidaAlvo.TimeID != meuSistemaVida.TimeID)
                {
                    // A) Aplica o Empurrão
                    Rigidbody rbAlvo = objeto.GetComponent<Rigidbody>();
                    if (rbAlvo != null)
                    {
                        Vector3 direcaoDoEmpurrao = objeto.transform.position - transform.position;
                        direcaoDoEmpurrao.y = 0; // Mantém a força horizontal
                        rbAlvo.AddForce(direcaoDoEmpurrao.normalized * forcaDoImpulso, ForceMode.Impulse);
                    }

                    // B) Monta as informações de dano e envia para o Sistemadevida do alvo
                    InformacaoDano infoDano = new InformacaoDano
                    {
                        quantidade = this.dano,
                        atacante = this.gameObject
                    };

                    vidaAlvo.ReceberDano(infoDano);

                    Debug.Log($"Empurrou {objeto.name} e causou {dano} de dano!");
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 centroDaCaixa = transform.position + (transform.forward * distanciaNaFrente);
        Gizmos.matrix = Matrix4x4.TRS(centroDaCaixa, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, tamanhoDaCaixa);
    }
}