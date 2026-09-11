using UnityEngine; // Importa a biblioteca principal do motor da Unity para acessar MonoBehaviour, Vector3, Physics, etc.

public class HabilidadeCinto : MonoBehaviour, IHabilidade // Declara a classe pública HabilidadeCinto herdando de MonoBehaviour e implementando o contrato da interface IHabilidade.
{
    // ==========================================
    // 1. CONFIGURAÇÕES DA CÂMERA E MIRA
    // ==========================================

    [Header("Configurações da Câmera e Raio")] // Organiza a seção no Inspector da Unity.

    [SerializeField] // Permite que a variável privada seja configurada no Inspector da Unity mantendo o encapsulamento.
    private Camera cameraPrincipal; // Armazena a referência da câmera do jogador usada para projetar a trajetória de lançamento da fivela.

    [SerializeField] // Exibe o campo configurável no Inspector.
    private float distanciaMaxima = 15f; // Alcance máximo em metros que a fivela conseguirá viajar pelo ar a partir do ponto de origem.


    // ==========================================
    // 2. REGRAS DE TEMPO E VELOCIDADE
    // ==========================================

    [Header("Controle de Tempo e Velocidades")] // Seção de tempos e velocidades no Inspector.

    [SerializeField] // Exibe o campo no Inspector.
    private float tempoCooldown = 3.0f; // Armazena o tempo em segundos que o jogador precisa esperar após disparar a habilidade para usar novamente.

    private float tempoProximoUso = 0f; // Guarda o segundo exato do relógio do jogo (Time.time) em que a habilidade ficará disponível novamente.

    [SerializeField] // Exibe o campo no Inspector.
    private float velocidadeLancamento = 30f; // Armazena a velocidade em metros por segundo com que a fivela viaja para a frente ao ser disparada.

    [SerializeField] // Exibe o campo no Inspector.
    private float velocidadePuxao = 20f; // Armazena a velocidade em metros por segundo com que a fivela e o inimigo são arrastados de volta ao jogador.


    // ==========================================
    // 3. REFERÊNCIAS DE COMPONENTES
    // ==========================================

    [Header("Controle De Referência")] // Seção de conexões de scripts no Inspector.

    [SerializeField] // Exibe o campo no Inspector para atribuição de referência.
    private GerenciadorDeEquipamentos gerenciadorDeEquipamentos; // Armazena a referência do script gerenciador responsável por alternar as armas e itens da mão.


    // ==========================================
    // 4. PREFABS E PONTOS VISUAIS (FIVELA E CINTO)
    // ==========================================

    [Header("Prefab e Pontos Visuais")] // Seção de objetos e elementos visuais 3D.

    [SerializeField] // Exibe o campo no Inspector.
    private Transform fivelaPontos; // Armazena o Transform do objeto da fivela que fica dentro do Prefab_Cinto para ser ativado em conjunto pelo Gerenciador.

    [SerializeField] // Exibe o campo no Inspector.
    private GameObject prefabCinto; // Armazena a referência do GameObject pai ativador da habilidade na mão.

    [SerializeField] // Exibe no Inspector a referência exclusiva da tira/malha visual do cinto.
    private Transform fitaCintoVisual; // Armazena o Transform do filho 'Cinto' que sofrerá o esticamento visual no eixo Z.

    [SerializeField] // Exibe o campo no Inspector.
    private Transform pontoOrigemMao; // Armazena o Transform do ponto fixo na mão do personagem de onde a tira do cinto nasce e é ancorada.


    // ==========================================
    // 5. FILTRAGEM DE TIME E PUXÃO
    // ==========================================

    [Header("Filtragem de Inimigos e Puxão")] // Seção das regras de combate PvP.

    [SerializeField] // Exibe o campo no Inspector.
    private int idDoTime = 1; // Armazena o número identificador do time do personagem atual (Ex: Time 1 = Azul, Time 2 = Vermelho).

