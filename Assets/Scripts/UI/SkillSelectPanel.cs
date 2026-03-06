using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class SkillSelectPanel : MonoBehaviour
{
    [Header("Skill Buttons")]
    [SerializeField] private Button[] m_SkillButtons;   //4개
    [SerializeField] private TextMeshProUGUI[] m_SkillNames; //4개

    [Header("Pass Button")]
    [SerializeField] private Button m_PassButton;

    [Header("Move Button")]
    [SerializeField] private Button m_MoveButton;

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

    public void Show(CombatUnit unit, SkillSelectedHandler onSkillSelected, PassHandler onPass, MoveHandler onMove)
    {
        m_CurrentUnit = unit;
        m_OnSkillSelected = onSkillSelected;
        m_OnPass = onPass;
        m_OnMove = onMove;
        RefreshButtons();
        gameObject.SetActive(true);
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

}
