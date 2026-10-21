using UnityEngine; // Importa as bibliotecas padrão da Unity.
using Photon.Pun; // Importa as bibliotecas do Photon Multiplayer.
using Unity.Cinemachine; // Namespace atualizado para a Unity 6 (Cinemachine 3)

// A classe herda de MonoBehaviourPun para acessarmos o photonView facilmente.
public class SetupJogadorMultiplayer : MonoBehaviourPun
{
    [Header("Referência de Câmera")]
    [Tooltip("Opcional: Arraste o 'Character Camera Anchor' aqui se a câmera foca nele em vez da raiz do jogador.")]
    [SerializeField] private Transform alvoDaCamera;

    private void Start()
    {
        // Se a variável alvoDaCamera não for preenchida no Inspector, usa a raiz do jogador.
        if (alvoDaCamera == null)
        {
            alvoDaCamera = this.transform;
        }

        // VERIFICAÇÃO MULTIPLAYER: Este personagem PERTENCE à minha tela?
        if (photonView.IsMine)
        {
            CinemachineCamera cameraVirtual = Object.FindFirstObjectByType<CinemachineCamera>();

            if (cameraVirtual != null)
            {
                // Define que a câmera da cena agora vai seguir e olhar para este personagem.
                cameraVirtual.Follow = alvoDaCamera;
                cameraVirtual.LookAt = alvoDaCamera;

                Debug.Log("[MULTIPLAYER] Câmera conectada ao jogador local com sucesso!");
            }
            else
            {
                Debug.LogWarning("[MULTIPLAYER] Nenhuma CinemachineCamera encontrada na cena!");
            }

            // Conecta à UI Local (Opcional, mas útil)
            GerenciadorUI gerenciadorUI = Object.FindFirstObjectByType<GerenciadorUI>();
            if (gerenciadorUI != null)
            {
                gerenciadorUI.AtribuirJogadorLocal(this.gameObject);
            }
        }
        else
        {
            Debug.Log("[MULTIPLAYER] Clone remoto recebido. Câmera não alterada.");
        }
    }
}