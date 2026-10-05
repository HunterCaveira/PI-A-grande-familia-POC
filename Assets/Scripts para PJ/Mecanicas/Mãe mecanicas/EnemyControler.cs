using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private CharacterController characterController;
    private bool estaSendoEmpurrado = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    // Método chamado pela Hitbox para aplicar o empurrão
    public void ReceberEmpurrao(Vector3 direcao, float forca, float duracao)
    {
        if (characterController != null)
        {
            StartCoroutine(AplicarEmpurraoRoutine(direcao, forca, duracao));
        }
    }

    private IEnumerator AplicarEmpurraoRoutine(Vector3 direcao, float forca, float duracao)
    {
        estaSendoEmpurrado = true;
        float tempoDecorrido = 0f;

        // Ignora a gravidade/movimento normal do inimigo durante o empurrão (opcional)
        while (tempoDecorrido < duracao)
        {
            // Diminui a força gradualmente para o empurrão ficar suave (fade out)
            float fatorForca = Mathf.Lerp(forca, 0f, tempoDecorrido / duracao);

            // Move o CharacterController na direção do empurrão
            characterController.Move(direcao * fatorForca * Time.deltaTime);

            tempoDecorrido += Time.deltaTime;
            yield return null; // Espera o próximo frame
        }

        estaSendoEmpurrado = false;
    }
}