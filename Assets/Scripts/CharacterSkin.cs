using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharactersSkin : MonoBehaviour
{
    public int Skin;

    public TMP_Dropdown Selection;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        
    }

    // Update is called once per frame
    public void ChangeSkin()
    {

        Skin = Selection.value;
    }
}
