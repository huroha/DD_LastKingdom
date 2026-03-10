using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class NikkeInfoPanel : MonoBehaviour
{
    private CombatUnit m_CurrentUnit;

    [Header("Portrait & Identity")]
    [SerializeField] private Image m_Portrait;
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private TextMeshProUGUI m_ClassText;

    [Header("Skills")]
    [SerializeField] private Image[] m_SkillIcons;      // Å©±â 4

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI m_HpText;
    [SerializeField] private TextMeshProUGUI m_EblaText;


    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI m_AccText;
    [SerializeField] private TextMeshProUGUI m_CritText;
    [SerializeField] private TextMeshProUGUI m_DmgText;
    [SerializeField] private TextMeshProUGUI m_DodgeText;
    [SerializeField] private TextMeshProUGUI m_ProtText;




    public void Show(CombatUnit unit)
    {
        m_CurrentUnit = unit;
        NikkeData data = unit.NikkeData;
        StatBlock stats = unit.CurrentStats;

        //Portrait & Identity
        m_Portrait.sprite = data.PortraitSprite;
        m_NameText.text = data.NikkeName;
        m_ClassText.text = data.NikkeClass.ToString();

        // Skill Icon
        for (int i=0; i< m_SkillIcons.Length; ++i)
        {
            bool hasSkill = i < unit.Skills.Count && unit.Skills[i] != null;
            m_SkillIcons[i].gameObject.SetActive(hasSkill);
            if (hasSkill)
                m_SkillIcons[i].sprite = unit.Skills[i].SkillIcon;
        }

        // hp & Ebla
        m_HpText.text = $"{unit.CurrentHp} / {unit.MaxHp}";
        m_EblaText.text = $"{unit.Ebla} / 200";

        //Stats
        m_AccText.text = stats.accuracyMod.ToString();
        m_CritText.text = $"{stats.critChance:F1}%";
        m_DmgText.text = $"{stats.minDamage} - {stats.maxDamage}";
        m_DodgeText.text = stats.dodge.ToString();
        m_ProtText.text = $"{stats.defense:F0}%";


    }

    private void OnEnable()
    {
        EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
        EventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
        EventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
    }

    private void OnTurnStarted(TurnStartedEvent e)
    {
        if (e.Unit.UnitType == CombatUnitType.Nikke)
            Show(e.Unit);
    }

    private void OnTurnEnded(TurnEndedEvent e)
    {
        if (m_CurrentUnit == null) return;
        m_HpText.text = $"{m_CurrentUnit.CurrentHp} / {m_CurrentUnit.MaxHp}";
        m_EblaText.text = $"{m_CurrentUnit.Ebla} / 200";
    }
}
