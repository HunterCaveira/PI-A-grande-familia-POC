using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.PlayerLoop;
public class BalaPool : MonoBehaviour
{
    [Header("Pool Settings")]
    //Prefab dos Objetos
    [SerializeField] private GameObject balaPrefab;

    //Definindo tamanho da piscina (quantidade de objetos criados)
    [SerializeField] private int PooledAmount;

    //Lista de objetos criados
    [SerializeField] private List<GameObject> PoolBalaList;

    //Metodo que inicializa a "pscina" de objetos (slime)
    private void Awake()
    {
        InitializePool();

    }
    private void InitializePool()
    {

        PoolBalaList = new List<GameObject>();

        for (int i = 0; i < PooledAmount; i++)
        {

            //Criando um novo item.
            GameObject newitem = Instantiate(balaPrefab);

            //Desativando o objeto
            newitem.SetActive(false);

            //Definindo o zumbi criado como filho do object pooling (game object)
            newitem.transform.SetParent(this.gameObject.transform);

            //Adicionando o item na lista
            PoolBalaList.Add(newitem);


        }
    }

    //Metodo resposavel por retorna algum objeto inativo da "psicina"
    public GameObject GetitemFromPool()
    {
        //Acessando os objetos da lista por meio do for
        for (int i = 0; i < PoolBalaList.Count; i++)
        {

            //Verificando se o objeto acessado esta desativado na cena
            if (PoolBalaList[i].activeInHierarchy == false)
           {

                //Retornando o objeto desativado da cena
                return PoolBalaList[i];

            }

        }

        //Criando e adincionando novos zumbis na lita, caso nao haja mais objetos disponiveis na "piscina"

        //Criando um novo Zumbi
        GameObject newBala = Instantiate(balaPrefab);

        // Definindo o zumbi criado com filho do object pooling (game object)
        newBala.transform.SetParent(this.gameObject.transform);

        //Adicionanod o item na lista
        PoolBalaList.Add(newBala);

        //Retonando o novo item criado
        return newBala;


    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
