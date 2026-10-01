using UnityEngine; // Importa a biblioteca principal do motor da Unity.

public class HabilidadeCinto : MonoBehaviour, IHabilidade // Declara a classe pública HabilidadeCinto herdando de MonoBehaviour e implementando o contrato IHabilidade.
{
    // ==========================================
    // 1. CONFIGURAÇÕES DA CÂMERA E ALCANCE
    // ==========================================

    [Header("Configurações da Câmera e Alcançe")] // Seção de mira e distância no Inspector.

    [SerializeField] // Exibe o campo no Inspector mantendo a variável privada.
    private Camera cameraPrincipal; // Armazena a referência da câmera usada para calcular a direção da mira.

    [SerializeField] // Exibe o campo no Inspector.
    private float distanciaMaxima = 15f; // Alcance máximo em metros que o cinto percorrerá.


    // ==========================================
    // 2. TEMPOS E VELOCIDADES
    // ==========================================

    [Header("Controle de Tempo e Velocidades")] // Seção de tempos e velocidades no Inspector.

    [SerializeField] // Exibe o campo no Inspector.
    private float tempoCooldown = 3.0f; // Tempo de recarga em segundos.

    private float tempoProximoUso = 0f; // Tempo liberado no relógio da Unity (Time.time) para o próximo uso.

    [SerializeField] // Exibe o campo no Inspector.
    private float velocidadeAvanco = 30f; // Velocidade com que o cinto avança em direção à mira.

    [SerializeField] // Exibe o campo no Inspector.
    private float velocidadeRetorno = 20f; // Velocidade com que o cinto e o inimigo retornam para a mão.


    // ==========================================
    // 3. REFERÊNCIAS DE COMPONENTES E VISUAIS
    // ==========================================

    [Header("Controle De Referência e Visuais")] // Seção de conexões e visuais no Inspector.

    [SerializeField] // Exibe o campo no Inspector.
    private GerenciadorDeEquipamentos gerenciadorDeEquipamentos; // Referência do gerenciador de trocas das mãos.

    [SerializeField] // Exibe o campo no Inspector.
    private GameObject prefabCinto; // O objeto do cinto/fivela que se deslocará pelo espaço.

    [SerializeField] // Exibe o campo no Inspector.
    private Transform pontoFivelaPontos; // Ponto da fivela na ponta do cinto usado para detectar colisões.

    [SerializeField] // Exibe o campo no Inspector.
    private Transform pontoDestinoPuxao; // Ponto fixo na mão/peito para onde o inimigo e o cinto retornam.


    // ==========================================
    // 4. COMBATE E MÁQUINA DE ESTADOS
    // ==========================================

    [Header("Filtragem de Time")] // Seção de regras PvP.

    [SerializeField] // Exibe o campo no Inspector.
    private int idDoTime = 1; // ID do time do jogador atual.

    public int IdDoTime => idDoTime; // Propriedade pública de leitura do idDoTime.

    private enum EstadoCinto { Inativo, Avancando, PuxandoInimigo, RecolhendoVazio } // Os 4 estados lógicos do movimento.
    private EstadoCinto estadoAtual = EstadoCinto.Inativo; // Guarda o estado atual da habilidade.

    private Transform inimigoCapturado; // Guarda o Transform do inimigo preso no cinto.
    private CharacterController controllerInimigo; // [NOVO]: Guarda o CharacterController do inimigo para desativar colisão durante o puxão.
    private Rigidbody rbInimigo; // [NOVO]: Guarda o Rigidbody do inimigo para aplicar Kinematic durante o puxão.

    private Vector3 posicaoInicialLocal; // Guarda a posição local neutra do cinto guardado na mão.
    private Vector3 pontoFinalMundo; // Guarda o ponto final tridimensional para onde o cinto avança no disparo.


    // ==========================================
    // 5. CICLO DE VIDA DA UNITY
    // ==========================================

    private void Awake() // Inicialização do script na memória.
    {
        gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>(); // Auto-busca o Gerenciador no próprio GameObject.

        if (prefabCinto != null) // SE a referência do objeto do cinto foi atribuída.
        {
            posicaoInicialLocal = prefabCinto.transform.localPosition; // Guarda a posição local de descanso dentro da mão.
        }
    }

