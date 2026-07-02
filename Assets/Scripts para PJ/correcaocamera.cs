using UnityEngine;

public class correcalcamera : MonoBehaviour
{

    // varivel declarada para Rotação da camera.
    [SerializeField] private float velocidadeRotacao;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {




    }

    // Update is called once per frame
    void Update()
    {

        Vector3 camFrente = Camera.main.transform.forward;
        camFrente.y = 0;

        if (camFrente.sqrMagnitude < 0.0001f)
            return;

        Quaternion rotacaoAlvo = Quaternion.LookRotation(camFrente);

        transform.rotation = Quaternion.Lerp(transform.rotation, rotacaoAlvo, velocidadeRotacao * Time.deltaTime);

    }
}
