using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;

public class NikkeDetailPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatStateMachine m_CombatStateMachine;
    [SerializeField] private CombatTooltip m_Tooltip;

    [Header("Identity")]
    [SerializeField] private Image m_Portrait;
    [SerializeField] private TMP_InputField m_NameInput;
    [SerializeField] private TextMeshProUGUI m_ClassText;
    [SerializeField] private TextMeshProUGUI m_RankText;
    [SerializeField] private Image m_StandingIdle;

    [Header("Combat Skills")]
    [SerializeField] private Image[] m_SkillIcons;      // 크기 7
    [SerializeField] private RectTransform[] m_SkillSelectIcons;
    [SerializeField] private TextMeshProUGUI[] m_SkillLevelTexts;

    [Header("Skill Recommendation")]
    [SerializeField] private Image[] m_RecommendPositions;       // 크기 4
    [SerializeField] private Image[] m_RecommendTargets;         // 크기 4
    [SerializeField] private Sprite[] m_RecommendPositionSprites;  // 크기 5 (0~4)
    [SerializeField] private Sprite[] m_RecommendTargetSprites;

    [Header("Camp Skills")]
    [SerializeField] private Image[] m_CampSkillIcons;
    [SerializeField] private RectTransform[] m_CampSkillSelectIcons;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI m_MaxHpText;
    [SerializeField] private TextMeshProUGUI m_AccText;
    [SerializeField] private TextMeshProUGUI m_CritText;
    [SerializeField] private TextMeshProUGUI m_DmgText;
    [SerializeField] private TextMeshProUGUI m_DodgeText;
    [SerializeField] private TextMeshProUGUI m_ProtText;
    [SerializeField] private TextMeshProUGUI m_SpeedText;

    [Header("Resistance")]
    [SerializeField] private TextMeshProUGUI m_ResistanceText1;  // 기절, 중독, 질병, 죽음의일격
    [SerializeField] private TextMeshProUGUI m_ResistanceText2; // 이동, 출혈, 약화, 함정

    [Header("Equipment")]
    [SerializeField] private Image m_WeaponIcon;
    [SerializeField] private Image m_ArmorIcon;
    [SerializeField] private Image[] m_TrinketIcons;        // 크기 2

    [Header("Quirks")]
    [SerializeField] private TextMeshProUGUI[] m_PosQuirkTexts;
    [SerializeField] private TextMeshProUGUI[] m_NegQuirkTexts;

    [Header("Diseases")]
    [SerializeField] private TextMeshProUGUI[] m_DiseaseTexts;


    private static readonly Color COLOR_NORMAL = new Color(0.8f, 0.8f, 0.8f, 1f);
    private static readonly Color COLOR_BUFF = new Color(0.8f, 0.76f, 0.56f, 1f);
    private static readonly Color COLOR_DEBUFF = new Color(0.75f, 0.1f, 0.1f, 1f);
    private static readonly Color COLOR_SKILL_ACTIVE = Color.white;
    private static readonly Color COLOR_SKILL_INACTIVE = new Color(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color COLOR_QUIRK_POS = new Color(0.4f, 0.85f, 0.4f, 1f);
    private static readonly Color COLOR_QUIRK_NEG = new Color(0.85f, 0.25f, 0.25f, 1f);

    private static readonly Vector3 SELECT_ICON_OFFSET = new Vector3(0f, 2f, 0f);

    private readonly int[] m_PosCount = new int[4];
    private readonly int[] m_TgtCount = new int[4];

    private StringBuilder m_Sb = new StringBuilder(128);
    private NikkeInstance m_CurrentInstance;

    private void Awake()
    {
        for (int i = 0; i < m_SkillIcons.Length; ++i)
        {
            int captured = i;
            EnsureButton(m_SkillIcons[i].gameObject)
                .onClick.AddListener(() => OnSkillClicked(captured));
        }
        for (int i = 0; i < m_CampSkillIcons.Length; ++i)
        {
            int captured = i;
            EnsureButton(m_CampSkillIcons[i].gameObject)
                .onClick.AddListener(() => OnCampSkillClicked(captured));
        }
    }
    public void Show(CombatUnit unit)
    {
        m_CurrentInstance = unit.NikkeInstance;
        PopulateIdentity(unit);
        PopulateSkills(unit);
        RefreshRecommendation(unit.NikkeInstance);
        PopulateCampSkills(unit.NikkeInstance);
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
        m_RankText.SetText(GetRankLabel(inst.Level));
    }
    private static string GetClassLabel(NikkeClass nikkeClass)
    {
        switch (nikkeClass)
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
    private static string GetRankLabel(int level)
    {
        switch (level)
        {
            case 0: return "풋내기";
            case 1: return "견습";
            case 2: return "모험가";
            case 3: return "베테랑";
            case 4: return "달인";
            case 5: return "영웅";
            case 6: return "전설";
            default: return level.ToString();
        }
    }
    private void PopulateSkills(CombatUnit unit)
    {
        NikkeInstance inst = unit.NikkeInstance;
        IReadOnlyList<SkillData> allSkills = inst.Data.Skills;

        for (int i = 0; i < m_SkillIcons.Length; ++i)
        {
            bool hasSkill = i < allSkills.Count && allSkills[i] != null;
            m_SkillIcons[i].gameObject.SetActive(hasSkill);
            if (!hasSkill) continue;

            m_SkillIcons[i].sprite = allSkills[i].SkillIcon;
        }
        RefreshSkillUI(inst);
    }
    private void RefreshSkillUI(NikkeInstance inst)
    {
        IReadOnlyList<int> activeIndices = inst.ActiveSkillIndices;
        IReadOnlyList<SkillData> allSkills = inst.Data.Skills;

        int count = Mathf.Min(m_SkillIcons.Length, allSkills.Count);
        for (int i = 0; i < count; ++i)
        {
            if (allSkills[i] == null) continue;

            bool isActive = false;
            for (int j = 0; j < activeIndices.Count; ++j)
            {
                if (activeIndices[j] == i) { isActive = true; break; }
            }
            m_SkillIcons[i].color = isActive ? COLOR_SKILL_ACTIVE : COLOR_SKILL_INACTIVE;
            m_SkillLevelTexts[i].SetText("{0}", inst.SkillLevels[i]);
        }
        UpdateSelectIconPositions(activeIndices, m_SkillIcons, m_SkillSelectIcons);
    }
    private void RefreshRecommendation(NikkeInstance inst)
    {
        Array.Clear(m_PosCount, 0, m_PosCount.Length);
        Array.Clear(m_TgtCount, 0, m_TgtCount.Length);

        IReadOnlyList<int> activeIndices = inst.ActiveSkillIndices;
        IReadOnlyList<SkillData> allSkills = inst.Data.Skills;

        for (int i = 0; i < activeIndices.Count; ++i)
        {
            int idx = activeIndices[i];
            if (idx < 0 || idx >= allSkills.Count || allSkills[idx] == null) continue;

            SkillData skill = allSkills[idx];
            for (int p = 0; p < 4; ++p)
            {
                if (skill.UsablePositions[p]) ++m_PosCount[p];
                if (skill.TargetPositions[p]) ++m_TgtCount[p];
            }
        }

        for (int p = 0; p < 4; ++p)
        {
            m_RecommendPositions[p].sprite = m_RecommendPositionSprites[m_PosCount[p]];
            m_RecommendTargets[p].sprite = m_RecommendTargetSprites[m_TgtCount[p]];
        }
    }
    private void PopulateCampSkills(NikkeInstance inst)
    {
        IReadOnlyList<CampSkillData> allSkills = inst.Data.CampSkills;

        for (int i = 0; i < m_CampSkillIcons.Length; ++i)
        {
            bool hasSkill = allSkills != null && i < allSkills.Count && allSkills[i] != null;
            m_CampSkillIcons[i].gameObject.SetActive(hasSkill);
            if (!hasSkill) continue;

            m_CampSkillIcons[i].sprite = allSkills[i].Icon;
        }
        RefreshCampSkillUI(inst);
    }
    private void RefreshCampSkillUI(NikkeInstance inst)
    {
        IReadOnlyList<int> activeIndices = inst.ActiveCampSkillIndices;
        IReadOnlyList<CampSkillData> allSkills = inst.Data.CampSkills;

        for (int i = 0; i < m_CampSkillIcons.Length; ++i)
        {
            if (allSkills[i] == null) continue;

            bool isActive = false;
            for (int j = 0; j < activeIndices.Count; ++j)
            {
                if (activeIndices[j] == i) { isActive = true; break; }
            }
            m_CampSkillIcons[i].color = isActive ? COLOR_SKILL_ACTIVE : COLOR_SKILL_INACTIVE;
        }
        UpdateSelectIconPositions(activeIndices, m_CampSkillIcons, m_CampSkillSelectIcons);
    }
    private static int FindToggleSlot(IReadOnlyList<int> activeIndices, int skillIdx, out bool deactivate)
    {
        for (int i = 0; i < activeIndices.Count; ++i)
        {
            if (activeIndices[i] == skillIdx) { deactivate = true; return i; }
        }
        for (int i = 0; i < activeIndices.Count; ++i)
        {
            if (activeIndices[i] == -1) { deactivate = false; return i; }
        }
        deactivate = false;
        return -1;
    }
    private void OnSkillClicked(int skillIdx)
    {
        bool deactivate;
        int slot = FindToggleSlot(m_CurrentInstance.ActiveSkillIndices, skillIdx, out deactivate);
        if (slot < 0) return;
        m_CurrentInstance.SetSkillIndex(slot, deactivate ? -1 : skillIdx);
        RefreshSkillUI(m_CurrentInstance);
        RefreshRecommendation(m_CurrentInstance);
    }

    private void OnCampSkillClicked(int skillIdx)
    {
        bool deactivate;
        int slot = FindToggleSlot(m_CurrentInstance.ActiveCampSkillIndices, skillIdx, out deactivate);
        if (slot < 0) return;
        m_CurrentInstance.SetCampSkillIndex(slot, deactivate ? -1 : skillIdx);
        RefreshCampSkillUI(m_CurrentInstance);
    }
    private static Button EnsureButton(GameObject go)
    {
        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        return btn;
    }
    private void PopulateStats(CombatUnit unit)
    {
        StatBlock stats = unit.CurrentStats;
        StatBlock baseStats = unit.BaseStats;

        m_MaxHpText.SetText("{0}", stats.maxHp);
        m_MaxHpText.color = GetStatColor(stats.maxHp, baseStats.maxHp);

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
        StatBlock stats = unit.CurrentStats;

        m_Sb.Clear();
        m_Sb.Append(Mathf.RoundToInt(res.stun)).Append('%').AppendLine();
        m_Sb.Append(Mathf.RoundToInt(res.poison)).Append('%').AppendLine();
        m_Sb.Append(Mathf.RoundToInt(res.disease)).Append('%').AppendLine();
        m_Sb.Append(Mathf.RoundToInt(stats.deathBlowResist)).Append('%');
        m_ResistanceText1.SetText(m_Sb);

        m_Sb.Clear();
        m_Sb.Append(Mathf.RoundToInt(res.move)).Append('%').AppendLine();
        m_Sb.Append(Mathf.RoundToInt(res.bleed)).Append('%').AppendLine();
        m_Sb.Append(Mathf.RoundToInt(res.debuff)).Append('%').AppendLine();
        m_Sb.Append(Mathf.RoundToInt(res.trap)).Append('%');
        m_ResistanceText2.SetText(m_Sb);
    }
    private void PopulateEquipment(NikkeInstance inst)
    {
        NikkeData data = inst.Data;

        if (data.Weapon != null)
        {
            m_WeaponIcon.gameObject.SetActive(true);
            m_WeaponIcon.sprite = data.Weapon.GetSprite(inst.WeaponLevel);
            SetupTooltip(m_WeaponIcon.gameObject, sb => BuildWeaponTooltip(sb, data.Weapon, inst.WeaponLevel));
        }
        else
        {
            m_WeaponIcon.gameObject.SetActive(false);
        }

        if (data.Armor != null)
        {
            m_ArmorIcon.gameObject.SetActive(true);
            m_ArmorIcon.sprite = data.Armor.GetSprite(inst.ArmorLevel);
            SetupTooltip(m_ArmorIcon.gameObject, sb => BuildArmorTooltip(sb, data.Armor, inst.ArmorLevel));
        }
        else
        {
            m_ArmorIcon.gameObject.SetActive(false);
        }

        for (int i = 0; i < m_TrinketIcons.Length; ++i)
        {
            TrinketData trinket = inst.Trinkets[i];
            if (trinket != null)
            {
                m_TrinketIcons[i].gameObject.SetActive(true);
                m_TrinketIcons[i].sprite = trinket.Icon;
                SetupTooltip(m_TrinketIcons[i].gameObject, sb => BuildTrinketTooltip(sb, trinket));
            }
            else
            {
                m_TrinketIcons[i].gameObject.SetActive(false);
            }
        }
    }
    private void PopulateQuirks(NikkeInstance inst)
    {
        IReadOnlyList<QuirkData> posQuirks = inst.PosQuirks;
        for (int i = 0; i < m_PosQuirkTexts.Length; ++i)
        {
            bool has = i < posQuirks.Count && posQuirks[i] != null;
            m_PosQuirkTexts[i].gameObject.SetActive(has);
            if (!has) continue;
            m_PosQuirkTexts[i].text = posQuirks[i].QuirkName;
            m_PosQuirkTexts[i].color = COLOR_QUIRK_POS;
        }

        IReadOnlyList<QuirkData> negQuirks = inst.NegQuirks;
        for (int i = 0; i < m_NegQuirkTexts.Length; ++i)
        {
            bool has = i < negQuirks.Count && negQuirks[i] != null;
            m_NegQuirkTexts[i].gameObject.SetActive(has);
            if (!has) continue;
            m_NegQuirkTexts[i].text = negQuirks[i].QuirkName;
            m_NegQuirkTexts[i].color = COLOR_QUIRK_NEG;
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
    private void SetupTooltip(GameObject go, TooltipTrigger.ContentBuilderHandler builder)
    {
        TooltipTrigger trigger = go.GetComponent<TooltipTrigger>();
        if (trigger == null)
            trigger = go.AddComponent<TooltipTrigger>();
        trigger.Initialize(m_Tooltip, builder, new Vector2(10f, 0f));
    }
    private static string GetRarityLabel(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return "일반";
            case ItemRarity.Uncommon: return "고급";
            case ItemRarity.Rare: return "희귀";
            case ItemRarity.Epic: return "영웅";
            case ItemRarity.Legendary: return "전설";
            default: return rarity.ToString();
        }
    }
    private static void BuildWeaponTooltip(StringBuilder sb, GearData gear, int level)
    {
        StatBlock stats = gear.GetStats(level);
        sb.Append("<b>").Append(gear.GearName).Append("</b>\n");
        sb.Append("기본 피해: ").Append(stats.minDamage).Append(" ~ ").Append(stats.maxDamage).Append('\n');
        sb.Append("기본 치명타: ").Append(stats.critChance).Append("%\n");
        sb.Append("기본 속도: ").Append(stats.speed);
    }
    private static void BuildArmorTooltip(StringBuilder sb, GearData gear, int level)
    {
        StatBlock stats = gear.GetStats(level);
        sb.Append("<b>").Append(gear.GearName).Append("</b>\n");
        sb.Append("기본 회피: ").Append(stats.dodge).Append('\n');
        sb.Append("기본 체력: ").Append(stats.maxHp);
    }
    private static void BuildTrinketTooltip(StringBuilder sb, TrinketData trinket)
    {
        sb.Append("<b>").Append(trinket.ItemName).Append("</b>\n");
        sb.Append(GetRarityLabel(trinket.Rarity)).Append('\n');
        TooltipHelper.AppendStatBlock(sb, trinket.StatDelta);
    }
    private static void UpdateSelectIconPositions(
     IReadOnlyList<int> activeIndices,
     Image[] icons,
     RectTransform[] selectIcons)
    {
        for (int s = 0; s < activeIndices.Count; ++s)
        {
            int idx = activeIndices[s];
            bool valid = idx >= 0 && idx < icons.Length && icons[idx] != null;
            selectIcons[s].gameObject.SetActive(valid);
            if (!valid) continue;
            selectIcons[s].position = icons[idx].rectTransform.position + SELECT_ICON_OFFSET;
        }
    }
}