    private void Update() // Atualização frame a frame.
    {
        if (estadoAtual == EstadoCinto.Inativo) return; // SE estiver Inativo, ignora a atualização.

        // MÁQUINA DE ESTADOS DO CINTO
        switch (estadoAtual) // Avalia a variável estadoAtual.
        {
            case EstadoCinto.Avancando: // CASO esteja viajando para a frente em direção à mira.
                ProcessarAvanco(); // Executa o deslocamento e verifica colisões com inimigos.
                break;

            case EstadoCinto.PuxandoInimigo: // CASO tenha capturado um inimigo.
                ProcessarPuxao(); // Traz o cinto e o inimigo colado de volta para a mão.
                break;

            case EstadoCinto.RecolhendoVazio: // CASO esteja voltando para a mão (continua detectando inimigos!).
                ProcessarRecolhimento(); // Traz o cinto de volta e escaneia a área no caminho.
                break;
        }
    }


    // ==========================================
    // 6. LÓGICA DE DETECÇÃO, AVANÇO E PUXÃO
    // ==========================================

    private void ProcessarAvanco() // Desloca o cinto no mundo até o ponto final calculado na mira.
    {
        if (prefabCinto == null) return; // Cancela se o objeto não existir.

        // Mover o cinto em direção ao ponto final no espaço do mundo
        prefabCinto.transform.position = Vector3.MoveTowards(prefabCinto.transform.position, pontoFinalMundo, velocidadeAvanco * Time.deltaTime);

        // Tenta capturar um inimigo no trajeto de avanço
        if (ChecarEPrenderInimigo()) return; // SE capturou alguém, interrompe o avanço (o estado muda dentro da função).

        // SE o cinto atingiu o limite de 15m sem prender nenhum inimigo
        if (Vector3.Distance(prefabCinto.transform.position, pontoFinalMundo) <= 0.2f)
        {
            estadoAtual = EstadoCinto.RecolhendoVazio; // Inicia o recolhimento de volta para a mão.
        }
    }

    private void ProcessarRecolhimento() // Recolhe o cinto para a mão e CONTINUA checando inimigos no caminho de volta.
    {
        if (prefabCinto == null) return; // Cancela se o objeto não existir.

        Vector3 destinoMao = pontoDestinoPuxao != null ? pontoDestinoPuxao.position : transform.position;

        // Mover o cinto de volta para a posição da mão no mundo
        prefabCinto.transform.position = Vector3.MoveTowards(prefabCinto.transform.position, destinoMao, velocidadeAvanco * Time.deltaTime);

        // [NOVO / AJUSTADO]: Continua verificando se algum inimigo entra em contato com a fivela mesmo enquanto o cinto está voltando!
        if (ChecarEPrenderInimigo()) return; // SE um inimigo encostar na fivela na volta, prende ele e muda para PuxandoInimigo!

        // SE o cinto chegou de volta à mão sem pegar ninguém
        if (Vector3.Distance(prefabCinto.transform.position, destinoMao) <= 0.2f)
        {
            FinalizarHabilidade(); // Conclui e troca para a arma principal.
        }
    }

    private bool ChecarEPrenderInimigo() // [NOVO]: Função unificada para checar colisão da fivela tanto no avanço quanto no retorno.
    {
        Vector3 posicaoCheck = pontoFivelaPontos != null ? pontoFivelaPontos.position : prefabCinto.transform.position;
        Collider[] hits = Physics.OverlapSphere(posicaoCheck, 0.6f); // Esfera invisível de impacto na fivela.

        foreach (var col in hits)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue; // Ignora o próprio jogador.

            Sistemadevida vidaAlvo = col.GetComponent<Sistemadevida>();
            HabilidadeCinto scriptAlvo = col.GetComponent<HabilidadeCinto>();

            int timeAlvo = -1;
            if (vidaAlvo != null) timeAlvo = vidaAlvo.TimeID;
            else if (scriptAlvo != null) timeAlvo = scriptAlvo.IdDoTime;

            // SE for um inimigo válido no PvP
            if (timeAlvo != -1 && timeAlvo != this.idDoTime)
            {
                inimigoCapturado = col.transform; // Captura o Transform do inimigo.

                // [SOLUÇÃO DO EMPURRÃO]: Desativa temporariamente a física do inimigo para não dar conflito durante o movimento
                controllerInimigo = col.GetComponent<CharacterController>();
                if (controllerInimigo != null) controllerInimigo.enabled = false; // Desativa o CharacterController para não "chutar" o inimigo.

                rbInimigo = col.GetComponent<Rigidbody>();
                if (rbInimigo != null) rbInimigo.isKinematic = true; // Trava a física do Rigidbody.

                estadoAtual = EstadoCinto.PuxandoInimigo; // Muda o estado para puxar.
                return true; // Retorna true confirmando a captura.
            }
        }

