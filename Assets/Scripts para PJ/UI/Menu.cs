using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [Header("Painéis de Navegação")]
    [SerializeField] private GameObject configuração;
    [SerializeField] private GameObject menuPrincipal;
    [SerializeField] private GameObject creditos;

    [Header("Sub-Painéis de Configuração")]
    [SerializeField] public GameObject painelGeral;
    [SerializeField] public GameObject painelSom;
    [SerializeField] public GameObject painelControle;

    [Header("Controles de Áudio")]
    [SerializeField] private Slider sliderVolume;
    [SerializeField] private Scrollbar scrollbarMusica;

    /// <summary>
    /// Carrega a cena de HUD Server onde o jogador digita o nickname.
    /// </summary>
    public void NovoJogo()
    {
        // Certifique-se de que a cena "HUD Server" está adicionada em File -> Build Settings
        SceneManager.LoadScene("HUD Server");
    }

    public void Opção()
    {
        if (menuPrincipal != null) menuPrincipal.SetActive(false);
        if (configuração != null) configuração.SetActive(true);
        if (creditos != null) creditos.SetActive(false);
    }

    public void Voltar()
    {
        if (menuPrincipal != null) menuPrincipal.SetActive(true);
        if (configuração != null) configuração.SetActive(false);
        if (creditos != null) creditos.SetActive(false);

        DesativarTodosOsPainéisConfig();
    }

    public void Credito()
    {
        if (creditos != null) creditos.SetActive(true);
        if (menuPrincipal != null) menuPrincipal.SetActive(false);
        if (configuração != null) configuração.SetActive(false);
    }

    public void Sair()
    {
        Application.Quit();
    }

    public void MudarVolume()
    {
        if (sliderVolume != null)
        {
            Debug.Log($"Volume alterado: {sliderVolume.value}");
        }
    }

    public void VolumeMusica()
    {
        if (scrollbarMusica != null)
        {
            Debug.Log($"Volume música alterado: {scrollbarMusica.value}");
        }
    }

    public void TrocarSOM()
    {
        DesativarTodosOsPainéisConfig();
        if (painelSom != null) painelSom.SetActive(true);
    }

    public void TrocarGeral()
    {
        DesativarTodosOsPainéisConfig();
        if (painelGeral != null) painelGeral.SetActive(true);
    }

    public void TrocarControle()
    {
        DesativarTodosOsPainéisConfig();
        if (painelControle != null) painelControle.SetActive(true);
    }

    /// <summary>
    /// Método auxiliar para desativar com segurança todos os sub-painéis de configuração.
    /// </summary>
    private void DesativarTodosOsPainéisConfig()
    {
        if (painelGeral != null) painelGeral.SetActive(false);
        if (painelSom != null) painelSom.SetActive(false);
        if (painelControle != null) painelControle.SetActive(false);
    }
}