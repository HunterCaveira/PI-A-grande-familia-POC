using UnityEngine;

// Isso garante que a Unity exija um CharacterController neste objeto
[RequireComponent(typeof(CharacterController))]
public class ReceptorDeImpacto : MonoBehaviour
{
    private CharacterController cc;
    private Vector3 velocidadeDeImpacto;

    [Tooltip("O quão rápido o personagem para de escorregar após o impacto")]
    public float atrito = 3f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    // Essa função é chamada pelo objeto externo!
    public void ReceberImpulso(Vector3 direcao, float forca)
    {
        // Adiciona a força à velocidade atual de impacto
        velocidadeDeImpacto += direcao * forca;
    }

    void Update()
    {
        // Só tenta mover se houver alguma força de impacto sobrando
        if (velocidadeDeImpacto.magnitude > 0.1f)
        {
            // Move o Character Controller usando a velocidade do impacto
            cc.Move(velocidadeDeImpacto * Time.deltaTime);

            // Vai diminuindo a velocidade aos poucos para simular o atrito com o chão
            velocidadeDeImpacto = Vector3.Lerp(velocidadeDeImpacto, Vector3.zero, atrito * Time.deltaTime);
        }
        else
        {
            // Zera completamente quando a força for muito pequena
            velocidadeDeImpacto = Vector3.zero;
        }
    }
}