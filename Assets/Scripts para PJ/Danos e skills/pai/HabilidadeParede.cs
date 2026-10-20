using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Photon.Pun; // Importa a API do Photon

public class HabilidadeParede : MonoBehaviourPun, IHabilidade, IasUI
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
    [Tooltip("Nome EXATO do prefab da Parede Real que está salvo dentro da pasta Resources!")]
    [SerializeField] private string nomePrefabParedeReal = "ParedeReal";

    [Header("Regras de Uso (Cooldown e Limites)")]
    [SerializeField] private float tempoDeVida = 10f;
    [SerializeField] private int limiteParedes = 2;
    [SerializeField] private float tempoCooldown = 5f;

    [Header("Configuração do Slot")]
    [SerializeField] private int slotIndex = 1; // 1 = Q, 2 = E, 3 = F
    public int SlotIndex => slotIndex;

    [Header("Imagens da UI")]
    [SerializeField] private Sprite fotoPronta;
    [SerializeField] private Sprite fotoRecarregando;
    public Sprite FotoPronta => fotoPronta;
    public Sprite FotoRecarregando => fotoRecarregando;

    [Header("Tempo de Recarga para UI")]
    [SerializeField] private float tempoDeRecarga = 4f;
    private float tempoProximouso = 0f;
    public float TempoProximouso => tempoProximoUso;

    private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;
    private float tempoProximoUso = 0f;
    private List<GameObject> paredesAtivas = new List<GameObject>();

    private void Awake()
    {
        gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
        if (cameraPrincipal == null) cameraPrincipal = Camera.main;
    }

    void Update()
    {
        // Garante que apenas o jogador local gerencie a mira e colocação da parece fantasma
        if (!photonView.IsMine) return;

        if (modoPosicionamentoAtivo)
        {
            PosicionarECriarParede();
        }
    }

    // --- MÉTODOS DA INTERFACE IHABILIDADE ---
    public bool PodeUsar()
    {
        return Time.time >= tempoProximouso;
    }

    public bool EstaPronta()
    {
        return Time.time >= tempoProximoUso;
    }

    public void AtivarHabilidade()
    {
        if (!photonView.IsMine) return;

        if (!EstaPronta())
        {
            if (gerenciadorDeEquipamentos != null)
            {
                gerenciadorDeEquipamentos.TrocarEquipamento(0);
            }
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
        if (!photonView.IsMine) return;

        modoPosicionamentoAtivo = false;

        if (prefabParedeFantasma != null)
        {
            prefabParedeFantasma.SetActive(false);
        }
    }

    // --- LÓGICA DE POSICIONAMENTO E CRIAÇÃO ---

    private void PosicionarECriarParede()
    {
        if (cameraPrincipal == null || prefabParedeFantasma == null) return;

        Ray ray = cameraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, distanciaMaxima, camadaChao))
        {
            if (!prefabParedeFantasma.activeSelf)
            {
                prefabParedeFantasma.SetActive(true);
            }

            AjustarLarguraDaParede(prefabParedeFantasma.transform);

            float metadeDaAltura = prefabParedeFantasma.transform.localScale.y / 2f;
            Vector3 posicaoAjustadaNoChao = hit.point + Vector3.up * metadeDaAltura;

            prefabParedeFantasma.transform.position = posicaoAjustadaNoChao;

            // Confirmação de criação ao clicar com o Botão Direito do Mouse
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                paredesAtivas.RemoveAll(parede => parede == null);

                if (paredesAtivas.Count >= limiteParedes)
                {
                    if (paredesAtivas[0] != null)
                    {
                        PhotonNetwork.Destroy(paredesAtivas[0]);
                    }
                    paredesAtivas.RemoveAt(0);
                }

                // INSTANCIAÇÃO REDE: Cria o prefab carregado da pasta Resources/
                GameObject paredeCriada = PhotonNetwork.Instantiate(
                    nomePrefabParedeReal,
                    posicaoAjustadaNoChao,
                    prefabParedeFantasma.transform.rotation
                );

                // Aplica a escala redimensionada e envia RPC para os outros jogadores atualizarem o tamanho
                Vector3 escalaCalculada = prefabParedeFantasma.transform.localScale;
                PhotonView viewParede = paredeCriada.GetComponent<PhotonView>();

                if (viewParede != null)
                {
                    viewParede.RPC("SincronizarEscalaParedeRPC", RpcTarget.AllBuffered, escalaCalculada);
                }

                paredesAtivas.Add(paredeCriada);

                // Aplica Cooldown
                tempoProximoUso = Time.time + tempoCooldown;

                // Cancela o modo fantasma
                CancelarHabilidade();

                // Retorna para a arma principal (Slot 0)
                if (gerenciadorDeEquipamentos != null)
                {
                    gerenciadorDeEquipamentos.TrocarEquipamento(0);
                }
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