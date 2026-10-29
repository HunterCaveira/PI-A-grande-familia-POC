// Importa bibliotecas essenciais da Unity e do Photon
using UnityEngine;
using Photon.Pun;

// A classe herda do Photon e implementa a interface da UI para os tempos de recarga
public class HabilidadeCinto : MonoBehaviourPun, IasUI
{
    [Header("Configurações da Câmera e Alcance")]
    // Removido o [SerializeField], agora procura a câmera sozinho!
    private Camera cameraPrincipal;
    // Distância máxima que o cinto estica no ar
    [SerializeField] private float distanciaMaxima = 15f;

    [Header("Controle de Tempo e Velocidades")]
    // Tempo que o jogador tem de esperar para usar o cinto outra vez
    [SerializeField] private float tempoCooldown = 3.0f;
    // Variável interna que guarda o exato segundo em que o cinto estará pronto
    private float tempoProximoUso = 0f;
    public float TempoProximouso => tempoProximoUso;

    // Velocidade de voo do cinto ao ir para a frente
    [SerializeField] private float velocidadeAvanco = 30f;
    // Velocidade ao puxar o inimigo de volta (deixei mais lento para dar chance de reação)
    [SerializeField] private float velocidadeRetorno = 20f;

    [Header("Configuração de Slot e UI")]
    // Indica que o cinto é a habilidade secundária (Slot 1 ou 2)
    [SerializeField] private int slotIndex = 1;
    public int SlotIndex => slotIndex;

    // Imagens que aparecem na tela do jogador (UI)
    [SerializeField] private Sprite fotoPronta;
    public Sprite FotoPronta => fotoPronta;
    [SerializeField] private Sprite fotoRecarregando;
    public Sprite FotoRecarregando => fotoRecarregando;

    [Header("Controle De Referência e Visuais")]
    // Referência ao script que troca as armas/itens
    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;
    // O modelo 3D do cinto que voa da mão
    [SerializeField] private GameObject prefabCinto;
    // O ponto exato (fivela) que deteta o embate no inimigo
    [SerializeField] private Transform pontoFivelaPontos;
    // Onde a vítima vai parar quando o cinto terminar de recolher
    [SerializeField] private Transform pontoDestinoPuxao;

    [Header("Filtragem de Time")]
    // Para não puxar aliados, configuramos o ID do nosso time
    [SerializeField] private int idDoTime = 1;
    public int IdDoTime => idDoTime;

    // Máquina de estados simples: define o que o cinto está a fazer neste momento
    private enum EstadoCinto { Inativo, Avancando, PuxandoInimigo, RecolhendoVazio }
    private EstadoCinto estadoAtual = EstadoCinto.Inativo;

    // Variáveis que guardam o inimigo que foi laçado para aplicar a física nele
    private Transform inimigoCapturado;
    private CharacterController controllerInimigo;
    private PhotonView pvInimigo;

    // Variáveis de voo corrigidas para o Bug do Tônico
    private Vector3 posicaoInicialLocal;
    private float distanciaAtualVoo = 0f;

    // Awake chamado ao nascer
    private void Awake()
    {
        // Pega as referências do gerenciador e da câmera AUTOMATICAMENTE
        if (gerenciadorDeEquipamentos == null) gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();
        cameraPrincipal = Camera.main;

        // Guarda a posição original do cinto encolhido na mão
        if (prefabCinto != null)
        {
            posicaoInicialLocal = prefabCinto.transform.localPosition;
        }
    }

    // Update chamado todos os frames
    private void Update()
    {
        // Bloqueio de Rede: se não for o meu boneco, não calculo as físicas do cinto
        if (photonView != null && !photonView.IsMine) return;

        // Se a habilidade estiver guardada, ignora o Update inteiro para poupar CPU
        if (estadoAtual == EstadoCinto.Inativo) return;

        // Máquina de Estados decide qual animação/física rodar
        switch (estadoAtual)
        {
            case EstadoCinto.Avancando:
                ProcessarAvanco();
                break;
            case EstadoCinto.PuxandoInimigo:
                ProcessarPuxao();
                break;
            case EstadoCinto.RecolhendoVazio:
                ProcessarRecolhimento();
                break;
        }
    }

