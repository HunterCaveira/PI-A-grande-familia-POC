using UnityEngine;
using Unity.Cinemachine;

public class CameraEvitaParede : MonoBehaviour
{
    [Header("Configurações do Cinemachine 3")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private Transform cameraFollowTarget;
        
    // Caches privados para alta performance e suporte a P2P/Multiplayer
    private CinemachineOrbitalFollow orbitalFollow;
    private Transform mainCameraTransform;

    void Awake()
    {
        // 1. Cache da Câmera Principal (evita a busca custosa de 'Camera.main' em cada frame)
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        // 2. Cache do componente de movimento orbital
        if (virtualCamera != null)
        {
            orbitalFollow = virtualCamera.GetComponent<CinemachineOrbitalFollow>();

            if (orbitalFollow == null)
            {
                Debug.LogWarning("A 'virtualCamera' precisa do componente 'Cinemachine Orbital Follow'!");
            }
        }
    }

    void Start()
    {
        // Garante que o alvo da câmera seja o ponto fixo no ombro
        if (virtualCamera != null && cameraFollowTarget != null)
        {
            virtualCamera.Target.TrackingTarget = cameraFollowTarget;
        }
    }

    // Usamos LateUpdate para sincronizar o enquadramento APÓS o movimento do personagem e física
    void LateUpdate()
    {
        // Validação de segurança
        if (virtualCamera == null || cameraFollowTarget == null || orbitalFollow == null)
            return;

        // A colisão com paredes agora é gerenciada 100% pelo componente "Cinemachine Deoccluder" 
        // configurado no Inspector da própria Virtual Camera.
        // Isso previne que o 'cameraFollowTarget' saia do lugar e desalinhe os tiros em P2P.
    }

    /// <summary>
    /// Permite alterar a referência do jogador dinamicamente quando um novo herói for instanciado.
    /// </summary>
    public void ConfigurarNovoHeroi(Transform novoTarget)
    {
        cameraFollowTarget = novoTarget;

        if (virtualCamera != null)
        {
            virtualCamera.Target.TrackingTarget = cameraFollowTarget;
        }
    }
}