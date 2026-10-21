using Photon.Pun;
using UnityEngine;

public class Andando : MonoBehaviour
{

    private CharacterController controller;

    private PhotonView view;

    public float playerSpeed = 5.0f;

        void Start()
    {

        controller = GetComponent<CharacterController>();   
        view = GetComponent<PhotonView>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (view.IsMine)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 direction = new Vector3(horizontal, 0f, vertical);

            if (direction.magnitude > 1)
                direction.Normalize();

            transform.Translate(direction * playerSpeed * Time.deltaTime, Space.World);
        }        
    }
}