    // MUDANÇA PARA O BUG DO TÓNICO: O cinto avança de forma relativa à mira e à mão, não num ponto fixo do ar
    private void ProcessarAvanco()
    {
        if (prefabCinto == null || cameraPrincipal == null) return;

        // Aumenta a distância de voo baseada na velocidade e tempo
        distanciaAtualVoo += velocidadeAvanco * Time.deltaTime;

        // Move o cinto para a frente usando a direção para onde a câmera aponta
        // Isso resolve o bug do tónico, porque se o boneco correr, a mira (câmera) acompanha.
        prefabCinto.transform.position = transform.position + (cameraPrincipal.transform.forward * distanciaAtualVoo);

        // Enquanto voa, checa a cada milímetro se bateu num inimigo (CharacterController/Capsule)
        if (ChecarEPrenderInimigo()) return;

        // Se o cinto voou até ao limite máximo e não achou ninguém
        if (distanciaAtualVoo >= distanciaMaxima)
        {
            estadoAtual = EstadoCinto.RecolhendoVazio; // Muda o estado para puxar de volta
        }
    }

    // Puxa o cinto de volta caso não tenha acertado em ninguém
    private void ProcessarRecolhimento()
    {
        if (prefabCinto == null) return;

        // Encolhe a distância de voo até chegar a zero (volta para a mão)
        distanciaAtualVoo -= velocidadeAvanco * Time.deltaTime;
        prefabCinto.transform.position = transform.position + (cameraPrincipal.transform.forward * distanciaAtualVoo);

        // Se durante a volta ele acidentalmente acertar em alguém que se meteu no caminho, prende a pessoa
        if (ChecarEPrenderInimigo()) return;

        // Quando o cinto voltar totalmente à mão (distância zero ou menor)
        if (distanciaAtualVoo <= 0f)
        {
            FinalizarHabilidade(); // Guarda o cinto
        }
    }

    // Corrigido para Capsule Collider e Character Controller (Raiz do Prefab do Inimigo)
    private bool ChecarEPrenderInimigo()
    {
        // Ponto de checagem do choque é a ponta da fivela
        Vector3 posicaoCheck = pontoFivelaPontos != null ? pontoFivelaPontos.position : prefabCinto.transform.position;
        // Cria uma esfera invisível na fivela para detetar colisões
        Collider[] hits = Physics.OverlapSphere(posicaoCheck, 0.6f);

        // Varre tudo o que a esfera tocou
        foreach (var col in hits)
        {
            // Ignora o meu próprio corpo ou partes do meu modelo
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            // Busca o sistema de vida para ver em que equipa a pessoa tocada está
            Sistemadevida vidaAlvo = col.GetComponent<Sistemadevida>();
            int timeAlvo = -1;
            if (vidaAlvo != null) timeAlvo = vidaAlvo.TimeID;

            // Se achou alguém, e não é do meu time (aliado), a caça começou!
            if (timeAlvo != -1 && timeAlvo != this.idDoTime)
            {
                inimigoCapturado = col.transform; // Guarda a raiz do inimigo capturado
                pvInimigo = col.GetComponent<PhotonView>();

                if (pvInimigo != null)
                {
                    // ROUBA a posse de rede do inimigo e manda o aviso visual para a tela dele
                    pvInimigo.RequestOwnership();
                    pvInimigo.RPC("RPC_EstadoPuxao", RpcTarget.All, true);
                }

                // Acha o CharacterController do inimigo na raiz (Pai) e desliga para podermos arrastar o corpo dele sem o sistema trancar
                controllerInimigo = col.GetComponent<CharacterController>();
                if (controllerInimigo != null) controllerInimigo.enabled = false;

                // Muda o estado para começar o recolhimento com carga
                estadoAtual = EstadoCinto.PuxandoInimigo;
                return true; // Para o Loop, só pega 1 pessoa!
            }
        }
        return false;
    }

