using UnityEngine;

public class GerenciadorDeEquipamentos : MonoBehaviour
{
    [Header("Estados das Habilidades")]
    [SerializeField] private bool habilitado = false;

    [Header("Tempo de animação")]
    [SerializeField] private float tempoAnimacaoTroca = 0.05f;
    private float tempoLiberadoParaTrocar = 0f;
    private float indiceEquipamentoAtual = 0f;

    //[Header("Prefabs das Habilidades")]
    //[SerializeField] private List<GameObject> Pai;
    
    


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time < tempoLiberadoParaTrocar) return;
        
    }
}
