using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{

    [SerializeField] private GameObject Configuração;
    [SerializeField] private GameObject menuPrincipal;
    [SerializeField] private GameObject Creditos;
    [SerializeField] private Slider sliderVolume;
    [SerializeField] private Scrollbar scrollbarMusica;
    [SerializeField] public GameObject painelGeral;
    [SerializeField] public GameObject painelSom;
    [SerializeField] public GameObject painelControle;

    public void NovoJogo()
    {
        SceneManager.LoadScene(1);

    }
    public void opção()
    {
        menuPrincipal.SetActive(false);
        Configuração.SetActive(true);
        Creditos.SetActive(false);

    }
    public void voltar()
    {
        menuPrincipal.SetActive(true);
        Configuração.SetActive(false);
        Creditos.SetActive(false);
        painelGeral.SetActive(false);
        painelSom.SetActive(false);
        painelControle.SetActive(false);

    }
    public void Credito()
    {
        Creditos.SetActive(true);
        menuPrincipal.SetActive(false);
        Configuração.SetActive(false);

    }
    public void sair()
    {
        Application.Quit();

    }
    public void mudarVolume()
    {
        Debug.Log(sliderVolume.value);
    }
    public void volumemusica()
    {
        Debug.Log(scrollbarMusica.value);
    }
    public void TrocarSOM()
    {
        painelSom.SetActive(true);
        painelGeral.SetActive(false);
        painelControle.SetActive(false);
    }
    public void TrocarGeral()
    {
        painelGeral.SetActive(true);
        painelSom.SetActive(false);
        painelControle.SetActive(false);
    }

    public void TrocarControle()
    {
        painelControle.SetActive(true);
        painelGeral.SetActive(false);
        painelSom.SetActive(false);
    }
}