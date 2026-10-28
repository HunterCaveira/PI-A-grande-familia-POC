// Importa as funções básicas da Unity
using UnityEngine;
// Importa a biblioteca do Photon para permitir comunicação multiplayer pela internet
using Photon.Pun;

// A classe herda de MonoBehaviourPun (para ter photonView) e implementa a interface de UI
public class HabilidadeCinto : MonoBehaviourPun, IasUI
{
    [Header("Configurações da Câmera e Alcance")]
    // Removido o [SerializeField]. A câmera agora é achada automaticamente pelo script.
    private Camera cameraPrincipal;
    // Distância máxima que o cinto consegue esticar para pegar um inimigo
    [SerializeField] private float distanciaMaxima = 15f;

    [Header("Controle de Tempo e Velocidades")]
    // Tempo de espera para usar o cinto novamente (Cooldown)
    [SerializeField] private float tempoCooldown = 3.0f;
    // Guarda o momento exato em que o cinto poderá ser usado de novo
    private float tempoProximoUso = 0f;
    // Propriedade para a Interface (UI) ler o tempo do cooldown
    public float TempoProximouso => tempoProximoUso;

    // Velocidade em que o cinto voa na direção do inimigo
    [SerializeField] private float velocidadeAvanco = 30f;
    // Velocidade em que o cinto puxa o inimigo de volta para si
    [SerializeField] private float velocidadeRetorno = 20f;

    [Header("Configuração de Slot e UI")]
    // Posição desta habilidade na barra de ícones da tela
    [SerializeField] private int slotIndex = 1;
    // Permite que outros scripts saibam qual é o slot
    public int SlotIndex => slotIndex;

    // Foto iluminada quando o cinto está pronto
    [SerializeField] private Sprite fotoPronta;
    public Sprite FotoPronta => fotoPronta;

    // Foto escura para quando o cinto está recarregando
    [SerializeField] private Sprite fotoRecarregando;
    public Sprite FotoRecarregando => fotoRecarregando;

    [Header("Controle De Referência e Visuais")]
    // Script que troca os itens na mão do jogador
    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;
    // O objeto 3D do cinto que será ativado e esticado
    [SerializeField] private GameObject prefabCinto;
    // A ponta do cinto (fivela) que detecta colisões
    [SerializeField] private Transform pontoFivelaPontos;
    // O ponto final para onde o inimigo será puxado (geralmente perto de você)
    [SerializeField] private Transform pontoDestinoPuxao;

    [Header("Filtragem de Time")]
    // Define o número do time deste jogador para não puxar aliados
    [SerializeField] private int idDoTime = 1;
    public int IdDoTime => idDoTime;

    // Máquina de estados simples para saber o que o cinto está a fazer no momento
    private enum EstadoCinto { Inativo, Avancando, PuxandoInimigo, RecolhendoVazio }
    // Começa desligado
    private EstadoCinto estadoAtual = EstadoCinto.Inativo;

    // Guarda quem foi capturado para arrastá-lo
    private Transform inimigoCapturado;
    // Guarda o CharacterController do inimigo para desativá-lo no puxão
    private CharacterController controllerInimigo;
    // Guarda o CapsuleCollider do inimigo para evitar que bata em paredes enquanto voa
    private CapsuleCollider colliderInimigo;
    // Guarda o Rigidbody do inimigo (se existir)
    private Rigidbody rbInimigo;
    // Guarda o componente de rede do inimigo para lhe roubar a posse
    private PhotonView pvInimigo;

    // Posição original do cinto na mão para quando voltar
    private Vector3 posicaoInicialLocal;
    // O ponto no mapa para onde a mira estava a apontar
    private Vector3 pontoFinalMundo;

