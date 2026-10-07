using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public TMP_InputField NomeDaSala;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void CreatRoom()
    {

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;
        PhotonNetwork.CreateRoom(NomeDaSala.text, roomOptions);

    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom(NomeDaSala.text);
            
    }

    public override void OnJoinedRoom()
    { 
        
        PhotonNetwork.LoadLevel("Main Park");
    }


    // Update is called once per frame
   
}
