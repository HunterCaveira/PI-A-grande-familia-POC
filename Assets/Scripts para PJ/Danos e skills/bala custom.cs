using System.Collections;
using UnityEngine;

public class balacustom : MonoBehaviour

{

    [Tooltip("Tempo máximo de vida da bala caso ela não acerte nada (evita pesar o jogo).")]

    public float tempoVidaMaximo = 5f;



    private void Start()

    {

        // Garante que a bala seja deletada depois de alguns segundos se voar para o infinito

        //Destroy(gameObject, tempoVidaMaximo);
        StartCoroutine(StartDisableObject());
    }
    


    // Este método é chamado automaticamente pela Unity quando o Rigidbody da bala bate em qualquer Collider

    private void OnCollisionEnter(Collision collision)

    {

        // Se quiser que ela ignore o próprio jogador, você pode checar a Tag:

        // if (collision.gameObject.CompareTag("Player")) return;



        // Opcional: Criar um efeito de faísca ou poeira no ponto do impacto antes de sumir



        // Deleta o objeto da bala imediatamente !!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        

    }

    IEnumerator StartDisableObject() 
    {
        yield return new WaitForSeconds(tempoVidaMaximo);
        this.gameObject.SetActive(false);
    }

}

