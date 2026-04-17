using UnityEngine;
using TMPro;

public class EnemyInfoPanel : MonoBehaviour
{
    [Header("Basic Section")]
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private TextMeshProUGUI m_HpText;
    [SerializeField] private TextMeshProUGUI m_TypeText;
    [SerializeField] private TextMeshProUGUI m_ElementText;
    [SerializeField] private TextMeshProUGUI m_ProtText;
    [SerializeField] private TextMeshProUGUI m_SpeedText;
    [SerializeField] private TextMeshProUGUI m_DodgeText;
    [SerializeField] private TextMeshProUGUI m_ResistanceText;
    [SerializeField] private TextMeshProUGUI m_SkillListText;
    [SerializeField] private GameObject m_AliveOnly;

    [Header("Preview Section")]
    [SerializeField] private GameObject m_PreviewRoot;
    [SerializeField] private TextMeshProUGUI m_HitChanceText;
    [SerializeField] private TextMeshProUGUI m_CritChanceText;
    [SerializeField] private TextMeshProUGUI m_DamageRangeText;

    private const string CORPSE = "시체";

    private static readonly Color COLOR_NORMAL = new Color(0.8f, 0.8f, 0.8f, 1f);
    private static readonly Color COLOR_BUFF = new Color(0.8f, 0.76f, 0.56f, 1f);
    private static readonly Color COLOR_DEBUFF = new Color(0.75f, 0.1f, 0.1f, 1f);

    // Resistance 표기용
    private System.Text.StringBuilder m_Sb = new System.Text.StringBuilder(128);

    public void Populate(CombatUnit unit)
    {
        bool isCorpse = unit.State == UnitState.Corpse;

        m_Sb.Clear();

        if (isCorpse)
        {
            m_NameText.text = CORPSE;
            m_HpText.SetText("{0} / {1}", unit.CurrentHp, unit.EnemyData.CorpseHp);
            m_TypeText.text = CORPSE;
            m_ProtText.SetText("{0}%", 0);
            m_DodgeText.SetText("{0}", 0);
            m_SpeedText.text = "x";
        }
        else
        {
            StatBlock baseStats = unit.BaseStats;
            m_NameText.text = unit.UnitName;
            m_HpText.SetText("{0} / {1}", unit.CurrentHp, unit.MaxHp);
            m_TypeText.text = unit.EnemyData.EnemyType.ToString();
            m_ElementText.text = unit.EnemyData.Element.ToString();
            m_ProtText.SetText("{0}%", unit.CurrentStats.defense);
            m_ProtText.color = GetStatColor(unit.CurrentStats.defense, baseStats.defense);
            m_SpeedText.SetText("{0}", unit.CurrentStats.speed);
            m_SpeedText.color = GetStatColor(unit.CurrentStats.speed, baseStats.speed);
            m_DodgeText.SetText("{0}", unit.CurrentStats.dodge);
            m_DodgeText.color = GetStatColor(unit.CurrentStats.dodge, baseStats.dodge);
            ResistanceBlock res = unit.CurrentStats.resistance;
            m_Sb.Append(Mathf.RoundToInt(res.stun)).Append('%').AppendLine();
            m_Sb.Append(Mathf.RoundToInt(res.poison)).Append('%').AppendLine();
            m_Sb.Append(Mathf.RoundToInt(res.bleed)).Append('%').AppendLine();
            m_Sb.Append(Mathf.RoundToInt(res.debuff)).Append('%').AppendLine();
            m_Sb.Append(Mathf.RoundToInt(res.move)).Append('%');
            m_ResistanceText.SetText(m_Sb);
            m_Sb.Clear();
            for (int i = 0; i < unit.EnemyData.Skills.Count; ++i)
                m_Sb.AppendLine(unit.EnemyData.Skills[i].SkillName);
            m_SkillListText.SetText(m_Sb);
        }
        m_AliveOnly.SetActive(!isCorpse);
    }

    public void PopulatePreview(AttackPreview preview)
    {
        m_HitChanceText.SetText("{0}%", preview.HitChance);
        m_CritChanceText.SetText("{0}%", preview.CritChance);
        m_DamageRangeText.SetText("{0} - {1}", preview.MinDamage, preview.MaxDamage);
    }

    public void ShowPreviewSection()
    {
        m_PreviewRoot.SetActive(true);
    }
    public void HidePreviewSection()
    {
        m_PreviewRoot.SetActive(false);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    private static Color GetStatColor(float current, float baseValue)
    {
        if (current > baseValue) return COLOR_BUFF;
        if (current < baseValue) return COLOR_DEBUFF;
        return COLOR_NORMAL;
    }

}
