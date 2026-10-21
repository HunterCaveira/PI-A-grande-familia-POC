using UnityEngine;
using Unity.Cinemachine;
using Photon.Pun; // Adicionado

public class CameraEvitaParede : MonoBehaviourPun // Herda de MonoBehaviourPun
{
    [Header("Configurações do Cinemachine 3")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private Transform cameraFollowTarget;

    private CinemachineOrbitalFollow orbitalFollow;
    private Transform mainCameraTransform;

    void Awake()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

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
        // MULTIPLAYER: Impede que o boneco do inimigo altere o alvo da SUA câmera
        if (!photonView.IsMine) return;

        if (virtualCamera != null && cameraFollowTarget != null)
        {
            virtualCamera.Target.TrackingTarget = cameraFollowTarget;
        }
    }

    void LateUpdate()
    {
        if (!photonView.IsMine) return;

        if (virtualCamera == null || cameraFollowTarget == null || orbitalFollow == null)
            return;
    }

    public void ConfigurarNovoHeroi(Transform novoTarget)
    {
        if (!photonView.IsMine) return;

        cameraFollowTarget = novoTarget;

        if (virtualCamera != null)
        {
            virtualCamera.Target.TrackingTarget = cameraFollowTarget;
        }
    }
}