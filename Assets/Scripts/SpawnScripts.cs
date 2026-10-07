using UnityEngine;
using Photon.Pun;

public class SpawnScripts : MonoBehaviour
{
    public GameObject player;
    public Transform Spawns;

    public GameObject PlayerLUA, PlayerSOL, PlayerDark;

    public CharactersSkin manager;

    public int Skin;

    private void Start()
    {
        manager = GameObject.Find("GameManager").GetComponent<CharactersSkin>();

        Skin = manager.Skin;

        

        switch (Skin)
        {
           case 0:
                PhotonNetwork.Instantiate(PlayerLUA.name, Spawns.position, Quaternion.identity);
                break;
           case 1:
                PhotonNetwork.Instantiate(PlayerSOL.name, Spawns.position, Quaternion.identity);
                break;
            case 2:
                PhotonNetwork.Instantiate(PlayerDark.name, Spawns.position, Quaternion.identity);
                break;


        }

        PhotonNetwork.Instantiate(player.name, Spawns.position, Quaternion.identity);


    }

        
}
