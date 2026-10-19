using UnityEngine; // Importa as bibliotecas padrão da Unity.
using Photon.Pun; // Importa as bibliotecas do Photon Multiplayer.
using Unity.Cinemachine; // <-- CORREÇÃO AQUI: Namespace atualizado para a Unity 6 (Cinemachine 3)

// A classe herda de MonoBehaviourPun para acessarmos o photonView facilmente.
public class SetupJogadorMultiplayer : MonoBehaviourPun
{
    [Header("Scripts Locais para Desativar nos Clones")]
    [Tooltip("Arraste aqui os scripts de movimento e câmera que não devem rodar nos personagens dos inimigos/aliados.")]
    [SerializeField] private MonoBehaviour[] scriptsLocais;

    [Header("Referência de Câmera")]
    [Tooltip("Opcional: Arraste o 'Character Camera Anchor' aqui se a câmera foca nele em vez da raiz do jogador.")]
    [SerializeField] private Transform alvoDaCamera;

    private void Start()
    {
        // Se a variável alvoDaCamera não for preenchida no Inspector, ele usa o Transform principal (a raiz) do jogador.
        if (alvoDaCamera == null)
        {
            alvoDaCamera = this.transform;
        }

        // VERIFICAÇÃO MULTIPLAYER: Este personagem PERTENCE à minha tela (ao Jogador Local)?
        if (photonView.IsMine)
        {
            // CORREÇÃO: Busca a câmera do Cinemachine atualizada para a Unity 6.
            CinemachineCamera cameraVirtual = Object.FindFirstObjectByType<CinemachineCamera>();

            if (cameraVirtual != null)
            {
                // Define que a câmera da cena agora vai seguir este personagem específico.
                cameraVirtual.Follow = alvoDaCamera;
                // Define que a câmera da cena agora vai olhar para este personagem específico.
                cameraVirtual.LookAt = alvoDaCamera;

                Debug.Log("[MULTIPLAYER] Câmera conectada ao jogador local com sucesso!");
            }
            else
            {
                Debug.LogWarning("[MULTIPLAYER] Nenhuma CinemachineCamera encontrada na cena Main Park!");
            }
        }
        else
        {
            // SE NÃO FOR O MEU PERSONAGEM (é o clone de outro jogador na rede):

            // Desativamos todos os scripts de movimento e controle de câmera dele.
            // Assim, quando eu apertar "W" ou mexer o rato, apenas o meu personagem se move.
            foreach (MonoBehaviour script in scriptsLocais)
            {
                if (script != null)
                {
                    script.enabled = false;
                }
            }

            Debug.Log("[MULTIPLAYER] Controles desativados para o clone do jogador remoto.");
        }
    }
}