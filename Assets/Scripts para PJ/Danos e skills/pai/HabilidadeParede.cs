using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HabilidadeParede : MonoBehaviour, IHabilidade, IasUI
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

    private bool modoPosicionamentoAtivo = false;

    [Header("Prefabs da Parede")]
    [SerializeField] private GameObject prefabParedeFantasma;
    [Tooltip("Arraste diretamente o Prefab da Parede Real aqui")]
    [SerializeField] private GameObject prefabParedeRealObjeto;
    [SerializeField] private string nomePrefabParedeReal = "ParedeReal";

    [Header("Regras de Uso")]
    [SerializeField] private float tempoDeVida = 10f;
    [SerializeField] private int limiteParedes = 2;
    [SerializeField] private float tempoCooldown = 5f;

    [Header("Configuração do Slot UI")]
    [SerializeField] private int slotIndex = 1;
    public int SlotIndex => slotIndex;

    [SerializeField] private Sprite fotoPronta;
    [SerializeField] private Sprite fotoRecarregando;
    public Sprite FotoPronta => fotoPronta;
    public Sprite FotoRecarregando => fotoRecarregando;

    [SerializeField] private float tempoDeRecarga = 4f;
    private float tempoProximouso = 0f;
    public float TempoProximouso => tempoProximoUso;

    private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;
    private float tempoProximoUso = 0f;
    private List<GameObject> paredesAtivas = new List<GameObject>();

    private void Awake()
    {
        gerenciadorDeEquipamentos = GetComponentInParent<GerenciadorDeEquipamentos>();
        if (cameraPrincipal == null) cameraPrincipal = Camera.main;

        // Desativa colisão da parede fantasma para não bloquear o Raycast do mouse
        if (prefabParedeFantasma != null)
        {
            Collider col = prefabParedeFantasma.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    private void Update()
    {
        if (modoPosicionamentoAtivo)
        {
            PosicionarECriarParede();
        }
    }

    public bool PodeUsar() => Time.time >= tempoProximouso;
    public bool EstaPronta() => Time.time >= tempoProximoUso;

    public void AtivarHabilidade()
    {
        if (!EstaPronta())
        {
            if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0);
            return;
        }

        modoPosicionamentoAtivo = !modoPosicionamentoAtivo;

        if (prefabParedeFantasma != null)
        {
            prefabParedeFantasma.SetActive(modoPosicionamentoAtivo);
        }

        if (PodeUsar())
        {
            tempoProximouso = Time.time + tempoDeRecarga;
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

    private void PosicionarECriarParede()
    {
        if (cameraPrincipal == null) cameraPrincipal = Camera.main;
        if (cameraPrincipal == null || prefabParedeFantasma == null) return;

        Ray ray = cameraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        // Realiza o Raycast ignorando Triggers
        if (Physics.Raycast(ray, out RaycastHit hit, distanciaMaxima, camadaChao, QueryTriggerInteraction.Ignore))
        {
            if (!prefabParedeFantasma.activeSelf)
            {
                prefabParedeFantasma.SetActive(true);
            }

            AjustarLarguraDaParede(prefabParedeFantasma.transform);

            float metadeDaAltura = prefabParedeFantasma.transform.localScale.y / 2f;
            Vector3 posicaoAjustadaNoChao = hit.point + Vector3.up * metadeDaAltura;

            prefabParedeFantasma.transform.position = posicaoAjustadaNoChao;

            // Aceita o clique com Botão Esquerdo ou Botão Direito do Mouse para confirmar
            bool clicou = (Mouse.current != null && (Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame));

            if (clicou)
            {
                CriarParedeReal(posicaoAjustadaNoChao, prefabParedeFantasma.transform.rotation, prefabParedeFantasma.transform.localScale);
            }
        }
        else
        {
            if (prefabParedeFantasma.activeSelf)
            {
                prefabParedeFantasma.SetActive(false);
            }
        }
    }

    private void CriarParedeReal(Vector3 posicao, Quaternion rotacao, Vector3 escala)
    {
        GameObject modeloParaInstanciar = prefabParedeRealObjeto;

        if (modeloParaInstanciar == null)
        {
            modeloParaInstanciar = Resources.Load<GameObject>(nomePrefabParedeReal);
        }

        if (modeloParaInstanciar == null)
        {
            Debug.LogError($"[HabilidadeParede] ERRO: Não foi possível encontrar o prefab da parede real! Arraste o Prefab para o campo 'Prefab Parede Real Objeto' no Inspector ou salve-o em 'Assets/Resources/{nomePrefabParedeReal}'.");
            return;
        }

        paredesAtivas.RemoveAll(parede => parede == null);

        if (paredesAtivas.Count >= limiteParedes)
        {
            if (paredesAtivas[0] != null) Destroy(paredesAtivas[0]);
            paredesAtivas.RemoveAt(0);
        }

        GameObject paredeCriada = Instantiate(modeloParaInstanciar, posicao, rotacao);
        paredeCriada.transform.localScale = escala;
        paredesAtivas.Add(paredeCriada);

        Destroy(paredeCriada, tempoDeVida);

        tempoProximoUso = Time.time + tempoCooldown;
        CancelarHabilidade();

        if (gerenciadorDeEquipamentos != null)
        {
            gerenciadorDeEquipamentos.TrocarEquipamento(0);
        }

        Debug.Log("[HabilidadeParede] Parede real posicionada na cena com sucesso!");
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