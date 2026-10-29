// Importa a biblioteca base da Unity
using UnityEngine;
// Importa a biblioteca de textos avançados da Unity (UI)
using TMPro;
// Importa a biblioteca do Photon para funções multiplayer (rede)
using Photon.Pun;

// Herda de MonoBehaviourPun para termos acesso à variável photonView
public class tiroPistola : MonoBehaviourPun
{
    [Header("Configurações do Raycast")]
    // Distância máxima que o raio (mira) do tiro alcança
    [SerializeField] private float alcanceDoTiro = 100f;
    // Espalhamento da bala no eixo X (recoil/imprecisão)
    [SerializeField] private float espalhamentoHorizontal = 0.5f;
    // Espalhamento da bala no eixo Y (recoil/imprecisão)
    [SerializeField] private float espalhamentoVertical = 0.5f;
    // Define em quais camadas o tiro pode bater (inimigos, chão, paredes)
    public LayerMask mascaraDeTiro;

    [Header("Visual do Tiro")]
    // Rastro visual do tiro (laser opcional)
    [SerializeField] private LineRenderer lineRenderer;
    // Quanto tempo o rastro visual fica na tela
    [SerializeField] private float duracaoLinha = 0.05f;

    [Header("Pool de Balas")]
    [Tooltip("Pode ser arrastado no Inspector ou deixado vazio para busca automática na cena")]
    // Referência ao script que guarda as balas desativadas
    public BalaPool meuPoolDeBalas;

    [Header("Dano da Arma")]
    // Quantidade de dano que esta arma causa por tiro
    [SerializeField] private float danoPorTiro = 20f;
    // Tipo de dano (Físico, Mágico, etc.) para o sistema de vida
    [SerializeField] private TipoDano tipoDeDano = TipoDano.Fisico;
    // Nome da arma (usado para killfeed no futuro)
    [SerializeField] private string nomeDaArma = "Pistola";

    [Header("Projétil")]
    // Força com que a bala é empurrada para a frente
    public float forçaTiro;
    [Tooltip("Força extra para cima (arco da bala). Deixe 0 para tiro reto seguindo a mira")]
    // Força vertical (útil para granadas ou flechas, para pistola deve ser 0)
    public float forçaCima;

    [Header("Status da Arma")]
    // Cadência (tempo de espera entre um tiro e outro)
    public float tempoEntreAtirar;
    // Tempo que demora a recarregar a arma
    public float tempoRecarga;
    // Quantidade máxima de balas no pente
    public int tamanhoCarregador;
    // Quantas balas saem por cada clique (1 para pistola, vários para shotgun)
    public int tiroPorClick;
    // Se verdadeiro, atira como metralhadora ao segurar o botão. Se falso, tem de clicar uma vez por tiro.
    public bool permitirSegurarBotão;

    [Header("Referências")]
    [Tooltip("Arraste a câmera que orbita o personagem aqui (ou deixe vazio para pegar a Camera.main)")]
    // Câmera do jogador (usada para fazer a mira a partir do centro da tela)
    public Camera playerCamera;
    // Ponto na ponta da arma de onde a bala nasce
    public Transform pontoAtaque;
    // Efeito visual de fogo/faísca na ponta do cano
    public GameObject muzzleFlash;
    // Texto da interface que mostra a munição
    public TextMeshProUGUI ammunitionDisplay;

    // Balas que ainda estão no pente atualmente
    protected int balasSobrando;
    // Referência ao mapa de inputs (teclado/rato)
    private AcoesPlayer acoesPlayer;
    // Variáveis de controle de estado do tiro
    private bool atirando, preparadoParaAtirar, recarregando;
    // Guarda quem é o boneco dono desta arma
    private GameObject donoDaArma;
    // Informa se a arma ficou sem balas
    public bool balaZerada;

