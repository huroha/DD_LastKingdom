using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TitleSceneUI : MonoBehaviour
{
    [Header("StartButton")]
    [SerializeField] private Button m_StartButton;
    [Header("BG")]
    [SerializeField] private Transform m_BgTransform;
    [SerializeField] private CanvasGroup m_StartBtnGroup;
    [SerializeField] private float m_PanDuration = 3.5f;
    [SerializeField] private float m_BtnFadeInDuration = 1.0f;
    [SerializeField] private float m_BgStartY = -5.4f;
    [SerializeField] private float m_BgEndY = 5.4f;


    private void Awake()
    {
        m_StartBtnGroup.alpha = 0f;
        m_StartBtnGroup.interactable = false;
        m_StartButton.onClick.AddListener(OnStartClicked);
    }
    private void Start()
    {
        StartCoroutine(BgPanRoutine());
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
    }
    private IEnumerator BtnFadeInRoutine()
    {
        float elapsed = 0f;
        Vector3 pos = m_BgTransform.position;
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
}
