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
    [Tooltip("Nome exato da Câmera na cena (opcional, busca automática por padrão)")]
    [SerializeField] private string nomeDaCamera = "Main Camera";
    private Camera cameraPrincipal;
    // Distância máxima que o jogador pode colocar a parede a partir da câmera
    [SerializeField] private float distanciaMaxima = 15f;
    // Define qual a camada (Layer) que representa o chão para a parede não atravessar
    [SerializeField] private LayerMask camadaChao;
    // Define qual a camada representa obstáculos/paredes para limitar o crescimento da parede
    [SerializeField] private LayerMask camadaParedesEObstaculos;

    [Header("Sons da Habilidade")]
    public AudioSource audioSource;
    public AudioClip somUsar;

    [Header("Dimensionamento da Parede")]
    // Largura máxima que a parede pode atingir
    [SerializeField] private float larguraMaxima = 5f;
    // Largura mínima que a parede pode atingir se estiver num corredor apertado
    [SerializeField] private float larguraMinima = 1f;
    // Altura máxima da parede
    [SerializeField] private float alturaMaxima = 4f;
    // Altura mínima da parede caso o teto seja baixo
    [SerializeField] private float alturaMinima = 0.5f;

    [Header("Teclas e Estado")]
    // Variável que diz se o jogador está com o holograma fantasma ativado na tela
    private bool modoPosicionamentoAtivo = false;

    [Header("Prefabs da Parede")]
    // O modelo transparente (holograma) que aparece antes de construir
    [SerializeField] private GameObject prefabParedeFantasma;
    // O modelo sólido da parede (não é mais instanciado por aqui, mas mantido por compatibilidade)
    [SerializeField] private GameObject prefabParedeReal;
    // Nome EXATO do prefab sólido que deve estar obrigatoriamente na pasta "Resources"
    [SerializeField] private string nomePrefabRede = "ParedeReal";

    [Header("Regras de Uso (Cooldown e Limites)")]
    // Quanto tempo a parede dura no mapa antes de sumir
    [SerializeField] private float tempoDeVida = 10f;
    // Número máximo de paredes que este jogador pode ter ativas ao mesmo tempo
    [SerializeField] private int limiteParedes = 2;
    // Tempo de espera (cooldown) para usar a habilidade novamente
    [SerializeField] private float tempoCooldown = 5f;

    [Header("Configuração do Slot")]
    // Indica em qual slot da interface (UI) esta habilidade fica
    [SerializeField] private int slotIndex = 1;
    // Propriedade pública para outras classes lerem o index do slot
    public int SlotIndex => slotIndex;

    [Header("Imagens da UI")]
    // Imagem mostrada quando a habilidade está pronta para uso
    [SerializeField] private Sprite fotoPronta;
    // Imagem escurecida mostrada enquanto está em cooldown
    [SerializeField] private Sprite fotoRecarregando;
    // Propriedade para a UI ler a foto pronta
    public Sprite FotoPronta => fotoPronta;
    // Propriedade para a UI ler a foto descarregada
    public Sprite FotoRecarregando => fotoRecarregando;

    [Header("Tempo de Recarga para UI")]
    // Variável visual de tempo para a UI (parece duplicada com tempoCooldown, mas usada para interface)
    [SerializeField] private float tempoDeRecarga = 4f;
    // Variável interna que guarda em que momento exato o cooldown acaba
    private float tempoProximouso = 0f;
    // Propriedade pública para a interface saber quando acaba o cooldown
    public float TempoProximouso => tempoProximouso;

    // Guarda a referência do script que gerencia os itens/armas do jogador
    private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;
    // Lista que guarda as paredes que este jogador já colocou no mapa
    private List<GameObject> paredesAtivas = new List<GameObject>();

    // Awake é chamado uma única vez quando o objeto nasce na cena
    private void Awake()
    {
        // Busca o gerenciador de equipamentos no próprio personagem
        gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();

        // Procura a câmera e o AudioSource na cena logo ao iniciar
        VerificarEConectarCamera();
    }

    // Método responsável por encontrar a câmera e o AudioSource na cena dinamicamente
    private void VerificarEConectarCamera()
    {
        if (cameraPrincipal == null)
        {
            // 1. Tenta pegar a câmera principal por Tag
            cameraPrincipal = Camera.main;

            // 2. Se não encontrou, procura pelo nome configurado
            if (cameraPrincipal == null)
            {
                GameObject camObj = GameObject.Find(nomeDaCamera);
                if (camObj != null)
                {
                    cameraPrincipal = camObj.GetComponent<Camera>();
                }
            }

            // 3. Fallback: Pega qualquer câmera ativa na cena
            if (cameraPrincipal == null)
            {
                cameraPrincipal = FindFirstObjectByType<Camera>();
            }
        }

        // Se o AudioSource não foi definido no Inspetor nem no Player, busca ou cria na câmera
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && cameraPrincipal != null)
            {
                audioSource = cameraPrincipal.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = cameraPrincipal.gameObject.AddComponent<AudioSource>();
                }
            }
        }
    }

    // Update roda todos os frames do jogo
    void Update()
    {
        // Trava de rede: Se o personagem não for do meu computador, não leio os inputs de parede dele
        if (photonView != null && !photonView.IsMine) return;

        // Garante que a câmera está conectada caso tenha mudado ou nascido depois
        if (cameraPrincipal == null || audioSource == null)
        {
            VerificarEConectarCamera();
        }

        // Se o jogador ativou o holograma, atualiza a posição dele acompanhando a mira
        if (modoPosicionamentoAtivo)
        {
            PosicionarECriarParede();
        }
    }

    // Retorna verdadeiro se o tempo atual do jogo já passou do tempo de cooldown
    public bool PodeUsar()
    {
        return Time.time >= tempoProximouso;
    }

    // Mesmo que o PodeUsar, usado por outras interfaces para checar se está pronta
    public bool EstaPronta()
    {
        return Time.time >= tempoProximouso;
    }

    // Método chamado quando o jogador aperta o botão da habilidade
    public void AtivarHabilidade()
    {
        // Garante que só o dono do boneco pode ativar
        if (photonView != null && !photonView.IsMine) return;

        VerificarEConectarCamera();

        // Se a habilidade estiver em cooldown
        if (!EstaPronta())
        {
            // Volta para a arma principal (slot 0)
            if (gerenciadorDeEquipamentos != null)
            {
                gerenciadorDeEquipamentos.TrocarEquipamento(0);
            }
            return; // Sai do método e não faz mais nada
        }

        // Inverte o estado do modo fantasma (liga se tava desligado, desliga se tava ligado)
        modoPosicionamentoAtivo = !modoPosicionamentoAtivo;

        // Liga ou desliga o GameObject do holograma na cena
        if (prefabParedeFantasma != null)
        {
            prefabParedeFantasma.SetActive(modoPosicionamentoAtivo);
        }
    }

    // Força o cancelamento da habilidade (usado ao morrer ou trocar de arma)
    public void CancelarHabilidade()
    {
        // Desliga a variável do modo de posicionamento
        modoPosicionamentoAtivo = false;

        // Desativa visualmente o holograma
        if (prefabParedeFantasma != null)
        {
            prefabParedeFantasma.SetActive(false);
        }
    }

    // Método complexo que posiciona o holograma e constrói a parede
    private void PosicionarECriarParede()
    {
        // Se a câmera ou o fantasma não existirem, aborta a execução para não dar erro
        if (cameraPrincipal == null || prefabParedeFantasma == null) return;

        // Cria um raio (laser) que sai exatamente do centro da tela (mira)
        Ray ray = cameraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        // Variável para guardar informações sobre onde o raio bateu
        RaycastHit hit;

        // Atira o raio e checa se bateu em algo que seja da camadaChao dentro da distanciaMaxima
        if (Physics.Raycast(ray, out hit, distanciaMaxima, camadaChao))
        {
            // Se o holograma estava escondido, ativa-o
            if (!prefabParedeFantasma.activeSelf)
            {
                prefabParedeFantasma.SetActive(true);
            }

            // Chama a função que estica ou encolhe o holograma baseado nos obstáculos próximos
            AjustarLarguraDaParede(prefabParedeFantasma.transform);

            // Calcula metade da altura do holograma para não afundar no chão
            float metadeDaAltura = prefabParedeFantasma.transform.localScale.y / 2f;
            // Define a posição final somando o ponto que o raio bateu com a metade da altura
            Vector3 posicaoAjustadaNoChao = hit.point + Vector3.up * metadeDaAltura;

            // Move o holograma para a posição calculada
            prefabParedeFantasma.transform.position = posicaoAjustadaNoChao;

            // Verifica se o botão direito do rato foi clicado neste frame
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                // Limpa a lista removendo paredes que já sumiram sozinhas
                paredesAtivas.RemoveAll(parede => parede == null);

                // Se o jogador já atingiu o limite máximo de paredes
                if (paredesAtivas.Count >= limiteParedes)
                {
                    // Verifica se a parede mais antiga ainda existe
                    if (paredesAtivas[0] != null)
                    {
                        // Tenta pegar o PhotonView da parede mais velha
                        PhotonView pvParedeAntiga = paredesAtivas[0].GetComponent<PhotonView>();
                        // Se o view existe e é meu, destroi pela rede
                        if (pvParedeAntiga != null && pvParedeAntiga.IsMine)
                        {
                            PhotonNetwork.Destroy(paredesAtivas[0]);
                        }
                        // Senão, destroi só localmente (fallback de segurança)
                        else
                        {
                            Destroy(paredesAtivas[0]);
                        }
                    }
                    // Remove o registro da parede velha da lista
                    paredesAtivas.RemoveAt(0);
                }

                // Cria a parede sólida definitiva na rede usando a pasta Resources
                GameObject paredeCriada = PhotonNetwork.Instantiate(nomePrefabRede, posicaoAjustadaNoChao, prefabParedeFantasma.transform.rotation);

                // Se a parede foi criada com sucesso
                if (paredeCriada != null)
                {
                    // Toca o som ao criar/posicionar a parede
                    if (audioSource != null && somUsar != null)
                    {
                        audioSource.PlayOneShot(somUsar);
                    }

                    // Copia o tamanho espremido/esticado do holograma para a parede real
                    paredeCriada.transform.localScale = prefabParedeFantasma.transform.localScale;
                    // Adiciona a parede nova na lista de controle
                    paredesAtivas.Add(paredeCriada);
                    // Inicia o cronômetro para a parede se destruir sozinha após o tempoDeVida
                    StartCoroutine(DestruirParedeAposTempo(paredeCriada, tempoDeVida));
                }

                // Define que o próximo uso só será permitido no tempo atual + o cooldown
                tempoProximouso = Time.time + tempoCooldown;
                // Desativa o modo holograma
                CancelarHabilidade();

                // Puxa a arma principal de volta à mão
                if (gerenciadorDeEquipamentos != null)
                {
                    gerenciadorDeEquipamentos.TrocarEquipamento(0);
                }
            }
        }
        else // Se o raio da mira não estiver a bater no chão (ex: a olhar para o céu)
        {
            // Esconde o holograma pois a posição é inválida
            if (prefabParedeFantasma.activeSelf)
            {
                prefabParedeFantasma.SetActive(false);
            }
        }
    }

    // Rotina (Coroutine) que roda em paralelo esperando o tempo acabar
    private System.Collections.IEnumerator DestruirParedeAposTempo(GameObject parede, float tempo)
    {
        // Espera os segundos definidos sem travar o jogo
        yield return new WaitForSeconds(tempo);

        // Se a parede ainda existir depois do tempo
        if (parede != null)
        {
            // Pega o componente de rede
            PhotonView pv = parede.GetComponent<PhotonView>();
            // Se eu for o dono da parede, peço para o Photon destruí-la da sala para todos
            if (pv != null && pv.IsMine)
            {
                PhotonNetwork.Destroy(parede);
            }
        }
    }

    // Método que recalcula o tamanho físico da parede com base no ambiente ao redor
    private void AjustarLarguraDaParede(Transform parede)
    {
        // Calcula qual o raio máximo de busca para os lados
        float raioBusca = larguraMaxima / 2f;
        // Inicia a distância do teto com o valor máximo possível
        float distanciaTeto = alturaMaxima;
        // Inicia as distâncias laterais com o raio máximo
        float distanciaEsquerda = raioBusca;
        float distanciaDireita = raioBusca;

        // Ponto de origem para atirar os raios, um pouco acima do chão
        Vector3 pontoOrigem = parede.position + Vector3.up * 0.5f;

        // Dispara um raio para cima; se bater num teto baixo, guarda a distância
        if (Physics.Raycast(parede.position, Vector3.up, out RaycastHit hitTeto, alturaMaxima, camadaParedesEObstaculos))
        {
            distanciaTeto = hitTeto.distance;
        }

        // Garante que a altura não passe do máximo, nem seja menor que o mínimo
        float novaAltura = Mathf.Clamp(distanciaTeto, alturaMinima, alturaMaxima);

        // Dispara um raio para a esquerda; se bater noutra parede, guarda a distância
        if (Physics.Raycast(pontoOrigem, -parede.right, out RaycastHit hitEsquerda, raioBusca, camadaParedesEObstaculos))
        {
            distanciaEsquerda = hitEsquerda.distance;
        }

        // Dispara um raio para a direita; se bater noutra parede, guarda a distância
        if (Physics.Raycast(pontoOrigem, parede.right, out RaycastHit hitDireita, raioBusca, camadaParedesEObstaculos))
        {
            distanciaDireita = hitDireita.distance;
        }

        // Soma o espaço livre da esquerda e direita, e trava nos limites mínimo/máximo da largura
        float novaLargura = Mathf.Clamp(distanciaEsquerda + distanciaDireita, larguraMinima, larguraMaxima);

        // Guarda a escala atual do holograma
        Vector3 escalaAtual = parede.localScale;
        // Aplica o novo tamanho calculado ao holograma sem mexer na profundidade (Z)
        parede.localScale = new Vector3(novaLargura, novaAltura, escalaAtual.z);
    }
}