    // Chamado uma única vez ao nascer na cena
    private void Awake()
    {
        // Instancia os controlos do jogador
        acoesPlayer = new AcoesPlayer();
        // Enche a arma de balas logo no início
        balasSobrando = tamanhoCarregador;
        // Liberta a trava do gatilho
        preparadoParaAtirar = true;
        // Salva a raiz do prefab (o próprio 'Pai') como dono da arma
        donoDaArma = transform.root.gameObject;

        // MUDANÇA: Segurança de câmera para a rede!
        // Só procura a Main Camera se este boneco for o SEU boneco.
        if (photonView != null && photonView.IsMine)
        {
            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        // BUSCA AUTOMÁTICA DO POOL DE BALAS (Funciona em todos os clientes)
        if (meuPoolDeBalas == null)
        {
            meuPoolDeBalas = Object.FindFirstObjectByType<BalaPool>();
            if (meuPoolDeBalas == null)
            {
                Debug.LogWarning($"[tiroPistola] Nenhum objeto do tipo 'BalaPool' foi encontrado na cena!");
            }
        }
    }

    // Chamado quando o jogador puxa a arma para a mão
    private void OnEnable()
    {
        // 🔒 Bloqueia a ativação do teclado se a arma for do boneco de outro jogador
        if (photonView != null && !photonView.IsMine) return;

        // Liga a leitura dos botões de atirar e recarregar
        acoesPlayer.MovimentoPlayer.Atirar.Enable();
        acoesPlayer.MovimentoPlayer.Recarregar.Enable();
    }

    // Chamado quando o jogador guarda a arma ou morre
    private void OnDisable()
    {
        // Desliga a leitura dos botões para não causar conflitos com outras armas
        if (acoesPlayer != null)
        {
            acoesPlayer.MovimentoPlayer.Atirar.Disable();
            acoesPlayer.MovimentoPlayer.Recarregar.Disable();
        }
    }

    // Chamado em todos os frames do jogo
    private void Update()
    {
        // 🔒 Trava absoluta: Impede que o computador tente processar inputs em armas inimigas
        if (photonView != null && !photonView.IsMine) return;

        // Chama a função que verifica se o rato foi clicado
        MyInput();

        // Atualiza o texto na tela (balas atuais / balas máximas)
        if (ammunitionDisplay != null && tiroPorClick > 0)
        {
            ammunitionDisplay.SetText(balasSobrando / tiroPorClick + " / " + tamanhoCarregador / tiroPorClick);
        }
    }

    // Processa os cliques do jogador
    private void MyInput()
    {
        // Decide se lê o botão a ser segurado (metralhadora) ou o clique único (pistola)
        if (permitirSegurarBotão)
            atirando = acoesPlayer.MovimentoPlayer.Atirar.IsPressed();
        else
            atirando = acoesPlayer.MovimentoPlayer.Atirar.WasPressedThisFrame();

        // Se o botão de recarregar foi premido E faltam balas E não está já a recarregar
        if (acoesPlayer.MovimentoPlayer.Recarregar.WasPressedThisFrame() && balasSobrando < tamanhoCarregador && !recarregando)
            Reload();

        // Se o gatilho está pronto, clicou, não está a recarregar e ainda tem balas
        if (preparadoParaAtirar && atirando && !recarregando && balasSobrando > 0)
            Atirar();
    }

    // Lógica para atirar a bala
    private void Atirar()
    {
        // Se a piscina de balas sumiu, aborta para não dar erro
        if (meuPoolDeBalas == null)
        {
            Debug.LogError("[tiroPistola] Impossível atirar: 'meuPoolDeBalas' não foi encontrado na cena!");
            return;
        }

        // Tranca o gatilho para respeitar o tempo da cadência
        preparadoParaAtirar = false;

        // Cria um raio mágico saindo do exato centro da tela (mira)
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 pontoAcerto;

        // Se o raio bater em alguma coisa na mascaraDeTiro, guarda o ponto exato da colisão
        if (Physics.Raycast(ray, out RaycastHit acerto, alcanceDoTiro, mascaraDeTiro, QueryTriggerInteraction.Ignore))
            pontoAcerto = acerto.point;
        else
            // Se atirar para o céu vazio, o ponto será no limite do alcance máximo
            pontoAcerto = ray.GetPoint(alcanceDoTiro);

        // Calcula a direção que a bala física tem de percorrer (da ponta do cano até ao alvo na mira)
        Vector3 direçãoBase = (pontoAcerto - pontoAtaque.position).normalized;

        // Loop que permite armas de múltiplos tiros (shotgun) no mesmo clique
        for (int i = 0; i < tiroPorClick; i++)
        {
            // Para de criar balas se o pente esvaziar a meio
            if (balasSobrando <= 0) break;
            // Sinaliza se a arma ficou vazia
            balaZerada = (tamanhoCarregador <= 0);

            // Sorteia o espalhamento (imprecisão) do tiro atual
            float x = Random.Range(-espalhamentoHorizontal, espalhamentoHorizontal) * 0.05f;
            float y = Random.Range(-espalhamentoVertical, espalhamentoVertical) * 0.05f;

            // Calcula o vetor de erro
            Vector3 vetorEspalhamento = (playerCamera.transform.right * x) + (playerCamera.transform.up * y);
            // Soma a direção perfeita com o erro de espalhamento
            Vector3 direçãoFinal = (direçãoBase + vetorEspalhamento).normalized;

            // 🌐 MULTIPLAYER: Manda um sinal pela internet. 
            // Todos os jogadores na sala recebem o comando "Atirar" usando as coordenadas exatas calculadas por ti.
            photonView.RPC("RPC_AtirarSincronizado", RpcTarget.All, pontoAtaque.position, direçãoFinal, pontoAcerto);

            // Tira uma bala do pente do atirador
            balasSobrando--;
        }

        // Agenda o destravamento do gatilho para o futuro, baseado no tempoEntreAtirar
        Invoke(nameof(ResetShot), tempoEntreAtirar);
    }

    // ===============================================================
    // 🌐 FUNÇÃO DE REDE: Todos os computadores executam esta função 
    // e puxam uma bala do seu próprio Pool local ao mesmo tempo!
    // ===============================================================
    [PunRPC]
    private void RPC_AtirarSincronizado(Vector3 origemTiro, Vector3 direcaoTiro, Vector3 pontoAcertoLinha)
    {
        // 1. Desenha o rastro visual do tiro na tela
        if (lineRenderer != null)
            StartCoroutine(DesenharRaio(origemTiro, pontoAcertoLinha));

        // 2. Instancia o efeito de fogo na arma
        if (muzzleFlash != null)
            Instantiate(muzzleFlash, origemTiro, Quaternion.identity, pontoAtaque);

        // 3. Pega a bala desativada da piscina (sem criar objetos novos na memória!)
        if (meuPoolDeBalas == null) return;
        GameObject balaAtual = meuPoolDeBalas.GetitemFromPool();

        // Se conseguiu puxar uma bala com sucesso
        if (balaAtual != null)
        {
            // Posiciona a bala na ponta do cano
            balaAtual.transform.position = origemTiro;
            // Vira a frente da bala para a direção certa
            balaAtual.transform.forward = direcaoTiro;
            // Liga o objeto da bala na cena
            balaAtual.SetActive(true);

            // AQUI É ONDE O DANO VAI PARA A BALA: Envia a ficha criminal para o projétil
            if (balaAtual.TryGetComponent<Bala>(out Bala scriptBala))
                scriptBala.Inicializar(danoPorTiro, tipoDeDano, donoDaArma, nomeDaArma);

            // Empurra a bala fisicamente para a frente
            if (balaAtual.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                // Zera as velocidades antigas para a bala não bugar
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                // Aplica o empurrão (Impulso) na direção calculada
                rb.AddForce(direcaoTiro * forçaTiro, ForceMode.Impulse);

                // Se houver força para cima (granadas), aplica o empurrão vertical
                if (forçaCima > 0f)
                    rb.AddForce(Vector3.up * forçaCima, ForceMode.Impulse);
            }
        }
    }

    // Destrava o gatilho
    private void ResetShot() => preparadoParaAtirar = true;

    // Inicia o processo de recarga
    private void Reload()
    {
        recarregando = true;
        // Agenda o fim da recarga
        Invoke(nameof(TerminarRecarga), tempoRecarga);
    }

    // Fim da recarga: enche o pente
    private void TerminarRecarga()
    {
        balasSobrando = tamanhoCarregador;
        recarregando = false;
    }

    // Rotina que desliga o laser do tiro rapidamente
    private System.Collections.IEnumerator DesenharRaio(Vector3 inicio, Vector3 fim)
    {
        lineRenderer.SetPosition(0, inicio);
        lineRenderer.SetPosition(1, fim);
        lineRenderer.enabled = true;
        yield return new WaitForSeconds(duracaoLinha);
        lineRenderer.enabled = false;
    }
}