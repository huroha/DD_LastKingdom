using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class TownNikkePanel : NikkePanelBase
{
    private IReadOnlyList<NikkeInstance> m_NavInstances;
    private bool m_NavReversed;
    [SerializeField] private Button m_FireButton;
    [SerializeField] private ConfirmPopup m_ConfirmPopup;
    private const string MSG_FIRE = "정말 작별하시겠습니까?";


    protected override void Awake()
    {
        base.Awake();
        if (m_FireButton != null) m_FireButton.onClick.AddListener(OnFireClicked);
    }
    public void Show(NikkeInstance inst, IReadOnlyList<NikkeInstance> navList = null, bool reverseNav = false)
    {
        m_CurrentInstance = inst;
        m_NavInstances = navList;
        m_NavReversed = reverseNav;
        PopulateIdentity(inst);
        PopulateSkills(inst);
        RefreshRecommendation(inst);
        PopulateCampSkills(inst);
        PopulateStats();
        PopulateResistance();
        PopulateEquipment(inst);
        PopulateQuirks(inst);
        PopulateDiseases(inst);
        PopulateEbla(0);
        gameObject.SetActive(true);
    }

    protected override void PopulateStats()
    {
        StatBlock stats = m_CurrentInstance.GetEffectiveBaseStats();
        m_MaxHpText.SetText("{0}", stats.maxHp);
        m_MaxHpText.color = COLOR_NORMAL;
        m_AccText.SetText("{0}", stats.accuracyMod);
        m_AccText.color = COLOR_NORMAL;
        m_CritText.SetText("{0}%", stats.critChance);
        m_CritText.color = COLOR_NORMAL;
        float dmgMul = 1f + stats.damageMultiplier / 100f;
        int displayMin = Mathf.Max((int)(stats.minDamage * dmgMul), 0);
        int displayMax = Mathf.Max((int)(stats.maxDamage * dmgMul), 0);
        m_DmgText.SetText("{0} - {1}", displayMin, displayMax);
        m_DmgText.color = COLOR_NORMAL;
        m_DodgeText.SetText("{0}", stats.dodge);
        m_DodgeText.color = COLOR_NORMAL;
        m_ProtText.SetText("{0}%", stats.defense);
        m_ProtText.color = COLOR_NORMAL;
        m_SpeedText.SetText("{0}", stats.speed);
        m_SpeedText.color = COLOR_NORMAL;
    }

    protected override void PopulateResistance()
    {
        StatBlock stats = m_CurrentInstance.GetEffectiveBaseStats();
        ResistanceBlock res = stats.resistance;
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

    protected override void Navigate(int delta)
    {
        if (m_NavInstances == null || m_NavInstances.Count == 0) return;
        int curIdx = -1;
        for (int i = 0; i < m_NavInstances.Count; ++i)
            if (m_NavInstances[i] == m_CurrentInstance) { curIdx = i; break; }
        if (curIdx < 0) return;
        int actualDelta = m_NavReversed ? -delta : delta;
        int nextIdx = (curIdx + actualDelta + m_NavInstances.Count) % m_NavInstances.Count;
        Show(m_NavInstances[nextIdx], m_NavInstances, m_NavReversed);
    }

    protected override void OnSkillClicked(int skillIdx)
    {
        bool deactivate;
        int slot = FindToggleSlot(m_CurrentInstance.ActiveSkillIndices, skillIdx, out deactivate);
        if (slot < 0) return;
        m_CurrentInstance.SetSkillIndex(slot, deactivate ? -1 : skillIdx);
        RefreshSkillUI(m_CurrentInstance);
        RefreshRecommendation(m_CurrentInstance);
    }

    protected override void OnCampSkillClicked(int skillIdx)
    {
        bool deactivate;
        int slot = FindToggleSlot(m_CurrentInstance.ActiveCampSkillIndices, skillIdx, out deactivate);
        if (slot < 0) return;
        m_CurrentInstance.SetCampSkillIndex(slot, deactivate ? -1 : skillIdx);
        RefreshCampSkillUI(m_CurrentInstance);
    }
    private void OnFireClicked()
    {
        if (m_CurrentInstance == null) return;
        m_ConfirmPopup.Show(MSG_FIRE, DoFire);
    }
    private void DoFire()
    {
        RosterManager.Instance.FireNikke(m_CurrentInstance);
        Hide();
    }
}
