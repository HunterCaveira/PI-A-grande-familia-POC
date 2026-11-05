// Importa as coleções para usar Coroutines (temporizadores invisíveis)
using System.Collections;
// Importa a biblioteca base da Unity
using UnityEngine;
// Importa a biblioteca do Photon para multiplayer
using Photon.Pun;

// A classe herda de MonoBehaviourPun para usar rede e IasUI para comunicar com a interface
public class HabilidadeTonico : MonoBehaviourPun, IasUI
{
    [Header("Configurações da Câmera")]
    [Tooltip("Nome exato da Câmera na cena (opcional, busca automática por padrão)")]
    [SerializeField] private string nomeDaCamera = "Main Camera";
    private Camera cameraPrincipal;

    [Header("Configuração do Slot e UI")]
    // Define que o tónico é a habilidade terciária/ultimate (Slot 3)
    [SerializeField] private int slotIndex = 3;
    public int SlotIndex => slotIndex;

    // Foto exibida na interface quando o tónico pode ser bebido
    [SerializeField] private Sprite fotoPronta;
    public Sprite FotoPronta => fotoPronta;

    // Foto escurecida para quando o tónico está em recarga
    [SerializeField] private Sprite fotoRecarregando;
    public Sprite FotoRecarregando => fotoRecarregando;

    // Propriedade que guarda o tempo exato em que a habilidade estará pronta
    public float TempoProximouso { get; set; } = 0f;

    [Header("Sons da Habilidade")]
    public AudioSource audioSource;
    public AudioClip somUsar;

    [Header("Configurações do Buff")]
    // A velocidade que o jogador atinge ao beber o tónico (O Cinto agora acompanha esta velocidade!)
    [SerializeField] private float velocidadeComBuff = 20f;
    // O quanto o jogador "tanca" de dano extra (ex: 0.3 = recebe 30% menos dano)
    [SerializeField][Range(0f, 1f)] private float porcentagemReducaoDano = 0.3f;
    // Quantos segundos a velocidade e a defesa extra duram
    [SerializeField] private float duracaoDoEfeito = 6.0f;

    [Header("Tempo de Animação e Cooldown")]
    // Tempo que o boneco fica com o frasco na mão antes do efeito entrar
    [SerializeField] private float tempoAnimacaoConsumo = 1.0f;
    // Tempo de penalidade até poder beber de novo
    [SerializeField] private float tempoCooldown = 10.0f;

    // Trava de segurança para impedir que o jogador clique várias vezes e beba 2 tónicos seguidos
    private bool emUsoOuAnimacao = false;

    [Header("Controle De Referência")]
    // Referência ao sistema que troca de armas
    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;
    // Referência ao sistema de vida para aplicar a redução de dano
    [SerializeField] private Sistemadevida sistemaDeVida;
    // Ficha original do personagem (velocidade normal, etc.)
    [SerializeField] private AtributosPersonagem atributosOriginal;
    // O modelo 3D do frasco na mão do personagem
    [SerializeField] private GameObject prefabTonicoMao;

    // Ficha temporária criada no momento do buff para não estragar a ficha original
    private AtributosPersonagem atributosBuffados;

    // Chamado ao nascer na cena
    private void Awake()
    {
        // Procura os componentes automaticamente no próprio boneco se esquecer de arrastar no Inspector
        if (gerenciadorDeEquipamentos == null) gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
        if (sistemaDeVida == null) sistemaDeVida = GetComponent<Sistemadevida>();

        // Procura a câmera na cena logo ao iniciar
        VerificarEConectarCamera();
    }

    private void Update()
    {
        if (photonView != null && !photonView.IsMine) return;

        // Garante que a câmera e o AudioSource estão conectados caso mudem na cena
        if (cameraPrincipal == null)
        {
            VerificarEConectarCamera();
        }
    }

    // Método responsável por encontrar a câmera na cena dinamicamente
    private void VerificarEConectarCamera()
    {
        if (cameraPrincipal != null) return;

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

        // Se o AudioSource não foi definido no Inspetor nem no Player, tenta utilizar/criar o da câmera
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

    // Chamado se o boneco for apagado da cena (ex: quando morre ou a partida acaba)
    private void OnDisable()
    {
        // Trava de rede: só limpa as variáveis se o boneco for o do meu PC
        if (photonView != null && !photonView.IsMine) return;

        // MUDANÇA CRÍTICA: Não envia RPC aqui! Apenas limpa a memória localmente.
        // Enviar mensagens de rede num objeto que está a morrer causa erros fatais no Photon.
        StopAllCoroutines();
        RemoverBuffs();
        emUsoOuAnimacao = false;
        if (prefabTonicoMao != null) prefabTonicoMao.SetActive(false);
    }

    // Verifica para a UI se o tempo de recarga acabou e se não está no meio da animação
    public bool EstaPronta()
    {
        return Time.time >= TempoProximouso && !emUsoOuAnimacao;
    }

    public bool PodeUsar()
    {
        return EstaPronta();
    }

    // Método chamado quando o jogador clica para usar a habilidade
    public void AtivarHabilidade()
    {
        // Garante que a habilidade só roda para quem é o dono do personagem
        if (photonView != null && !photonView.IsMine) return;

        VerificarEConectarCamera();

        // Se estiver em cooldown, aborta e volta a puxar a arma principal (slot 0)
        if (!EstaPronta())
        {
            if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0);
            return;
        }

        // Inicia a rotina de tempo para beber e ganhar os buffs
        StartCoroutine(RotinaConsumirEAplicarTonico());
    }

