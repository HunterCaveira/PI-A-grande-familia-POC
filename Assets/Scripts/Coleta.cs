using UnityEngine;
using Photon.Pun;

public class Coleta : MonoBehaviour
{
    public ScoreManager manager;

    private PhotonView view;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        view = GetComponent<PhotonView>();
        manager = GameObject.Find("ScoreManager").GetComponent<ScoreManager>();
        
    }

    public void OnTriggerEnter(Collider other)
    {

        if (other.tag == "Score")
        {

            manager.UpgradeScore();
            Destroy(other.gameObject);
        
        }

        if(other.tag =="Money")
        {
            if(view.IsMine)
            {
                manager.UpgradeMoney();
            }

            Destroy(other.gameObject);
        }


    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
