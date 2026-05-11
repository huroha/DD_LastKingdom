using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
public class EncounterCardView : MonoBehaviour, IPointerClickHandler
{

    [SerializeField] private Image m_BgIcon;
    [SerializeField] private Image m_Icon;
    [SerializeField] private Image m_SelectOverlay;
    [SerializeField] private float m_PulseSpeed = 1.2f;
    [SerializeField] private float m_PulseAmplitude = 0.12f;

    private Coroutine m_PulseCoroutine;
    private EncounterData m_Data;
    public delegate void ClickHandler(EncounterCardView card, EncounterData data);
    public event ClickHandler OnClicked;

    public void Bind(EncounterData data)
    {
        if (data == null) return;
        m_Data = data;
        m_BgIcon.sprite = data.BgIconSprite;
        m_Icon.sprite = data.IconSprite;
        SetSelected(false);
    }
    public void SetSelected(bool selected)
    {
        m_SelectOverlay.enabled = selected;
        if (!selected)
        {
            m_SelectOverlay.rectTransform.localRotation = Quaternion.identity;
            if (m_PulseCoroutine != null)
            {
                StopCoroutine(m_PulseCoroutine);
                m_PulseCoroutine = null;
            }
            m_BgIcon.transform.localScale = Vector3.one;
            m_Icon.transform.localScale = Vector3.one;
        }
        else
        {
            if (m_PulseCoroutine != null)
                StopCoroutine(m_PulseCoroutine);
            m_PulseCoroutine = StartCoroutine(PulseRoutine());
        }
    }
    private IEnumerator PulseRoutine()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * m_PulseSpeed;
            float normalized = Mathf.Sin(t * Mathf.PI * 2f) * 0.5f + 0.5f;
            float scale = 1f + normalized * m_PulseAmplitude;
            Vector3 s = new Vector3(scale, scale, 1f);
            m_BgIcon.transform.localScale = s;
            m_Icon.transform.localScale = s;
            yield return null;
        }
    }
    public void OnPointerClick(PointerEventData e) => OnClicked?.Invoke(this, m_Data);
    public void ShuffleRotation()
    {
        m_SelectOverlay.rectTransform.localRotation = Quaternion.Euler(0, 0f, Random.Range(-50, 50f));
    }
}
