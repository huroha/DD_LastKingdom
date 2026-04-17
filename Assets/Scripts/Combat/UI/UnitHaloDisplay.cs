using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class UnitHaloDisplay : MonoBehaviour, IStunHaloDisplay
{
    [SerializeField] private CanvasGroup m_StunHalo;
    [SerializeField] private CanvasGroup m_DeathsDoorHalo;
    [SerializeField] private CanvasGroup m_EblaUpHalo;
    [SerializeField] private CanvasGroup m_EblaDownHalo;
    [SerializeField] private TextMeshProUGUI m_EblaUpText;
    [SerializeField] private TextMeshProUGUI m_EblaDownText;


    [Header("Popup Timing")]
    [SerializeField] private float m_HoldDuration = 1.0f;
    [SerializeField] private float m_FadeDuration = 0.3f;

    private Coroutine m_DeathsDoorRoutine;
    private Coroutine m_EblaUpRoutine;
    private Coroutine m_EblaDownRoutine;
    private Coroutine m_StunPulseRoutine;

    [Header("Stun Effect")]
    [SerializeField] private float m_PulseSpeed;
    [SerializeField] private float m_PulseMin;
    [SerializeField] private float m_PulseOffset;
    [SerializeField] private Image[] m_PulseTargets;
    private bool m_IsPulsing;

    private WaitForSeconds m_WaitHold;

    private void Awake()
    {
        m_StunHalo.alpha = 0;
        m_StunHalo.gameObject.SetActive(false);

        m_DeathsDoorHalo.alpha = 0;
        m_DeathsDoorHalo.gameObject.SetActive(false);

        m_EblaUpHalo.alpha = 0;
        m_EblaUpHalo.gameObject.SetActive(false);

        m_EblaDownHalo.alpha = 0;
        m_EblaDownHalo.gameObject.SetActive(false);
        m_WaitHold = new WaitForSeconds(m_HoldDuration);
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
    public void PopupDeathsDoor()
    {
        if (m_DeathsDoorHalo == null) return;
        CoroutineHelper.Restart(this, ref m_DeathsDoorRoutine, DoPopup(m_DeathsDoorHalo));
    }
    public void PopupEblaUp(int delta)
    {
        if (m_EblaUpHalo == null) return;
        if (m_EblaUpText != null) m_EblaUpText.text = delta.ToString();
        CoroutineHelper.Restart(this, ref m_EblaUpRoutine, DoPopup(m_EblaUpHalo));
    }
    public void PopupEblaDown(int delta)
    {
        if (m_EblaDownHalo == null) return;
        if (m_EblaDownText != null) m_EblaDownText.text = delta.ToString();
        CoroutineHelper.Restart(this, ref m_EblaDownRoutine, DoPopup(m_EblaDownHalo));
    }
    private IEnumerator DoPopup(CanvasGroup group)
    {
        if (group == null)
            yield break;
        group.gameObject.SetActive(true);
        group.alpha = 1f;

        yield return m_WaitHold;
        float elapsed = 0f;
        while(elapsed < m_FadeDuration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(1f, 0f, elapsed / m_FadeDuration);
            yield return null;
        }
        group.alpha = 0f;
        group.gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        if (m_IsPulsing)
            CoroutineHelper.Restart(this, ref m_StunPulseRoutine, CoroutineHelper.PulseAlpha(m_PulseTargets, m_PulseSpeed, m_PulseMin, m_PulseOffset));
    }
}
