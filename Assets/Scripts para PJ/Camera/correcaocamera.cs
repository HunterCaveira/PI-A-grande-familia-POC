using UnityEngine;
using Unity.Cinemachine;

public class CorrecaoCameraPersonagem : MonoBehaviour
{
    [Header("Configuração de Foco da Câmera")]
    [Tooltip("Arraste o objeto filho 'CameraTarget' posicionado no ombro do personagem.")]
    [SerializeField] private Transform cameraTarget;

    [Header("Rotação do Personagem")]
    [SerializeField] private float velocidadeRotacao = 10f;
    [Tooltip("Gira o personagem apenas quando ele estiver se movendo?")]
    [SerializeField] private bool girarApenasEmMovimento = false;

    private Transform cameraTransform;

    private void Start()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Tenta encontrar o ponto de foco no próprio clone caso não esteja configurado
        if (cameraTarget == null)
        {
            Transform encontrado = transform.Find("CameraTarget");
            cameraTarget = (encontrado != null) ? encontrado : transform;
        }

        // Conecta o ponto de foco do clone à câmera principal do Cinemachine 3
        CinemachineCamera freeLookCam = FindFirstObjectByType<CinemachineCamera>();
        if (freeLookCam != null)
        {
            freeLookCam.Target.TrackingTarget = cameraTarget;
            freeLookCam.Follow = cameraTarget;
            freeLookCam.LookAt = cameraTarget;
        }
    }

    private void Update()
    {
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