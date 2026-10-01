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
    [SerializeField] private TMP_InputField nickName;
    [SerializeField] private TMP_Text novonome;


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
    public void seuNome(TMP_InputField input)
    {
        novonome.text = input.text;
    }
}
