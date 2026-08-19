using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem; //Adicionado para o novo Input System
public class HabilidadeParede : MonoBehaviour
{
    [Header("Configurações da Câmera e Raio")]
    [SerializeField] private Camera cameraPrincipal; // De onde sai o laser
    [SerializeField] private float distanciaMaxima = 15f;// alcance do laser em metros.
    [SerializeField] private LayerMask camadaChao;// Filtro para detectar APENAS o chão
    [SerializeField] private LayerMask camadaParedesEObstaculos; // Camada dos muros e portas do mapa

    [Header("Dimensionamento da Parede")]
    [SerializeField] private float larguraMaxima = 5f; //Largura total da paredeem campo aberto
    [SerializeField] private float larguraMinima = 1f; //Largura mínima para conseguir entrar em festas

    [Header("Teclas e Estado")]
    private bool modoPosicionamentoAtivo = false;// Controle interno do estado

    [Header("Prefabs da Parede")]
    [SerializeField] private GameObject prefabParedeFantasma; // Projeção transparente
    [SerializeField] private GameObject prefabParedeReal;// Parede Sólida Final9
    void Update()
    {
        //1. O INTERRUPTOR (Executa só no frame que apertou Q). Tinha feito com o sistema antigo agora mudei para o Novo.
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            // Inverte o estado (se era false vira true, se era true vira false)
            modoPosicionamentoAtivo = !modoPosicionamentoAtivo;

            if (prefabParedeFantasma != null)
            {
                // Liga ou desliga a parede fantasma no jogo
                prefabParedeFantasma.SetActive(modoPosicionamentoAtivo);

            }

        }

        // 2. O RRASTREAMENTO (Executa a cada frame ENQUANTO a habilidade estiver ativa)
        if (modoPosicionamentoAtivo)
        {
            PosicionarECriarParede();

        }
    }
    private void PosicionarECriarParede()
    {
        if (cameraPrincipal == null || prefabParedeFantasma == null) return;
        
            //Dispara do centro da tela (mira)
            Ray ray = cameraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit; // Guardará as informações de onde o laser bater

        // Se o laser rebater no chão dentro da distância máxima
        if (Physics.Raycast(ray, out hit, distanciaMaxima, camadaChao))
        {
            // 1 Move para o ponto do chão
            prefabParedeFantasma.transform.position = hit.point;

            //2. Adpta a largura de acordo com o espaço disponível (portas /corredoes)
            AjustarLarguraDaParede(prefabParedeFantasma.transform);

            // 3. CONFIRMAÇÃO (Clique Esquerdo do Mouse no Novo Input System)
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Instancia a parede real
                GameObject paredeCriada = Instantiate(prefabParedeReal, hit.point, prefabParedeFantasma.transform.rotation);

                //Aplica a mesma escala ajustada da parede fantasma na parede real
                paredeCriada.transform.localScale = prefabParedeFantasma.transform.localScale;

                //Desliga o modo posicionamento após colocar a parede
                modoPosicionamentoAtivo = false;
                prefabParedeFantasma.SetActive(false);

            }
        }
            

        
    }
    private void AjustarLarguraDaParede(Transform parede)
    {
        float raioBusca = larguraMaxima / 2f;

        float distanciaEsquerda = raioBusca;
        float distanciaDireita = raioBusca;

        //Eleva o ponto de Origem do raio em 1 metro para não raspar no chão
        Vector3 pontoOrigem = parede.position + Vector3.up * 1f;

        // Dispara raio para a esquerda da parede
        if (Physics.Raycast(parede.position, -parede.right, out RaycastHit hitEsquerda, raioBusca, camadaParedesEObstaculos))
        {

            distanciaEsquerda = hitEsquerda.distance;  

        }

        //Dispara raio para a direita da parede
        if (Physics.Raycast(parede.position, parede.right, out RaycastHit hitDireita, raioBusca, camadaParedesEObstaculos))
        { 
            distanciaDireita = hitDireita.distance;        
        
        }
        
        //Calcula a nova largura baseada no espaço livre encontrado nas duas direções
       float novaLargura = distanciaEsquerda + distanciaDireita;

        //Garante que a largura fique entre o mínimo e o máximo configurado
        novaLargura = Mathf.Clamp(novaLargura, larguraMinima, larguraMaxima);

        //Aplica a nova escala no eixo X (Largura da parede fantasma)
        Vector3 escalaAtual = parede.localScale;
        parede.localScale = new Vector3(novaLargura, escalaAtual.y, escalaAtual.z); 
      
    
    }


}

