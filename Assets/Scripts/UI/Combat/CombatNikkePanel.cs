using UnityEngine;
using System.Collections.Generic;
public class CombatNikkePanel : NikkePanelBase
{
    [Header("Combat Reference")]
    [SerializeField] private CombatStateMachine m_CombatStateMachine;

    private CombatUnit m_CurrentCombatUnit;
    private readonly List<CombatUnit> m_NavUnits = new List<CombatUnit>(4);

    public void Show(CombatUnit unit)
    {
        RefreshNavUnits();
        m_CurrentInstance = unit.NikkeInstance;
        m_CurrentCombatUnit = unit;
        PopulateIdentity(unit.NikkeInstance);
        PopulateSkills(unit.NikkeInstance);
        RefreshRecommendation(unit.NikkeInstance);
        PopulateCampSkills(unit.NikkeInstance);
        PopulateStats();
        PopulateResistance();
        PopulateEquipment(unit.NikkeInstance);
        PopulateQuirks(unit.NikkeInstance);
        PopulateDiseases(unit.NikkeInstance);
        PopulateEbla(unit.Ebla);
        gameObject.SetActive(true);
    }

    protected override void PopulateStats()
    {
        StatBlock stats = m_CurrentCombatUnit.CurrentStats;
        StatBlock baseStats = m_CurrentCombatUnit.BaseStats;

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

    protected override void PopulateResistance()
    {
        ResistanceBlock res = m_CurrentCombatUnit.CurrentStats.resistance;
        StatBlock stats = m_CurrentCombatUnit.CurrentStats;

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
        int idx = -1;
        for (int i = 0; i < m_NavUnits.Count; ++i)
            if (m_NavUnits[i] == m_CurrentCombatUnit) { idx = i; break; }
        if (idx < 0) return;
        int next = (idx - delta + m_NavUnits.Count) % m_NavUnits.Count;
        Show(m_NavUnits[next]);
    }

    private void RefreshNavUnits()
    {
        m_CombatStateMachine.PositionSystem.GetAllUnits(CombatUnitType.Nikke, m_NavUnits);
    }
}