    // Puxa o cinto E arrasta o Character Controller do inimigo pelo chão
    private void ProcessarPuxao()
    {
        if (prefabCinto == null) return;

        // Recolhe o cabo do cinto da mesma forma que o recolhimento vazio, mas mais devagar
        distanciaAtualVoo -= velocidadeRetorno * Time.deltaTime;
        // Evita que a distância fique negativa
        distanciaAtualVoo = Mathf.Max(0, distanciaAtualVoo);
        prefabCinto.transform.position = transform.position + (cameraPrincipal.transform.forward * distanciaAtualVoo);

        // Teleporta (arrasta) o corpo do inimigo para ficar colado à fivela a voltar
        if (inimigoCapturado != null)
        {
            Vector3 posicaoFivela = pontoFivelaPontos != null ? pontoFivelaPontos.position : prefabCinto.transform.position;
            inimigoCapturado.position = posicaoFivela;
        }

        // Quando a fivela (e o inimigo) chegarem à mão do caçador
        if (distanciaAtualVoo <= 0.2f)
        {
            FinalizarHabilidade();
        }
    }

    // Guarda a habilidade e solta o refém
    private void FinalizarHabilidade()
    {
        estadoAtual = EstadoCinto.Inativo;
        distanciaAtualVoo = 0f;

        // Se tinha apanhado alguém, liberta!
        if (inimigoCapturado != null)
        {
            // Devolve o comando do boneco e avisa a UI do refém
            if (pvInimigo != null) pvInimigo.RPC("RPC_EstadoPuxao", RpcTarget.All, false);

            // Religa o Character Controller para o inimigo voltar a andar
            if (controllerInimigo != null) controllerInimigo.enabled = true;

            inimigoCapturado = null;
            controllerInimigo = null;
            pvInimigo = null;
        }

        // Devolve o cinto para o inventário
        CancelarHabilidade();

        // Volta a equipar a arma primária
        if (gerenciadorDeEquipamentos != null)
        {
            gerenciadorDeEquipamentos.TrocarEquipamento(0);
        }
    }

    // Cancela abruptamente (usado ao morrer ou trocar de arma enquanto o cinto voava)
    public void CancelarHabilidade()
    {
        estadoAtual = EstadoCinto.Inativo;
        distanciaAtualVoo = 0f;

        // Força a devolução do Character Controller da vítima
        if (controllerInimigo != null)
        {
            controllerInimigo.enabled = true;
            controllerInimigo = null;
        }

        inimigoCapturado = null;
        pvInimigo = null;

        // Recolhe visualmente o cinto para a mão do jogador e esconde
        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.transform.localRotation = Quaternion.identity;
            prefabCinto.SetActive(false);
        }
    }

    // Função de verificação para UI
    public bool EstaPronta()
    {
        return Time.time >= tempoProximoUso && estadoAtual == EstadoCinto.Inativo;
    }
    public bool PodeUsar() { return EstaPronta(); }

    // O Player apertou o botão
    public void AtivarHabilidade()
    {
        // Trava de rede básica
        if (photonView != null && !photonView.IsMine) return;

        // Se está em cooldown, força o saque da arma e aborta
        if (!PodeUsar())
        {
            if (gerenciadorDeEquipamentos != null) gerenciadorDeEquipamentos.TrocarEquipamento(0);
            return;
        }

        // Prepara o cinto visualmente
        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.SetActive(true);
        }

        // Inicia a marcação do tempo de voo e o cooldown
        distanciaAtualVoo = 0f;
        tempoProximoUso = Time.time + tempoCooldown;
        estadoAtual = EstadoCinto.Avancando;
    }

    // 🌐 RPC PARA FEEDBACK DO INIMIGO (Como prometido no problema anterior!)
    [PunRPC]
    public void RPC_EstadoPuxao(bool estaSendoPuxado)
    {
        // Se eu sou a vítima, travo/destravo a leitura do meu teclado e mostro um aviso
        if (photonView.IsMine)
        {
            MovimentodoPlayer mov = GetComponent<MovimentodoPlayer>();
            if (mov != null) mov.enabled = !estaSendoPuxado;

            if (estaSendoPuxado)
            {
                Debug.Log("<color=red>⚠️ VOCÊ FOI LAÇADO!</color>");
            }
            else
            {
                Debug.Log("<color=green>Você foi solto.</color>");
            }
        }
    }
}