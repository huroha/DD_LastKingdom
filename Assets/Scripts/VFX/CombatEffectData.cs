using UnityEngine;
using System.Collections;

public enum EffectPlayMode
{
    Static, // 1장 표시 -> hold -> fade-out
    Sequential, // N장 순차 교체 -> 마지막 fade_out
    PopIn   // 각 sprite마다 popin -> 짧은 fade 반복
}

public abstract class CombatEffectData : ScriptableObject
{
    [Header("Position")]
    [SerializeField] private Vector3 m_LocalOffset;

    [Header("Flip")]
    [SerializeField] private bool m_FlipXOnNikkeTarget;
    [SerializeField] private bool m_FlipXOnEnemyTarget;

    public Vector3 LocalOffset => m_LocalOffset;
    public bool FlipXOnNikkeTarget => m_FlipXOnNikkeTarget;
    public bool FlipXOnEnemyTarget => m_FlipXOnEnemyTarget;
    public abstract Coroutine Play(MonoBehaviour runner, GameObject instance, Transform parent, int sortingOrder, bool flipX);
}
