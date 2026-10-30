// Importa as coleções para usar Coroutines (temporizadores invisíveis)
using System.Collections;
// Importa a biblioteca base da Unity
using UnityEngine;
// Importa a biblioteca do Photon para usarmos o PhotonView e RPCs
using Photon.Pun;

// A classe foi renomeada de 'balacustom' para 'Bala' para funcionar com o script tiroPistola
public class Bala : MonoBehaviour
{
    [Tooltip("Tempo máximo de vida da bala caso ela não acerte em nada (evita pesar o jogo).")]
    public float tempoVidaMaximo = 5f;

    // Variáveis internas para guardar a "ficha criminal" do tiro
    private float danoDaBala;
    private TipoDano tipoDanoBala;
    private GameObject donoDaArma;
    private string nomeDaArmaOrigem;

    // MUDANÇA: Substituímos o Start() por OnEnable().
    // O OnEnable roda SEMPRE que o objeto é ativado (SetActive(true)), perfeito para o BalaPool!
    private void OnEnable()
    {
        // Inicia o cronómetro para desativar a bala se ela voar para o infinito
        StartCoroutine(StartDisableObject());
    }

    // Método que o nosso tiroPistola usa para "ensinar" a bala quem a atirou e quanto de dano ela dá
    public void Inicializar(float dano, TipoDano tipo, GameObject dono, string arma)
    {
        danoDaBala = dano;
        tipoDanoBala = tipo;
        donoDaArma = dono;
        nomeDaArmaOrigem = arma;
    }

    // Este método é chamado automaticamente pela Unity quando a bala física (Rigidbody) bate noutro Collider
    private void OnCollisionEnter(Collision collision)
    {
        // 1. TRAVA MULTIPLAYER VITAL: Só o computador do ATIRADOR tem o direito de calcular o dano.
        // Isso impede que, se 4 jogadores virem a bala bater, o inimigo tome 4 vezes o mesmo dano.
        if (donoDaArma != null)
        {
            PhotonView pvDono = donoDaArma.GetComponent<PhotonView>();
            // Se a arma que atirou não for a minha, eu apenas desativo o visual da bala e saio do método (não dou dano)
            if (pvDono != null && !pvDono.IsMine)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        // Se cheguei aqui, EU sou o atirador. Procura o Sistema de Vida no objeto em que a bala bateu.
        Sistemadevida vidaAlvo = collision.gameObject.GetComponent<Sistemadevida>();

        // 2. EVITAR FOGO AMIGO/SUICÍDIO: Verifica se bateu em alguém com vida E se não bateu no próprio atirador
        if (vidaAlvo != null && collision.gameObject != donoDaArma)
        {
            // Pega o componente de rede do inimigo atingido
            PhotonView pvInimigo = collision.gameObject.GetComponent<PhotonView>();

            if (pvInimigo != null)
            {
                // 3. ENVIANDO O DANO: 
                // O meu computador manda uma mensagem pela internet direto para o inimigo: "Toma X de dano!"
                // (Nota: O script Sistemadevida do inimigo precisa de ter a função "ReceberDanoRPC" com [PunRPC])
                pvInimigo.RPC("ReceberDanoRPC", RpcTarget.All, danoDaBala, donoDaArma.GetComponent<PhotonView>().ViewID);
            }
        }

        // 4. Opcional: Criar um efeito de faísca ou poeira no ponto do impacto (sangue, poeira da parede)
        // Instantiate(efeitoImpacto, collision.contacts[0].point, Quaternion.LookRotation(collision.contacts[0].normal));

        // 5. Devolve o objeto da bala para o Pool imediatamente, escondendo-a da cena
        gameObject.SetActive(false);
    }

    // Rotina que desliga a bala após os 5 segundos se ela não bater em nada
    IEnumerator StartDisableObject()
    {
        // Espera o tempo máximo
        yield return new WaitForSeconds(tempoVidaMaximo);
        // Esconde a bala (o que a devolve automaticamente para ser reutilizada pelo BalaPool)
        this.gameObject.SetActive(false);
    }
}