    [SerializeField] // Exibe o campo no Inspector.
    private Transform pontoDestinoPuxao; // Armazena o Transform da posição (perto do peito ou mão) para onde o inimigo capturado será trazido.


    // ==========================================
    // 6. ESTADO INTERNO DO GANCHO (MÁQUINA DE ESTADOS)
    // ==========================================

    public int IdDoTime => idDoTime; // Propriedade pública de leitura que expõe o número idDoTime para outros scripts consultarem de forma segura.

    private enum EstadoCinto { Inativo, Lancando, PuxandoInimigo, RecolhendoVazio } // Enumeração que define os 4 estados lógicos possíveis para a execução do cinto.
    private EstadoCinto estadoAtual = EstadoCinto.Inativo; // Armazena a variável com o estado atual em que a habilidade se encontra no momento.

    private Vector3 pontoAlvoDestino; // Guarda as coordenadas x, y, z no espaço tridimensional para onde a fivela deve viajar durante o disparo.
    private Transform inimigoCapturado; // Armazena a referência do Transform do inimigo atingido e preso na fivela durante a fase de puxão.
    private Vector3 posInicialFivelaLocal; // Guarda a posição local original da fivela presa à estrutura da mão para restaurar ao término da habilidade.


    // ==========================================
    // 7. MÉTODOS DO CICLO DE VIDA DA UNITY
    // ==========================================

    private void Awake() // Método executado automaticamente pela Unity no momento em que a cena e o objeto são carregados na memória.
    {
        gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>(); // Busca e atribui o GerenciadorDeEquipamentos anexado ao mesmo GameObject do jogador.

        if (fivelaPontos != null) // SE a referência do Transform da fivela tiver sido atribuída no Inspector.
        {
            posInicialFivelaLocal = fivelaPontos.localPosition; // Armazena as coordenadas locais de descanso da fivela dentro do modelo da mão.
        }
    }

    private void Update() // Método executado automaticamente pela Unity a cada frame do jogo para atualizar lógicas em tempo real.
    {
        if (estadoAtual == EstadoCinto.Inativo) return; // SE a habilidade estiver no estado Inativo, cancela o resto do método para economizar processamento.

        AtualizarEsticamentoDoCinto(); // Executa o cálculo de redimensionamento e alinhamento da malha do cinto entre a mão e a fivela no frame atual.

        // MÁQUINA DE ESTADOS DO CINTO: Executa o código apropriado dependendo do estado atual do gancho
        switch (estadoAtual) // Avalia o valor da variável estadoAtual.
        {
            case EstadoCinto.Lancando: // CASO o cinto esteja na fase de lançamento para a frente.
                ProcessarLancamentoFivela(); // Chama o método responsável por mover a fivela e testar impactos com inimigos no trajeto.
                break; // Interrompe a checagem do switch.

            case EstadoCinto.PuxandoInimigo: // CASO a fivela já tenha agarrado um inimigo e esteja trazendo ele de volta.
                ProcessarPuxaoInimigo(); // Chama o método responsável por arrastar a fivela e o inimigo em direção ao jogador.
                break; // Interrompe a checagem do switch.

            case EstadoCinto.RecolhendoVazio: // CASO o disparo tenha errado o alvo ou atingido o ar/cenário.
                ProcessarRecolhimentoVazio(); // Chama o método responsável por trazer a fivela de volta para a mão de forma limpa.
                break; // Interrompe a checagem do switch.
        }
    }


    // ==========================================
    // 8. LÓGICA DE MOVIMENTAÇÃO E DETECÇÃO DA FIVELA
    // ==========================================