        return false; // Nenhum inimigo capturado.
    }

    private void ProcessarPuxao() // Trava o inimigo capturado e arrasta de volta para a mão do jogador.
    {
        if (prefabCinto == null) return; // Cancela se o objeto não existir.

        Vector3 destinoMao = pontoDestinoPuxao != null ? pontoDestinoPuxao.position : transform.position;

        // Move a posição do cinto de volta para a mão
        prefabCinto.transform.position = Vector3.MoveTowards(prefabCinto.transform.position, destinoMao, velocidadeRetorno * Time.deltaTime);

        // Trava o corpo do inimigo colado na fivela durante o movimento de retorno
        if (inimigoCapturado != null)
        {
            Vector3 posicaoFivela = pontoFivelaPontos != null ? pontoFivelaPontos.position : prefabCinto.transform.position;
            inimigoCapturado.position = posicaoFivela; // Garante que o inimigo venha colado na fivela sem sofrer empurrão físico.
        }

        // SE o cinto e o inimigo chegaram perto da mão do jogador (distância <= 0.8m)
        if (Vector3.Distance(prefabCinto.transform.position, destinoMao) <= 0.8f)
        {
            FinalizarHabilidade(); // Conclui o puxão e libera a física do inimigo.
        }
    }

    private void FinalizarHabilidade() // Restaura as físicas, reseta as posições locais e efetua a troca no Gerenciador.
    {
        estadoAtual = EstadoCinto.Inativo; // Reseta o estado interno.

        // [RESTAURAÇÃO DA FÍSICA DO INIMIGO]: Devolve o controle do CharacterController e Rigidbody do inimigo ao chegar
        if (controllerInimigo != null)
        {
            controllerInimigo.enabled = true; // Reativa a movimentação normal do inimigo.
            controllerInimigo = null;
        }

        if (rbInimigo != null)
        {
            rbInimigo.isKinematic = false; // Reativa a física do Rigidbody.
            rbInimigo = null;
        }

        inimigoCapturado = null; // Libera o vínculo do inimigo.

        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal; // Restaura a posição local neutra de descanso na mão.
            prefabCinto.transform.localRotation = Quaternion.identity; // Restaura a rotação local padrão.
        }

        CancelarHabilidade(); // Oculta o visual no jogo.

        if (gerenciadorDeEquipamentos != null)
        {
            gerenciadorDeEquipamentos.TrocarEquipamento(0); // Solicita a troca do visual da mão para a Arma Principal (Slot 0).
        }
    }


    // ==========================================
    // 7. MÉTODOS DA INTERFACE IHABILIDADE
    // ==========================================

    public bool EstaPronta() // Pergunta contratada pela interface IHabilidade.
    {
        return Time.time >= tempoProximoUso && estadoAtual == EstadoCinto.Inativo;
    }

    public void AtivarHabilidade() // Método chamado ao apertar a tecla associada à habilidade.
    {
        if (!EstaPronta())
        {
            if (gerenciadorDeEquipamentos != null)
            {
                gerenciadorDeEquipamentos.TrocarEquipamento(0);
            }
            return;
        }

        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.SetActive(true);
        }

        // CÁLCULO DA MIRA EM QUALQUER DIREÇÃO
        Ray ray = cameraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        pontoFinalMundo = ray.origin + ray.direction * distanciaMaxima;

        if (Physics.Raycast(ray, out RaycastHit hit, distanciaMaxima))
        {
            pontoFinalMundo = hit.point;
        }

        if (prefabCinto != null)
        {
            prefabCinto.transform.LookAt(pontoFinalMundo); // Aponta o cinto para o ponto da mira.
        }

        tempoProximoUso = Time.time + tempoCooldown;
        estadoAtual = EstadoCinto.Avancando; // Inicia o avanço.
    }

    public void CancelarHabilidade() // Método de cancelamento e limpeza.
    {
        estadoAtual = EstadoCinto.Inativo;

        // Garante a reativação da física do inimigo caso a habilidade seja cancelada bruscamente
        if (controllerInimigo != null)
        {
            controllerInimigo.enabled = true;
            controllerInimigo = null;
        }

        if (rbInimigo != null)
        {
            rbInimigo.isKinematic = false;
            rbInimigo = null;
        }

        inimigoCapturado = null;

        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.transform.localRotation = Quaternion.identity;
            prefabCinto.SetActive(false); // Oculta o cinto.
        }
    }
}