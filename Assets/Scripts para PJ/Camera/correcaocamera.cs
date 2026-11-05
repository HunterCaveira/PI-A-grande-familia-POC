// Importa a biblioteca base da Unity para funções padrão
using UnityEngine;
// Importa a biblioteca do Cinemachine v3 para manipular a câmera
using Unity.Cinemachine;
// Importa a biblioteca do Photon para funções multiplayer (rede)
using Photon.Pun;

public class CorrecaoCameraPersonagem : MonoBehaviour
{
    [Header("Configuração de Foco da Câmera")]
    [Tooltip("Arraste o objeto filho 'CameraTarget' posicionado no ombro do personagem.")]
    // Variável que guarda a posição do ombro onde a câmera vai focar
    [SerializeField] private Transform cameraTarget;

    [Header("Rotação do Personagem")]
    // Velocidade com que o boneco gira para acompanhar a câmera
    [SerializeField] private float velocidadeRotacao = 10f;
    [Tooltip("Gira o personagem apenas quando ele estiver se movendo? (Recomendado para jogos estilo Marvel Rivals)")]
    // Se verdadeiro, o boneco só vira de costas quando você aperta W, A, S ou D
    [SerializeField] private bool girarApenasEmMovimento = false;

    // Referência privada para guardar a câmera principal da sua tela
    private Transform cameraTransform;
    // Referência privada para o componente de rede do Photon
    private PhotonView view;

    // Start é chamado na primeira frame em que o boneco nasce
    void Start()
    {
        // Pega o componente PhotonView anexado a este personagem
        view = GetComponent<PhotonView>();

        // MUDANÇA: Trava de Segurança no Start!
        // Se este boneco for um clone (inimigo ou aliado), aborta o Start aqui mesmo.
        // Isso impede que o seu Cinemachine tente focar no ombro do outro jogador.
        if (view != null && !view.IsMine) return;

        // Encontra a câmera principal da cena usando a tag "MainCamera"
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Se você esqueceu de arrastar o alvo no ombro no Inspector, o script procura sozinho
        if (cameraTarget == null)
        {
            // Busca um objeto filho chamado exatamente "CameraTarget"
            Transform encontrado = transform.Find("CameraTarget");
            // Se achar, usa ele. Se não achar, usa o centro do próprio personagem (fallback)
            cameraTarget = encontrado != null ? encontrado : transform;
        }

        // Procura a câmera do Cinemachine v3 na cena
        CinemachineCamera freeLookCam = FindFirstObjectByType<CinemachineCamera>();
        if (freeLookCam != null)
        {
            // Como já garantimos lá em cima que este boneco é IsMine, 
            // manda a câmera focar exclusivamente no ombro DESTE jogador.
            freeLookCam.Target.TrackingTarget = cameraTarget;
        }
    }

    // Update roda dezenas de vezes por segundo (a cada frame)
    void Update()
    {
        // MUDANÇA PRINCIPAL: A Trava de Rede Crítica!
        // Se este boneco for de outro jogador, o Update para de ler o código aqui.
        // Isso resolve o Bug 2! Impede que o seu PC gire o boneco dos outros.
        if (view != null && !view.IsMine) return;

        // Se for o seu boneco, chama a função para alinhar o corpo dele com a câmera
        AlinharPersonagemComCamera();
    }

    // Método que calcula a matemática para rodar o personagem
    private void AlinharPersonagemComCamera()
    {
        // Se a câmera não foi achada no Start, cancela para não dar erro vermelho
        if (cameraTransform == null) return;

        // Se a opção de girar apenas em movimento estiver marcada no Inspector
        if (girarApenasEmMovimento)
        {
            // Lê os botões do teclado/analógico (W, A, S, D)
            float inputHorizontal = Input.GetAxisRaw("Horizontal");
            float inputVertical = Input.GetAxisRaw("Vertical");

            // Se o jogador estiver com as mãos fora do teclado (parado), cancela a rotação
            if (inputHorizontal == 0 && inputVertical == 0) return;
        }

        // Pega a seta azul (forward/frente) da sua câmera
        Vector3 direcaoCamera = cameraTransform.forward;
        // Zera o eixo Y (altura) para que o boneco não tente inclinar para cima ou para baixo
        direcaoCamera.y = 0;

        // Evita erros matemáticos se a câmera estiver a olhar perfeitamente para baixo/cima
        if (direcaoCamera.sqrMagnitude < 0.0001f) return;

        // Calcula a rotação final exata para onde as costas do boneco devem apontar
        Quaternion rotacaoAlvo = Quaternion.LookRotation(direcaoCamera);

        // Rotaciona o corpo do personagem de forma suave usando Slerp (interpolação esférica)
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoAlvo, velocidadeRotacao * Time.deltaTime);
    }

    // Método exclusivo para os desenvolvedores visualizarem coisas na Unity (não vai para o jogo final)
    private void OnDrawGizmosSelected()
    {
        // Se o alvo da câmera existir
        if (cameraTarget != null)
        {
            // Desenha uma esfera amarela no ombro do personagem para você saber onde a câmera está a mirar
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(cameraTarget.position, 0.15f);
        }
    }
}