using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EnemyHaloDisplay : MonoBehaviour , IStunHaloDisplay
{
    [SerializeField] private CanvasGroup m_StunHalo;

    private Coroutine m_StunPulseRoutine;

    [Header("Stun Effect")]
    [SerializeField] private float m_PulseSpeed;
    [SerializeField] private float m_PulseMin;
    [SerializeField] private float m_PulseOffset;
    [SerializeField] private Image[] m_PulseTargets;
    private bool m_IsPulsing;
    private void Awake()
    {
        m_StunHalo.alpha = 0;
        m_StunHalo.gameObject.SetActive(false);
    }

    public void ShowStunHalo()
    {
        m_IsPulsing = true;
        m_StunHalo.gameObject.SetActive(true);
        m_StunHalo.alpha = 1;
        CoroutineHelper.Restart(this, ref m_StunPulseRoutine, CoroutineHelper.PulseAlpha(m_PulseTargets, m_PulseSpeed, m_PulseMin, m_PulseOffset));
    }
    public void HideStunHalo()
    {
        m_IsPulsing = false;
        CoroutineHelper.Stop(this, ref m_StunPulseRoutine);
        m_StunHalo.alpha = 0;
        m_StunHalo.gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        if (m_IsPulsing)
            CoroutineHelper.Restart(this, ref m_StunPulseRoutine, CoroutineHelper.PulseAlpha(m_PulseTargets, m_PulseSpeed, m_PulseMin, m_PulseOffset));
    }
}
