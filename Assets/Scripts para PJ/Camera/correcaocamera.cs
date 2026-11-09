using UnityEngine;
using Unity.Cinemachine;
using Photon.Pun;

public class CorrecaoCameraPersonagem : MonoBehaviour
{
    [Header("Configuração de Foco da Câmera")]
    [SerializeField] private Transform cameraTarget;

    [Header("Rotação do Personagem")]
    [SerializeField] private float velocidadeRotacao = 10f;
    [SerializeField] private bool girarApenasEmMovimento = false;

    private Transform cameraTransform;
    private PhotonView view;

    // Variável para guardar quem é o dono original do boneco
    private bool souODonoOriginal = false;

    void Start()
    {
        view = GetComponent<PhotonView>();

        if (view != null)
        {
            souODonoOriginal = (view.CreatorActorNr == PhotonNetwork.LocalPlayer.ActorNumber);
        }

        // Se eu não criei este boneco, aborta
        if (!souODonoOriginal) return;

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

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

    void Update()
    {
        // TRAVA CRÍTICA MULTIPLAYER: Só o criador original pode rodar o próprio corpo
        if (!souODonoOriginal) return;

        AlinharPersonagemComCamera();
    }

    private void AlinharPersonagemComCamera()
    {
        if (cameraTransform == null) return;

        if (girarApenasEmMovimento)
        {
            float inputHorizontal = Input.GetAxisRaw("Horizontal");
            float inputVertical = Input.GetAxisRaw("Vertical");

            if (inputHorizontal == 0 && inputVertical == 0) return;
        }

        Vector3 direcaoCamera = cameraTransform.forward;
        direcaoCamera.y = 0;

        if (direcaoCamera.sqrMagnitude < 0.0001f) return;

        Quaternion rotacaoAlvo = Quaternion.LookRotation(direcaoCamera);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoAlvo, velocidadeRotacao * Time.deltaTime);
    }
}