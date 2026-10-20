using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class GerenciadorDeEquipamentos : MonoBehaviourPun
{
    [Header("Configurações Visuais das Mãos")]
    [SerializeField] private Transform pontoMaos;

    [Header("Configurações de Tempo")]
    [SerializeField] private float tempoAnimacaoTroca = 0.5f;
    private float tempoLiberadoParaTrocar = 0f;

    [Header("Controle Interno")]
    private int indiceEquipamentoAtual = 0;
    private int proximoIndicePendente = -1;

    private IasUI[] habilidadesDoPersonagem;

    void Awake()
    {
        habilidadesDoPersonagem = GetComponents<IasUI>();
    }

    void Update()
    {
        // Apenas o jogador dono processa comandos de teclado localmente
        if (!photonView.IsMine) return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 1;
            }
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 2;
            }
            else if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                proximoIndicePendente = 3;
            }
        }

        if (Time.time >= tempoLiberadoParaTrocar && proximoIndicePendente != -1)
        {
            TrocarEquipamento(proximoIndicePendente);
            proximoIndicePendente = -1;
        }
    }

    public void TrocarEquipamento(int novoIndice)
    {
        if (!photonView.IsMine) return;

        if (novoIndice == indiceEquipamentoAtual)
        {
            novoIndice = 0;
        }

        if (novoIndice != 0 && !PodeTrocarParaHabilidade(novoIndice))
        {
            return;
        }

        CancelarTodasAsHabilidades();

        // Atualiza a malha 3D visual da mão em todos os computadores da sala
        photonView.RPC("SincronizarTrocaEquipamentoRPC", RpcTarget.All, novoIndice);

        AtivarHabilidadePorSlot(novoIndice);

        indiceEquipamentoAtual = novoIndice;
        tempoLiberadoParaTrocar = Time.time + tempoAnimacaoTroca;
    }

    [PunRPC]
    private void SincronizarTrocaEquipamentoRPC(int novoIndice)
    {
        if (pontoMaos != null)
        {
            for (int i = 0; i < pontoMaos.childCount; i++)
            {
                pontoMaos.GetChild(i).gameObject.SetActive(i == novoIndice);
            }
        }
    }

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

    private void CancelarTodasAsHabilidades()
    {
        if (habilidadesDoPersonagem == null) return;

        foreach (var hab in habilidadesDoPersonagem)
        {
            hab.CancelarHabilidade();
        }
    }

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