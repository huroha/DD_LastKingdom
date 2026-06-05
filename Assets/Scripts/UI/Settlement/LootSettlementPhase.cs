using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;
public class LootSettlementPhase : SettlementPhase
{
    [Header("Panel")]
    [SerializeField] private GameObject m_Root;
    [SerializeField] private GameObject m_Settle;
    [SerializeField] private Button m_NextButton;

    [Header("정산표")]
    [SerializeField] private TextMeshProUGUI m_TotalCreditText;
    [SerializeField] private TextMeshProUGUI m_CoreText;
    [SerializeField] private TextMeshProUGUI m_BattleDataText;
    [SerializeField] private TextMeshProUGUI m_GemsText;
    [SerializeField] private Transform m_CreditLineRoot;
    [SerializeField] private Transform m_ResourceLineRoot;

    [SerializeField] private float m_StackOffset = 20f;         // 겹침 간격
    [SerializeField] private SettlementLineView m_LineViewPrefab;
    [SerializeField] private SettlementLineView m_LineViewPrefab2;

    private float m_ActualCreditOffset;
    private float m_ActualResourceOffset;

    [Header("연출")]
    [SerializeField] private float m_LineInterval = 0.25f;
    [SerializeField] private float m_StartDelay = 1f;

    [Header("Credit popup")]
    [SerializeField] private FloatPopup m_FloatPopupPrefab;
    [SerializeField] private RectTransform m_PopupRoot;

    [Header("Outcome Header")]
    [SerializeField] private Image m_TitleImage;
    [SerializeField] private Sprite[] m_TitleSprites;   // 0:Clear, 1:Retreat, 2:Wipe
    [SerializeField] private TextMeshProUGUI m_TitleText;
    [SerializeField] private string[] m_TitleStrings;   // 0:Clear, 1:Retreat, 2:Wipe
    [SerializeField] private GameObject m_SettleDim;

    private float m_LastClickTime = -1f;
    private const float DoubleClickThreshold = 0.3f;
    private bool m_IsSequencePlaying;

    private Action m_OnComplete;
    private SettlementReport m_Report;
    private int m_RunningCredit;
    private int m_CreditLineCount;
    private int m_ResourceLineCount;
    private int m_RunningCore;
    private int m_RunningBattleData;
    private int m_RunningGems;

