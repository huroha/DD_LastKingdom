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


    [Header("Preview Section")]
    [SerializeField] private GameObject      m_PreviewRoot;
    [SerializeField] private TextMeshProUGUI m_HitChanceText;
    [SerializeField] private TextMeshProUGUI m_CritChanceText;
    [SerializeField] private TextMeshProUGUI m_DamageRangeText;

    // Resistance Ç¥±â¿ë
    private System.Text.StringBuilder m_Sb = new System.Text.StringBuilder(128);

    public void Populate(CombatUnit unit)
    {
        m_Sb.Clear();
        m_NameText.text = unit.UnitName;
        m_HpText.text = $"{unit.CurrentHp} / {unit.MaxHp}";
        m_TypeText.text = unit.EnemyData.EnemyType.ToString();
        m_ElementText.text = unit.EnemyData.Element.ToString();
        m_ProtText.text = $"{unit.CurrentStats.defense:F0}%";
        m_SpeedText.text = unit.CurrentStats.speed.ToString();
        m_DodgeText.text = unit.CurrentStats.dodge.ToString();
        ResistanceBlock res = unit.CurrentStats.resistance;
        m_Sb.AppendLine($"{res.stun:F0}%");
        m_Sb.AppendLine($"{res.poison:F0}%");
        m_Sb.AppendLine($"{res.bleed:F0}%");
        m_Sb.AppendLine($"{res.debuff:F0}%");
        m_Sb.Append($"{res.move:F0}%");
        m_ResistanceText.text = m_Sb.ToString();
        m_Sb.Clear();
        for (int i = 0; i < unit.EnemyData.Skills.Count; ++i)
            m_Sb.AppendLine(unit.EnemyData.Skills[i].SkillName);
        m_SkillListText.text = m_Sb.ToString();
    }

    public void PopulatePreview(AttackPreview preview)
    {
        m_HitChanceText.text = $"{preview.HitChance:F0}%";
        m_CritChanceText.text = $"{preview.CritChance:F0}%";
        m_DamageRangeText.text = $"{preview.MinDamage} - {preview.MaxDamage}";
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
}
