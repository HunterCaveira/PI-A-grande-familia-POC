using UnityEngine; // Importa as ferramentas principais do motor da Unity.

public class ProjetilLentidao : MonoBehaviour
{
    // ==========================================
    // 1. CONFIGURAÇÕES DO PROJÉTIL
    // ==========================================

    [Header("Configurações do Projétil")]
    [SerializeField] private float velocidadeProjetil = 25f;
    [SerializeField] private float tempoDeVida = 5f;

    [Header("Posição de Espera (Teleporte ao Desativar)")]
    [Tooltip("Ponto de estoque/espera na cena para onde o projétil se move ao desativar.")]
    [SerializeField] private Transform pontoDeEspera;

    [Header("Efeito de Lentidão")]
    [SerializeField, Range(0f, 1f)] private float porcentagemReducaoVelocidade = 0.5f; // Ex: 0.5 = 50% de lentidão.
    [SerializeField] private float duracaoLentidao = 3.0f; // Tempo que a lentidão permanece no alvo em segundos.

    [Header("Controle de Referências e Time")]
    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;

    private int timeAtacante = -1;
    private GameObject donoDoProjetil;

    private void Awake()
    {
        if (gerenciadorDeEquipamentos == null)
        {
            gerenciadorDeEquipamentos = FindFirstObjectByType<GerenciadorDeEquipamentos>();
        }
    }

    private void OnEnable()
    {
        CancelInvoke(nameof(DesativarProjetil));
        Invoke(nameof(DesativarProjetil), tempoDeVida);
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * velocidadeProjetil * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Ignora colisão com o próprio jogador que disparou
        if (donoDoProjetil != null && other.gameObject == donoDoProjetil) return;

        // 2. Checa o Sistema de Vida do alvo para validação de time (Fogo Amigo)
        if (other.TryGetComponent<Sistemadevida>(out Sistemadevida sistemaVidaAlvo))
        {
            if (sistemaVidaAlvo.TimeID == this.timeAtacante)
            {
                return; // Se for do mesmo time, ignora
            }
        }

        // 3. Caso o alvo seja um Jogador com MovimentodoPlayer
        if (other.TryGetComponent<MovimentodoPlayer>(out MovimentodoPlayer movimentoPlayer))
        {
            movimentoPlayer.AplicarLentidao(porcentagemReducaoVelocidade, duracaoLentidao);
            DesativarProjetil();
        }
        // 4. Caso o alvo seja um Inimigo com EnemyController (usa o método nativo do script que você enviou)
        else if (other.TryGetComponent<EnemyController>(out EnemyController enemyController))
        {
            enemyController.AplicarLentidao(porcentagemReducaoVelocidade, duracaoLentidao);
            DesativarProjetil();
        }
    }

    public void Inicializar(int teamID, GameObject dono)
    {
        this.timeAtacante = teamID;
        this.donoDoProjetil = dono;
    }

    private void DesativarProjetil()
    {
        CancelInvoke(nameof(DesativarProjetil));

        if (pontoDeEspera != null)
        {
            transform.position = pontoDeEspera.position;
            transform.rotation = pontoDeEspera.rotation;
        }

        if (gerenciadorDeEquipamentos != null)
        {
            gerenciadorDeEquipamentos.TrocarEquipamento(0);
        }

        gameObject.SetActive(false);
    }
}