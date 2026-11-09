using UnityEngine;

public class Bala : MonoBehaviour
{
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
    {
        // 1. Evita colidir com o próprio atirador no momento do disparo
        if (other.gameObject == atirador) return;

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
}