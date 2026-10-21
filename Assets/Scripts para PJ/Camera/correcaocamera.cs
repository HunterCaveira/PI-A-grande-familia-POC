using UnityEngine;
using Unity.Cinemachine;
using Photon.Pun; // Adicionado

public class CorrecaoCameraPersonagem : MonoBehaviourPun // Herda de MonoBehaviourPun
{
    [Header("Configuração de Foco da Câmera")]
    [Tooltip("Arraste o objeto filho 'CameraTarget' posicionado no ombro do personagem.")]
    [SerializeField] private Transform cameraTarget;

    [Header("Rotação do Personagem")]
    [SerializeField] private float velocidadeRotacao = 10f;
    [Tooltip("Gira o personagem apenas quando ele estiver se movendo?")]
    [SerializeField] private bool girarApenasEmMovimento = false;

    private Transform cameraTransform;

    void Start()
    {
        // MULTIPLAYER: Só alinha a câmera e permite buscar as referências se for seu boneco
        if (!photonView.IsMine) return;

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
        // MULTIPLAYER: Inimigos não rodam essa lógica de rotação local
        if (!photonView.IsMine) return;

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

    private void OnDrawGizmosSelected()
    {
        if (cameraTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(cameraTarget.position, 0.15f);
        }
    }
}