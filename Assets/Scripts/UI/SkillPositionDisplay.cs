using UnityEngine;
using UnityEngine.UI;

public class SkillPositionDisplay : MonoBehaviour
{
    [SerializeField] private Image[] m_UsableCircles; // 4개
    [SerializeField] private Image[] m_TargetCircles; // 4개
    [SerializeField] private Image m_ConnectionBar;

    [SerializeField] private Sprite m_ActiveSprite;
    [SerializeField] private Sprite m_InactiveSprite;

    public void Refresh(SkillData skill)
    {
        int usableCount = Mathf.Min(m_UsableCircles.Length, skill.UsablePositions.Count);
        for (int i = 0; i < usableCount; ++i)
            m_UsableCircles[i].sprite = skill.UsablePositions[i] ? m_ActiveSprite : m_InactiveSprite;

        int targetCount = Mathf.Min(m_TargetCircles.Length, skill.TargetPositions.Count);
        for (int i = 0; i < targetCount; ++i)
            m_TargetCircles[i].sprite = skill.TargetPositions[i] ? m_ActiveSprite : m_InactiveSprite;

        RefreshConnectionBar(skill);
    }

    private void RefreshConnectionBar(SkillData skill)
    {
        int firstActive = -1;
        int lastActive = -1;
        int count = Mathf.Min(m_TargetCircles.Length, skill.TargetPositions.Count);
        for (int i=0; i< count; ++i)
        {
            if (!skill.TargetPositions[i])
                continue;
            if (firstActive == -1)
                firstActive = i;
            lastActive = i;
        }

        // 단일 타겟이나 활성 원 없으면 숨김
        if (firstActive == lastActive)
        {
            m_ConnectionBar.gameObject.SetActive(false);
            return;
        }
        // 레이아웃 확정 후 위치 계산

        RectTransform firstRect = m_TargetCircles[firstActive].rectTransform;
        RectTransform lastRect = m_TargetCircles[lastActive].rectTransform;

        Vector2 firstPos = firstRect.anchoredPosition;
        Vector2 lastPos = lastRect.anchoredPosition;

        float width = Mathf.Abs(lastPos.x - firstPos.x) + firstRect.rect.width;
        Vector2 mid = (firstPos + lastPos) * 0.5f;

        m_ConnectionBar.rectTransform.anchoredPosition = mid;
        m_ConnectionBar.rectTransform.sizeDelta = new Vector2(width, firstRect.rect.height);
        m_ConnectionBar.gameObject.SetActive(true);
    }
}
