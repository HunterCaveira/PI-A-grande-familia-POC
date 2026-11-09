using UnityEngine;
using Unity.Cinemachine; // Cinemachine v3
using Photon.Pun;   

public class CorrecaoCameraPersonagem : MonoBehaviour
{
    [Header("Configuração de Foco da Câmera")]
    [Tooltip("Arraste o objeto filho 'CameraTarget' posicionado no ombro do personagem.")]
    [SerializeField] private Transform cameraTarget;

    [Header("Rotação do Personagem")]
    [SerializeField] private float velocidadeRotacao = 10f;
    [Tooltip("Gira o personagem apenas quando ele estiver se movendo? (Recomendado para jogos estilo Marvel Rivals)")]
    [SerializeField] private bool girarApenasEmMovimento = false;

    private Transform cameraTransform;

    private PhotonView view;


    void Start()
    {
        view = GetComponent<PhotonView>();
        // Encontra a câmera principal
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Se não atribuiu o ponto no ombro no Inspector, tenta achar o objeto filho "CameraTarget"
        if (cameraTarget == null)
        {
            Transform encontrado = transform.Find("CameraTarget");
            cameraTarget = encontrado != null ? encontrado : transform;
        }

        // Conecta o alvo do Cinemachine v3 de forma correta
        CinemachineCamera freeLookCam = FindFirstObjectByType<CinemachineCamera>();
        if (freeLookCam != null)
        {
            if(view.IsMine)
            freeLookCam.Target.TrackingTarget = cameraTarget;
        }
    }

    void Update()
    {
        AlinharPersonagemComCamera();
    }

    private void AlinharPersonagemComCamera()
    {
        if (cameraTransform == null) return;

        // Se a opção estiver ativa, só gira se houver input de movimento
        if (girarApenasEmMovimento)
        {
            float inputHorizontal = Input.GetAxisRaw("Horizontal");
            float inputVertical = Input.GetAxisRaw("Vertical");

            if (inputHorizontal == 0 && inputVertical == 0) return;
        }

        // Obtém a direção para onde a câmera está olhando no plano horizontal
        Vector3 direcaoCamera = cameraTransform.forward;
        direcaoCamera.y = 0;

        if (direcaoCamera.sqrMagnitude < 0.0001f) return;

        // Rotaciona APENAS o corpo do personagem suavemente
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