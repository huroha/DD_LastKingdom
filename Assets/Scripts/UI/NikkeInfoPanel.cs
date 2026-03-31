using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;


public class NikkeInfoPanel : MonoBehaviour
{
    private CombatUnit m_CurrentUnit;
    [Header("References")]
    [SerializeField] private CombatStateMachine m_CombatStateMachine;

    [Header("Portrait & Identity")]
    [SerializeField] private Image m_Portrait;
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private TextMeshProUGUI m_ClassText;

    [Header("Skills")]
    [SerializeField] private Image[] m_SkillIcons;      // 크기 4

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI m_HpText;
    [SerializeField] private TextMeshProUGUI m_EblaText;


    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI m_AccText;
    [SerializeField] private TextMeshProUGUI m_CritText;
    [SerializeField] private TextMeshProUGUI m_DmgText;
    [SerializeField] private TextMeshProUGUI m_DodgeText;
    [SerializeField] private TextMeshProUGUI m_ProtText;
    [SerializeField] private TextMeshProUGUI m_SpeedText;

    private static readonly Color COLOR_NORMAL = new Color(0.8f, 0.8f, 0.8f, 1f);
    private static readonly Color COLOR_BUFF = new Color(0.8f, 0.76f, 0.56f, 1f);
    private static readonly Color COLOR_DEBUFF = new Color(0.75f, 0.1f, 0.1f, 1f);
    private static readonly Color COLOR_SKILL_DISABLED = new Color(0.4f, 0.4f, 0.4f, 1f);

    private StringBuilder m_Sb = new StringBuilder(64);



    public void Show(CombatUnit unit)
    {
        m_CurrentUnit = unit;
        NikkeData data = unit.NikkeData;
        StatBlock stats = unit.CurrentStats;

        //Portrait & Identity
        m_Portrait.sprite = data.PortraitSprite;
        m_NameText.text = data.NikkeName;
        m_Sb.Clear();
        m_Sb.Append(GetManufacturerDisplayName(data.Manufacturer)).Append("  ").Append(GetClassDisplayName(data.NikkeClass));
        m_ClassText.SetText(m_Sb);
        // Skill Icon
        for (int i=0; i< m_SkillIcons.Length; ++i)
        {

            bool hasSkill = i < unit.Skills.Count && unit.Skills[i] != null;
            m_SkillIcons[i].gameObject.SetActive(hasSkill);
            if (hasSkill)
            {
                m_SkillIcons[i].sprite = unit.Skills[i].SkillIcon;
                bool isValid =(m_CombatStateMachine != null) && m_CombatStateMachine.ValidateSkill(unit, unit.Skills[i]);
                m_SkillIcons[i].color = isValid ? Color.white : COLOR_SKILL_DISABLED;
            }
        }

        // hp & Ebla
        m_HpText.SetText("{0} / {1}", unit.CurrentHp, unit.MaxHp);
        m_EblaText.SetText("{0} / {1}", unit.Ebla, CombatUnit.MaxEbla);

        //Stats
        StatBlock baseStats = unit.BaseStats;

        m_AccText.SetText("{0}", stats.accuracyMod);
        m_AccText.color = GetStatColor(stats.accuracyMod, baseStats.accuracyMod);

        m_CritText.SetText("{0}%", stats.critChance);
        m_CritText.color = GetStatColor(stats.critChance, baseStats.critChance);

        float dmgMul = 1f + stats.damageMultiplier / 100f;
        int displayMin = Mathf.Max((int)(stats.minDamage * dmgMul), 0);
        int displayMax = Mathf.Max((int)(stats.maxDamage * dmgMul), 0);
        m_DmgText.SetText("{0} - {1}", displayMin, displayMax);
        m_DmgText.color = GetStatColor(displayMin + displayMax, baseStats.minDamage + baseStats.maxDamage);

        m_DodgeText.SetText("{0}", stats.dodge);
        m_DodgeText.color = GetStatColor(stats.dodge, baseStats.dodge);

        m_ProtText.SetText("{0}%", stats.defense);
        m_ProtText.color = GetStatColor(stats.defense, baseStats.defense);

        m_SpeedText.SetText("{0}", stats.speed);
        m_SpeedText.color = GetStatColor(stats.speed, baseStats.speed);


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
        m_HpText.SetText("{0} / {1}", m_CurrentUnit.CurrentHp, m_CurrentUnit.MaxHp);
        m_EblaText.SetText("{0} / 200", m_CurrentUnit.Ebla);
    }

    private static string GetClassDisplayName(NikkeClass nikkeClass)
    {
        switch (nikkeClass)
        {
            case NikkeClass.Attacker: return "공격형";
            case NikkeClass.Supporter: return "지원형";
            case NikkeClass.Defender: return "방어형";
            default: return nikkeClass.ToString();
        }
    }

    private static string GetManufacturerDisplayName(Manufacturer manufacturer)
    {
        switch (manufacturer)
        {
            case Manufacturer.Pilgrim: return "필그림";
            case Manufacturer.Elysion: return "엘리시온";
            case Manufacturer.Missilis: return "미실리스";
            case Manufacturer.Tetra: return "테트라";
            case Manufacturer.Abnormal: return "어브노멀";
            default: return manufacturer.ToString();
        }
    }

    private static Color GetStatColor(float current, float baseValue)
    {
        if (current > baseValue)
            return COLOR_BUFF;
        if (current < baseValue)
            return COLOR_DEBUFF;
        return COLOR_NORMAL;
    }

}
