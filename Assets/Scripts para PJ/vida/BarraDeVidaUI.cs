// Importa as bibliotecas base da Unity
using UnityEngine;
// Importa a biblioteca System para usar Actions (Eventos)
using System;
// Importa a biblioteca do Photon
using Photon.Pun;

public class Sistemadevida : MonoBehaviourPun
{
    [Header("Configurações Base")]
    // Referência aos atributos base do personagem
    [SerializeField] private AtributosPersonagem fichaAtributos;

    // ID do time para impedir fogo amigo (Usado também pela Habilidade Cinto)
    public int TimeID = 1;

    // 🔊 O ALTO-FALANTE: Este é o evento que o seu script BarraDeVidaUI está a escutar!
    public event Action<float, float> OnVidaMudou;

    // Variáveis internas de vida
    private float vidaAtual;
    private float vidaMaxima = 100f; // Valor padrão caso não tenha ficha

    // Variável modificada pela Habilidade Tonico para "tancar" dano
    public float PorcentagemReducaoDano { get; set; } = 0f;

    private void Start()
    {
        // Puxa a vida máxima da ficha de atributos (se existir)
        if (fichaAtributos != null)
        {
            vidaMaxima = fichaAtributos.vidaMaxima;
        }

        // Enche a vida ao nascer
        vidaAtual = vidaMaxima;

        // 🔗 CONEXÃO COM A UI: 
        // Se este for o MEU boneco, grito no alto-falante para a BarraDeVidaUI atualizar no primeiro frame!
        if (photonView.IsMine)
        {
            OnVidaMudou?.Invoke(vidaAtual, vidaMaxima);
        }
    }

    // Método usado pela Habilidade Tonico para atualizar a velocidade e vida dinamicamente
    public void AtualizarFichaAtributos(AtributosPersonagem novaFicha)
    {
        fichaAtributos = novaFicha;

        // Se o Tonico também aumentar a vida máxima, recalcula aqui
        if (novaFicha != null && novaFicha.vidaMaxima > vidaMaxima)
        {
            vidaMaxima = novaFicha.vidaMaxima;
            // Atualiza a UI se a barra esticar
            if (photonView.IsMine) OnVidaMudou?.Invoke(vidaAtual, vidaMaxima);
        }
    }

    // ===============================================================
    // 🌐 RECEPÇÃO DE DANO MULTIPLAYER: Chamado pela Bala.cs
    // ===============================================================
    [PunRPC]
    public void ReceberDanoRPC(float dano, int viewIdAtirador)
    {
        // 1. Aplica a redução de dano (Ex: Tonico dá 30% de redução, então 1 - 0.3 = 0.7)
        float danoFinal = dano * (1f - PorcentagemReducaoDano);

        // 2. Subtrai a vida
        vidaAtual -= danoFinal;

        // 3. Trava a vida para não ficar negativa nem passar do máximo
        vidaAtual = Mathf.Clamp(vidaAtual, 0, vidaMaxima);

        // 4. Se este for o meu boneco, aviso a minha UI para esvaziar a barrinha vermelha
        if (photonView.IsMine)
        {
            OnVidaMudou?.Invoke(vidaAtual, vidaMaxima);
        }

        // 5. Verifica se morreu
        if (vidaAtual <= 0)
        {
            Morrer(viewIdAtirador);
        }
    }

    private void Morrer(int viewIdAssassino)
    {
        // Só o dono do boneco decide como e quando ele morre para evitar bugs de rede
        if (photonView.IsMine)
        {
            Debug.Log("<color=red>Morri! Fui morto pelo jogador de ID: " + viewIdAssassino + "</color>");

            // Aqui você pode chamar o seu script de Respawn (que já moveu para o GameManager!)
            // Exemplo: FindFirstObjectByType<RespawnManager>().Reviver(photonView.ViewID);

            // Opcional: Destrói o boneco atual
            PhotonNetwork.Destroy(gameObject);
        }
    }
}