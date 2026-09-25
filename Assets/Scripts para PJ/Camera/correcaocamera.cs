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

// Resolução conta: (leituraInput.x * transform.right) + (leituraInput.y * transform.forward);
// se eu apertar a tecla D ou a  SETA PARA DIREITA ele retorna um valor positivo em x = 1
// leituraInput. = 1
// transform.right = vector3 (1, 0, 0)
// leituraInput.x (1) x transform.right(1,0,0) = (1,0,0)

// se eu aperta a tecla W ou a SETA PARA CIMA ELE retorna um valor positivo em y = 1
// leituraInput.y = 1
// tranform.forward = Vector3 (0, 0, 1)
// leituraInput.y (1) X transdorm.forward (0, 0, 1) = (0, 0, 1)

//soma as duas partes
// (1, 0, 0,) + (0, 0, 1) = (1, 0, 1)
