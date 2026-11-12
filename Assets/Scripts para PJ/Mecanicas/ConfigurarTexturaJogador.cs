using System;
using UnityEngine;
using Photon.Pun;

public class ConfigurarTexturaJogador : MonoBehaviourPun
{
    [Header("Configuração de Materiais/Texturas")]
    [SerializeField] private Material materialTime1; // Azul
    [SerializeField] private Material materialTime2; // Vermelho

    private void Start()
    {
        AtualizarTexturaPorTime();
    }

    public void AtualizarTexturaPorTime()
    {
        // 1. Pega automaticamente o SkinnedMeshRenderer nos filhos deste Prefab
        SkinnedMeshRenderer renderizador = GetComponentInChildren<SkinnedMeshRenderer>(true);

        if (renderizador == null)
        {
            Debug.LogWarning($"[Textura] Nenhum SkinnedMeshRenderer encontrado dentro de {gameObject.name}!");
            return;
        }

        // 2. Define qual jogador consultar usando IsMine
        int timeDoJogador = 1;

        if (photonView.IsMine)
        {
            // Se o personagem é meu, lê das minhas propriedades locais
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("TimeJogador", out object meuTime))
            {
                timeDoJogador = Convert.ToInt32(meuTime);
            }
        }
        else
        {
            // Se é outro jogador na sala, lê das propriedades do dono deste PhotonView
            if (photonView.Owner != null && photonView.Owner.CustomProperties.TryGetValue("TimeJogador", out object timeOutro))
            {
                timeDoJogador = Convert.ToInt32(timeOutro);
            }
        }

        // 3. Aplica o material correspondente
        if (timeDoJogador == 1 && materialTime1 != null)
        {
            renderizador.material = materialTime1;
        }
        else if (timeDoJogador == 2 && materialTime2 != null)
        {
            renderizador.material = materialTime2;
        }
    }
}