    private void ProcessarLancamentoFivela() // Método privado para controlar o movimento da fivela para a frente durante o lançamento.
    {
        // Move a posição tridimensional da fivela suavemente em direção ao pontoAlvoDestino no ar com base na velocidade configurada
        fivelaPontos.position = Vector3.MoveTowards(fivelaPontos.position, pontoAlvoDestino, velocidadeLancamento * Time.deltaTime); // Mover fivela frame a frame de forma independente da taxa de quadros.

        // DETECÇÃO NA PONTA: Cria uma esfera invisível de colisão ao redor da fivela no espaço para checar o impacto no alvo
        Collider[] hits = Physics.OverlapSphere(fivelaPontos.position, 0.5f); // Salva em um array todos os colliders que encostarem na esfera de 0.5m da fivela.

        foreach (var col in hits) // Percorre cada collider detectado na esfera de impacto através de um laço foreach.
        {
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue; // SE o collider pertencer ao próprio jogador ou a seus filhos, ignora e passa para o próximo.

            Sistemadevida vidaAlvo = col.GetComponent<Sistemadevida>(); // Busca o componente Sistemadevida no objeto atingido pela esfera da fivela.
            HabilidadeCinto scriptAlvo = col.GetComponent<HabilidadeCinto>(); // Busca o componente HabilidadeCinto no objeto para extrair a identificação do time.

            int timeAlvo = -1; // Declara uma variável inteira local para armazenar o ID do time encontrado no objeto (-1 representa sem time).
            if (vidaAlvo != null) timeAlvo = vidaAlvo.TimeID; // SE encontrou Sistemadevida, armazena o TimeID contido nele.
            else if (scriptAlvo != null) timeAlvo = scriptAlvo.IdDoTime; // SENÃO SE encontrou HabilidadeCinto, armazena a propriedade IdDoTime dele.

            // SE encontrou um time válido (!= -1) E o time do alvo for diferente do meu time atual (é um inimigo válido no PvP)
            if (timeAlvo != -1 && timeAlvo != this.idDoTime)
            {
                inimigoCapturado = col.transform; // Guarda o Transform do inimigo atingido para o puxão acompanhar o seu corpo.
                estadoAtual = EstadoCinto.PuxandoInimigo; // Altera o estado da habilidade para a fase de puxar o inimigo de volta.
                return; // Encerra o método de lançamento para começar a fase de recolhimento no próximo frame.
            }
        }

        // SE a fivela chegou muito próxima da distância máxima limite do pontoAlvoDestino sem prender nenhum inimigo
        if (Vector3.Distance(fivelaPontos.position, pontoAlvoDestino) <= 0.2f)
        {
            estadoAtual = EstadoCinto.RecolhendoVazio; // Altera o estado para iniciar o recolhimento da fivela de volta à mão.
        }
    }

    private void ProcessarPuxaoInimigo() // Método privado para arrastar o inimigo capturado na fivela até a posição do jogador.
    {
        if (inimigoCapturado == null) // SE o objeto do inimigo capturado for destruído ou desconectado durante o trajeto.
        {
            FinalizarHabilidade(); // Executa o encerramento seguro da habilidade para evitar erros de referência nula.
            return; // Aborta a execução do puxão.
        }

        // Mantém a posição do inimigo travada exatamente na mesma coordenada da fivela enquanto ela é puxada
        inimigoCapturado.position = fivelaPontos.position; // Força o transform do inimigo a acompanhar a fivela no espaço 3D.

        // Desloca a fivela (com o inimigo colado nela) em direção ao pontoDestinoPuxao próximo ao peito/mão do jogador
        fivelaPontos.position = Vector3.MoveTowards(fivelaPontos.position, pontoDestinoPuxao.position, velocidadePuxao * Time.deltaTime); // Deslocamento suave com velocidade constante.

        // SE a fivela e o inimigo chegaram perto o suficiente da posição de destino final do jogador (distância <= 0.8 metros)
        if (Vector3.Distance(fivelaPontos.position, pontoDestinoPuxao.position) <= 0.8f)
        {
            FinalizarHabilidade(); // Conclui o processo de puxão e restaura o estado inicial do jogador.
        }
    }

