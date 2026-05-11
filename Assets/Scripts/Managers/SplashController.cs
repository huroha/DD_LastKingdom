using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public enum LogoType { Animated, Static }
[Serializable]
public struct LogoEntry
{
    [SerializeField] private LogoType m_Type;
    [SerializeField] private Sprite[] m_Sprites;
    [SerializeField] private float m_Duration;
    [SerializeField] private float m_Interval;
    [SerializeField] private float m_FadeInDuration;
    [SerializeField] private string m_LogoText;
    [SerializeField] private Vector2 m_TextPosition;

    public LogoType Type => m_Type;
    public Sprite[] Sprites => m_Sprites;
    public float Duration => m_Duration;
    public float Interval => m_Interval;
    public float FadeInDuration => m_FadeInDuration;
    public string LogoText => m_LogoText;
    public Vector2 TextPosition => m_TextPosition;
}
public class SplashController : MonoBehaviour
{
    [SerializeField] private LogoEntry[] m_Logos;
    [SerializeField] private Image m_LogoImage;
    [SerializeField] private CanvasGroup m_LogoCanvasGroup;
    [SerializeField] private TextMeshProUGUI m_LogoTextField;

    [SerializeField] private GameObject m_ConfirmPanel;
    [SerializeField] private Button m_ConfirmButton;

    [SerializeField] private CanvasGroup m_ConfirmCanvasGroup;
    [SerializeField] private float m_LogoFadeOutDuration;
    [SerializeField] private float m_ConfirmFadeInDuration;


    private void Start()
    {
        StartCoroutine(PlaySequence());
    }
    private IEnumerator PlaySequence()
    {
        foreach (LogoEntry entry in m_Logos)
        {
            if (entry.Sprites == null || entry.Sprites.Length == 0) continue;
            if (entry.Type == LogoType.Animated)
            {
                bool hasText = !string.IsNullOrEmpty(entry.LogoText);
                m_LogoTextField.gameObject.SetActive(hasText);
                if (hasText)
                {
                    m_LogoTextField.text = entry.LogoText;
                    m_LogoTextField.rectTransform.anchoredPosition = entry.TextPosition;
                }
                float elapsed = 0f;
                int imageIndex = 0;
                while (elapsed < entry.Duration)
                {
                    m_LogoImage.sprite = entry.Sprites[imageIndex % entry.Sprites.Length];
                    yield return new WaitForSeconds(entry.Interval);
                    elapsed += entry.Interval;
                    ++imageIndex;
                }
            }
            else if (entry.Type == LogoType.Static)
            {
                bool hasText = !string.IsNullOrEmpty(entry.LogoText);
                m_LogoTextField.gameObject.SetActive(hasText);
                if (hasText)
                {
                    m_LogoTextField.text = entry.LogoText;
                    m_LogoTextField.rectTransform.anchoredPosition = entry.TextPosition;
                }
                m_LogoCanvasGroup.alpha = 0f;
                m_LogoImage.sprite = entry.Sprites[0];
                yield return StartCoroutine(FadeIn(m_LogoCanvasGroup, entry.FadeInDuration));
                float remaining = Mathf.Max(0f, entry.Duration - entry.FadeInDuration);
                yield return new WaitForSeconds(remaining);
            }
        }
        yield return StartCoroutine(FadeOut(m_LogoCanvasGroup, m_LogoFadeOutDuration));
        m_LogoImage.gameObject.SetActive(false);
        m_ConfirmCanvasGroup.alpha = 0f;
        m_ConfirmPanel.SetActive(true);
        yield return StartCoroutine(FadeIn(m_ConfirmCanvasGroup, m_ConfirmFadeInDuration));
        yield return StartCoroutine(WaitForConfirm());
    }
    private IEnumerator FadeIn(CanvasGroup cg, float duration)
    {
        cg.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }
    private IEnumerator FadeOut(CanvasGroup cg, float duration)
    {
        cg.alpha = 1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = 1 - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        cg.alpha = 0f;
    }
    private IEnumerator WaitForConfirm()
    {
        bool confirmed = false;
        m_ConfirmButton.onClick.AddListener(() => confirmed = true);

        while (!confirmed)
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                confirmed = true;
            yield return null;
        }

        m_ConfirmButton.onClick.RemoveAllListeners();
        GameManager.Instance.ChangeState(GameState.Title);
    }
}
