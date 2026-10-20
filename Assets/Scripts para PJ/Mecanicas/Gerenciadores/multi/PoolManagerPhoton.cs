using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PoolManagerPhoton : MonoBehaviour, IPunPrefabPool
{
    public static PoolManagerPhoton Instancia { get; private set; }

    // Dicionário com os prefabs registrados e suas filas de objetos inativos
    private readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, Queue<GameObject>> pool = new Dictionary<string, Queue<GameObject>>();

    [Header("Prefabs para colocar na Pool (Devem estar na pasta Resources)")]
    [SerializeField] private string[] nomesPrefabs = new string[] { "ParedeReal", "ProjetilPistola" };

    private void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            // Define esta classe como o gerenciador oficial de carregamento do Photon
            PhotonNetwork.PrefabPool = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Carrega os prefabs e inicializa as listas na memória
        foreach (string nome in nomesPrefabs)
        {
            GameObject prefabCarregado = Resources.Load<GameObject>(nome);
            if (prefabCarregado != null)
            {
                prefabs.Add(nome, prefabCarregado);
                pool.Add(nome, new Queue<GameObject>());
            }
        }
    }

    // Chamado automaticamente pelo PhotonNetwork.Instantiate
    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        if (pool.ContainsKey(prefabId) && pool[prefabId].Count > 0)
        {
            // Pega um objeto inativo que já estava guardado na memória
            GameObject obj = pool[prefabId].Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true); // Reativa o objeto sem instanciar do zero!
            return obj;
        }

        // Se não houver nenhum objeto inativo guardado na pool, cria um novo
        if (prefabs.ContainsKey(prefabId))
        {
            GameObject novoObj = Instantiate(prefabs[prefabId], position, rotation);
            return novoObj;
        }

        return null;
    }

    // Chamado automaticamente pelo PhotonNetwork.Destroy
    public void Destroy(GameObject gameObject)
    {
        // Em vez de deletar o GameObject do jogo, desativa-o e guarda na fila!
        gameObject.SetActive(false);

        // Remove a extensão "(Clone)" que a Unity adiciona ao nome para achar a chave
        string nomePrefab = gameObject.name.Replace("(Clone)", "").Trim();

        if (pool.ContainsKey(nomePrefab))
        {
            pool[nomePrefab].Enqueue(gameObject);
        }
        else
        {
            // Se o objeto não fizer parte do pool registrado, destrói normalmente
            Destroy(gameObject);
        }
    }
}