using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;



public class SkillSelectPanel : MonoBehaviour
{
    public static readonly Key[] SkillKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };

    private int m_PendingSkillIndex = -1;
    private bool m_PendingPass = false;
    private bool m_PendingMove = false;

    [Header("Skill Buttons")]
    [SerializeField] private Button[] m_SkillButtons;   //4개
    [SerializeField] private TextMeshProUGUI[] m_SkillNames; //4개

    [Header("Pass Button")]
    [SerializeField] private Button m_PassButton;

    [Header("Move Button")]
    [SerializeField] private Button m_MoveButton;

    [Header("Skill Select Icon")]
    [SerializeField] private GameObject m_SkillSelectIcon;
    [SerializeField] private RectTransform[] m_SkillIconTransforms; // select 참조용

    [Header("References")]
    [SerializeField] private CombatStateMachine m_CombatStateMachine;
    [SerializeField] private TargetSelectPanel m_TargetSelectPanel;
    [SerializeField] private SkillTooltip m_SkillTooltip;

    [Header("Tooltip Offset")]
    [SerializeField] private Vector2 m_TooltipOffset;

    [SerializeField] private RectTransform m_SkillSelectIconRect;
    [SerializeField] private float m_PopInDuration = 0.15f;
    private int m_SelectedSkillIndex = -1;

    public delegate void SkillSelectedHandler(SkillData skill);
    public delegate void PassHandler();
    public delegate void MoveHandler();

    private SkillSelectedHandler m_OnSkillSelected;
    private PassHandler m_OnPass;
    private MoveHandler m_OnMove;

    private CombatUnit m_CurrentUnit;

    private void Awake()
    {
        for (int i = 0; i < m_SkillButtons.Length; ++i)
        {
            int index = i; // 루프 변수를 별도 변수에 캡처
            m_SkillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(index));

            EventTrigger trigger = m_SkillButtons[i].gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry enter = new EventTrigger.Entry();
            enter.eventID = EventTriggerType.PointerEnter;
            enter.callback.AddListener(_ => OnSkillHoverEnter(index));
            trigger.triggers.Add(enter);

            EventTrigger.Entry exit = new EventTrigger.Entry();
            exit.eventID = EventTriggerType.PointerExit;
            exit.callback.AddListener(_ => OnSkillHoverExit());
            trigger.triggers.Add(exit);

        }
        m_PassButton.onClick.AddListener(OnPassButtonClicked);
        m_MoveButton.onClick.AddListener(OnMoveButtonClicked);
    }
    private void Update()
    {
        if (m_TargetSelectPanel != null && m_TargetSelectPanel.gameObject.activeSelf)
            return;
        for (int i = 0; i < SkillKeys.Length; ++i)
        {
            if (Keyboard.current[SkillKeys[i]].wasPressedThisFrame)
            {
                if (i < m_SkillButtons.Length && m_SkillButtons[i].interactable)
                    OnSkillButtonClicked(i);
            }
        }
        if (Keyboard.current[Key.Digit5].wasPressedThisFrame)
            if (m_MoveButton.interactable)
                OnMoveButtonClicked();
    }

    public void Show(CombatUnit unit, SkillSelectedHandler onSkillSelected, PassHandler onPass, MoveHandler onMove)
    {
        m_CurrentUnit = unit;
        m_OnSkillSelected = onSkillSelected;
        m_OnPass = onPass;
        m_OnMove = onMove;
        RefreshButtons();
        m_SkillSelectIcon.SetActive(false);
        m_SelectedSkillIndex = -1;
        m_PassButton.gameObject.SetActive(true);
        m_MoveButton.gameObject.SetActive(true);
        gameObject.SetActive(true);

        if (m_PendingPass)
        {
            m_PendingPass = false;
            OnPassButtonClicked();
        }
        else if (m_PendingMove)
        {
            m_PendingMove = false;
            OnMoveButtonClicked();
        }
        else if (m_PendingSkillIndex >= 0)
        {
            int pending = m_PendingSkillIndex;
            m_PendingSkillIndex = -1;
            if (pending < m_SkillButtons.Length && m_SkillButtons[pending].interactable)
                OnSkillButtonClicked(pending);

        }
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ShowSkillsOnly()
    {
        gameObject.SetActive(true);
    }
    private void RefreshButtons()
    {
        for (int i = 0; i < m_SkillButtons.Length; ++i)
        {
            SkillData skill = (i < m_CurrentUnit.Skills.Count) ? m_CurrentUnit.Skills[i] : null;
            if (skill == null)
            {
                m_SkillButtons[i].interactable = false;
                m_SkillNames[i].text = "";
                continue;
            }
            bool isValid = m_CombatStateMachine.ValidateSkill(m_CurrentUnit, skill);
            m_SkillButtons[i].interactable = isValid;
            m_SkillNames[i].text = skill.SkillName;
        }
    }
    private void OnSkillButtonClicked(int index)
    {
        if (m_TargetSelectPanel != null && m_TargetSelectPanel.gameObject.activeSelf)
        {
            SetPendingSkill(index);
            m_TargetSelectPanel.TriggerCancel();
            return;
        }
        SkillData skill = m_CurrentUnit.Skills[index];
        m_OnSkillSelected(skill);
        if (m_SelectedSkillIndex >= 0 && m_SelectedSkillIndex != index)
            StartCoroutine(PopIn(m_SkillIconTransforms[m_SelectedSkillIndex]));

        // 새 선택 스킬 팝인
        m_CombatStateMachine.StartCoroutine(PopIn(m_SkillIconTransforms[index]));

        // 셀렉트 아이콘 이동 + 팝인
        m_SkillSelectIconRect.position = m_SkillIconTransforms[index].position;
        m_SkillSelectIcon.SetActive(true);
        m_CombatStateMachine.StartCoroutine(PopIn(m_SkillSelectIconRect));

        m_SelectedSkillIndex = index;
        Hide();
    }
    private void OnPassButtonClicked()
    {
        if (m_TargetSelectPanel != null && m_TargetSelectPanel.gameObject.activeSelf)
        {
            m_PendingPass = true;
            m_TargetSelectPanel.TriggerCancel();
            return;
        }
        m_OnPass();
        Hide();
    }
    private void OnMoveButtonClicked()
    {
        if (m_TargetSelectPanel != null && m_TargetSelectPanel.gameObject.activeSelf)
        {
            m_PendingMove = true;
            m_TargetSelectPanel.TriggerCancel();
            return;
        }
        m_OnMove();
        Hide();
    }
    public void SetPendingSkill(int index)
    {
        m_PendingSkillIndex = index;
    }
    private void OnSkillHoverEnter(int index)
    {
        if (m_CurrentUnit == null)
            return;
        if (index >= m_CurrentUnit.Skills.Count || m_CurrentUnit.Skills[index] == null)
            return;

        Vector2 screenPos = m_SkillButtons[index].transform.position;
        m_SkillTooltip.Show(m_CurrentUnit.Skills[index], screenPos, m_TooltipOffset);
    }

    private void OnSkillHoverExit()
    {
        m_SkillTooltip.Hide();
    }

    private IEnumerator PopIn(RectTransform rt)
    {
        float elapsed = 0f;
        while (elapsed < m_PopInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / m_PopInDuration;
            float scale = t < 0.7f ? Mathf.Lerp(1f, 1.15f, t / 0.7f) : Mathf.Lerp(1.15f, 1f, (t - 0.7f) / 0.3f);
            rt.localScale = Vector3.one * scale;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    public void SetPendingMove()
    {
        m_PendingMove = true;
    }
}
