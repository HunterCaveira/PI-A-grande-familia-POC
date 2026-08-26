using UnityEngine;
using UnityEngine.InputSystem; // Adicionado para suporte ao Novo Input System
using System.Collections.Generic; // Necessário para usar List<>

public class HabilidadeParede : MonoBehaviour
{
    [Header("Configurações da Câmera e Raio")]
    [SerializeField] private Camera cameraPrincipal; // Referência da câmera para lançar o laser da mira
    [SerializeField] private float distanciaMaxima = 15f; // Alcance máximo do laser em metros
    [SerializeField] private LayerMask camadaChao; // Filtro para detectar APENAS o chão no Raycast
    [SerializeField] private LayerMask camadaParedesEObstaculos; // Camada dos muros, tetos e portas do mapa

    [Header("Dimensionamento da Parede")]
    [SerializeField] private float larguraMaxima = 5f; // Largura total da parede em campo aberto
    [SerializeField] private float larguraMinima = 1f; // Largura mínima para conseguir entrar em frestas
    [SerializeField] private float alturaMaxima = 4f; // Altura máxima baseada no tamanho do personagem
    [SerializeField] private float alturaMinima = 0.5f; // Altura mínima para a parede não sumir sob tetos baixos

    [Header("Teclas e Estado")]
    private bool modoPosicionamentoAtivo = false; // Controle interno do estado da habilidade

    [Header("Prefabs da Parede")]
    [SerializeField] private GameObject prefabParedeFantasma; // Projeção transparente para visualização
    [SerializeField] private GameObject prefabParedeReal; // Parede sólida final instanciada no mapa

    [Header("Regras de Uso (Cooldown e Limites)")]
    [SerializeField] private float tempoDeVida = 10f; // Tempo em segundos para a parede sumir
    [SerializeField] private int limiteParedes = 2; // Quantidade máxima de paredes no mapa
    [SerializeField] private float tempoCooldown = 5f; // Tempo de espera para reusar a habilidade

    private float tempoProximoUso = 0f; // Guarda o momento do tempo em que o Q poderá ser usado
    private List<GameObject> paredesAtivas = new List<GameObject>(); // Fila de paredes vivas

    void Update()
    {
        // 1. O INTERRUPTOR COM COOLDOWN: Só permite ativar se o tempo atual for maior ou igual ao tempo liberado
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame && Time.time >= tempoProximoUso)
        {
            // Inverte o estado atual (true vira false, false vira true)
            modoPosicionamentoAtivo = !modoPosicionamentoAtivo;

            if (prefabParedeFantasma != null)
            {
                // Liga ou desliga a parede fantasma no jogo
                prefabParedeFantasma.SetActive(modoPosicionamentoAtivo);
            }
        }

        // 2. O RASTREAMENTO: Atualiza o posicionamento enquanto a habilidade estiver ativa
        if (modoPosicionamentoAtivo)
        {
            PosicionarECriarParede();
        }
    }

    private void PosicionarECriarParede()
    {
        if (cameraPrincipal == null || prefabParedeFantasma == null) return;

        // Dispara o raio a partir do centro exato da tela (mira)
        Ray ray = cameraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit; // Guardará as informações do impacto no chão

        // Se o laser rebater no chão dentro da distância máxima
        if (Physics.Raycast(ray, out hit, distanciaMaxima, camadaChao))
        {
            // 1. Adapta as dimensões (largura e altura) da parede fantasma primeiro
            AjustarLarguraDaParede(prefabParedeFantasma.transform);

            // 2. Eleva a parede em metade da sua altura final para alinhar o pivô central ao piso
            float metadeDaAltura = prefabParedeFantasma.transform.localScale.y / 2f;
            Vector3 posicaoAjustadaNoChao = hit.point + Vector3.up * metadeDaAltura;

            // 3. Move a parede fantasma para a posição suspensa correta
            prefabParedeFantasma.transform.position = posicaoAjustadaNoChao;

            // 4. CONFIRMAÇÃO: Instancia a parede real ao clicar com o botão direito do mouse
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                // A. Limpa da lista paredes que já foram destruídas pelo tempo
                paredesAtivas.RemoveAll(parede => parede == null);

                // B. Se atingiu o limite máximo, destrói a parede mais antiga (a primeira da fila)
                if (paredesAtivas.Count >= limiteParedes)
                {
                    Destroy(paredesAtivas[0]); // Destrói no jogo
                    paredesAtivas.RemoveAt(0);  // Remove da lista
                }

                // C. Instancia a parede real na posição e rotação calculadas
                GameObject paredeCriada = Instantiate(prefabParedeReal, posicaoAjustadaNoChao, prefabParedeFantasma.transform.rotation);
                paredeCriada.transform.localScale = prefabParedeFantasma.transform.localScale;

                // D. Adiciona a nova parede à lista de controle
                paredesAtivas.Add(paredeCriada);

                // E. Programa a auto-destruição automática após X segundos
                Destroy(paredeCriada, tempoDeVida);

                // F. Inicia o Cooldown (tempo atual + tempo de espera)
                tempoProximoUso = Time.time + tempoCooldown;

                // G. Desliga o modo posicionamento e esconde a projeção fantasma
                modoPosicionamentoAtivo = false;
                prefabParedeFantasma.SetActive(false);
            }
        }
    }

    private void AjustarLarguraDaParede(Transform parede)
    {
        float raioBusca = larguraMaxima / 2f;
        float distanciaTeto = alturaMaxima; // Valor padrão caso não haja obstrução acima
        float distanciaEsquerda = raioBusca;
        float distanciaDireita = raioBusca;

        // Ponto de origem dos raios laterais elevado para não raspar no chão
        Vector3 pontoOrigem = parede.position + Vector3.up * 0.5f;

        // 1. Dispara raio para CIMA a partir do chão para medir a altura livre até o teto
        if (Physics.Raycast(parede.position, Vector3.up, out RaycastHit hitTeto, alturaMaxima, camadaParedesEObstaculos))
        {
            distanciaTeto = hitTeto.distance; // Captura a distância exata até a colisão do teto
        }

        // Limita a altura calculada entre o valor mínimo e máximo
        float novaAltura = Mathf.Clamp(distanciaTeto, alturaMinima, alturaMaxima);

        // 2. Dispara raio para a ESQUERDA da parede
        if (Physics.Raycast(pontoOrigem, -parede.right, out RaycastHit hitEsquerda, raioBusca, camadaParedesEObstaculos))
        {
            distanciaEsquerda = hitEsquerda.distance;
        }

        // 3. Dispara raio para a DIREITA da parede
        if (Physics.Raycast(pontoOrigem, parede.right, out RaycastHit hitDireita, raioBusca, camadaParedesEObstaculos))
        {
            distanciaDireita = hitDireita.distance;
        }

        // Calcula a nova largura combinada e limita entre o mínimo e o máximo
        float novaLargura = Mathf.Clamp(distanciaEsquerda + distanciaDireita, larguraMinima, larguraMaxima);

        // Aplica a nova escala final nos eixos X (largura) e Y (altura)
        Vector3 escalaAtual = parede.localScale;
        parede.localScale = new Vector3(novaLargura, novaAltura, escalaAtual.z);
    }

    public void CancelarPosicionamento()
    {
        prefabParedeFantasma.SetActive(false); 
        modoPosicionamentoAtivo = false;


    }



}