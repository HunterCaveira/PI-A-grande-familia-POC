using UnityEngine;
using Unity.Cinemachine;

public class CharacterCameraAnchor : MonoBehaviour
{
    [Header("Configuração de Foco")]
    [Tooltip("Arraste aqui o GameObject filho que representa a posição do ombro/mira")]
    [SerializeField] private Transform cameraTarget;

    private void Start()
    {
        if (cameraTarget == null)
        {
            Transform encontrado = transform.Find("CameraTarget");
            cameraTarget = (encontrado != null) ? encontrado : transform;
        }

        CinemachineCamera freeLookCam = FindFirstObjectByType<CinemachineCamera>();
        if (freeLookCam != null)
        {
            freeLookCam.Target.TrackingTarget = cameraTarget;
            freeLookCam.Follow = cameraTarget;
            freeLookCam.LookAt = cameraTarget;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (cameraTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(cameraTarget.position, 0.15f);
        }
    }
}