// Importa a biblioteca padrão do C# para manipulação de eventos e dados
using System;
// Importa a biblioteca principal da Unity
using UnityEngine;
// Importa a biblioteca do Photon para rede e sincronização multiplayer
using Photon.Pun;

public class Sistemadevida : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    // ==========================================
    // 1. ATRIBUTOS E VARIÁVEIS
    // ==========================================

    [Header("Configurações do Herói")]
    [SerializeField] private AtributosPersonagem dadosDoHeroi;

    [Header("PvP e Partida")]
    [SerializeField] private int timeID = 1;
    [SerializeField] public float vidaAtual;

    [Header("Debug")]
    [SerializeField] private bool logsDeRede = true;

    // Setter público mantém compatível com GerenciadorDeSpawn e Respawn (vida.TimeID = x)
    public int TimeID
    {
        get => timeID;
        set => timeID = value;
    }

    // Porcentagem de redução de dano vinda de buffs (ex: Tónico)
    public float PorcentagemReducaoDano { get; set; } = 0f;

    // Evento C# que avisa a barra de vida na UI sempre que a vida muda (Vida Atual, Vida Máxima)
    public event Action<float, float> OnVidaMudou;

    // ==========================================
    // 2. CICLO DE VIDA
    // ==========================================

    private void Awake()
    {
        // Se a ficha de atributos estiver preenchida, define a vida inicial como a vida máxima
        if (dadosDoHeroi != null)
            vidaAtual = dadosDoHeroi.vidaMaxima;
    }

    private void Start()
    {
        // Notifica a UI logo no início para a barra não começar vazia
        NotificarMudancaDeVida();
    }

    // Chamado em TODOS os clientes quando o personagem chega pela rede via PhotonNetwork.Instantiate
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // Lê os dados extras enviados no momento do spawn (ex: ID do time)
        object[] dados = info.photonView.InstantiationData;
        if (dados != null && dados.Length > 0)
            timeID = (int)dados[0];

        // Exibe logs de depuração se ativado
        if (logsDeRede)
            Debug.Log($"[SPAWN REDE] viewID={info.photonView.ViewID} dono={info.photonView.Owner?.NickName} souEu={info.photonView.IsMine} time={timeID}");
    }

    private void OnDestroy()
    {
        // Evita logs desnecessários se o photonView for nulo ao destruir o objeto
        if (!logsDeRede || photonView == null) return;
        Debug.Log($"[DESTROY] viewID={photonView.ViewID} souEu={photonView.IsMine}\n{Environment.StackTrace}");
    }

    // ==========================================
    // 3. FICHA DE ATRIBUTOS
    // ==========================================

    // Atualiza os atributos do herói em tempo real (usado quando o Tónico é bebido)
    public void AtualizarFichaAtributos(AtributosPersonagem novaFicha)
    {
        if (novaFicha != null)
        {
            dadosDoHeroi = novaFicha;
            NotificarMudancaDeVida();
            Debug.Log($"[SISTEMA DE VIDA] Ficha de atributos atualizada! Vida máxima da ficha: {dadosDoHeroi.vidaMaxima}");
        }
    }

    // ==========================================
    // 4. DANO E MORTE (rede)
    // ==========================================

    // Método antigo mantido por compatibilidade com outros scripts do projeto
    public void ReceberDano(InformacaoDano dano)
    {
        if (vidaAtual <= 0) return;

        // Filtro de Fogo Amigo: Impede que você cause dano a aliados do mesmo time
        if (dano.atacante != null && dano.atacante.TryGetComponent<Sistemadevida>(out Sistemadevida atacanteVida))
        {
            if (atacanteVida.TimeID == timeID && dano.atacante != gameObject)
                return;
        }

        // Se eu sou o dono, aplico o dano direto. Se não, mando um RPC para o dono do boneco.
        if (photonView.IsMine)
            AplicarDano(dano.quantidade);
        else
            photonView.RPC(nameof(RPC_AplicarDano), photonView.Owner, dano.quantidade);
    }

    // ==========================================
    // 🔗 PONTE COM A BALA (NOVO & OTIMIZADO)
    // ==========================================
    // Este é o método exato que o script da Bala.cs chama via RPC:
    // pvInimigo.RPC("ReceberDanoRPC", RpcTarget.All, danoDaBala, viewIDDoAtirador);
    [PunRPC]
    public void ReceberDanoRPC(float quantidadeDano, int viewIDAtirador)
    {
        // Trava de segurança: Se o personagem já estiver morto, ignora novos danos
        if (vidaAtual <= 0) return;

        // Se este computador é o dono legítimo deste boneco, processa o dano na vida real
        if (photonView.IsMine)
        {
            AplicarDano(quantidadeDano);
        }
    }

    // RPC auxiliar caso outros scripts chamem diretamente o dano cru na rede
    [PunRPC]
    private void RPC_AplicarDano(float quantidade)
    {
        AplicarDano(quantidade);
    }

    // Só roda no dono do personagem (autoridade da vida)
    private void AplicarDano(float quantidade)
    {
        if (vidaAtual <= 0) return;

        // Calcula o dano final aplicando a redução de armadura (ex: buff do tónico)
        float danoFinal = quantidade * (1f - PorcentagemReducaoDano);
        // Subtrai a vida e garante que ela nunca fique abaixo de zero
        vidaAtual = Mathf.Max(vidaAtual - danoFinal, 0f);

        // Atualiza a UI localmente
        NotificarMudancaDeVida();
        // Sincroniza a vida atualizada com os outros jogadores na sala
        photonView.RPC(nameof(RPC_SincronizarVida), RpcTarget.Others, vidaAtual);

        // Se a vida chegou a zero, aciona o método de morte
        if (vidaAtual <= 0)
            Morrer();
    }

    // RPC para atualizar o valor da vida em tempo real nos computadores dos outros jogadores
    [PunRPC]
    private void RPC_SincronizarVida(float novaVida)
    {
        vidaAtual = novaVida;
        NotificarMudancaDeVida();
    }

    // Restaura a vida máxima do herói ao renascer
    public void Renascer()
    {
        if (dadosDoHeroi == null) return;

        vidaAtual = dadosDoHeroi.vidaMaxima;
        PorcentagemReducaoDano = 0f;
        NotificarMudancaDeVida();

        // Avisa os outros jogadores que o herói renasceu com vida cheia
        if (photonView.IsMine)
            photonView.RPC(nameof(RPC_SincronizarVida), RpcTarget.Others, vidaAtual);
    }

    // Dispara o evento de escuta para atualizar qualquer barra de vida conectada
    private void NotificarMudancaDeVida()
    {
        if (dadosDoHeroi != null)
            OnVidaMudou?.Invoke(vidaAtual, dadosDoHeroi.vidaMaxima);
    }

    // Processa a morte do herói e aciona o sistema centralizado de Respawn no GameManager
    private void Morrer()
    {
        if (!photonView.IsMine) return;

        Debug.Log($"[MORTE] {gameObject.name} viewID={photonView.ViewID} time={timeID}");

        // Fluxo principal moderno: Usa o gerenciador de Respawn centralizado no GameManager do mapa
        if (Respawn.Instancia != null)
        {
            Respawn.Instancia.SolicitarRespawn(timeID, "Pai");
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            // Fluxo antigo (fallback de segurança caso o GameManager não tenha o script Respawn)
            GerenciadorRespawn gerenciador = FindFirstObjectByType<GerenciadorRespawn>();
            if (gerenciador != null)
                gerenciador.ProcessarMorte(gameObject);
            else
                Debug.LogWarning("Nenhum Respawn nem GerenciadorRespawn encontrado na cena!");
        }
    }
}