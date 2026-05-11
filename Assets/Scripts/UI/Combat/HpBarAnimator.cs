using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HpBarAnimator : MonoBehaviour
{
    [SerializeField] private Slider m_MainSlider;
    [SerializeField] private Image m_GhostFill;

    [Header("Timing")]
    [SerializeField] private float m_HoldDuration = 0.4f;
    [SerializeField] private float m_DrainDuration = 0.5f;

    [Header("Colors")]
    [SerializeField] private Color m_DamageColor;
    [SerializeField] private Color m_HealColor;

    private Coroutine m_ActiveCoroutine;
    private RectTransform m_GhostRect;

    private float m_PendingGhostStart;
    private float m_PendingTargetValue;
    private bool m_PendingIsDamage;

    public bool IsAnimating => m_ActiveCoroutine != null;
    private void Awake()
    {
        m_GhostRect = m_GhostFill.GetComponent<RectTransform>();
    }

    public void InitHp(int currentHp, int maxHp)
    {
        if (m_ActiveCoroutine != null)
        {
            StopCoroutine(m_ActiveCoroutine);
            m_ActiveCoroutine = null;
        }
        float value = (float)currentHp / maxHp;
        m_MainSlider.value = value;
        m_GhostRect.anchorMax = new Vector2(value, m_GhostRect.anchorMax.y);
        m_GhostFill.gameObject.SetActive(false);
    }

    public void SetHp(int currentHp, int maxHp)
    {
        float newValue = (float)currentHp / maxHp;
        if (m_ActiveCoroutine != null && Mathf.Approximately(m_PendingTargetValue, newValue))
            return;
        if (newValue == m_MainSlider.value)
            return;
        int previousHp = Mathf.RoundToInt(m_MainSlider.value * maxHp);
        PrepareGhost(previousHp, currentHp, maxHp);
        StartGhostDrain();
    }

    public void PrepareGhost(int previousHp,int currentHp, int maxHp)
    {
        if (m_ActiveCoroutine != null)
        {
            StopCoroutine(m_ActiveCoroutine);
            m_ActiveCoroutine = null;
        }
        float ghostStart = (float)previousHp / maxHp;
        float newValue = (float)currentHp / maxHp;
        if (Mathf.Approximately(newValue, ghostStart))
            return;

        bool isDamage = newValue < ghostStart;

        if (isDamage)
        {
            m_MainSlider.value = newValue;
            m_GhostFill.transform.SetAsFirstSibling();
            m_GhostRect.anchorMin = new Vector2(0f, m_GhostRect.anchorMin.y);
            m_GhostRect.anchorMax = new Vector2(ghostStart, m_GhostRect.anchorMax.y);
            m_GhostFill.color = m_DamageColor;
        }
        else
        {
            m_GhostFill.transform.SetAsLastSibling();
            m_GhostRect.anchorMin = new Vector2(ghostStart, m_GhostRect.anchorMin.y);
            m_GhostRect.anchorMax = new Vector2(newValue, m_GhostRect.anchorMax.y);
            m_GhostFill.color = m_HealColor;
            m_MainSlider.value = ghostStart;
        }
        m_GhostFill.gameObject.SetActive(true);

        m_PendingGhostStart = ghostStart;
        m_PendingTargetValue = newValue;
        m_PendingIsDamage = isDamage;
    }

    public void StartGhostDrain()
    {
        if (!m_GhostFill.gameObject.activeSelf)
            return;
        if (!gameObject.activeInHierarchy)
            return;
        m_ActiveCoroutine = StartCoroutine(DrainRoutine(m_PendingGhostStart, m_PendingTargetValue, m_PendingIsDamage));
    }

    private IEnumerator DrainRoutine(float ghostStart, float targetValue, bool isDamage)
    {
        yield return new WaitForSeconds(m_HoldDuration);

        float elapsed = 0f;
        while (elapsed < m_DrainDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / m_DrainDuration;
            if (isDamage)
            {
                m_GhostRect.anchorMax = new Vector2(Mathf.Lerp(ghostStart, targetValue, t), m_GhostRect.anchorMax.y);
            }
            else
            {
                float current = Mathf.Lerp(ghostStart, targetValue, t);
                m_MainSlider.value = current;
                m_GhostRect.anchorMin = new Vector2(current, m_GhostRect.anchorMin.y);
            }
            yield return null;
        }

        if (isDamage)
            m_GhostRect.anchorMax = new Vector2(targetValue, m_GhostRect.anchorMax.y);
        else
        {
            m_MainSlider.value = targetValue;
            m_GhostRect.anchorMin = new Vector2(0f, m_GhostRect.anchorMin.y);
        }

        m_GhostFill.gameObject.SetActive(false);
        m_ActiveCoroutine = null;
    }
}