    // Método chamado ao cancelar a habilidade (ex: tomou um stun antes de terminar de beber)
    public void CancelarHabilidade()
    {
        if (photonView != null && !photonView.IsMine) return;

        // Se o tónico já começou a agir, ele não pode ser guardado (esconde só o frasco visual)
        if (emUsoOuAnimacao)
        {
            photonView.RPC("RPC_AlternarFrasco", RpcTarget.All, false);
            return;
        }

        // Cancela tudo à força
        CancelarHabilidadeForcado();
    }

    // Limpeza forçada chamada ao cancelar voluntariamente
    private void CancelarHabilidadeForcado()
    {
        StopAllCoroutines(); // Para os cronómetros
        RemoverBuffs(); // Tira a velocidade extra
        emUsoOuAnimacao = false;

        // Avisa a todos na sala multiplayer para esconderem o frasco da minha mão
        if (photonView != null && photonView.IsMine)
        {
            photonView.RPC("RPC_AlternarFrasco", RpcTarget.All, false);
        }
    }

    // O coração da habilidade: a rotina que controla o tempo
    private IEnumerator RotinaConsumirEAplicarTonico()
    {
        emUsoOuAnimacao = true; // Trava o script

        // Toca o som ao iniciar o uso do tónico
        if (audioSource != null && somUsar != null)
        {
            audioSource.PlayOneShot(somUsar);
        }

        // Mostra o frasco na mão para todos na sala
        photonView.RPC("RPC_AlternarFrasco", RpcTarget.All, true);

        // Espera X segundos simulando o jogador a "beber"
        yield return new WaitForSeconds(tempoAnimacaoConsumo);

        // O líquido fez efeito: aplica velocidade e armadura
        AplicarBuffs();

        // Esconde o frasco vazio da mão para todos
        photonView.RPC("RPC_AlternarFrasco", RpcTarget.All, false);

        // Puxa a arma de volta automaticamente para o jogador poder atirar enquanto corre
        if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0);

        // Fica super rápido durante os segundos da duração
        yield return new WaitForSeconds(duracaoDoEfeito);

        // Acabou o efeito: devolve o personagem ao normal
        RemoverBuffs();

        // Configura o cronómetro para permitir usar novamente no futuro
        TempoProximouso = Time.time + tempoCooldown;
        emUsoOuAnimacao = false; // Destrava o script
    }

    // 🌐 Sincronização Visual na Rede: Ativa ou desativa a malha 3D do frasco na mão
    [PunRPC]
    private void RPC_AlternarFrasco(bool mostrar)
    {
        if (prefabTonicoMao != null)
        {
            prefabTonicoMao.SetActive(mostrar);
        }
    }

    // Lógica de manipulação de Atributos
    private void AplicarBuffs()
    {
        // Confirma se existe uma ficha base para clonar
        if (atributosOriginal != null)
        {
            // Segurança: Se já existisse um buff preso, destrói-o antes de criar outro
            if (atributosBuffados != null) Destroy(atributosBuffados);

            // Cria uma cópia independente da ficha de atributos para não alterar o arquivo original na pasta do projeto
            atributosBuffados = Instantiate(atributosOriginal);
            // Aplica a velocidade "sónica"
            atributosBuffados.velocidadeMovimento = velocidadeComBuff;

            // Envia a ficha nova temporária para o sistema de andar
            MovimentodoPlayer movimento = GetComponent<MovimentodoPlayer>();
            if (movimento != null)
            {
                movimento.AtualizarFichaAtributos(atributosBuffados);
            }

            // Envia a ficha nova e a resistência a danos para o sistema de vida
            if (sistemaDeVida != null)
            {
                sistemaDeVida.AtualizarFichaAtributos(atributosBuffados);
                sistemaDeVida.PorcentagemReducaoDano = porcentagemReducaoDano;
            }
        }
        else
        {
            Debug.LogError("[TÔNICO] O campo 'Atributos Original' está vazio no Inspetor!");
        }
    }

    // Limpeza pesada quando o efeito acaba
    private void RemoverBuffs()
    {
        // Devolve a ficha lenta original para as pernas
        MovimentodoPlayer movimento = GetComponent<MovimentodoPlayer>();
        if (movimento != null && atributosOriginal != null)
        {
            movimento.AtualizarFichaAtributos(atributosOriginal);
        }

        // Devolve a ficha original e tira a armadura do sistema de vida
        if (sistemaDeVida != null && atributosOriginal != null)
        {
            sistemaDeVida.AtualizarFichaAtributos(atributosOriginal);
            sistemaDeVida.PorcentagemReducaoDano = 0f;
        }

        // Se a ficha temporária existir, elimina-a da memória do computador para evitar travamentos
        if (atributosBuffados != null)
        {
            Destroy(atributosBuffados);
            atributosBuffados = null;
        }
    }
}