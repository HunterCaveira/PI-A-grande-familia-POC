using UnityEngine;
using Unity.Cinemachine;

public class CameraEvitaParede : MonoBehaviour
{
    [Header("Configurações do Cinemachine 3")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private Transform cameraFollowTarget;

    private CinemachineOrbitalFollow orbitalFollow;
    private Transform mainCameraTransform;

    private void Awake()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        // Caso a câmera não tenha sido arrastada pelo Inspector, busca na cena
        if (virtualCamera == null)
        {
            virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        }

        if (virtualCamera != null)
        {
            orbitalFollow = virtualCamera.GetComponent<CinemachineOrbitalFollow>();

            if (orbitalFollow == null)
            {
                Debug.LogWarning("[CameraEvitaParede] A 'virtualCamera' precisa do componente 'Cinemachine Orbital Follow'!");
            }
        }
    }

    /// <summary>
    /// Reassocia a câmera do Cinemachine ao novo clone instanciado.
    /// </summary>
    /// <param name="novoTarget">Transform do clone do personagem</param>
    public void ConfigurarNovoHeroi(Transform novoTarget)
    {
        if (novoTarget == null) return;

        cameraFollowTarget = novoTarget;

        if (virtualCamera != null)
        {
            // Atualiza os alvos do Cinemachine 3 para focar e seguir o novo clone
            virtualCamera.Target.TrackingTarget = novoTarget;
            virtualCamera.Follow = novoTarget;
            virtualCamera.LookAt = novoTarget;

            Debug.Log($"[CameraEvitaParede] Câmera reconfigurada para seguir o clone: {novoTarget.name}");
        }
    }
}