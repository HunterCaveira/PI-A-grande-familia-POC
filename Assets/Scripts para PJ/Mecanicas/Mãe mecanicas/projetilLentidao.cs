using UnityEngine;

public class ProjetilLentidao : MonoBehaviour
{
    [Header("Configurações do Projétil")]
    [SerializeField] private float velocidadeProjetil = 25f;
    [SerializeField] private float tempoDeVida = 5f;

    [Header("Posição de Espera (Teleporte ao Desativar)")]
    [Tooltip("Arraste um GameObject vazio que servirá como 'base' ou 'estoque' para onde o projétil vai ao ser desativado.")]
    [SerializeField] private Transform pontoDeEspera;

    [Header("Efeito de Lentidão")]
    [SerializeField, Range(0f, 1f)] private float porcentagemReducao = 0.5f; // 50% de lentidão
    [SerializeField] private float duracaoLentidao = 3f;

    [Header("Referência ao Gerenciador")]
    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;

    private void Awake()
    {
        // Caso não tenha sido arrastado no Inspector, busca automaticamente na cena/personagem
        if (gerenciadorDeEquipamentos == null)
        {
            gerenciadorDeEquipamentos = FindFirstObjectByType<GerenciadorDeEquipamentos>();
        }
    }

    private void OnEnable()
    {
        // Sempre que o projétil é ativado, cancela o timer anterior e agenda a desativação pelo tempo
        CancelInvoke(nameof(DesativarProjetil));
        Invoke(nameof(DesativarProjetil), tempoDeVida);
    }

    private void Update()
    {
        // Move o projétil continuamente para frente
        transform.Translate(Vector3.forward * velocidadeProjetil * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Se colidir com o inimigo, aplica a lentidão
        if (other.TryGetComponent<EnemyController>(out EnemyController enemyController))
        {
            enemyController.AplicarLentidao(porcentagemReducao, duracaoLentidao);
        }
    }

    /// <summary>
    /// Teleporta o projétil para o local de espera predefinido, o desativa e reativa a arma principal.
    /// </summary>
    private void DesativarProjetil()
    {
        CancelInvoke(nameof(DesativarProjetil));

        // Teleporta para o ponto de espera antes de sumir
        if (pontoDeEspera != null)
        {
            transform.position = pontoDeEspera.position;
            transform.rotation = pontoDeEspera.rotation;
        }

        // Volta para a arma principal (Slot 0) se o gerenciador existir
        if (gerenciadorDeEquipamentos != null)
        {
            gerenciadorDeEquipamentos.TrocarEquipamento(0);
        }

        // Desativa o objeto da cena
        gameObject.SetActive(false);
    }
}