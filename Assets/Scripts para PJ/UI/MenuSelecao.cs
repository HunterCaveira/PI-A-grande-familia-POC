using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSelecao : MonoBehaviour
{
  

    // Função para o Botão/Imagem do Personagem 1
    public void SelecionarPersonagem1()
    {
        PlayerPrefs.SetInt("PersonagemEscolhido", 1);
        PlayerPrefs.Save(); // Garante o salvamento imediato
        SceneManager.LoadScene(1);
    }

    // Função para o Botão/Imagem do Personagem 2
    public void SelecionarPersonagem2()
    {
        PlayerPrefs.SetInt("PersonagemEscolhido", 2);
        PlayerPrefs.Save(); // Garante o salvamento imediato
        SceneManager.LoadScene(1);
    }

  
}