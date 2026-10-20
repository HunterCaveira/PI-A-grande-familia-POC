using UnityEngine;
using Unity.Cinemachine;
using Photon.Pun; // Adicionado

public class CharacterCameraAnchor : MonoBehaviourPun // Herda de MonoBehaviourPun
{
    [Header("Configuração de Foco")]
    [Tooltip("Arraste aqui o GameObject filho que representa a posição do ombro/mira")]
    [SerializeField] private Transform cameraTarget;

    void Start()
    {
        // MULTIPLAYER: Só conecta a câmera se este personagem for seu!
        if (!photonView.IsMine) return;

        if (cameraTarget == null)
        {
            Transform encontrado = transform.Find("CameraTarget");
            cameraTarget = encontrado != null ? encontrado : transform;
        }

        CinemachineCamera freeLookCam = FindFirstObjectByType<CinemachineCamera>();
        if (freeLookCam != null)
        {
            freeLookCam.Target.TrackingTarget = cameraTarget;
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