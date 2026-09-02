using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class HabilidadeParede : MonoBehaviour, IHabilidade
{
    [Header("Configurações da Câmera e Raio")]
    [SerializeField] private Camera cameraPrincipal;
    [SerializeField] private float distanciaMaxima = 15f;
    [SerializeField] private LayerMask camadaChao;
    [SerializeField] private LayerMask camadaParedesEObstaculos;

    [Header("Dimensionamento da Parede")]
    [SerializeField] private float larguraMaxima = 5f;
    [SerializeField] private float larguraMinima = 1f;
    [SerializeField] private float alturaMaxima = 4f;
    [SerializeField] private float alturaMinima = 0.5f;

    [Header("Teclas e Estado")]
    private bool modoPosicionamentoAtivo = false;

    [Header("Prefabs da Parede")]
    [SerializeField] private GameObject prefabParedeFantasma;
    [SerializeField] private GameObject prefabParedeReal;

    [Header("Regras de Uso (Cooldown e Limites)")]
    [SerializeField] private float tempoDeVida = 10f;
    [SerializeField] private int limiteParedes = 2;
    [SerializeField] private float tempoCooldown = 5f;

    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;

    private float tempoProximoUso = 0f;
    private List<GameObject> paredesAtivas = new List<GameObject>();

    void Update()
    {
        // O Update agora SÓ cuida de rastrear a posição no chão se o posicionamento estiver ativo
        if (modoPosicionamentoAtivo)
        {
            PosicionarECriarParede();
        }
    }
    private void Awake()
    {
        // Busca o gerenciador no mesmo GameObject do jogador
        gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
    }


    // --- MÉTODOS DA INTERFACE (Contratados pelo Gerenciador) ---

    public void AtivarHabilidade()
    {
        // Checa cooldown antes de ativar
        if (Time.time < tempoProximoUso) return;

        modoPosicionamentoAtivo = !modoPosicionamentoAtivo;

        if (prefabParedeFantasma != null)
        {
            prefabParedeFantasma.SetActive(modoPosicionamentoAtivo);
        }
    }

    public void CancelarHabilidade()
    {
        modoPosicionamentoAtivo = false;

        if (prefabParedeFantasma != null)
        {
            prefabParedeFantasma.SetActive(false);
        }
    }

    // --- LÓGICA INTERNA DE POSICIONAMENTO ---

    private void PosicionarECriarParede()
    {
        if (cameraPrincipal == null || prefabParedeFantasma == null) return;

        Ray ray = cameraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, distanciaMaxima, camadaChao))
        {
            // Se o raio encontrou o chão e o objeto fantasma estava desativado, reativa ele
            if (!prefabParedeFantasma.activeSelf)
            {
                prefabParedeFantasma.SetActive(true);
            }

            AjustarLarguraDaParede(prefabParedeFantasma.transform);

            float metadeDaAltura = prefabParedeFantasma.transform.localScale.y / 2f;
            Vector3 posicaoAjustadaNoChao = hit.point + Vector3.up * metadeDaAltura;

            prefabParedeFantasma.transform.position = posicaoAjustadaNoChao;

            // Confirmação com Botão Direito do Mouse
            // CONFIRMAÇÃO: Instancia a parede real ao clicar com o botão direito do mouse
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                // A. Limpa da lista paredes destruídas
                paredesAtivas.RemoveAll(parede => parede == null);

                // B. Destrói a parede antiga se atingiu o limite
                if (paredesAtivas.Count >= limiteParedes)
                {
                    Destroy(paredesAtivas[0]);
                    paredesAtivas.RemoveAt(0);
                }

                // C. Instancia a parede sólida real
                GameObject paredeCriada = Instantiate(prefabParedeReal, posicaoAjustadaNoChao, prefabParedeFantasma.transform.rotation);
                paredeCriada.transform.localScale = prefabParedeFantasma.transform.localScale;

                // D. Adiciona à lista e programa autodestruição
                paredesAtivas.Add(paredeCriada);
                Destroy(paredeCriada, tempoDeVida);

                // E. Aplica o cooldown
                tempoProximoUso = Time.time + tempoCooldown;

                // F. Cancela a projeção fantasma
                CancelarHabilidade();

                // G. RETORNO AUTOMÁTICO PARA A ARMA (ÍNDICE 0)
                if (gerenciadorDeEquipamentos != null)
                {
                    gerenciadorDeEquipamentos.TrocarEquipamento(0);
                }
            }
        }
        else
        {
            // Se o raio da mira for para o céu/fora do chão, oculta a fantasma para não flutuar
            if (prefabParedeFantasma.activeSelf)
            {
                prefabParedeFantasma.SetActive(false);
            }
        }
    }

    private void AjustarLarguraDaParede(Transform parede)
    {
        float raioBusca = larguraMaxima / 2f;
        float distanciaTeto = alturaMaxima;
        float distanciaEsquerda = raioBusca;
        float distanciaDireita = raioBusca;

        Vector3 pontoOrigem = parede.position + Vector3.up * 0.5f;

        if (Physics.Raycast(parede.position, Vector3.up, out RaycastHit hitTeto, alturaMaxima, camadaParedesEObstaculos))
        {
            distanciaTeto = hitTeto.distance;
        }

        float novaAltura = Mathf.Clamp(distanciaTeto, alturaMinima, alturaMaxima);

        if (Physics.Raycast(pontoOrigem, -parede.right, out RaycastHit hitEsquerda, raioBusca, camadaParedesEObstaculos))
        {
            distanciaEsquerda = hitEsquerda.distance;
        }

        if (Physics.Raycast(pontoOrigem, parede.right, out RaycastHit hitDireita, raioBusca, camadaParedesEObstaculos))
        {
            distanciaDireita = hitDireita.distance;
        }

        float novaLargura = Mathf.Clamp(distanciaEsquerda + distanciaDireita, larguraMinima, larguraMaxima);

        Vector3 escalaAtual = parede.localScale;
        parede.localScale = new Vector3(novaLargura, novaAltura, escalaAtual.z);
    }
}