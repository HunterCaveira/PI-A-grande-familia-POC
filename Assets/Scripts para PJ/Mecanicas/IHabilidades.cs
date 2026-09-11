
using UnityEngine;

public interface ISkill
{
    void AtivarHabilidade();
    void CancelarHabilidade();

    // Pergunta que responde 'true' se estiver pronta ou 'false' se estiver em recarga
    bool EstaPronta();
}