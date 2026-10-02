using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;
using JetBrains.Annotations;

public class ScoreManager : MonoBehaviour
{
    public int score;

    public int Money;

    public TMP_Text TextScore;

    public TMP_Text MoneyScore;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpgradeMoney()
    {

        Money++;
        MoneyScore.text = "Money" + Money.ToString();
            
    }

    [PunRPC]

    public void UpgradeScore()
    {
        score++;
        TextScore.text = "Score" + score.ToString();

    }
}
