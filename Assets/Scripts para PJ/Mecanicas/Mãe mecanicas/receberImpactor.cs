using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ReceptorDeImpacto : MonoBehaviour
{
    private CharacterController cc;
    private Vector3 velocidadeDeImpacto;

    [Tooltip("O quão rápido o personagem para de escorregar (Atrito)")]
    public float atrito = 3f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    // Essa função é chamada pela área de ataque (hitbox)
    public void ReceberImpulso(Vector3 direcao, float forca)
    {
        velocidadeDeImpacto += direcao * forca;
    }

    void Update()
    {
        // Se ainda houver força acumulada, move o personagem
        if (velocidadeDeImpacto.magnitude > 0.1f)
        {
            cc.Move(velocidadeDeImpacto * Time.deltaTime);

            // O Lerp reduz a força suavemente até zero, simulando o pé arrastando no chão
            velocidadeDeImpacto = Vector3.Lerp(velocidadeDeImpacto, Vector3.zero, atrito * Time.deltaTime);
        }
        else
        {
            velocidadeDeImpacto = Vector3.zero;
        }
    }
}