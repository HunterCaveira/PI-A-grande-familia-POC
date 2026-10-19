using System.Collections.Generic;
using UnityEngine;

public class ZonaDeComando : MonoBehaviour
{
    // Lista interna de todos os colliders/objetos atualmente dentro da zona
    private List<GameObject> objetosNaZona = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (!objetosNaZona.Contains(other.gameObject))
        {
            objetosNaZona.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (objetosNaZona.Contains(other.gameObject))
        {
            objetosNaZona.Remove(other.gameObject);
        }
    }

    /// <summary>
    /// Retorna apenas os inimigos que estão dentro da zona, comparando o TimeID.
    /// </summary>
    public List<GameObject> ObterInimigosNaZona(int timeDoJogadorDono)
    {
        List<GameObject> inimigosFiltrados = new List<GameObject>();

        // Limpa referências nulas caso algum objeto tenha sido destruído na zona
        objetosNaZona.RemoveAll(obj => obj == null);

        foreach (var obj in objetosNaZona)
        {
            // Verifica se o objeto possui o seu Sistemadevida
            if (obj.TryGetComponent<Sistemadevida>(out Sistemadevida vidaInimigo))
            {
                // Se o time do personagem dentro da zona for DIFERENTE do time do jogador, é inimigo!
                if (vidaInimigo.TimeID != timeDoJogadorDono)
                {
                    inimigosFiltrados.Add(obj);
                }
            }
        }

        return inimigosFiltrados;
    }
}