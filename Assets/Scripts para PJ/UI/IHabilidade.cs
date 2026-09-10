using UnityEngine;

public interface ISkills
{
    int SlotIndex { get; }        // 1 (Q), 2 (E), 3 (F)
    float TempoProximouso { get; }
    Sprite FotoPronta { get; }
    Sprite FotoRecarregando { get; }

    bool PodeUsar();
}