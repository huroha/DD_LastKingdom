using UnityEngine;
using System.Collections;
public class CombatFeedback : MonoBehaviour
{
    [Header("Hit Stop")]
    [SerializeField] private float m_HitStopDuration = 0.05f;

    [Header("Flash")]
    [SerializeField] private Color m_FlashColor = Color.white;
    [SerializeField] private float m_FlashDuration = 0.1f;

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
    public Coroutine PlayFlash(SpriteRenderer target)
    {
        return StartCoroutine(FlashRoutine(target));
    }
    private IEnumerator FlashRoutine(SpriteRenderer target)
    {
        target.GetPropertyBlock(m_PropBlock);
        m_PropBlock.SetColor("_Color", m_FlashColor);
        target.SetPropertyBlock(m_PropBlock);
        yield return new WaitForSecondsRealtime(m_FlashDuration);
        m_PropBlock.SetColor("_Color", Color.white);
        target.SetPropertyBlock(m_PropBlock);
    }    
}
