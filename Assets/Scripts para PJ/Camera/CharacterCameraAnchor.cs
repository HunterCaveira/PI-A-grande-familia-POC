using UnityEngine;
using Unity.Cinemachine; // Cinemachine v3

public class CharacterCameraAnchor : MonoBehaviour
{
    [Header("Configuração de Foco")]
    [Tooltip("Arraste aqui o GameObject filho que representa a posição do ombro/mira")]
    [SerializeField] private Transform cameraTarget;

    void Start()
    {
        // Se não atribuiu um ponto manualmente, busca um filho chamado 'CameraTarget' ou usa o próprio transform
        if (cameraTarget == null)
        {
            Transform encontrado = transform.Find("CameraTarget");
            cameraTarget = encontrado != null ? encontrado : transform;
        }

        // Conecta o ponto do ombro à câmera ativa
        CinemachineCamera freeLookCam = FindFirstObjectByType<CinemachineCamera>();
        if (freeLookCam != null)
        {
            freeLookCam.Target.TrackingTarget = cameraTarget;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Desenha uma esfera amarela na Scene para facilitar a visualização do ponto no ombro
        if (cameraTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(cameraTarget.position, 0.15f);
        }
    }
}