using UnityEngine;

public class ActiveStatusEffect
{
    public StatusEffectData Data { get; }
    public int RemainingTurns { get; set; }
    public int CurrentStacks { get; set; }
    public ActiveStatusEffect(StatusEffectData data)
    {
        Data = data;
        RemainingTurns = data.Duration;
        CurrentStacks = 1;
    }
}