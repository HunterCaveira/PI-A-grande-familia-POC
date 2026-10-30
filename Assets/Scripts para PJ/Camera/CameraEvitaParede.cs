// Importa a biblioteca base da Unity
using UnityEngine;
// Importa a biblioteca do Cinemachine 3
using Unity.Cinemachine;
// MUDANÇA: Importa o Photon para permitir o bloqueio de rede
using Photon.Pun;

// MUDANÇA: Agora herda de MonoBehaviourPun para usar o photonView
public class CameraEvitaParede : MonoBehaviourPun
{
    [Header("Configurações do Alvo")]
    [Tooltip("Arraste o ponto do ombro (CameraTarget) do seu personagem aqui.")]
    [SerializeField] private Transform cameraFollowTarget;

    // Caches privados para alta performance e suporte a P2P/Multiplayer
    // MUDANÇA: Retirámos o [SerializeField] da câmera. O código vai achá-la sozinho agora!
    private CinemachineCamera virtualCamera;
    private CinemachineOrbitalFollow orbitalFollow;
    private Transform mainCameraTransform;

    // Start é chamado quando o personagem nasce na cena
    void Start()
    {
        // 🔒 MUDANÇA CRÍTICA: Trava de Rede!
        // Se este script estiver rodando no boneco de outro jogador (inimigo), 
        // ele aborta a execução na hora. Assim, ninguém rouba a sua câmera.
        if (photonView != null && !photonView.IsMine) return;

        // 1. Cache da Câmera Principal da cena (evita lag de busca)
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        // 2. MUDANÇA: Busca a CinemachineCamera (Virtual Camera) automaticamente no mapa.
        virtualCamera = Object.FindFirstObjectByType<CinemachineCamera>();

        // 3. Cache do componente orbital e configuração do alvo
        if (virtualCamera != null)
        {
            orbitalFollow = virtualCamera.GetComponent<CinemachineOrbitalFollow>();

            if (orbitalFollow == null)
            {
                Debug.LogWarning("A câmera não tem 'Cinemachine Orbital Follow'!");
            }

            // MUDANÇA: Autocorreção caso esqueça de arrastar o CameraTarget no Inspector
            if (cameraFollowTarget == null)
            {
                Transform achado = transform.Find("CameraTarget");
                // Se achar o filho "CameraTarget", usa ele. Se não achar, foca na barriga (transform raiz).
                cameraFollowTarget = achado != null ? achado : transform;
            }

            // Define oficialmente que a câmera da cena deve seguir o ombro DESTE jogador
            virtualCamera.Target.TrackingTarget = cameraFollowTarget;
        }
    }

    // Usamos LateUpdate para sincronizar enquadramentos APÓS a física do personagem (evita tremedeira)
    void LateUpdate()
    {
        // 🔒 Trava de Rede de Segurança
        if (photonView != null && !photonView.IsMine) return;

        // Validação de segurança para evitar erros vermelhos na consola
        if (virtualCamera == null || cameraFollowTarget == null || orbitalFollow == null)
            return;

        // NOTA DE DESIGN DO HERO SHOOTER:
        // A colisão com paredes agora é gerenciada 100% pelo componente "Cinemachine Deoccluder" 
        // que você deve adicionar diretamente no Inspector da própria Virtual Camera que fica na cena.
        // Como o Deoccluder faz tudo, não precisamos forçar Raycasts complexos aqui dentro, 
        // mantendo a mira estável e o jogo muito mais leve em rede!
    }

    /// <summary>
    /// Permite alterar a referência do jogador dinamicamente (ex: reviver com outro herói).
    /// </summary>
    public void ConfigurarNovoHeroi(Transform novoTarget)
    {
        // 🔒 Trava de Rede de Segurança
        if (photonView != null && !photonView.IsMine) return;

        cameraFollowTarget = novoTarget;

        if (virtualCamera != null)
        {
            virtualCamera.Target.TrackingTarget = cameraFollowTarget;
        }
    }
}