    // Awake roda uma vez quando o objeto nasce na cena
    private void Awake()
    {
        // Se o gerenciador estiver vazio, procura-o no próprio corpo
        if (gerenciadorDeEquipamentos == null) gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();

        // MUDANÇA: Procura automaticamente a câmera principal da cena (A que tem a Tag "MainCamera")
        if (cameraPrincipal == null) cameraPrincipal = Camera.main;

        // Se o modelo do cinto existir, guarda onde ele fica encostado na mão do jogador
        if (prefabCinto != null)
        {
            posicaoInicialLocal = prefabCinto.transform.localPosition;
        }
    }

    // Update roda a todos os frames
    private void Update()
    {
        // Trava multiplayer: Se este boneco não for seu, ignora os cálculos (evita bug de atirar junto)
        if (photonView != null && !photonView.IsMine) return;

        // Se o cinto não foi jogado, não faz nada
        if (estadoAtual == EstadoCinto.Inativo) return;

        // Dependendo do que o cinto está a fazer, executa uma função diferente
        switch (estadoAtual)
        {
            case EstadoCinto.Avancando: // Cinto a voar para a frente
                ProcessarAvanco();
                break;
            case EstadoCinto.PuxandoInimigo: // Pegou alguém, está a trazer de volta
                ProcessarPuxao();
                break;
            case EstadoCinto.RecolhendoVazio: // Errou o alvo, voltando para a mão
                ProcessarRecolhimento();
                break;
        }
    }

    // Move o cinto para a frente em direção à mira
    private void ProcessarAvanco()
    {
        if (prefabCinto == null) return;

        // Move o objeto 3D do cinto do ponto atual para o ponto da mira usando velocidade
        prefabCinto.transform.position = Vector3.MoveTowards(prefabCinto.transform.position, pontoFinalMundo, velocidadeAvanco * Time.deltaTime);

        // A cada passo, verifica se bateu em alguém. Se bateu, sai da função.
        if (ChecarEPrenderInimigo()) return;

        // Se o cinto chegou ao limite da mira e não pegou ninguém
        if (Vector3.Distance(prefabCinto.transform.position, pontoFinalMundo) <= 0.2f)
        {
            // Muda o estado para voltar para o dono vazio
            estadoAtual = EstadoCinto.RecolhendoVazio;
        }
    }

    // Move o cinto de volta se tiver errado o tiro
    private void ProcessarRecolhimento()
    {
        if (prefabCinto == null) return;

        // Define que o destino de volta é o ponto de puxão (mão) ou o próprio corpo
        Vector3 destinoMao = pontoDestinoPuxao != null ? pontoDestinoPuxao.position : transform.position;

        // Move o cinto de volta para a mão
        prefabCinto.transform.position = Vector3.MoveTowards(prefabCinto.transform.position, destinoMao, velocidadeAvanco * Time.deltaTime);

        // Mesmo a voltar, se raspar num inimigo, prende-o (opcional, pode ser removido se quiser)
        if (ChecarEPrenderInimigo()) return;

        // Se já voltou totalmente para a mão
        if (Vector3.Distance(prefabCinto.transform.position, destinoMao) <= 0.2f)
        {
            // Limpa tudo e desativa a habilidade
            FinalizarHabilidade();
        }
    }

