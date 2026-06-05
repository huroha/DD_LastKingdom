using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QuestCompletePanel : MonoBehaviour
{
    [SerializeField] private GameObject m_Content;
    [SerializeField] private Button m_ReturnTownBtn;
    [SerializeField] private Button m_ContinueBtn;
    [SerializeField] private CombatHUD m_CombatHUD;

    [SerializeField] private FocusBlurController m_BlurController;
    [SerializeField] private float m_BlurStrength = 1f;

    [Header("Appear Animation")]
    [SerializeField] private float m_AppearDelay = 1f;
    [SerializeField] private RectTransform m_PopInTarget;
    [SerializeField] private float m_PopInStartScale = 1.3f;
    [SerializeField] private float m_PopInMidScale = 0.85f;
    [SerializeField] private float m_PopInDuration = 0.35f;

    [SerializeField] private RectTransform m_ScaleInTarget;
    [SerializeField] private float m_ScaleInDuration = 0.25f;
    [SerializeField] private GameObject[] m_LateActiveTargets;


    private void Awake()
    {
        m_Content.SetActive(false);
    }
    private void OnEnable()
    {
        EventBus.Subscribe<LootDismissedEvent>(OnLootDismissed);
        m_ReturnTownBtn.onClick.AddListener(OnReturnTownClicked);
        m_ContinueBtn.onClick.AddListener(OnContinueClicked);
    }
    private void OnDisable()
    {
        EventBus.Unsubscribe<LootDismissedEvent>(OnLootDismissed);
        m_ReturnTownBtn.onClick.RemoveListener(OnReturnTownClicked);
        m_ContinueBtn.onClick.RemoveListener(OnContinueClicked);
    }
    private void OnLootDismissed(LootDismissedEvent e)
    {
        if (!ExpeditionManager.Instance.IsQuestComplete) return;
        StartCoroutine(ShowCoroutine());
    }
    private void OnReturnTownClicked()
    {
        m_Content.SetActive(false);
        m_BlurController.SetBlurStrength(0f);
        ExpeditionManager.Instance.SetOutcome(ExpeditionOutcome.Cleared);
        GameManager.Instance.ChangeState(GameState.Settlement);
    }
    private void OnContinueClicked()
    {
        m_Content.SetActive(false);
        m_BlurController.SetBlurStrength(0f);
        m_CombatHUD.ArmQuestComplete();
    }
    private IEnumerator ShowCoroutine()
    {
        yield return new WaitForSeconds(m_AppearDelay);
        m_Content.SetActive(true);
        m_BlurController.SetBlurStrength(m_BlurStrength);
        m_ScaleInTarget.localScale = Vector3.zero;
        yield return StartCoroutine(CoroutineHelper.PopScale(m_PopInTarget, m_PopInStartScale, m_PopInMidScale, 1f, m_PopInDuration));
        foreach (GameObject go in m_LateActiveTargets)
            go.SetActive(true);
        yield return StartCoroutine(CoroutineHelper.ScaleTo(m_ScaleInTarget, 0f, 1f, m_ScaleInDuration));
    }
    
}
