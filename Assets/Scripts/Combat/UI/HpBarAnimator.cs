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
        if (newValue == m_MainSlider.value)
            return;
        float ghostStart = m_ActiveCoroutine != null ? m_GhostFill.fillAmount : m_MainSlider.value;
        if (m_ActiveCoroutine != null)
            StopCoroutine(m_ActiveCoroutine);
        bool isDamage = newValue < m_MainSlider.value;
        m_ActiveCoroutine = StartCoroutine(AnimateRoutine(ghostStart, newValue, isDamage));
    }

    private IEnumerator AnimateRoutine(float ghostStart, float targetValue, bool isDamage)
    {
        m_MainSlider.value = targetValue;

        if (isDamage)
        {
            // 데미지: Fill 뒤에서 오렌지 고스트 줄어들기
            m_GhostFill.transform.SetAsFirstSibling();   // Fill보다 앞 sibling index로
            m_GhostRect.anchorMin = new Vector2(0f, m_GhostRect.anchorMin.y);
            m_GhostRect.anchorMax = new Vector2(ghostStart, m_GhostRect.anchorMax.y);
            m_GhostFill.color = m_DamageColor;
            m_GhostFill.gameObject.SetActive(true);
            yield return new WaitForSeconds(m_HoldDuration);

            float elapsed = 0f;
            while (elapsed < m_DrainDuration)
            {
                elapsed += Time.deltaTime;
                float value = Mathf.Lerp(ghostStart, targetValue, elapsed / m_DrainDuration);
                m_GhostRect.anchorMax = new Vector2(value, m_GhostRect.anchorMax.y);
                yield return null;
            }
            m_GhostRect.anchorMax = new Vector2(targetValue, m_GhostRect.anchorMax.y);
        }
        else
        {
            // 힐: Fill 앞에서 초록 구간 알파 페이드
            m_GhostFill.transform.SetAsLastSibling();    // Fill보다 뒤 sibling index로
            m_GhostRect.anchorMin = new Vector2(ghostStart, m_GhostRect.anchorMin.y);
            m_GhostRect.anchorMax = new Vector2(targetValue, m_GhostRect.anchorMax.y);
            Color healColor = m_HealColor;
            m_GhostFill.color = healColor;
            m_GhostFill.gameObject.SetActive(true);
            yield return new WaitForSeconds(m_HoldDuration);

            float elapsed = 0f;
            while (elapsed < m_DrainDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / m_DrainDuration;
                m_GhostFill.color = new Color(healColor.r, healColor.g, healColor.b, Mathf.Lerp(1f, 0f, t));
                yield return null;
            }
            m_GhostRect.anchorMin = new Vector2(0f, m_GhostRect.anchorMin.y);
        }

        m_GhostFill.gameObject.SetActive(false);
        m_ActiveCoroutine = null;
    }
}