    // A lógica principal que detecta e "laça" o inimigo
    private bool ChecarEPrenderInimigo()
    {
        // Define de onde sai o radar de colisão (Fivela ou do meio do cinto)
        Vector3 posicaoCheck = pontoFivelaPontos != null ? pontoFivelaPontos.position : prefabCinto.transform.position;
        // Cria uma esfera invisível de 0.6 metros para ver quem está dentro
        Collider[] hits = Physics.OverlapSphere(posicaoCheck, 0.6f);

        // Vasculha todos os objetos que tocaram na esfera
        foreach (var col in hits)
        {
            // Ignora a si mesmo e aos próprios equipamentos
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            // Tenta achar o sistema de vida ou o cinto do alvo para ver se é um jogador
            Sistemadevida vidaAlvo = col.GetComponent<Sistemadevida>();
            HabilidadeCinto scriptAlvo = col.GetComponent<HabilidadeCinto>();

            // Inicializa a variável do time como -1 (desconhecido)
            int timeAlvo = -1;
            // Se tiver vida, pega o ID do time dele
            if (vidaAlvo != null) timeAlvo = vidaAlvo.TimeID;
            // Senão, tenta pegar pelo script do cinto dele
            else if (scriptAlvo != null) timeAlvo = scriptAlvo.IdDoTime;

            // Se achou um time e NÃO for do seu time (é inimigo!)
            if (timeAlvo != -1 && timeAlvo != this.idDoTime)
            {
                // Guarda o inimigo para arrastá-lo
                inimigoCapturado = col.transform;
                pvInimigo = col.GetComponent<PhotonView>();

                if (pvInimigo != null)
                {
                    // Rouba a permissão da rede para poder mexer no corpo dele
                    pvInimigo.RequestOwnership();
                    // MUDANÇA: Chama um RPC pela rede para travar o teclado da vítima na tela DELA
                    pvInimigo.RPC("RPC_EstadoPuxao", RpcTarget.All, true);
                }

                // MUDANÇA: Desliga o Character Controller do inimigo para a Unity não bloquear o puxão
                controllerInimigo = col.GetComponent<CharacterController>();
                if (controllerInimigo != null) controllerInimigo.enabled = false;

                // MUDANÇA: Desliga o Capsule Collider para ele não agarrar em escadas ou paredes enquanto voa
                colliderInimigo = col.GetComponent<CapsuleCollider>();
                if (colliderInimigo != null) colliderInimigo.enabled = false;

                // Transforma o rigidbody em cinemático para não ter gravidade a atrapalhar
                rbInimigo = col.GetComponent<Rigidbody>();
                if (rbInimigo != null) rbInimigo.isKinematic = true;

                // Muda o estado para começar a puxá-lo na próxima frame
                estadoAtual = EstadoCinto.PuxandoInimigo;
                return true;
            }
        }
        // Se ninguém foi pego, retorna falso
        return false;
    }

    // Traz o cinto (e o inimigo colado nele) de volta para o jogador
    private void ProcessarPuxao()
    {
        if (prefabCinto == null) return;

        // Calcula a rota de volta para a mão
        Vector3 destinoMao = pontoDestinoPuxao != null ? pontoDestinoPuxao.position : transform.position;

        // Move o cinto de volta, agora usando a velocidade específica de retorno
        prefabCinto.transform.position = Vector3.MoveTowards(prefabCinto.transform.position, destinoMao, velocidadeRetorno * Time.deltaTime);

        // Se tem um inimigo preso
        if (inimigoCapturado != null)
        {
            // O corpo do inimigo é forçado a ficar exatamente na fivela do cinto enquanto ele volta
            Vector3 posicaoFivela = pontoFivelaPontos != null ? pontoFivelaPontos.position : prefabCinto.transform.position;
            inimigoCapturado.position = posicaoFivela;
        }

        // Se o inimigo já chegou muito perto do jogador (0.8 metros)
        if (Vector3.Distance(prefabCinto.transform.position, destinoMao) <= 0.8f)
        {
            // Finaliza o puxão e liberta o inimigo
            FinalizarHabilidade();
        }
    }

    // Método que arruma a casa depois que o tiro acaba
    private void FinalizarHabilidade()
    {
        // Coloca o cinto em repouso
        estadoAtual = EstadoCinto.Inativo;

        // Se tínhamos desligado o Character Controller do inimigo, ligamos de volta para ele andar
        if (controllerInimigo != null)
        {
            controllerInimigo.enabled = true;
            controllerInimigo = null;
        }

        // Religamos o Capsule Collider do inimigo
        if (colliderInimigo != null)
        {
            colliderInimigo.enabled = true;
            colliderInimigo = null;
        }

        // Devolvemos a física de gravidade ao inimigo
        if (rbInimigo != null)
        {
            rbInimigo.isKinematic = false;
            rbInimigo = null;
        }

        // MUDANÇA: Avisamos a rede e a vítima que ela já pode voltar a andar
        if (pvInimigo != null)
        {
            pvInimigo.RPC("RPC_EstadoPuxao", RpcTarget.All, false);
        }

        // Limpamos a memória do alvo capturado
        inimigoCapturado = null;
        pvInimigo = null;

        // Garantimos que o cinto volta exatamente para a pose inicial dentro da mão
        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.transform.localRotation = Quaternion.identity;
        }

