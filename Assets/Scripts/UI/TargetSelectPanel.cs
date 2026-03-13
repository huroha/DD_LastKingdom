using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;


public class TargetSelectPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatStateMachine m_CombatStateMachine;

    [Header("Target Buttons")]
    [SerializeField] private Button[]       m_EnemyButtons;   // 4개
    [SerializeField] private Button[]       m_NikkeButtons;   // 4개

    [Header("Unit Names")]
    [SerializeField] private TextMeshProUGUI[] m_EnemyNames;
    [SerializeField] private TextMeshProUGUI[] m_NikkeNames;

    [Header("Highlights")]
    [SerializeField] private Image[] m_EnemyHighlights; // 4개
    [SerializeField] private Image[] m_NikkeHighlights;

    [Header("Cancel Button")]
    [SerializeField] private Button m_CancelButton;


    public delegate void TargetSelectedHandler(CombatUnit target);
    public delegate void CancelHandler();


    private TargetSelectedHandler m_OnTargetSelected;
    private CancelHandler m_OnCancel;

    private List<CombatUnit> m_ValidTargets;

    private static readonly Color HIGHLIGHT_DIM = new Color(1f, 1f, 1f, 0.35f);
    private static readonly Color HIGHLIGHT_BRIGHT = new Color(1f, 1f, 1f, 1f);

    private SkillData m_CurrentSkill;

    private void Awake()
    {
        HideAllHighlights();

        for (int i = 0; i < m_EnemyButtons.Length; ++i)
        {
            int index = i; // 루프 변수를 별도 변수에 캡처
            m_EnemyButtons[i].onClick.AddListener(() =>
            {
                CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, index);
                OnTargetButtonClicked(unit);
            });
        }
        for (int i=0; i<m_NikkeButtons.Length; ++i)
        {
            int index = i;
            m_NikkeButtons[i].onClick.AddListener(() =>
            {
                CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, index);
                OnTargetButtonClicked(unit);
            });
        }

        m_CancelButton.onClick.AddListener(OnCancelButtonClicked);

        for (int i=0; i<m_EnemyButtons.Length; ++i)
        {
            int index = i;
            UnityEngine.EventSystems.EventTrigger trigger = m_EnemyButtons[i].gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            UnityEngine.EventSystems.EventTrigger.Entry enter = new UnityEngine.EventSystems.EventTrigger.Entry();
            enter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            enter.callback.AddListener(_ => OnButtonHoverEnter(CombatUnitType.Enemy, index));
            trigger.triggers.Add(enter);

            UnityEngine.EventSystems.EventTrigger.Entry exit = new UnityEngine.EventSystems.EventTrigger.Entry();
            exit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            exit.callback.AddListener(_ => OnButtonHoverExit());
            trigger.triggers.Add(exit);

        }

        for (int i = 0; i < m_NikkeButtons.Length; ++i)
        {
            int index = i;
            UnityEngine.EventSystems.EventTrigger trigger =
                m_NikkeButtons[i].gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            UnityEngine.EventSystems.EventTrigger.Entry enter = new UnityEngine.EventSystems.EventTrigger.Entry();
            enter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            enter.callback.AddListener(_ => OnButtonHoverEnter(CombatUnitType.Nikke, index));
            trigger.triggers.Add(enter);

            UnityEngine.EventSystems.EventTrigger.Entry exit = new UnityEngine.EventSystems.EventTrigger.Entry();
            exit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            exit.callback.AddListener(_ => OnButtonHoverExit());
            trigger.triggers.Add(exit);
        }


        EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Subscribe<UnitMovedEvent>(OnUnitMoved);
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Unsubscribe<UnitMovedEvent>(OnUnitMoved);
    }

    public void Show(List<CombatUnit> validTargets, SkillData skill,
                     TargetSelectedHandler onTargetSelected, CancelHandler onCancel)
    { 
        m_ValidTargets = validTargets;
        m_CurrentSkill = skill;
        m_OnTargetSelected = onTargetSelected;
        m_OnCancel = onCancel;
        RefreshButtons();
        RefreshHighlights();
        gameObject.SetActive(true);

    }
    public void Hide() 
    {
        m_ValidTargets = null;
        HideAllHighlights();
        for (int i = 0; i < m_NikkeButtons.Length; ++i)
            m_NikkeButtons[i].interactable = false;
        for (int i = 0; i < m_EnemyButtons.Length; ++i)
            m_EnemyButtons[i].interactable = false;
        gameObject.SetActive(false);
    }

    private void RefreshButtons()
    {
        for (int i=0; i< m_EnemyButtons.Length; ++i)
        {
            CombatUnit unit =m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, i);
            m_EnemyButtons[i].interactable = m_ValidTargets.Contains(unit);
            if (unit != null)
                m_EnemyNames[i].text = unit.UnitName;
        }

        for (int i = 0; i < m_NikkeButtons.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, i);
            m_NikkeButtons[i].interactable = m_ValidTargets.Contains(unit);
            if (unit != null)
                m_NikkeNames[i].text = unit.UnitName;
        }
    }
    private void OnTargetButtonClicked(CombatUnit target)
    {
        if (target == null)
            return;

        if (m_OnTargetSelected == null) return;

        m_OnTargetSelected(target);
        Hide();
    }
    private void OnCancelButtonClicked() 
    {
        if (m_OnCancel == null)
            return;  
        m_OnCancel();
        Hide();
    }

    private void OnBattleStarted(BattleStartedEvent e)
    {
        for (int i = 0; i < m_NikkeButtons.Length; ++i)
        {
            m_NikkeButtons[i].gameObject.SetActive(i < e.Nikkes.Count);
            if (i < e.Nikkes.Count)
                m_NikkeNames[i].text = e.Nikkes[i].UnitName;
        }

        for (int i = 0; i < m_EnemyButtons.Length; ++i)
        {
            m_EnemyButtons[i].gameObject.SetActive(i < e.Enemies.Count);
            if (i < e.Enemies.Count)
                m_EnemyNames[i].text = e.Enemies[i].UnitName;
        }
    }
    private void OnUnitMoved(UnitMovedEvent e)
    {
        if (!gameObject.activeSelf)
            return;
        for (int i = 0; i < m_NikkeButtons.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, i);
            if (unit != null) m_NikkeNames[i].text = unit.UnitName;
        }
    }

    private void OnButtonHoverEnter(CombatUnitType type, int index)
    {
        if (m_ValidTargets == null)
            return;
        CombatUnit hovered = m_CombatStateMachine.PositionSystem.GetUnit(type, index);
        if (hovered == null || !m_ValidTargets.Contains(hovered))
            return;

        List<CombatUnit> brightTargets = ResolvePreviewTargets(hovered);

        // 유효 타겟은 전부 dim, brightTarget만 bright
        for (int i = 0; i < m_ValidTargets.Count; ++i)
            SetHighlightColor(m_ValidTargets[i], HIGHLIGHT_DIM);
        for (int i = 0; i < brightTargets.Count; ++i)
            SetHighlightColor(brightTargets[i], HIGHLIGHT_BRIGHT);
    }

    private void OnButtonHoverExit()
    {
        if (m_ValidTargets == null)
            return;
        RefreshHighlights();
    }

    private List<CombatUnit> ResolvePreviewTargets(CombatUnit unit)
    {
        if(m_CurrentSkill.TargetType == TargetType.EnemySingle ||
            m_CurrentSkill.TargetType == TargetType.AllySingle)
        {
            List<CombatUnit> single = new List<CombatUnit>();
            single.Add(unit);
            return single;
        }
        return m_ValidTargets;
    }

    private void RefreshHighlights()
    {
        HideAllHighlights();
        for (int i=0; i<m_ValidTargets.Count; ++i)
            SetHighlightColor(m_ValidTargets[i], HIGHLIGHT_DIM);
    }

    private void SetHighlightColor(CombatUnit unit, Color color)
    {
        Image[] highlights = (unit.UnitType == CombatUnitType.Enemy) ? m_EnemyHighlights : m_NikkeHighlights;

        int slot = unit.SlotIndex;
        if (slot < highlights.Length)
        {
            highlights[slot].gameObject.SetActive(true);
            highlights[slot].color = color;
        }
    }

    private void HideAllHighlights()
    {
        for (int i=0; i< m_EnemyHighlights.Length; ++i)
            m_EnemyHighlights[i].gameObject.SetActive(false);
        for (int i=0; i< m_NikkeHighlights.Length; ++i)
            m_NikkeHighlights[i].gameObject.SetActive(false);
    }

}

