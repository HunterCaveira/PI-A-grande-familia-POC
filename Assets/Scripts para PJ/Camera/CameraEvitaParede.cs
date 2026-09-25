using UnityEngine;
using Unity.Cinemachine;

public class CameraEvitaParede : MonoBehaviour
{
    [Header("Configurações do Cinemachine 3")]
    public CinemachineCamera virtualCamera;

    [Tooltip("O GameObject vazio que fica DENTRO do jogador e que a câmera segue")]
    public Transform cameraFollowTarget;

    [Tooltip("De onde sai o laser de colisão (ex: cabeça do jogador)")]
    public Transform raycastOrigin;

    [Header("Posição Normal (Centro)")]
    public float raioNormal = 5f;
    public Vector3 offsetNormal = new Vector3(0f, 0f, 0f);

    [Header("Posição na Parede (Ombro)")]
    public float raioParede = 2.5f;
    [Tooltip("X: Direita/Esquerda | Y: Cima/Baixo | Z: Frente/Trás")]
    public Vector3 offsetParede = new Vector3(1.5f, 1f, 0f); // Move 1.5 pra direita, 1 pra cima

    [Header("Configurações de Colisão")]
    public LayerMask wallLayer;
    public float rayThickness = 0.5f;
    public float velocidadeSuavizacao = 8f;

    private CinemachineOrbitalFollow orbitalFollow;

    void Start()
    {
        if (virtualCamera != null)
        {
            orbitalFollow = virtualCamera.GetComponent<CinemachineOrbitalFollow>();

            if (orbitalFollow == null)
            {
                Debug.LogWarning("Sua câmera precisa do componente 'Cinemachine Orbital Follow'!");
            }
        }
    }

    void Update()
    {
        if (virtualCamera == null || cameraFollowTarget == null || raycastOrigin == null || orbitalFollow == null) return;

        Vector3 direcaoPraCamera = (Camera.main.transform.position - raycastOrigin.position).normalized;

        float raioAlvo = raioNormal;
        Vector3 offsetAlvo = offsetNormal;

        // Se o SphereCast bater numa parede...
        if (Physics.SphereCast(raycastOrigin.position, rayThickness, direcaoPraCamera, out RaycastHit hit, raioNormal, wallLayer))
        {
            // Ativa o modo Ombro
            raioAlvo = raioParede;
            offsetAlvo = offsetParede;
        }

        // 1. Suaviza a distância da câmera (Radius)
        orbitalFollow.Radius = Mathf.Lerp(orbitalFollow.Radius, raioAlvo, Time.deltaTime * velocidadeSuavizacao);

        // 2. Suaviza a posição do Alvo da Câmera (movendo ele fisicamente para o ombro)
        cameraFollowTarget.localPosition = Vector3.Lerp(cameraFollowTarget.localPosition, offsetAlvo, Time.deltaTime * velocidadeSuavizacao);
    }
}
//CameraEvitaParede
