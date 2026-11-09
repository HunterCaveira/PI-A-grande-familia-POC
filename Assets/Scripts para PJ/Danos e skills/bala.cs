<<<<<<< HEAD
=======
// Importa as coleções para usar Coroutines (temporizadores invisíveis)
using System.Collections;
// Importa a biblioteca base da Unity
>>>>>>> parent of a587532 (Finalizado o multiplayer)
using UnityEngine;

public class Bala : MonoBehaviour
{
<<<<<<< HEAD
    private float dano;
    private TipoDano tipoDano;
    private GameObject atirador;
    private string nomeArma;

    [Header("Configurações de Colisão")]
    [Tooltip("Camadas que a bala deve ignorar completamente.")]
    [SerializeField] private LayerMask camadasParaIgnorar;

    // Método de inicialização invocado pela arma ao disparar
    public void Inicializar(float danoBase, TipoDano tipo, GameObject dono, string nomeDaArma)
    {
        dano = danoBase;
        tipoDano = tipo;
        atirador = dono;
        nomeArma = nomeDaArma;

        // Autodestrói a bala após 5 segundos se não atingir nada (evita acúmulo na memória)
        // Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter(Collider other)
=======
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
>>>>>>> parent of a587532 (Finalizado o multiplayer)
    {
        // 1. Evita colidir com o próprio atirador no momento do disparo
        if (other.gameObject == atirador) return;

<<<<<<< HEAD
        // 2. Evita colidir com outras balas
        if (other.gameObject.CompareTag("Bala")) return;

        // 3. Ignora se o objeto pertencer a uma Layer configurada para ser ignorada
        // (Converte a layer do objeto em bitmask e verifica se está inclusa na LayerMask)
        if ((camadasParaIgnorar.value & (1 << other.gameObject.layer)) != 0) return;

        // 4. Se atingiu alguém que possui o Sistema de Vida
        if (other.TryGetComponent<Sistemadevida>(out Sistemadevida vidaAtingida))
        {
            // Monta a struct de transporte de dano
            InformacaoDano infoDano = new InformacaoDano(
                dano,
                tipoDano,
                atirador,
                nomeArma
            );

            // Aplica o dano
            vidaAtingida.ReceberDano(infoDano);
        }

        // 5. Desativa a bala apenas se passou por todas as verificações anteriores
        // (Isso garante que ela não será desativada ao cruzar layers ignoradas)
        gameObject.SetActive(false);
    }
=======
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
>>>>>>> parent of a587532 (Finalizado o multiplayer)
}