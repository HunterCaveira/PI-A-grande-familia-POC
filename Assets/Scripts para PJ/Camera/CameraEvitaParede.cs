using UnityEngine;
using Unity.Cinemachine;
using Photon.Pun;

public class CameraEvitaParede : MonoBehaviourPun
{
    [Header("Configurações do Alvo")]
    [SerializeField] private Transform cameraFollowTarget;

    private CinemachineCamera virtualCamera;
    private CinemachineOrbitalFollow orbitalFollow;

    // Variável para guardar quem é o dono original do boneco
    private bool souODonoOriginal = false;

    void Start()
    {
        // 🔒 NOVA TRAVA: Verifica se nós somos os CRIADORES deste boneco, ignorando roubos temporários.
        if (photonView != null)
        {
            souODonoOriginal = (photonView.CreatorActorNr == PhotonNetwork.LocalPlayer.ActorNumber);
        }

        // Se eu não criei este boneco, desligo o script de câmera
        if (!souODonoOriginal)
        {
            enabled = false;
            return;
        }

        ConfigurarAlvoDaCamera();
    }

    private void ConfigurarAlvoDaCamera()
    {
        virtualCamera = Object.FindFirstObjectByType<CinemachineCamera>();

        if (virtualCamera != null)
        {
            orbitalFollow = virtualCamera.GetComponent<CinemachineOrbitalFollow>();

            if (cameraFollowTarget == null)
            {
                Transform achado = transform.Find("CameraTarget");
                cameraFollowTarget = achado != null ? achado : transform;
            }

            virtualCamera.Target.TrackingTarget = cameraFollowTarget;
        }
    }

    void LateUpdate()
    {
        // Continua a usar a trava de criador, não de dono atual
        if (!souODonoOriginal) return;

        if (virtualCamera != null && virtualCamera.Target.TrackingTarget == null)
        {
            if (cameraFollowTarget != null)
            {
                virtualCamera.Target.TrackingTarget = cameraFollowTarget;
            }
        }
    }

    public void ConfigurarNovoHeroi(Transform novoTarget)
    {
        if (!souODonoOriginal) return;

        cameraFollowTarget = novoTarget;
        if (virtualCamera != null && cameraFollowTarget != null)
        {
            virtualCamera.Target.TrackingTarget = cameraFollowTarget;
        }
    }
}