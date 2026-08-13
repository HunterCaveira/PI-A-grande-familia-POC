/*using Unity.VisualScripting;
using UnityEngine;

[Header("Configurações da Câmera e Raio")]
[SerializeField] private Camera cameraPrincipal; // De onde sai o laser
[SerializeField] private float distanciaMaximo =  15f;// alcance do laser em metros.
[SerializeField] private LayerMask camadaChao;// Filtro para detectar APENAS o chão

[Header("Teclas e Estado")]
[SerializeField] private KeyCode teclaHabilidade = KeyCode.Q;// Tecla para ativar/desativar
private bool modoPosicionamentoAtivo = false;// Controle interno do estado

[Header("Prefabs da Parede")]
[SerializeField] private GameObject prefabParedeFantasma; // Projeção transparente
[SerializeField] private GameObject prefabParedeReal;// Parede Sólida Final9

public class HabilidadeParede : ScriptableObject
{

    void Update()
    {
        if (Input.GetKeyDown(teclaHabilidade))
        {
            modoPosicionamentoAtivo = true; 
        }
       

        if (modoPosicionamentoAtivo)
        {

            modoPosicionamentoAtivo = !modoPosicionamentoAtivo;


        }

        



    }





}
*/