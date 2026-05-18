using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;
public abstract class NikkePanelBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected CombatTooltip m_Tooltip;

    [Header("Identity")]
    [SerializeField] protected Image m_HedaerPortrait;
    [SerializeField] protected TMP_InputField m_NameInput;
    [SerializeField] protected TextMeshProUGUI m_ClassText;
    [SerializeField] protected TextMeshProUGUI m_RankText;
    [SerializeField] protected Image m_StandingIdle;

    [Header("Level")]
    [SerializeField] protected Image m_LevelImage;
    [SerializeField] protected TextMeshProUGUI m_Leveltxt;
    [SerializeField] protected Image m_ExpBar;
    [SerializeField] protected Sprite[] m_LevelSprites;

    [Header("Ebla")]
    [SerializeField] protected Image[] m_EblaCells;
    [SerializeField] protected GameObject m_EblaCellRoot;
    [SerializeField] protected Sprite m_EblaEmptySprite;
    [SerializeField] protected Sprite m_EblaPhase1Sprite;
    [SerializeField] protected Sprite m_EblaPhase2Sprite;

    [Header("Combat Skills")]
    [SerializeField] protected Image[] m_SkillIcons;
    [SerializeField] protected RectTransform[] m_SkillSelectIcons;
    [SerializeField] protected TextMeshProUGUI[] m_SkillLevelTexts;
    [SerializeField] protected SkillTooltip m_SkillTooltip;

    [Header("Skill Recommendation")]
    [SerializeField] protected Image[] m_RecommendPositions;
    [SerializeField] protected Image[] m_RecommendTargets;
    [SerializeField] protected Sprite[] m_RecommendPositionSprites;
    [SerializeField] protected Sprite[] m_RecommendTargetSprites;

    [Header("Camp Skills")]
    [SerializeField] protected Image[] m_CampSkillIcons;
    [SerializeField] protected RectTransform[] m_CampSkillSelectIcons;

    [Header("Stats")]
    [SerializeField] protected TextMeshProUGUI m_MaxHpText;
    [SerializeField] protected TextMeshProUGUI m_AccText;
    [SerializeField] protected TextMeshProUGUI m_CritText;
    [SerializeField] protected TextMeshProUGUI m_DmgText;
    [SerializeField] protected TextMeshProUGUI m_DodgeText;
    [SerializeField] protected TextMeshProUGUI m_ProtText;
    [SerializeField] protected TextMeshProUGUI m_SpeedText;

    [Header("Resistance")]
    [SerializeField] protected TextMeshProUGUI m_ResistanceText1;
    [SerializeField] protected TextMeshProUGUI m_ResistanceText2;

    [Header("Equipment")]
    [SerializeField] protected Image m_WeaponIcon;
    [SerializeField] protected Image m_ArmorIcon;
    [SerializeField] protected Image[] m_TrinketIcons;

    [Header("Quirks")]
    [SerializeField] protected TextMeshProUGUI[] m_PosQuirkTexts;
    [SerializeField] protected TextMeshProUGUI[] m_NegQuirkTexts;

    [Header("Diseases")]
    [SerializeField] protected TextMeshProUGUI[] m_DiseaseTexts;

    [Header("Navigation")]
    [SerializeField] protected Button m_PrevButton;
    [SerializeField] protected Button m_NextButton;
    [SerializeField] protected Button m_CloseButton;

    protected NikkeInstance m_CurrentInstance;
    protected StringBuilder m_Sb = new StringBuilder(128);
    protected int m_EblaTooltipValue;
    protected TooltipTrigger.ContentBuilderHandler m_EblaTooltipBuilder;
    protected readonly int[] m_PosCount = new int[4];
    protected readonly int[] m_TgtCount = new int[4];

    protected static readonly Color COLOR_NORMAL = new Color(0.8f, 0.8f, 0.8f, 1f);
    protected static readonly Color COLOR_BUFF = new Color(0.8f, 0.76f, 0.56f, 1f);
    protected static readonly Color COLOR_DEBUFF = new Color(0.75f, 0.1f, 0.1f, 1f);
    protected static readonly Color COLOR_SKILL_ACTIVE = Color.white;
    protected static readonly Color COLOR_SKILL_INACTIVE = new Color(0.35f, 0.35f, 0.35f, 1f);
    protected static readonly Color COLOR_QUIRK_POS = new Color(0.4f, 0.85f, 0.4f, 1f);
    protected static readonly Color COLOR_QUIRK_NEG = new Color(0.85f, 0.25f, 0.25f, 1f);
    protected static readonly Vector3 SELECT_ICON_OFFSET = new Vector3(0f, 2f, 0f);

    protected abstract void PopulateStats();
    protected abstract void PopulateResistance();
    protected abstract void Navigate(int delta);
    protected virtual void OnSkillClicked(int idx) { }
    protected virtual void OnCampSkillClicked(int idx) { }

    private void OnPrev() => Navigate(-1);
    private void OnNext() => Navigate(+1);
    protected virtual void Awake()
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
        if (m_CloseButton != null) m_CloseButton.onClick.AddListener(Hide);
        m_PrevButton.onClick.AddListener(OnPrev);
        m_NextButton.onClick.AddListener(OnNext);
        m_EblaTooltipBuilder = sb => sb.Append("에블라: ").Append(m_EblaTooltipValue).Append(" / 200");
    }

    protected virtual void Update()
    {
        if (!gameObject.activeSelf) return;
        if (Keyboard.current[Key.Escape].wasPressedThisFrame) Hide();
        if (Keyboard.current[Key.Q].wasPressedThisFrame) OnPrev();
        if (Keyboard.current[Key.E].wasPressedThisFrame) OnNext();
    }

    public void Hide()
    {
        m_Tooltip.Hide();
        m_SkillTooltip.Hide();
        gameObject.SetActive(false);
    }

    protected void PopulateIdentity(NikkeInstance inst)
    {
        NikkeData data = inst.Data;
        m_StandingIdle.sprite = data.CombatIdleSprite;
        m_HedaerPortrait.sprite = data.HeaderSprite;
        m_NameInput.text = inst.DisplayName;
        m_NameInput.interactable = false;
        m_Sb.Clear();
        m_Sb.Append(LabelText.GetManufacturerLabel(data.Manufacturer)).Append(" ").Append(LabelText.GetClassLabel(data.NikkeClass));

        m_ClassText.SetText(m_Sb);
        m_RankText.SetText(LabelText.GetRankLabel(inst.Level));
        int lv = inst.Level;
        m_LevelImage.sprite = m_LevelSprites[Mathf.Clamp(lv, 0, m_LevelSprites.Length - 1)];
        m_Leveltxt.SetText("{0}", lv);
        IReadOnlyList<int> thresholds = data.ExpThresholds;
        bool isMaxLevel = lv >= thresholds.Count;
        m_ExpBar.fillAmount = isMaxLevel ? 1f : (float)inst.Exp / thresholds[lv];
    }

    protected void PopulateSkills(NikkeInstance inst)
    {
        IReadOnlyList<SkillData> allSkills = inst.Data.Skills;
        for (int i = 0; i < m_SkillIcons.Length; ++i)
        {
            bool hasSkill = i < allSkills.Count && allSkills[i] != null;
            m_SkillIcons[i].gameObject.SetActive(hasSkill);
            if (!hasSkill) continue;
            m_SkillIcons[i].sprite = allSkills[i].SkillIcon;
            SetupSkillTooltip(m_SkillIcons[i].gameObject, allSkills[i], inst.SkillLevels[i]);
        }
        RefreshSkillUI(inst);
    }

    protected void RefreshSkillUI(NikkeInstance inst)
    {
        IReadOnlyList<int> activeIndices = inst.ActiveSkillIndices;
        IReadOnlyList<SkillData> allSkills = inst.Data.Skills;
        int count = Mathf.Min(m_SkillIcons.Length, allSkills.Count);
        for (int i = 0; i < count; ++i)
        {
            if (allSkills[i] == null) continue;
            bool isActive = false;
            for (int j = 0; j < activeIndices.Count; ++j)
                if (activeIndices[j] == i) { isActive = true; break; }
            m_SkillIcons[i].color = isActive ? COLOR_SKILL_ACTIVE : COLOR_SKILL_INACTIVE;
            m_SkillLevelTexts[i].SetText("{0}", inst.SkillLevels[i]);
        }
        UpdateSelectIconPositions(activeIndices, m_SkillIcons, m_SkillSelectIcons);
    }

    protected void RefreshRecommendation(NikkeInstance inst)
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

    protected void PopulateCampSkills(NikkeInstance inst)
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

    protected void RefreshCampSkillUI(NikkeInstance inst)
    {
        IReadOnlyList<int> activeIndices = inst.ActiveCampSkillIndices;
        IReadOnlyList<CampSkillData> allSkills = inst.Data.CampSkills;
        if (allSkills == null) return;
        int count = Mathf.Min(m_CampSkillIcons.Length, allSkills.Count);
        for (int i = 0; i < count; ++i)
        {
            if (allSkills[i] == null) continue;
            bool isActive = false;
            for (int j = 0; j < activeIndices.Count; ++j)
                if (activeIndices[j] == i) { isActive = true; break; }
            m_CampSkillIcons[i].color = isActive ? COLOR_SKILL_ACTIVE : COLOR_SKILL_INACTIVE;
        }
        UpdateSelectIconPositions(activeIndices, m_CampSkillIcons, m_CampSkillSelectIcons);
    }

    protected void PopulateEbla(int ebla)
    {
        int phase1Count = Mathf.CeilToInt(Mathf.Min(ebla, CombatUnit.EblaPhaseThreshold) /
(float)CombatUnit.EblaCellValue);
        int phase2Count = ebla > CombatUnit.EblaPhaseThreshold
            ? Mathf.CeilToInt((ebla - CombatUnit.EblaPhaseThreshold) / (float)CombatUnit.EblaCellValue)
            : 0;
        for (int i = 0; i < m_EblaCells.Length; ++i)
        {
            if (i < phase2Count) m_EblaCells[i].sprite = m_EblaPhase2Sprite;
            else if (i < phase1Count) m_EblaCells[i].sprite = m_EblaPhase1Sprite;
            else m_EblaCells[i].sprite = m_EblaEmptySprite;
        }
        m_EblaTooltipValue = ebla;
        SetupTooltip(m_EblaCellRoot, m_EblaTooltipBuilder);
    }

    protected void PopulateEquipment(NikkeInstance inst)
    {
        NikkeData data = inst.Data;
        if (data.Weapon != null)
        {
            m_WeaponIcon.gameObject.SetActive(true);
            m_WeaponIcon.sprite = data.Weapon.GetSprite(inst.WeaponLevel);
            SetupTooltip(m_WeaponIcon.gameObject, sb => BuildWeaponTooltip(sb, data.Weapon, inst.WeaponLevel));
        }
        else m_WeaponIcon.gameObject.SetActive(false);

        if (data.Armor != null)
        {
            m_ArmorIcon.gameObject.SetActive(true);
            m_ArmorIcon.sprite = data.Armor.GetSprite(inst.ArmorLevel);
            SetupTooltip(m_ArmorIcon.gameObject, sb => BuildArmorTooltip(sb, data.Armor, inst.ArmorLevel));
        }
        else m_ArmorIcon.gameObject.SetActive(false);

        for (int i = 0; i < m_TrinketIcons.Length; ++i)
        {
            TrinketData trinket = inst.Trinkets[i];
            if (trinket != null)
            {
                m_TrinketIcons[i].gameObject.SetActive(true);
                m_TrinketIcons[i].sprite = trinket.Icon;
                SetupTooltip(m_TrinketIcons[i].gameObject, sb => BuildTrinketTooltip(sb, trinket));
            }
            else m_TrinketIcons[i].gameObject.SetActive(false);
        }
    }

    protected void PopulateQuirks(NikkeInstance inst)
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

    protected void PopulateDiseases(NikkeInstance inst)
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

    protected static Color GetStatColor(float current, float baseValue)
    {
        if (current > baseValue) return COLOR_BUFF;
        if (current < baseValue) return COLOR_DEBUFF;
        return COLOR_NORMAL;
    }

    protected static int FindToggleSlot(IReadOnlyList<int> activeIndices, int skillIdx, out bool deactivate)
    {
        for (int i = 0; i < activeIndices.Count; ++i)
            if (activeIndices[i] == skillIdx) { deactivate = true; return i; }
        for (int i = 0; i < activeIndices.Count; ++i)
            if (activeIndices[i] == -1) { deactivate = false; return i; }
        deactivate = false;
        return -1;
    }

    protected void SetupTooltip(GameObject go, TooltipTrigger.ContentBuilderHandler builder)
    {
        TooltipTrigger trigger = go.GetComponent<TooltipTrigger>();
        if (trigger == null) trigger = go.AddComponent<TooltipTrigger>();
        trigger.Initialize(m_Tooltip, builder, new Vector2(10f, 0f));
    }

    protected void SetupSkillTooltip(GameObject go, SkillData skill, int skillLevel)
    {
        SkillTooltipTrigger trigger = go.GetComponent<SkillTooltipTrigger>();
        if (trigger == null) trigger = go.AddComponent<SkillTooltipTrigger>();
        trigger.Initialize(m_SkillTooltip, skill,skillLevel, new Vector2(0f, -24f));
    }

    protected static Button EnsureButton(GameObject go)
    {
        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        return btn;
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
        sb.Append(LabelText.GetRarityLabel(trinket.Rarity)).Append('\n');
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
            bool valid = idx >= 0 && idx < icons.Length && icons[idx] != null && icons[idx].gameObject.activeSelf;
            selectIcons[s].gameObject.SetActive(valid);
            if (!valid) continue;
            selectIcons[s].position = icons[idx].rectTransform.position + SELECT_ICON_OFFSET;
        }
    }
}
