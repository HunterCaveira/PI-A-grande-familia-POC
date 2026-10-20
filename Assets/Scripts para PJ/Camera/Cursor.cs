using UnityEngine;
using Photon.Pun; // Adicionado

// O NOME FOI ALTERADO: Lembre-se de renomear o arquivo .cs no Windows para "ControleCursor.cs"
public class ControleCursor : MonoBehaviourPun
{
    void Start()
    {
        // MULTIPLAYER: Somente o jogador local trava o próprio mouse
        if (!photonView.IsMine) return;

        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {

    }
}