using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;

public class NikkeDetailPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatStateMachine m_CombatStateMachine;

    [Header("Identity")]
    [SerializeField] private Image m_Portrait;
    [SerializeField] private TMP_InputField m_NameInput;
    [SerializeField] private TextMeshProUGUI m_ClassText;

    [Header("Combat Skills")]
    [SerializeField] private Image[] m_SkillIcons;      // 크기 7
    [SerializeField] private GameObject[] m_SkillActiveMarkers;

    [Header("Camp Skills")]
    [SerializeField] private TextMeshProUGUI[] m_CampSkillTexts;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI m_AccText;
    [SerializeField] private TextMeshProUGUI m_CritText;
    [SerializeField] private TextMeshProUGUI m_DmgText;
    [SerializeField] private TextMeshProUGUI m_DodgeText;
    [SerializeField] private TextMeshProUGUI m_ProtText;
    [SerializeField] private TextMeshProUGUI m_SpeedText;

    [Header("Resistance")]
    [SerializeField] private TextMeshProUGUI m_ResistanceText;

    [Header("Equipment")]
    [SerializeField] private Image m_WeaponIcon;
    [SerializeField] private TextMeshProUGUI m_WeaponText;
    [SerializeField] private Image m_ArmorIcon;
    [SerializeField] private TextMeshProUGUI m_ArmorText;
    [SerializeField] private Image[] m_TrinketIcons;        // 크기 2
    [SerializeField] private TextMeshProUGUI[] m_TrinketTexts;

    [Header("Quirks")]
    [SerializeField] private TextMeshProUGUI[] m_QuirkTexts;

    [Header("Diseases")]
    [SerializeField] private TextMeshProUGUI[] m_DiseaseTexts;


    private static readonly Color COLOR_NORMAL = new Color(0.8f, 0.8f, 0.8f, 1f);
    private static readonly Color COLOR_BUFF = new Color(0.8f, 0.76f, 0.56f, 1f);
    private static readonly Color COLOR_DEBUFF = new Color(0.75f, 0.1f, 0.1f, 1f);
    private static readonly Color COLOR_SKILL_ACTIVE = Color.white;
    private static readonly Color COLOR_SKILL_INACTIVE = new Color(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color COLOR_QUIRK_POS = new Color(0.4f, 0.85f, 0.4f, 1f);
    private static readonly Color COLOR_QUIRK_NEG = new Color(0.85f, 0.25f, 0.25f, 1f);

    private StringBuilder m_Sb = new StringBuilder(128);

    public void Show(CombatUnit unit)
    {
        PopulateIdentity(unit);
        PopulateSkills(unit);
        PopulateCampSkills(unit.NikkeData);
        PopulateStats(unit);
        PopulateResistance(unit);
        PopulateEquipment(unit.NikkeInstance);
        PopulateQuirks(unit.NikkeInstance);
        PopulateDiseases(unit.NikkeInstance);
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;
        if (Keyboard.current[Key.Escape].wasPressedThisFrame)
            Hide();
    }
    private void PopulateIdentity(CombatUnit unit)
    {
        NikkeData data = unit.NikkeData;
        NikkeInstance inst = unit.NikkeInstance;
        m_Portrait.sprite = data.PortraitSprite;
        m_NameInput.text = inst.DisplayName;
        m_NameInput.interactable = false;
        m_Sb.Clear();
        m_Sb.Append(GetManufacturerLabel(data.Manufacturer)).Append(" ").Append(GetClassLabel(data.NikkeClass));
        m_ClassText.SetText(m_Sb);
    }
    private static string GetClassLabel(NikkeClass nikkeClass)
    {
        switch(nikkeClass)
        {
            case NikkeClass.Attacker: return "공격형";
            case NikkeClass.Supporter: return "지원형";
            case NikkeClass.Defender: return "방어형";
            default: return nikkeClass.ToString();
        }
    }
    private static string GetManufacturerLabel(Manufacturer manufacturer)
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
    private void PopulateSkills(CombatUnit unit)
    {
        NikkeInstance inst = unit.NikkeInstance;
        IReadOnlyList<SkillData> allSkills = inst.Data.Skills;
        IReadOnlyList<int> activeIndices = inst.ActiveSkillIndices;

        for (int i = 0; i < m_SkillIcons.Length; ++i)
        {
            bool hasSkill = i < allSkills.Count && allSkills[i] != null;
            m_SkillIcons[i].gameObject.SetActive(hasSkill);
            m_SkillActiveMarkers[i].SetActive(false);
            if (!hasSkill) continue;

            m_SkillIcons[i].sprite = allSkills[i].SkillIcon;

            bool isActive = false;
            for (int j = 0; j < activeIndices.Count; ++j)
            {
                if (activeIndices[j] == i) { isActive = true; break; }
            }
            m_SkillIcons[i].color = isActive ? COLOR_SKILL_ACTIVE : COLOR_SKILL_INACTIVE;
            m_SkillActiveMarkers[i].SetActive(isActive);
        }
    }
    private void PopulateCampSkills(NikkeData data)
    {
        IReadOnlyList<CampSkillData> skills = data.CampSkills;
        for (int i = 0; i < m_CampSkillTexts.Length; ++i)
        {
            bool hasSkill = skills != null && i < skills.Count && skills[i] != null;
            m_CampSkillTexts[i].gameObject.SetActive(hasSkill);
            if (!hasSkill) continue;
            m_CampSkillTexts[i].text = skills[i].SkillName;
        }
    }
    private void PopulateStats(CombatUnit unit)
    {
        StatBlock stats = unit.CurrentStats;
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

    private static Color GetStatColor(float current, float baseValue)
    {
        if (current > baseValue) return COLOR_BUFF;
        if (current < baseValue) return COLOR_DEBUFF;
        return COLOR_NORMAL;
    }
    private void PopulateResistance(CombatUnit unit)
    {
        ResistanceBlock res = unit.CurrentStats.resistance;
        m_Sb.Clear();
        m_Sb.Append(Mathf.RoundToInt(res.stun)).Append('%').AppendLine();
        m_Sb.Append(Mathf.RoundToInt(res.poison)).Append('%').AppendLine();
        m_Sb.Append(Mathf.RoundToInt(res.bleed)).Append('%').AppendLine();
        m_Sb.Append(Mathf.RoundToInt(res.debuff)).Append('%').AppendLine();
        m_Sb.Append(Mathf.RoundToInt(res.move)).Append('%');
        m_ResistanceText.SetText(m_Sb);
    }
    private void PopulateEquipment(NikkeInstance inst)
    {
        NikkeData data = inst.Data;

        if (data.Weapon != null)
        {
            m_WeaponIcon.gameObject.SetActive(true);
            m_WeaponIcon.sprite = data.Weapon.GetSprite(inst.WeaponLevel);
            m_Sb.Clear();
            m_Sb.Append(data.Weapon.GearName).Append("  Lv.").Append(inst.WeaponLevel);
            m_WeaponText.SetText(m_Sb);
        }
        else
        {
            m_WeaponIcon.gameObject.SetActive(false);
            m_WeaponText.text = "없음";
        }

        if (data.Armor != null)
        {
            m_ArmorIcon.gameObject.SetActive(true);
            m_ArmorIcon.sprite = data.Armor.GetSprite(inst.ArmorLevel);
            m_Sb.Clear();
            m_Sb.Append(data.Armor.GearName).Append("  Lv.").Append(inst.ArmorLevel);
            m_ArmorText.SetText(m_Sb);
        }
        else
        {
            m_ArmorIcon.gameObject.SetActive(false);
            m_ArmorText.text = "없음";
        }

        for (int i = 0; i < m_TrinketIcons.Length; ++i)
        {
            TrinketData trinket = inst.Trinkets[i];
            if (trinket != null)
            {
                m_TrinketIcons[i].gameObject.SetActive(true);
                m_TrinketIcons[i].sprite = trinket.Icon;
                m_TrinketTexts[i].text = trinket.ItemName;
            }
            else
            {
                m_TrinketIcons[i].gameObject.SetActive(false);
                m_TrinketTexts[i].text = "없음";
            }
        }
    }
    private void PopulateQuirks(NikkeInstance inst)
    {
        IReadOnlyList<QuirkData> quirks = inst.Quirks;
        for (int i = 0; i < m_QuirkTexts.Length; ++i)
        {
            bool has = i < quirks.Count && quirks[i] != null;
            m_QuirkTexts[i].gameObject.SetActive(has);
            if (!has) continue;
            m_QuirkTexts[i].text = quirks[i].QuirkName;
            m_QuirkTexts[i].color = quirks[i].IsPositive ? COLOR_QUIRK_POS : COLOR_QUIRK_NEG;
        }
    }

    private void PopulateDiseases(NikkeInstance inst)
    {
        IReadOnlyList<DiseaseData> diseases = inst.Diseases;
        for (int i = 0; i < m_DiseaseTexts.Length; ++i)
        {
            bool has = i < diseases.Count && diseases[i] != null;
            m_DiseaseTexts[i].gameObject.SetActive(has);
            if (!has) continue;
            m_DiseaseTexts[i].text = diseases[i].DiseaseName;
        }
    }
}