    public override void Begin(Action onComplete)
    {
        m_OnComplete = onComplete;
        m_Root.SetActive(true);
        ExpeditionOutcome outcome = ExpeditionManager.Instance.Outcome;
        ApplyOutcomeHeader(outcome);
        m_NextButton.gameObject.SetActive(false);

        if (outcome != ExpeditionOutcome.Cleared)
        {
            // 퇴각 전멸은 전리품 x
            if (m_SettleDim != null) m_SettleDim.SetActive(true);
            m_NextButton.gameObject.SetActive(true);
            m_NextButton.onClick.AddListener(OnNextClicked);
            return;
        }

        m_CreditLineCount = 0;
        m_ResourceLineCount = 0;
        m_TotalCreditText.SetText("0");
        if (m_CoreText != null) m_CoreText.SetText("0");
        if (m_BattleDataText != null) m_BattleDataText.SetText("0");
        if (m_GemsText != null) m_GemsText.SetText("0");
        m_Report = ExpeditionManager.Instance.BuildReport();
        m_IsSequencePlaying = true;
        StartCoroutine(PlaySequence(true));
    }
    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Time.time - m_LastClickTime < DoubleClickThreshold)
                SkipSequence();
            m_LastClickTime = Time.time;
        }
    }
    private IEnumerator PlaySequence(bool animate)
    {
        if (animate) yield return new WaitForSeconds(m_StartDelay);

        WaitForSeconds wait = animate ? new WaitForSeconds(m_LineInterval) : null;
        InventoryConfig cfg = DataManager.Instance.InventoryConfig;
        SettlementConfig scfg = DataManager.Instance.SettlementConfig;

        CalcOffsets();

        // 1. 직접 Credit
        for (int i = 0; i < m_Report.CreditSlots.Count; ++i)
        {
            m_RunningCredit += m_Report.CreditSlots[i];
            SettlementLineView view = SpawnCreditLine(cfg.Icon(LootType.Credit), m_Report.CreditSlots[i], animate);
            if (animate) SpawnPopup(view.GetComponent<RectTransform>(), m_Report.CreditSlots[i]);
            m_TotalCreditText.SetText(m_RunningCredit.ToString());
            if (animate) yield return wait;
        }

        // 2. Relic → Credit 변환 라인
        for (int i = 0; i < m_Report.Lines.Count; ++i)
        {
            SettlementLine line = m_Report.Lines[i];
            Sprite icon = cfg.Icon(line.SourceType, line.Relic);
            int creditPerUnit = scfg.RelicCredit(line.Relic);
            for (int j = 0; j < line.SourceQuantity; ++j)
            {
                m_RunningCredit += creditPerUnit;
                SettlementLineView view = SpawnCreditLine(icon, 0, animate);
                if (animate) SpawnPopup(view.GetComponent<RectTransform>(), creditPerUnit);
                m_TotalCreditText.SetText(m_RunningCredit.ToString());
                if (animate) yield return wait;
            }
        }

        // Core / BattleData / Gems (수량만큼 1개씩)
        for (int j = 0; j < m_Report.Core; ++j)
        {
            m_RunningCore++;
            SpawnResourceLine(cfg.Icon(LootType.Core), animate);
            m_CoreText.SetText(m_RunningCore.ToString());
            if (animate) yield return wait;
        }
        for (int j = 0; j < m_Report.BattleData; ++j)
        {
            m_RunningBattleData++;
            SpawnResourceLine(cfg.Icon(LootType.BattleData), animate);
            m_BattleDataText.SetText(m_RunningBattleData.ToString());
            if (animate) yield return wait;
        }
        for (int j = 0; j < m_Report.Gems; ++j)
        {
            m_RunningGems++;
            SpawnResourceLine(cfg.Icon(LootType.Gems), animate);
            m_GemsText.SetText(m_RunningGems.ToString());
            if (animate) yield return wait;
        }
        m_IsSequencePlaying = false;
        m_NextButton.gameObject.SetActive(true);
        m_NextButton.onClick.AddListener(OnNextClicked);
    }

    private void OnNextClicked()
    {
        m_NextButton.onClick.RemoveListener(OnNextClicked);
        if (ExpeditionManager.Instance.Outcome == ExpeditionOutcome.Cleared)
            ExpeditionManager.Instance.CommitLoot(m_Report);
        if (m_SettleDim != null) m_SettleDim.SetActive(false);
        m_Settle.SetActive(false);
        m_OnComplete?.Invoke();
    }
    private SettlementLineView SpawnCreditLine(Sprite icon, int quantity, bool playEffect = true)
    {
        SettlementLineView view = Instantiate(m_LineViewPrefab, m_CreditLineRoot);
        RectTransform rt = view.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(m_CreditLineCount * m_ActualCreditOffset, 0f);
        ++m_CreditLineCount;
        view.Setup(icon, quantity, playEffect);
        return view;
    }

    private void SpawnResourceLine(Sprite icon, bool playEffect = true)
    {
        SettlementLineView view = Instantiate(m_LineViewPrefab2, m_ResourceLineRoot);
        RectTransform rt = view.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(m_ResourceLineCount * m_ActualResourceOffset, 0f);
        ++m_ResourceLineCount;
        view.Setup(icon,0, playEffect);
    }
    private void SpawnPopup(RectTransform anchor, int amount)
    {
        FloatPopup popup = Instantiate(m_FloatPopupPrefab, m_PopupRoot);
        popup.transform.position = anchor.transform.position;
        popup.Play(amount);
    }
    private void CalcOffsets()
    {
        int creditCount = m_Report.CreditSlots.Count;
        for (int i = 0; i < m_Report.Lines.Count; ++i)
            creditCount += m_Report.Lines[i].SourceQuantity;

        int resourceCount = m_Report.Core + m_Report.BattleData + m_Report.Gems;

        float creditWidth = ((RectTransform)m_CreditLineRoot).rect.width;
        float resourceWidth = ((RectTransform)m_ResourceLineRoot).rect.width;

        m_ActualCreditOffset = creditCount > 1
            ? Mathf.Min(m_StackOffset, creditWidth / creditCount)
            : m_StackOffset;
        m_ActualResourceOffset = resourceCount > 1
            ? Mathf.Min(m_StackOffset, resourceWidth / resourceCount)
            : m_StackOffset;
    }
    private void SkipSequence()
    {
        if (!m_IsSequencePlaying) return;
        StopAllCoroutines();

        // 기존 LineView 제거 + running 값 리셋
        foreach (Transform child in m_CreditLineRoot) Destroy(child.gameObject);
        foreach (Transform child in m_ResourceLineRoot) Destroy(child.gameObject);
        m_CreditLineCount = 0;
        m_ResourceLineCount = 0;
        m_RunningCredit = 0;
        m_RunningCore = 0;
        m_RunningBattleData = 0;
        m_RunningGems = 0;

        StartCoroutine(PlaySequence(false));
    }
    private void ApplyOutcomeHeader(ExpeditionOutcome outcome)
    {
        Debug.Log($"[Settlement] outcome = {outcome}");
        int idx;
        switch (outcome)
        {
            case ExpeditionOutcome.Cleared: idx = 0; break;
            case ExpeditionOutcome.Retreated: idx = 1; break;
            case ExpeditionOutcome.Wiped: idx = 2; break;
            default: idx = 0; break;
        }
        if (m_TitleImage != null && idx < m_TitleSprites.Length)
            m_TitleImage.sprite = m_TitleSprites[idx];
        if (m_TitleText != null && idx < m_TitleStrings.Length)
            m_TitleText.SetText(m_TitleStrings[idx]);
    }
}
