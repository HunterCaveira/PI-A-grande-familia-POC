/* using UnityEngine;

public class HabilidadeCinto : MonoBehaviour, IHabilidade
{
    [Header("Configurações da Câmera e Raio")]
    [SerializeField] private Camera cameraPrincipal;
    [SerializeField] private float distanciaMaxima = 5f;
    

    [Header("Controle de Tempo")]
    [SerializeField] private float tempoCooldown = 1.0f;
    private float tempoProximoUso = 0f;

    [Header("Controle De Referencia ")]
    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;

    [Header("Teclas e Estado")]
    private bool modoPosicionamentoAtivo = false;

    [Header("Prefab_Cinto")]
    [SerializeField] private Transform fivelaPontos;
    [SerializeField] private GameObject prefabCinto;

    [Header("Filtragem de inimigos")]
    [SerializeField] private int idDoTime = 1;
    [SerializeField] private Transform pontoDestinoPuxao;
    [SerializeField] private float velocidadePuxao = 15f;

    public int IdDoTime => idDoTime;
    private Transform inimigoCapturado;
    public int timeAlvo;

    private void Awake()
    {
        gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
    }
    private void DisparodaHabilidade()
    {

       

        if (cameraPrincipal == null) return;

        Ray ray = cameraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        

        if (Physics.Raycast(ray, out RaycastHit hit, distanciaMaxima))
        { 
            HabilidadeCinto timeAlvo = hit.collider.GetComponent<HabilidadeCinto>();
        
        }

       if (timeAlvo != null && timeAlvo.IdDotime != this.idDoTime)
       {
            inimigoCapturado = hit.transform;
            tempoProximoUso = Time.time + tempoCooldown;
              
        }

        if (gerenciadorDeEquipamentos.TocarEquipamento(0));


    }

    private void Update()
    {

        if (!inimigoCapturado != null)
        {
            float distanciaAtual = Vector3.Distance(pontoDestinoPuxao.position, inimigoCapturado.position);
            prefabCinto.transform.localScale = new Vector3(1, 1, distanciaAtual);

            inimigoCapturado.position = Vector3.MoveTowards(inimigoCapturado.position, pontoDestinoPuxao.position, velocidadePuxao * Time.deltaTime);

        }

        if (distanciaMaxima <= 0.5f)
        { 
            gerenciadorDeEquipamentos.TrocarEquipamento(0);
        
        }


    }
    // --- MÉTODOS DA INTERFACE (Contratados pelo Gerenciador) ---
    public void AtivarHabilidade()
    {
        // Checa cooldown antes de ativar
        if (Time.time < tempoProximoUso) return;

        if (prefabCinto != null)
        {
            prefabCinto.SetActive(true);
        }
        DisparodaHabilidade();
    }


    public void CancelarHabilidade()
    {
        modoPosicionamentoAtivo = false;

        if (prefabCinto != null)
        {
            prefabCinto.SetActive(false);
        }
    }


}
*/