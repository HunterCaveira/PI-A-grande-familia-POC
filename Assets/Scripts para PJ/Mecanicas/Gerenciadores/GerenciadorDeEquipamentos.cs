// Importa a biblioteca base da Unity
using UnityEngine;
// Importa o sistema novo de botões do teclado e comando
using UnityEngine.InputSystem;
// Importa o Photon para controlar o multiplayer
using Photon.Pun;

// A classe herda de MonoBehaviourPun para termos o photonView embutido
public class GerenciadorDeEquipamentos : MonoBehaviourPun
{
    [Header("Configurações Visuais das Mãos")]
    [Tooltip("Arraste o objeto 'arma' ou 'Ponto_Maos' que contém as habilidades como filhos.")]
    // Ponto onde as armas e itens ficam anexados.
    // Ex: Filho 0 (Pistola), Filho 1 (Parede), Filho 2 (Cinto), Filho 3 (Tonico)
    [SerializeField] private Transform pontoMaos;

    [Header("Configurações de Tempo")]
    // Segundos que o jogador tem de esperar entre trocar de arma (evita spam de cliques)
    [SerializeField] private float tempoAnimacaoTroca = 0.5f;
    // Variável interna que guarda quando a próxima troca será permitida
    private float tempoLiberadoParaTrocar = 0f;

    [Header("Controle Interno")]
    // Memoriza qual é a arma ou habilidade que o boneco está a segurar no momento
    private int indiceEquipamentoAtual = 0;
    // Guarda o clique do jogador numa fila de espera (-1 significa que nada foi clicado)
    private int proximoIndicePendente = -1;

    // Lista com todos os scripts de habilidade do personagem (Parede, Cinto, Tonico, etc.)
    private IasUI[] habilidadesDoPersonagem;

    // Awake roda uma única vez no início
    void Awake()
    {
        // Vasculha este GameObject (o Pai) e guarda todos os scripts que possuam a interface IasUI
        habilidadesDoPersonagem = GetComponents<IasUI>();
    }

    // Start roda na primeira frame em que o boneco existe
    void Start()
    {
        // Força o boneco a segurar a arma primária (0) ao dar spawn.
        if (photonView != null && photonView.IsMine)
        {
            photonView.RPC("RPC_SincronizarMaos", RpcTarget.AllBuffered, 0);
        }
    }

    // Update roda a todos os frames
    void Update()
    {
        // 🔒 Trava de Rede e Morte: Se o personagem for de outro jogador OU se este script estiver desativado por morte, ignora os inputs
        if (photonView != null && !photonView.IsMine) return;
        if (!enabled) return;

        // Verifica se o teclado não está desconectado
        if (Keyboard.current != null)
        {
            // Se eu apertar Q, preparo a habilidade Parede (Slot 1)
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 1;
            }
            // Se eu apertar E, preparo a habilidade Cinto (Slot 2)
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 2;
            }
            // Se eu apertar F, preparo a habilidade Tonico (Slot 3)
            else if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 3;
            }
        }

        // Se o tempo da animação já acabou E o jogador clicou para trocar (diferente de -1)
        if (Time.time >= tempoLiberadoParaTrocar && proximoIndicePendente != -1)
        {
            // Executa a função que faz a troca de facto
            TrocarEquipamento(proximoIndicePendente);
            // Reseta a fila de espera
            proximoIndicePendente = -1;
        }
    }

    // Função que processa a mudança do item atual
    // Função que processa a mudança do item atual
    public void TrocarEquipamento(int novoIndice)
    {
        if (photonView != null && !photonView.IsMine) return;

        // SE o mesh foi desativado ou se tentamos selecionar a mesma arma, forçamos a busca pela pistola (Slot 0)
        if (novoIndice == indiceEquipamentoAtual)
        {
            novoIndice = 0;
        }

        if (novoIndice != 0 && !PodeTrocarParaHabilidade(novoIndice))
        {
            return;
        }

        CancelarTodasAsHabilidades();

        // Força a atualização do índice local ANTES do RPC
        indiceEquipamentoAtual = novoIndice;

        // RPC avisa todos os PCs da sala para ativarem o filho correto
        photonView.RPC("RPC_SincronizarMaos", RpcTarget.AllBuffered, novoIndice);

        AtivarHabilidadePorSlot(novoIndice);

        tempoLiberadoParaTrocar = Time.time + tempoAnimacaoTroca;
    }

    // 🌐 [PunRPC]: Garante que o mesh do filho correto seja reativado perfeitamente na rede
    [PunRPC]
    private void RPC_SincronizarMaos(int novoIndice)
    {
        if (pontoMaos != null)
        {
            for (int i = 0; i < pontoMaos.childCount; i++)
            {
                // Liga apenas o objeto do slot atual e desliga os outros
                pontoMaos.GetChild(i).gameObject.SetActive(i == novoIndice);
            }
        }
    }

    // 🌐 [PunRPC]: Chamado pelo Sistemadevida ao morrer/renascer
    [PunRPC]
    public void RPC_AlternarMaos(bool estado)
    {
        CancelarTodasAsHabilidades();

        if (pontoMaos != null)
        {
            if (!estado)
            {
                // Esconde todas as mãos ao morrer e reseta o memória do índice
                for (int i = 0; i < pontoMaos.childCount; i++)
                {
                    pontoMaos.GetChild(i).gameObject.SetActive(false);
                }
                indiceEquipamentoAtual = -1; // 👈 CRÍTICO: Reseta a memória para liberar a pistola ao renascer
            }
            else
            {
                // Ao renascer, força o reequipamento do Slot 0 (Pistola)
                indiceEquipamentoAtual = 0;
                RPC_SincronizarMaos(0);
            }
        }
    }

    // Procura na lista de habilidades para ver se a que o jogador clicou está pronta para uso
    private bool PodeTrocarParaHabilidade(int slotIndex)
    {
        if (habilidadesDoPersonagem == null) return true;

        foreach (var hab in habilidadesDoPersonagem)
        {
            if (hab.SlotIndex == slotIndex)
            {
                return hab.PodeUsar();
            }
        }
        return true;
    }

    // Varre todas as habilidades forçando-as a guardarem-se (usado antes de puxar uma arma nova ou morrer)
    public void CancelarTodasAsHabilidades()
    {
        if (habilidadesDoPersonagem == null) return;

        foreach (var hab in habilidadesDoPersonagem)
        {
            hab.CancelarHabilidade();
        }
    }

    // Varre todas as habilidades e diz "Ativa-te!" para aquela que o jogador escolheu
    private void AtivarHabilidadePorSlot(int slotIndex)
    {
        if (habilidadesDoPersonagem == null) return;

        foreach (var hab in habilidadesDoPersonagem)
        {
            if (hab.SlotIndex == slotIndex)
            {
                hab.AtivarHabilidade();
                break;
            }
        }
    }
}