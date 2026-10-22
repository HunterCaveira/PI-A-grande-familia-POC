using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class Gerenciadorderespawn : MonoBehaviour
{
    [SerializeField] protected tiroPistola Tiropistola;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera virtualCamera;

    [Header("Personagens Pré-gerados na Cena (Matrizes)")]
    [SerializeField] private GameObject personagem1Cena;
    [SerializeField] private GameObject personagem2Cena;

    [Header("Configurações de Respawn Oficial")]
    [SerializeField] private Transform[] pontosDeSpawn;
    [SerializeField] private float tempoParaRenascer = 3f;

    [Header("Segurança / Trava de Reativação Automática")]
    [Tooltip("Tempo (em segundos) que o clone pode ficar inativo por erro antes de ser forçado a ativar novamente.")]
    [SerializeField] private float tempoSegurancaReativacao = 2f;

    private GameObject cloneAtivo;
    public GameObject CloneAtivo => cloneAtivo;

    private bool emProcessoDeRespawn = false;
    private float tempoInativoAcumulado = 0f;

    private void Start()
    {
        if (pontosDeSpawn == null || pontosDeSpawn.Length == 0)
        {
            Debug.LogError("[GerenciadorRespawn] ERRO: Nenhum ponto de spawn atribuído no Inspector!");
            return;
        }

        if (virtualCamera == null)
        {
            virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        }

        OcultarPersonagensMatriz();

        int idPersonagem = PlayerPrefs.GetInt("PersonagemEscolhido", 1);
        if (idPersonagem <= 0) idPersonagem = 1;

        ClonarPersonagemInicial(idPersonagem);

        if (Tiropistola != null)
        {
            Tiropistola.gameObject.SendMessage("Reload", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void Update()
    {
        VerificarEstadoDoClone();
    }

    /// <summary>
    /// Checa constantemente se o clone está ativo. Caso tenha sido desativado por erro externo, força a reativação.
    /// </summary>
    private void VerificarEstadoDoClone()
    {
        // Se o jogador morreu legitimamente, o respawn normal assume o controle
        if (emProcessoDeRespawn) return;

        // CASO 1: O clone existe na cena, mas foi desativado sem autorização
        if (cloneAtivo != null && !cloneAtivo.activeInHierarchy)
        {
            tempoInativoAcumulado += Time.deltaTime;

            if (tempoInativoAcumulado >= tempoSegurancaReativacao)
            {
                Debug.LogWarning($"[GerenciadorRespawn] Clone '{cloneAtivo.name}' detectado como inativo fora do respawn! Reativando em emergência...");
                cloneAtivo.SetActive(true);
                AtualizarCameraCinemachine(cloneAtivo.transform);
                tempoInativoAcumulado = 0f;
            }
        }
        // CASO 2: O clone foi deletado por algum script antigo
        else if (cloneAtivo == null)
        {
            tempoInativoAcumulado += Time.deltaTime;

            if (tempoInativoAcumulado >= tempoSegurancaReativacao)
            {
                Debug.LogWarning("[GerenciadorRespawn] Clone nulo/destruído! Re-clonando personagem de emergência...");
                int idPersonagem = PlayerPrefs.GetInt("PersonagemEscolhido", 1);
                ClonarPersonagemInicial(idPersonagem);
                tempoInativoAcumulado = 0f;
            }
        }
        else
        {
            // Reset do cronômetro enquanto o clone estiver ativo e saudável
            tempoInativoAcumulado = 0f;
        }
    }

    private void OcultarPersonagensMatriz()
    {
        if (personagem1Cena != null) personagem1Cena.SetActive(false);
        if (personagem2Cena != null) personagem2Cena.SetActive(false);
    }

    private void ClonarPersonagemInicial(int id)
    {
        Transform spawnEscolhido = pontosDeSpawn[Random.Range(0, pontosDeSpawn.Length)];
        GameObject objetoMatriz = (id == 1) ? personagem1Cena : personagem2Cena;

        if (objetoMatriz != null)
        {
            cloneAtivo = Instantiate(objetoMatriz, spawnEscolhido.position, spawnEscolhido.rotation);
            cloneAtivo.name = $"Jogador_Clone_{(id == 1 ? "Pai" : "Mae")}";
            cloneAtivo.SetActive(true);

            AtualizarCameraCinemachine(cloneAtivo.transform);
            Debug.Log($"[GerenciadorRespawn] Clone '{cloneAtivo.name}' ativo na cena.");
        }
        else
        {
            Debug.LogError($"[GerenciadorRespawn] Objeto matriz do Personagem {id} não foi atribuído no Inspector!");
        }
    }

    private void AtualizarCameraCinemachine(Transform alvo)
    {
        if (virtualCamera != null)
        {
            virtualCamera.Follow = alvo;
            virtualCamera.LookAt = alvo;
        }
    }

    public void ProcessarMorte(GameObject jogador = null)
    {
        GameObject alvo = (cloneAtivo != null) ? cloneAtivo : (jogador != null ? jogador.transform.root.gameObject : null);

        if (alvo != null)
        {
            StartCoroutine(RotinaRespawn(alvo));
        }
    }

    private IEnumerator RotinaRespawn(GameObject clone)
    {
        // Ativa a trava para o Watchdog não reativar o clone antes do tempo de respawn terminar
        emProcessoDeRespawn = true;
        clone.SetActive(false);

        yield return new WaitForSeconds(tempoParaRenascer);

        if (pontosDeSpawn.Length > 0)
        {
            Transform spawnEscolhido = pontosDeSpawn[Random.Range(0, pontosDeSpawn.Length)];
            clone.transform.position = spawnEscolhido.position;
            clone.transform.rotation = spawnEscolhido.rotation;
        }

        clone.SetActive(true);

        if (clone.TryGetComponent<Sistemadevida>(out Sistemadevida vida))
        {
            vida.Renascer();
        }

        // Libera a checagem de segurança novamente
        emProcessoDeRespawn = false;
    }
}