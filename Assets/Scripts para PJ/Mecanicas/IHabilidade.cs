using UnityEngine;

public interface IHabilidade
{
    bool EstaPronta();
    void AtivarHabilidade();
    void CancelarHabilidade();
    bool PodeUsar();

}

