using UnityEngine;
using System.Collections;
using TMPro;

public class FloatPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_Text;
    [SerializeField] private CanvasGroup m_Group;
    [SerializeField] private float m_FloatY;
    [SerializeField] private float m_Duration;


    public void Play(int amount)
    {
        StartCoroutine(PlayCoroutine(amount));
    }

    private IEnumerator PlayCoroutine(int amount)
    {
        m_Text.SetText("+{0}", amount);
        m_Group.alpha = 1f;
        Vector3 startPos = transform.position;

        float elapsed = 0f;
        while (elapsed < m_Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / m_Duration;
            transform.position = new Vector3(
                startPos.x,
                startPos.y + Mathf.Lerp(0f, m_FloatY, t),
                startPos.z
            );
            m_Group.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        Destroy(gameObject);
    }

}
