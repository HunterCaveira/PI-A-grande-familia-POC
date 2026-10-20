using UnityEngine;
using Photon.Pun;
using Unity.Cinemachine; // Namespace oficial do Cinemachine na Unity 6

public class FocarCameraMultiplayer : MonoBehaviourPun
{
    [Header("Configuração de Alvo")]
    [Tooltip("Arraste aqui o objeto que a câmera deve focar. Se deixar vazio, foca no próprio jogador.")]
    public Transform alvoDaCamera;

    void Start()
    {
        // A câmera SÓ foca se este boneco for o seu!
        if (photonView.IsMine)
        {
            if (alvoDaCamera == null)
            {
                alvoDaCamera = this.transform;
            }

            // Procura a câmera Cinemachine FreeLook na cena (Unity 6)
            CinemachineCamera cameraNaCena = FindFirstObjectByType<CinemachineCamera>();

            if (cameraNaCena != null)
            {
                // Conecta os alvos da câmera
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