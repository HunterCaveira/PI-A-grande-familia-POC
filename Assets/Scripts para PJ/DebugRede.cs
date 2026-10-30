using UnityEngine;
using Photon.Pun;

public class DebugRede : MonoBehaviour
{
    private void OnGUI()
    {
        GUIStyle estilo = new GUIStyle();
        estilo.fontSize = 24;
        estilo.normal.textColor = Color.yellow;
        estilo.fontStyle = FontStyle.Bold;

        if (PhotonNetwork.InRoom)
        {
            GUI.Label(new Rect(20, 20, 500, 100), $"SALA: {PhotonNetwork.CurrentRoom.Name} | JOGADORES CONECTADOS: {PhotonNetwork.CurrentRoom.PlayerCount}", estilo);
        }
        else
        {
            GUI.Label(new Rect(20, 20, 500, 100), "FORA DA SALA / DESCONECTADO", estilo);
        }
    }
}