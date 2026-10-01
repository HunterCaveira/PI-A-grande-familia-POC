using UnityEngine;


public interface IasUI : IHabilidade
{
    int SlotIndex { get; }
    float TempoProximouso { get; }
    Sprite FotoPronta { get; }
    Sprite FotoRecarregando { get; }
}