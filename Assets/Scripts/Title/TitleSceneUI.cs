using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

public class TitleSceneUI : MonoBehaviour
{
    [Header("StartButton")]
    [SerializeField] private Button m_StartButton;

    [Header("Title")]
    [SerializeField] private ParticleSystem m_GlitterPS;
    [SerializeField] private CanvasGroup m_TitleGroup;
    [SerializeField] private float m_GlitterFadeInDuration = 1.5f;

    [Header("BG")]
    [SerializeField] private Transform m_BgTransform;
    [SerializeField] private CanvasGroup m_StartBtnGroup;
    [SerializeField] private float m_PanDuration = 3.5f;
    [SerializeField] private float m_BtnFadeInDuration = 1.0f;
    [SerializeField] private float m_BgStartY = -5.4f;
    [SerializeField] private float m_BgEndY = 5.4f;

    private float m_LastClickTime = -1f;
    private const float DoubleClickThreshold = 0.3f;


    private void Awake()
    {
        m_StartBtnGroup.alpha = 0f;
        m_StartBtnGroup.interactable = false;
        m_TitleGroup.alpha = 0f;
        m_StartButton.onClick.AddListener(OnStartClicked);
        m_GlitterPS.Stop();
    }
    private void Start()
    {
        StartCoroutine(BgPanRoutine());
    }
    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Time.time - m_LastClickTime < DoubleClickThreshold)
                SkipAnimation();
            m_LastClickTime = Time.time;
        }
    }
    private void SkipAnimation()
    {
        StopAllCoroutines();
        Vector3 pos = m_BgTransform.position;
        pos.y = m_BgEndY;
        m_BgTransform.position = pos;
        m_StartBtnGroup.alpha = 1f;
        m_StartBtnGroup.interactable = true;
        ParticleSystem.MainModule main = m_GlitterPS.main;
        Color c = main.startColor.color;
        c.a = 1f;
        main.startColor = c;
        m_TitleGroup.alpha = 1f;
        m_GlitterPS.Play();
    }
    private IEnumerator BgPanRoutine()
    {
        float elapsed = 0f;
        Vector3 pos = m_BgTransform.position;
        while (elapsed < m_PanDuration)
        {

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / m_PanDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            pos.y = Mathf.Lerp(m_BgStartY, m_BgEndY, eased);
            m_BgTransform.position = pos;
            yield return null;
        }
        pos.y = m_BgEndY;
        m_BgTransform.position = pos;
        StartCoroutine(BtnFadeInRoutine());
        StartCoroutine(GlitterFadeInRoutine());
    }
    private IEnumerator BtnFadeInRoutine()
    {
        float elapsed = 0f;
        while (elapsed < m_BtnFadeInDuration)
        {
            elapsed += Time.deltaTime;
            m_StartBtnGroup.alpha = Mathf.Clamp01(elapsed / m_BtnFadeInDuration);
            yield return null;
        }
        m_StartBtnGroup.alpha = 1f;
        m_StartBtnGroup.interactable = true;
    }
    private void OnStartClicked()
    {
        GameManager.Instance.ChangeState(GameState.Town);
    }
    private IEnumerator GlitterFadeInRoutine()
    {
        m_GlitterPS.Play();
        float elapsed = 0f;
        ParticleSystem.MainModule main = m_GlitterPS.main;
        while (elapsed < m_GlitterFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / m_GlitterFadeInDuration);
            Color c = main.startColor.color;
            c.a = alpha;
            main.startColor = c;
            m_TitleGroup.alpha = alpha;
            yield return null;
        }
        Color final = main.startColor.color;
        final.a = 1f;
        main.startColor = final;
        m_TitleGroup.alpha = 1f;
    }
}
