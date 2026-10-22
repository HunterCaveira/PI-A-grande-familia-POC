using UnityEngine;

public class ControleCursor : MonoBehaviour
{
    private void Start()
    {
        // Trava e esconde o cursor para o jogador
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }
}