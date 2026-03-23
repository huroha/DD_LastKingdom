using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;



public class SkillSelectPanel : MonoBehaviour
{
    private readonly Key[] m_SkillKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
    private int m_PendingSkillIndex = -1;

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
        }
        m_PassButton.onClick.AddListener(OnPassButtonClicked);
        m_MoveButton.onClick.AddListener(OnMoveButtonClicked);
    }
    private void Update()
    {

        for (int i = 0; i < m_SkillKeys.Length; ++i)
        {
            if (Keyboard.current[m_SkillKeys[i]].wasPressedThisFrame)
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
        gameObject.SetActive(true);

        if(m_PendingSkillIndex >= 0)
        {
            int pending = m_PendingSkillIndex;
            m_PendingSkillIndex = -1;
            if(pending < m_SkillButtons.Length && m_SkillButtons[pending].interactable)
                OnSkillButtonClicked(pending);
        }
        
    }
    public void Hide() 
    { 
        gameObject.SetActive(false);
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
        SkillData skill = m_CurrentUnit.Skills[index];
        m_OnSkillSelected(skill);
        m_SkillSelectIcon.transform.position = m_SkillIconTransforms[index].position;
        m_SkillSelectIcon.SetActive(true);
        Hide();
    }
    private void OnPassButtonClicked()
    {
        m_OnPass();
        Hide();
    }
    private void OnMoveButtonClicked()
    {
        m_OnMove();
        Hide();
    }
    public void SetPendingSkill(int index)
    {
        m_PendingSkillIndex = index;
    }



}
