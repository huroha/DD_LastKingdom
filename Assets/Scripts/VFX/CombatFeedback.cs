using UnityEngine;
using System.Collections;
public class CombatFeedback : MonoBehaviour
{
    [Header("Hit Stop")]
    [SerializeField] private float m_HitStopDuration = 0.05f;


    private MaterialPropertyBlock m_PropBlock;

    private void Awake()
    {
        m_PropBlock = new MaterialPropertyBlock();
    }
    public Coroutine PlayHitStop(bool isCrit = false)
    {
        return StartCoroutine(HitStopRoutine(isCrit));
    }
    private IEnumerator HitStopRoutine(bool isCrit)
    {
        float duration = isCrit ? m_HitStopDuration * 2f : m_HitStopDuration;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
   
}