    private void ProcessarRecolhimentoVazio() // Método privado para recolher a fivela de volta para a mão quando o tiro acerta o ar ou cenário.
    {
        // Move a posição da fivela de volta em direção ao pontoOrigemMao na mão do personagem
        fivelaPontos.position = Vector3.MoveTowards(fivelaPontos.position, pontoOrigemMao.position, velocidadeLancamento * Time.deltaTime); // Mover fivela de volta ao ponto de origem.

        // SE a fivela já retornou totalmente para a posição da mão do jogador (distância <= 0.2 metros)
        if (Vector3.Distance(fivelaPontos.position, pontoOrigemMao.position) <= 0.2f)
        {
            FinalizarHabilidade(); // Conclui o recolhimento e efetua a troca automática para a arma principal.
        }
    }

    private void AtualizarEsticamentoDoCinto() // Método privado para alinhar e ajustar a escala do cinto sem alterar a fivela.
    {
        // Define qual objeto visual sofrerá a alteração da escala (prioriza a fitaCintoVisual se estiver atribuída no Inspector).
        Transform alvoVisual = fitaCintoVisual != null ? fitaCintoVisual : (prefabCinto != null ? prefabCinto.transform : null);
        if (alvoVisual == null || fivelaPontos == null || pontoOrigemMao == null) return; // Cancela se faltarem referências no Inspector.

        // Calcula a direção e a distância do vetor 3D entre a mão do personagem e a posição da fivela voando pelo mundo
        Vector3 direcao = fivelaPontos.position - pontoOrigemMao.position;
        float distancia = direcao.magnitude; // Extrai a distância em metros a partir da magnitude do vetor.

        // TRAVA DE SEGURANÇA: Se a fivela estiver colada na mão (< 0.05m), aplica um valor mínimo para não corromper o PhysX da Unity com escala 0
        if (distancia < 0.05f)
        {
            distancia = 0.05f;
        }

        // Trava o ponto inicial da fita do cinto fixado na mão do personagem
        alvoVisual.position = pontoOrigemMao.position;

        // Só rotaciona a fita do cinto na direção da fivela se houver um vetor de direção válido (evita erros com vetor nulo)
        if (direcao != Vector3.zero)
        {
            alvoVisual.rotation = Quaternion.LookRotation(direcao); // Faz a fita apontar a sua frente diretamente para a fivela.
        }

        // Aplica o comprimento esticado no eixo Z equivalente à distância, preservando as dimensões neutras nos eixos X e Y
        alvoVisual.localScale = new Vector3(1f, 1f, distancia);
    }

    private void FinalizarHabilidade() // Método privado para restaurar o estado padrão do script e guardar a fivela.
    {
        estadoAtual = EstadoCinto.Inativo; // Redefine o estado interno para Inativo.
        inimigoCapturado = null; // Libera a referência do inimigo capturado.

        // RESTAURAÇÃO DO PARENTEAMENTO: Recoloca a Fivela como filha de volta dentro do prefabCinto antes de ocultar
        if (fivelaPontos != null && prefabCinto != null)
        {
            fivelaPontos.SetParent(prefabCinto.transform); // Reanexa a fivela na hierarquia do Prefab_Cinto na Unity.
            fivelaPontos.localPosition = posInicialFivelaLocal; // Restaura as coordenadas locais originais de descanso dentro do modelo.
            fivelaPontos.gameObject.SetActive(false); // Desativa o objeto visual da fivela.
        }

        Transform alvoVisual = fitaCintoVisual != null ? fitaCintoVisual : (prefabCinto != null ? prefabCinto.transform : null);
        if (alvoVisual != null) // SE a referência do cinto visual for válida.
        {
            alvoVisual.localScale = Vector3.one; // Restaura a escala tridimensional do cinto de volta para o padrão neutro (1, 1, 1).
        }

        CancelarHabilidade(); // Executa o desligamento do modo ativo e desativa os objetos visuais.

        if (gerenciadorDeEquipamentos != null) // SE a referência do GerenciadorDeEquipamentos estiver atribuída.
        {
            gerenciadorDeEquipamentos.TrocarEquipamento(0); // Solicita ao Gerenciador o retorno automático para a Arma Principal (Slot 0).
        }
    }


