// Importa a biblioteca base da Unity
using UnityEngine;
// Importa a biblioteca do Cinemachine 3
using Unity.Cinemachine;
// Importa o Photon para permitir o bloqueio de rede
using Photon.Pun;

public class CameraEvitaParede : MonoBehaviourPun
{
    [Header("Configurações do Alvo")]
    [Tooltip("Arraste o ponto do ombro (CameraTarget) do seu personagem aqui.")]
    [SerializeField] private Transform cameraFollowTarget;

    // Caches privados para alta performance e suporte a P2P/Multiplayer
    private CinemachineCamera virtualCamera;
    private CinemachineOrbitalFollow orbitalFollow;
    private Transform mainCameraTransform;

    void Start()
    {
        // 🔒 TRAVA DE REDE CRÍTICA: Se não for o meu boneco, desliga este script completamente
        // para economizar processamento e evitar conflitos de câmara.
        if (photonView != null && !photonView.IsMine)
        {
            enabled = false;
            return;
        }

        // Cache da Câmera Principal da cena
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        // Configuração inicial do alvo de seguimento
        ConfigurarAlvoDaCamera();
    }

    private void ConfigurarAlvoDaCamera()
    {
        // Busca a CinemachineCamera (Virtual Camera) ativa na cena
        virtualCamera = Object.FindFirstObjectByType<CinemachineCamera>();

        if (virtualCamera != null)
        {
            // Tenta obter o componente Orbital Follow do Cinemachine 3 de forma segura
            orbitalFollow = virtualCamera.GetComponent<CinemachineOrbitalFollow>();

            // Autocorreção caso esqueça de arrastar o CameraTarget no Inspector
            if (cameraFollowTarget == null)
            {
                Transform achado = transform.Find("CameraTarget");
                cameraFollowTarget = achado != null ? achado : transform;
            }

            // Define oficialmente que a câmara da cena deve seguir o ombro DESTE jogador
            virtualCamera.Target.TrackingTarget = cameraFollowTarget;
        }
        else
        {
            Debug.LogWarning("[CameraEvitaParede] Nenhuma CinemachineCamera foi encontrada na cena!");
        }
    }

    void LateUpdate()
    {
        // Trava de segurança extra de rede
        if (photonView != null && !photonView.IsMine) return;

        // Validação contínua: se por algum motivo a câmara perdeu o alvo (ex: após um respawn), reconfigura automaticamente
        if (virtualCamera != null && virtualCamera.Target.TrackingTarget == null)
        {
            if (cameraFollowTarget != null)
            {
                virtualCamera.Target.TrackingTarget = cameraFollowTarget;
            }
        }

        // NOTA DE DESIGN DO HERO SHOOTER:
        // A colisão com paredes é gerida pelo componente "Cinemachine Deoccluder" 
        // adicionado diretamente no Inspector da Virtual Camera na cena.
    }

    /// <summary>
    /// Permite alterar a referência do jogador dinamicamente (ex: respawn ou troca de herói).
    /// </summary>
    public void ConfigurarNovoHeroi(Transform novoTarget)
    {
        if (photonView != null && !photonView.IsMine) return;

        cameraFollowTarget = novoTarget;

        if (virtualCamera != null && cameraFollowTarget != null)
        {
            virtualCamera.Target.TrackingTarget = cameraFollowTarget;
        }
    }
}