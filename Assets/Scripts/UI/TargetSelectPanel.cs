using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;


public class TargetSelectPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatStateMachine m_CombatStateMachine;
    [SerializeField] private CombatHUD m_CombatHUD;

    [Header("Target Buttons")]
    [SerializeField] private Button[]       m_EnemyButtons;   // 4개
    [SerializeField] private Button[]       m_NikkeButtons;   // 4개
    [SerializeField] private Button[]       m_LargeEnemyButtons;


    [Header("Unit Names")]
    [SerializeField] private TextMeshProUGUI[] m_EnemyNames;
    [SerializeField] private TextMeshProUGUI[] m_NikkeNames;

    [Header("Highlights")]
    [SerializeField] private Image[] m_EnemyHighlights; // 4개
    [SerializeField] private Image[] m_NikkeHighlights;
    [SerializeField] private Image[] m_LargeEnemyHighlights;

    [Header("Cancel Button")]
    [SerializeField] private Button m_CancelButton;

    [SerializeField] private SkillSelectPanel m_SkillSelectPanel;
    private readonly Key[] m_SkillKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };

    private CombatUnit m_HoveredUnit = null;

    public delegate void TargetSelectedHandler(CombatUnit target);
    public delegate void CancelHandler();


    private TargetSelectedHandler m_OnTargetSelected;
    private CancelHandler m_OnCancel;

    private List<CombatUnit> m_ValidTargets;
    private List<CombatUnit> m_PreviewTargets = new List<CombatUnit>();

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

        for (int i = 0; i < m_LargeEnemyButtons.Length; ++i)
        {
            int index = i;
            m_LargeEnemyButtons[i].onClick.AddListener(() =>
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

        for (int i = 0; i < m_LargeEnemyButtons.Length; ++i)
        {
            int index = i;
            UnityEngine.EventSystems.EventTrigger trigger =
                m_LargeEnemyButtons[i].gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            UnityEngine.EventSystems.EventTrigger.Entry enter = new UnityEngine.EventSystems.EventTrigger.Entry();
            enter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            enter.callback.AddListener(_ => OnButtonHoverEnter(CombatUnitType.Enemy, index));
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

    private void Update()
    {
        for (int i = 0; i < m_SkillKeys.Length; ++i)
        {
            if (Keyboard.current[m_SkillKeys[i]].wasPressedThisFrame)
            {
                m_SkillSelectPanel.SetPendingSkill(i);
                OnCancelButtonClicked();
                return;
            }
        }
        if (Keyboard.current[Key.Escape].wasPressedThisFrame)
            OnCancelButtonClicked();
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
        if (m_HoveredUnit != null && m_ValidTargets.Contains(m_HoveredUnit))
            OnButtonHoverEnter(m_HoveredUnit.UnitType, m_HoveredUnit.SlotIndex);
        gameObject.SetActive(true);
        m_CombatHUD.RefreshHoveredPreview();

    }
    public void Hide() 
    {
        m_ValidTargets = null;
        HideAllHighlights();
        for (int i = 0; i < m_NikkeButtons.Length; ++i)
            m_NikkeButtons[i].interactable = false;
        for (int i = 0; i < m_EnemyButtons.Length; ++i)
            m_EnemyButtons[i].interactable = false;
        for (int i = 0; i < m_LargeEnemyButtons.Length; ++i)
            m_LargeEnemyButtons[i].interactable = false;

        gameObject.SetActive(false);
    }

    private void RefreshButtons()
    {
        for (int i = 0; i < m_EnemyButtons.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, i);
            if (unit == null || unit.SlotSize == 2)
            {
                m_EnemyButtons[i].gameObject.SetActive(false);
                continue;
            }
            m_EnemyButtons[i].gameObject.SetActive(true);
            m_EnemyButtons[i].interactable = m_ValidTargets.Contains(unit);
            m_EnemyNames[i].text = unit.UnitName;
        }

        for (int i = 0; i < m_LargeEnemyButtons.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, i);
            bool isAnchor = unit != null && unit.SlotSize == 2 && unit.SlotIndex == i;
            m_LargeEnemyButtons[i].gameObject.SetActive(isAnchor);
            if (isAnchor)
                m_LargeEnemyButtons[i].interactable = m_ValidTargets.Contains(unit);
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

        Hide();
        m_OnTargetSelected(target);
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
            if (i < e.Enemies.Count && e.Enemies[i].SlotSize == 1)
                m_EnemyNames[i].text = e.Enemies[i].UnitName;
        }

        for (int i = 0; i < m_LargeEnemyButtons.Length; ++i)
            m_LargeEnemyButtons[i].gameObject.SetActive(false);

        for (int i = 0; i < e.Enemies.Count; ++i)
        {
            CombatUnit enemy = e.Enemies[i];
            if (enemy.SlotSize == 2)
            {
                int largeIndex = enemy.SlotIndex;
                for (int s = enemy.SlotIndex; s < enemy.SlotIndex + enemy.SlotSize; ++s)
                    m_EnemyButtons[s].gameObject.SetActive(false);
                m_LargeEnemyButtons[largeIndex].gameObject.SetActive(true);
            }
        }
    }
    private void OnUnitMoved(UnitMovedEvent e)
    {
        if (!gameObject.activeSelf)
            return;
        RefreshButtons();
        RefreshHighlights();
    }

    private void OnButtonHoverEnter(CombatUnitType type, int index)
    {
        
        if (m_ValidTargets == null)
            return;
        CombatUnit hovered = m_CombatStateMachine.PositionSystem.GetUnit(type, index);
        if (hovered == null || !m_ValidTargets.Contains(hovered))
            return;
        m_HoveredUnit = hovered;

        List<CombatUnit> brightTargets = ResolvePreviewTargets(hovered);

        // 유효 타겟은 전부 dim, brightTarget만 bright
        for (int i = 0; i < m_ValidTargets.Count; ++i)
            SetHighlightColor(m_ValidTargets[i], HIGHLIGHT_DIM);
        for (int i = 0; i < brightTargets.Count; ++i)
            SetHighlightColor(brightTargets[i], HIGHLIGHT_BRIGHT);
    }

    private void OnButtonHoverExit()
    {
        m_HoveredUnit = null;
        if (m_ValidTargets == null)
            return;
        RefreshHighlights();
    }

    private List<CombatUnit> ResolvePreviewTargets(CombatUnit unit)
    {
        if (m_CurrentSkill == null)
        {
            m_PreviewTargets.Clear();
            m_PreviewTargets.Add(unit);
            return m_PreviewTargets;
        }

        if (m_CurrentSkill.TargetType == TargetType.EnemySingle ||
            m_CurrentSkill.TargetType == TargetType.AllySingle)
        {
            m_PreviewTargets.Clear();
            m_PreviewTargets.Add(unit);
            return m_PreviewTargets;
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
        if (unit.UnitType == CombatUnitType.Enemy && unit.SlotSize == 2)
        {
            int largeIndex = unit.SlotIndex;
            if (largeIndex < m_LargeEnemyHighlights.Length)
            {
                m_LargeEnemyHighlights[largeIndex].gameObject.SetActive(true);
                m_LargeEnemyHighlights[largeIndex].color = color;
            }
            return;
        }

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
        for (int i = 0; i < m_EnemyHighlights.Length; ++i)
            m_EnemyHighlights[i].gameObject.SetActive(false);
        for (int i = 0; i < m_NikkeHighlights.Length; ++i)
            m_NikkeHighlights[i].gameObject.SetActive(false);
        for (int i = 0; i < m_LargeEnemyHighlights.Length; ++i)
            m_LargeEnemyHighlights[i].gameObject.SetActive(false);
    }


}

