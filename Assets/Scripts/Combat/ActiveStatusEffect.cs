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
    public ActiveStatusEffect(StatusEffectData data, int durationOverride)
    {
        Data = data;
        RemainingTurns = durationOverride;
        CurrentStacks = 1;
    }
}