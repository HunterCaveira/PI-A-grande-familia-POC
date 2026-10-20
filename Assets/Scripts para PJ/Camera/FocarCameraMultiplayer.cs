using UnityEngine;
using Photon.Pun;
#if CINEMACHINE_3_OR_NEWER || UNITY_6_0_OR_NEWER
using Unity.Cinemachine; // Namespace do Cinemachine 3 (Unity 6)
#else
using Cinemachine; // Namespace antigo
#endif

// Herda de MonoBehaviourPun para podermos usar o photonView.IsMine
public class FocarCameraMultiplayer : MonoBehaviourPun
{
    [Header("Configuração de Alvo")]
    [Tooltip("Arraste aqui o objeto que a câmera deve focar (ex: o próprio Pai ou um ponto no peito/cabeça). Se deixar vazio, foca no pé.")]
    public Transform alvoDaCamera;

    void Start()
    {
        // REGRA DE OURO DO MULTIPLAYER: A câmera SÓ foca se este boneco for O SEU!
        if (photonView.IsMine)
        {
            // Se você esqueceu de arrastar um alvo no Inspector, ele foca no próprio objeto do script
            if (alvoDaCamera == null)
            {
                alvoDaCamera = this.transform;
            }

            // Procura a sua câmera FreeLook que está solta na cena
            CinemachineFreeLook cameraNaCena = FindFirstObjectByType<CinemachineFreeLook>();

            if (cameraNaCena != null)
            {
                // Conecta o Tracking Target (Follow e LookAt) da câmera ao seu boneco
                cameraNaCena.Follow = alvoDaCamera;
                cameraNaCena.LookAt = alvoDaCamera;

                Debug.Log("<color=green>[CÂMERA] Câmera conectada com sucesso ao jogador local!</color>");
            }
            else
            {
                Debug.LogWarning("<color=orange>[CÂMERA] Nenhuma Cinemachine FreeLook encontrada na cena Main Park!</color>");
            }
        }
    }
}