    // ==========================================
    // 9. MÉTODOS MANTIDOS PELA INTERFACE IHABILIDADE
    // ==========================================

    public bool EstaPronta() // Pergunta pública contratada pela interface IHabilidade para checar se a habilidade pode ser usada.
    {
        // Retorna true SE o tempo do jogo for maior/igual ao tempoLiberado E se o cinto estiver no estado Inativo.
        return Time.time >= tempoProximoUso && estadoAtual == EstadoCinto.Inativo;
    }

    public void AtivarHabilidade() // Método público contratado pela interface IHabilidade executado quando a tecla da habilidade é pressionada.
    {
        if (!EstaPronta()) // SE a pergunta EstaPronta() retornar falso (habilidade em recarga/cooldown ou em execução).
        {
            if (gerenciadorDeEquipamentos != null) // SE a referência do GerenciadorDeEquipamentos for válida.
            {
                gerenciadorDeEquipamentos.TrocarEquipamento(0); // Força a permanência ou retorno da mão na Arma Principal (Slot 0).
            }
            return; // Aborta a ativação da habilidade.
        }

        // 1. O GerenciadorDeEquipamentos liga o objeto pai Prefab_Cinto na mão do personagem
        if (prefabCinto != null)
        {
            prefabCinto.SetActive(true); // Exibe o modelo 3D do cinto na mão do jogador.
        }

        // 2. Garante a ativacao visual da Fivela e desvincula o parenteamento temporariamente durante o voo
        if (fivelaPontos != null)
        {
            fivelaPontos.gameObject.SetActive(true); // Exibe o modelo da fivela.
            fivelaPontos.SetParent(null); // [SOLUÇÃO]: Solta a fivela do pai temporariamente para o esticamento do cinto não deformar sua escala!
        }

        // 3. Projeta a mira: Cria um raio saindo do centro exato da tela (Viewport x:0.5, y:0.5) para definir o ponto de voo
        Ray ray = cameraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)); // Projeta o raio da câmera principal.
        pontoAlvoDestino = ray.origin + ray.direction * distanciaMaxima; // Calcula a posição tridimensional de alcance limite.

        // Se o raio encontrar uma parede ou superfície no caminho, ajusta o ponto limite final para essa colisão
        if (Physics.Raycast(ray, out RaycastHit hit, distanciaMaxima))
        {
            pontoAlvoDestino = hit.point; // Define o impacto exato na superfície como o destino da fivela.
        }

        tempoProximoUso = Time.time + tempoCooldown; // Registra o tempo do relógio em que a habilidade poderá ser reusada.
        estadoAtual = EstadoCinto.Lancando; // Altera o estado interno da habilidade para Lancando, liberando a movimentação no Update.
    }

    public void CancelarHabilidade() // Método público contratado pela interface IHabilidade para interromper e limpar o cinto limpamente.
    {
        estadoAtual = EstadoCinto.Inativo; // Reseta a variável de controle da máquina de estados para Inativo.
        inimigoCapturado = null; // Remove a referência de qualquer inimigo vinculado.

        // Garante que a fivela retorne para dentro do Prefab_Cinto antes de ser desativada
        if (fivelaPontos != null && prefabCinto != null)
        {
            fivelaPontos.SetParent(prefabCinto.transform); // Devolve o vínculo de filho do Prefab_Cinto.
            fivelaPontos.localPosition = posInicialFivelaLocal; // Restaura as coordenadas locais originais.
            fivelaPontos.gameObject.SetActive(false); // Oculta a fivela.
        }

        Transform alvoVisual = fitaCintoVisual != null ? fitaCintoVisual : (prefabCinto != null ? prefabCinto.transform : null);
        if (alvoVisual != null) // SE o objeto visual existir.
        {
            alvoVisual.localScale = Vector3.one; // Reseta a escala tridimensional de volta para o padrão (1, 1, 1).
        }

        if (prefabCinto != null)
        {
            prefabCinto.SetActive(false); // Oculta o objeto pai do cinto sem estragar o estado da mão da arma.
        }
    }
}