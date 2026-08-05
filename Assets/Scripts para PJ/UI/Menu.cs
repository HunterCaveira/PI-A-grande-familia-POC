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
    [SerializeField] private AudioClip sfxButton;
    [SerializeField] private AudioSource somMenu;


    public void NovoJogo()
    {
        SceneManager.LoadScene(1);
        somMenu.PlayOneShot(sfxButton);
    }
    public void opção()
    {
        menuPrincipal.SetActive(false);
        Configuração.SetActive(true);
        Creditos.SetActive(false);
        somMenu.PlayOneShot(sfxButton);
    }
    public void voltar()
    {
        menuPrincipal.SetActive(true);
        Configuração.SetActive(false);
        Creditos.SetActive(false);
        somMenu.PlayOneShot(sfxButton);
    }
    public void Credito()
    {
        Creditos.SetActive(true);
        menuPrincipal.SetActive(false);
        Configuração.SetActive(false);
        somMenu.PlayOneShot(sfxButton);
    }
    public void sair()
    {
        Application.Quit();
        somMenu.PlayOneShot(sfxButton);
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
