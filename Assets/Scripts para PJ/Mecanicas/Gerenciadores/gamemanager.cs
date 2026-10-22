using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int idPersonagemSelecionado { get; private set; } = 1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            // Em vez de Destroy(gameObject), desativa o objeto duplicado
            gameObject.SetActive(false);
        }
    }

    public void SetPersonagem(int id)
    {
        idPersonagemSelecionado = id;
    }
}