        // Desativamos o visual
        CancelarHabilidade();

        // Puxamos a arma principal de volta (slot 0)
        if (gerenciadorDeEquipamentos != null)
        {
            gerenciadorDeEquipamentos.TrocarEquipamento(0);
        }
    }

    // Checa se o cooldown já passou e se o cinto está guardado
    public bool EstaPronta()
    {
        return Time.time >= tempoProximoUso && estadoAtual == EstadoCinto.Inativo;
    }

    // Mesma função, usada pela interface IHabilidade
    public bool PodeUsar()
    {
        return EstaPronta();
    }

    // Inicia o ataque quando o jogador aperta o botão
    public void AtivarHabilidade()
    {
        // Trava para só o dono usar
        if (photonView != null && !photonView.IsMine) return;

        // Se ainda está a recarregar
        if (!PodeUsar())
        {
            // Volta para a arma sem atirar
            if (gerenciadorDeEquipamentos != null)
            {
                gerenciadorDeEquipamentos.TrocarEquipamento(0);
            }
            return;
        }

        // Liga o cinto visualmente
        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.SetActive(true);
        }

        // Faz o raio da mira a partir da câmera
        if (cameraPrincipal != null)
        {
            Ray ray = cameraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            pontoFinalMundo = ray.origin + ray.direction * distanciaMaxima;

            // Se o raio bater nalguma parede, o cinto só vai até à parede (não a atravessa)
            if (Physics.Raycast(ray, out RaycastHit hit, distanciaMaxima))
            {
                pontoFinalMundo = hit.point;
            }
        }

        // Vira o cinto para olhar para o alvo
        if (prefabCinto != null)
        {
            prefabCinto.transform.LookAt(pontoFinalMundo);
        }

        // Adiciona o tempo de cooldown
        tempoProximoUso = Time.time + tempoCooldown;
        // Coloca no modo "Avançando" para o Update começar a movê-lo
        estadoAtual = EstadoCinto.Avancando;
    }

    // Desliga a habilidade abruptamente (ex: se o jogador morrer)
    public void CancelarHabilidade()
    {
        estadoAtual = EstadoCinto.Inativo;

        if (controllerInimigo != null)
        {
            controllerInimigo.enabled = true;
            controllerInimigo = null;
        }

        if (colliderInimigo != null)
        {
            colliderInimigo.enabled = true;
            colliderInimigo = null;
        }

        if (rbInimigo != null)
        {
            rbInimigo.isKinematic = false;
            rbInimigo = null;
        }

        inimigoCapturado = null;
        pvInimigo = null;

        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.transform.localRotation = Quaternion.identity;
            prefabCinto.SetActive(false);
        }
    }

    // MUDANÇA: A Função RPC que é chamada via internet para notificar a vítima
    [PunRPC]
    public void RPC_EstadoPuxao(bool estaSendoPuxado)
    {
        // Trava ou destrava o movimento da vítima
        MovimentodoPlayer mov = GetComponent<MovimentodoPlayer>();
        if (mov != null) mov.enabled = !estaSendoPuxado;

        // Se o boneco for meu (eu sou a vítima), recebe um aviso visual na tela!
        if (photonView.IsMine)
        {
            if (estaSendoPuxado)
            {
                // Mostra um aviso vermelho gigante na consola
                Debug.Log("<color=red>⚠️ VOCÊ FOI LAÇADO PELO CINTO E ESTÁ SENDO PUXADO!</color>");
                // Aqui você pode ativar uma UI futuramente, como um flash na tela
            }
            else
            {
                Debug.Log("<color=green>O puxão acabou, você está livre.</color>");
            }
        }
    }
}