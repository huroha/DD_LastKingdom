using UnityEngine;
using System.Collections;
public class CombatFeedback : MonoBehaviour
{
    [Header("Hit Stop")]
    [SerializeField] private float m_HitStopDuration = 0.05f;

    [Header("Camera Shake")]
    [SerializeField] private Transform m_ShakeTarget;
    [SerializeField] private float m_ShakeIntensity;
    [SerializeField] private float m_ShakeDuration;

    private Vector3 m_ShakeOriginalPos;
    private Coroutine m_ShakeCoroutine;


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

    //Cam Shake
    public void PlayCameraShake()
    {
        if (m_ShakeCoroutine != null)
        {
            StopCoroutine(m_ShakeCoroutine);
            m_ShakeTarget.position = m_ShakeOriginalPos;
        }
        m_ShakeOriginalPos = m_ShakeTarget.position;
        m_ShakeCoroutine = StartCoroutine(ShakeRoutine());
    }
    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < m_ShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector2 offset = Random.insideUnitCircle * m_ShakeIntensity;
            m_ShakeTarget.position = m_ShakeOriginalPos + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }
        m_ShakeTarget.position = m_ShakeOriginalPos;
        m_ShakeCoroutine = null;
    }
}
