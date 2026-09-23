using UnityEngine;

public interface ISkills
{
    int SlotIndex { get; }
    float TempoProximouso { get; }
    Sprite FotoPronta { get; }
    Sprite FotoRecarregando { get; }

    bool PodeUsar();
    bool EstaPronta(); // <--- Adicionado aqui para aceitar chamadas antigas sem erro!
    void AtivarHabilidade();
    void CancelarHabilidade();
}