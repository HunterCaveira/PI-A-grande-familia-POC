
using UnityEngine;

public interface IHabilidade
{
    void AtivarHabilidade();
    void CancelarHabilidade();

    // Pergunta que responde 'true' se estiver pronta ou 'false' se estiver em recarga
    bool EstaPronta();
}