using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class InimigoIA : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Configurações de Movimento")]
    [SerializeField] private Transform alvo;

    [Header("Configurações de Visão e Tiro")]
    [Tooltip("Distância máxima que o inimigo consegue ver e atirar")]
    [SerializeField] private float alcanceVisao = 20f;
    [Tooltip("Tempo de espera entre cada tiro")]
    [SerializeField] private float tempoEntreTiros = 2f;
    [Tooltip("Camadas que bloqueiam a visão (ex: Parede, Jogador)")]
    [SerializeField] private LayerMask mascaraVisao;
    [Tooltip("De onde o tiro vai sair (crie um objeto vazio na arma do inimigo)")]
    [SerializeField] private Transform pontoDeTiro;

    [Header("Configurações do Projétil")]
    public BalaPool poolDeBalas;
    public float forcaTiro = 50f;
    public float danoInimigo = 10f;
    // Assumindo que você tem esse enum no seu projeto
    public TipoDano tipoDeDano = TipoDano.Fisico;

    private float timerTiro;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (alvo != null)
        {
            // Persegue o jogador
            agent.SetDestination(alvo.position);

            // Controle do tempo de tiro
            timerTiro -= Time.deltaTime;
            if (timerTiro <= 0f)
            {
                VerificarEAtirar();
            }
        }
    }

    private void VerificarEAtirar()
    {
        if (pontoDeTiro == null || alvo == null) return;

        // Calcula a direção do inimigo para o alvo (jogador)
        Vector3 direcaoParaAlvo = (alvo.position - pontoDeTiro.position).normalized;

        RaycastHit hit;

        // Desenha uma linha amarela no modo Scene para ajudar a debugar a visão do inimigo
        Debug.DrawRay(pontoDeTiro.position, direcaoParaAlvo * alcanceVisao, Color.yellow);

        // Lança o Raycast
        if (Physics.Raycast(pontoDeTiro.position, direcaoParaAlvo, out hit, alcanceVisao, mascaraVisao))
        {
            // Se o Raycast bater no alvo, significa que não há paredes na frente
            if (hit.transform == alvo)
            {
                Atirar(direcaoParaAlvo);
                timerTiro = tempoEntreTiros; // Reinicia o cronômetro após atirar
            }
        }
    }

    private void Atirar(Vector3 direcao)
    {
        if (poolDeBalas == null) return;

        // Pega a bala do pool igual ao script do player
        GameObject balaAtual = poolDeBalas.GetitemFromPool();
        balaAtual.transform.position = pontoDeTiro.position;
        balaAtual.transform.forward = direcao;
        balaAtual.SetActive(true);

        // Inicializa a bala com as informações do inimigo
        if (balaAtual.TryGetComponent<Bala>(out Bala scriptBala))
        {
            scriptBala.Inicializar(danoInimigo, tipoDeDano, this.gameObject, "ArmaInimigo");
        }

        // Aplica a força física na bala
        Rigidbody rb = balaAtual.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(direcao * forcaTiro, ForceMode.Impulse);
        }
    }
}