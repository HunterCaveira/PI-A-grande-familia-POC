using UnityEngine;
using Photon.Pun;
using Unity.Cinemachine;

public class FocarCameraMultiplayer : MonoBehaviourPun
{
    [Header("Configuração de Alvo")]
    [Tooltip("Arraste aqui o objeto que a câmera deve focar. Se deixar vazio, foca no próprio jogador.")]
    public Transform alvoDaCamera;

    void Start()
    {
        // Perfeito! O IsMine já está aqui.
        if (photonView.IsMine)
        {
            if (alvoDaCamera == null)
            {
                alvoDaCamera = this.transform;
            }

            CinemachineCamera cameraNaCena = FindFirstObjectByType<CinemachineCamera>();

            if (cameraNaCena != null)
            {
                cameraNaCena.Follow = alvoDaCamera;
                cameraNaCena.LookAt = alvoDaCamera;

                Debug.Log("<color=green>[CÂMERA] Câmera conectada com sucesso ao jogador local!</color>");
            }
            else
            {
                Debug.LogWarning("<color=orange>[CÂMERA] Nenhuma Cinemachine Camera encontrada na cena Main Park!</color>");
            }
        }
    }
}