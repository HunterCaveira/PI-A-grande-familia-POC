// Importa a biblioteca base da Unity
using UnityEngine;
// Importa o novo sistema de inputs da Unity (teclado/rato)
using UnityEngine.InputSystem;
// Importa o sistema de Listas (coleções de dados)
using System.Collections.Generic;
// Importa a biblioteca do Photon para funções multiplayer (rede)
using Photon.Pun;

// A classe herda de MonoBehaviourPun (para ter acesso ao photonView) e usa interfaces da Habilidade e UI
public class HabilidadeParede : MonoBehaviourPun, IHabilidade, IasUI
{
    [Header("Configurações da Câmera e Raio")]
    private Camera cameraPrincipal;
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
    [SerializeField] private string nomePrefabRede = "ParedeReal";

    [Header("Regras de Uso (Cooldown e Limites)")]
    [SerializeField] private float tempoDeVida = 10f;
    [SerializeField] private int limiteParedes = 2;
    [SerializeField] private float tempoCooldown = 5f;

    [Header("Configuração do Slot")]
    [SerializeField] private int slotIndex = 1;
    public int SlotIndex => slotIndex;

    [Header("Imagens da UI")]
    [SerializeField] private Sprite fotoPronta;
    [SerializeField] private Sprite fotoRecarregando;
    public Sprite FotoPronta => fotoPronta;
    public Sprite FotoRecarregando => fotoRecarregando;

    [Header("Tempo de Recarga para UI")]
    private float tempoProximouso = 0f;
    public float TempoProximouso => tempoProximouso;

    private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;
    private List<GameObject> paredesAtivas = new List<GameObject>();

    private void Awake()
    {
        gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
        cameraPrincipal = Camera.main;

        if (cameraPrincipal == null)
        {
            Debug.LogError("[Habilidade Parede] Nenhuma câmera com a tag 'MainCamera' foi encontrada na cena!");
        }
    }

    void Update()
    {
        if (photonView != null && !photonView.IsMine) return;

        if (modoPosicionamentoAtivo)
        {
            PosicionarECriarParede();
        }
    }

    public bool PodeUsar()
    {
        return Time.time >= tempoProximouso;
    }

    public bool EstaPronta()
    {
        return Time.time >= tempoProximouso;
    }

    public void AtivarHabilidade()
    {
        if (photonView != null && !photonView.IsMine) return;

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

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                paredesAtivas.RemoveAll(parede => parede == null);

                if (paredesAtivas.Count >= limiteParedes)
                {
                    if (paredesAtivas[0] != null)
                    {
                        PhotonView pvParedeAntiga = paredesAtivas[0].GetComponent<PhotonView>();
                        if (pvParedeAntiga != null && pvParedeAntiga.IsMine)
                        {
                            PhotonNetwork.Destroy(paredesAtivas[0]);
                        }
                        else
                        {
                            Destroy(paredesAtivas[0]);
                        }
                    }
                    paredesAtivas.RemoveAt(0);
                }

                // Cria a parede sólida definitiva na rede usando a pasta Resources
                GameObject paredeCriada = PhotonNetwork.Instantiate(nomePrefabRede, posicaoAjustadaNoChao, prefabParedeFantasma.transform.rotation);

                if (paredeCriada != null)
                {
                    PhotonView pvParede = paredeCriada.GetComponent<PhotonView>();
                    if (pvParede != null)
                    {
                        // 🌐 CORREÇÃO DO BUG: Sincroniza a escala exata do holograma para todos os clientes na rede via RPC
                        photonView.RPC(nameof(RPC_SincronizarEscalaParede), RpcTarget.AllBuffered, pvParede.ViewID, prefabParedeFantasma.transform.localScale);
                    }

                    paredesAtivas.Add(paredeCriada);
                    StartCoroutine(DestruirParedeAposTempo(paredeCriada, tempoDeVida));
                }

                tempoProximouso = Time.time + tempoCooldown;
                CancelarHabilidade();

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

    // ==========================================
    // 🌐 RPC PARA CORrigir A ESCALA DA PAREDE NA REDE
    // ==========================================
    [PunRPC]
    public void RPC_SincronizarEscalaParede(int viewIDDaParede, Vector3 escalaCorreta)
    {
        PhotonView pvParede = PhotonView.Find(viewIDDaParede);
        if (pvParede != null)
        {
            pvParede.transform.localScale = escalaCorreta;
        }
    }

    private System.Collections.IEnumerator DestruirParedeAposTempo(GameObject parede, float tempo)
    {
        yield return new WaitForSeconds(tempo);

        if (parede != null)
        {
            PhotonView pv = parede.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                PhotonNetwork.Destroy(parede);
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