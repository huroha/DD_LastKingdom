using UnityEngine;
using System.Collections.Generic;

public enum CombatUnitType
{
    Nikke,
    Enemy
}

public enum UnitState
{
    Alive,
    DeathsDoor,     // Nikke 전용 HP 0, 다음 피해 시 deathblow 판정
    Corpse,         // Enemy 전용 - 별도 hp 보유, 일반/ Dot 피해 모두 적용
    Dead
}

public enum EblaState
{
    Normal,
    Afflicted,
    Virtuous,
}
public enum EblaResolutionType
{
    Afflicted,
    Virtuous,
}
public class CombatUnit
{
    public const int DEATHS_DOOR_EBLA = 18;
    public const int MaxEbla = 200;
    public const int EblaPhaseThreshold = 100;  //  Affliction/Virtue 임계값
    public const int EblaCellValue = 10;        // 셀 하나당 ebla 값

    // 유닛 식별
    public CombatUnitType   UnitType { get; }
    public string           UnitName { get; }
    public int              SlotIndex { get; private set; }

    public int SlotSize     { get; }
    public CombatUnit Protecting { get; private set; }
   
    // 행동 수
    public int ActionsPerRound { get; }

    // 원본 데이터 참조
    public NikkeInstance NikkeInstance { get; }
    public NikkeData NikkeData => NikkeInstance?.Data;
    public EnemyData        EnemyData { get; }

    // 스킬
    public IReadOnlyList<SkillData> Skills { get; }

    // 런타임 상태
    public UnitState State { get; private set; }
    public bool      IsAlive => State == UnitState.Alive || State == UnitState.DeathsDoor;

    // HP
    public int CurrentHp { get; private set; }
    public int MaxHp { get; }
    
    // 스탯
    public StatBlock BaseStats { get; }
    public StatBlock CurrentStats { get; private set; }

    // 에블라 
    public int Ebla { get; private set; }
    public EblaState EblaState { get; private set; }
    public void SetEblaState(EblaState state)
    {
        EblaState = state;
    }
    public AfflictionTypeData CurrentAfflictionType { get; private set; }
    public VirtueTypeData CurrentVirtueType { get; private set; }

    public void SetCurrentAfflictionType(AfflictionTypeData type)
    {
        CurrentAfflictionType = type;
    }
    public void SetCurrentVirtueType(VirtueTypeData type)
    {
        CurrentVirtueType = type;
    }

    // 적용중인 상태이상
    private List<ActiveStatusEffect> m_ActiveEffects;
    public IReadOnlyList<ActiveStatusEffect> ActiveEffects => m_ActiveEffects;

    public void AddEffect(ActiveStatusEffect effect) { m_ActiveEffects.Add(effect); }
    public void RemoveEffectAt(int index) { m_ActiveEffects.RemoveAt(index); }

    public float TurnOrderTieBreaker { get; private set; }

    public int CorpseTimer { get; private set; }

    public void SetSlotIndex(int index)
    {
        SlotIndex = index;
    }
    public void SetTurnOrderTieBreaker(float v)
    {
        TurnOrderTieBreaker = v;
    }
    public void TickCorpseTimer() { CorpseTimer--; }

    public CombatUnit GuardedBy { get; private set; }
    public void SetGuardedBy(CombatUnit guardian, int turns) {  GuardedBy = guardian; GuardTurnsRemaining = turns; }
    public void SetProtecting(CombatUnit target) { Protecting = target; }

    public int GuardTurnsRemaining { get; private set; }


    // 생성자
    public CombatUnit(NikkeInstance instance, int slotIndex, int currentHp, int ebla,
                 List<ActiveStatusEffect> activeEffects)
    {
        UnitType = CombatUnitType.Nikke;
        NikkeInstance = instance;
        UnitName = instance.DisplayName;
        SlotIndex = slotIndex;
        Skills = BuildSkillList(instance);
        SlotSize = 1;
        StatBlock effective = instance.GetEffectiveBaseStats();
        MaxHp = effective.maxHp;
        BaseStats = effective;
        CurrentHp = currentHp;
        CurrentStats = BaseStats;
        State = currentHp > 0 ? UnitState.Alive : UnitState.DeathsDoor;
        Ebla = ebla;
        ActionsPerRound = 1;
        m_ActiveEffects = activeEffects ?? new List<ActiveStatusEffect>();  // Null 병합 연산자 왼쪽이 null이면 오른쪽 사용
        RecalculateStats();
    }

    public CombatUnit(EnemyData data, int slotIndex)
    {
        UnitType = CombatUnitType.Enemy;
        UnitName = data.EnemyName;
        SlotIndex = slotIndex;
        EnemyData = data;
        Skills = data.Skills;
        SlotSize = data.SlotSize;

        MaxHp = data.BaseStats.maxHp;
        CurrentHp = MaxHp;
        BaseStats = data.BaseStats;
        CurrentStats = data.BaseStats;
        State = UnitState.Alive;
        ActionsPerRound = data.ActionsPerRound;
        m_ActiveEffects = new List<ActiveStatusEffect>();
    }

    // 데미지
    public UnitState TakeDamage(int damage, bool isDot = false, bool isCrit = false)
    {
        if (State == UnitState.Dead)
            return UnitState.Dead;

        if(UnitType == CombatUnitType.Nikke)
        {
            if (State == UnitState.Alive)
            {
                CurrentHp -= damage;
                if(CurrentHp <= 0)
                {
                    CurrentHp = 0;
                    State = UnitState.DeathsDoor;
                }
            }
            else if (State == UnitState.DeathsDoor)
            {
                // hp 0 고정, deathblow 판정만 수행
                float roll = Random.Range(0f, 100f);
                if (roll > CurrentStats.deathBlowResist)
                    State = UnitState.Dead;
            }
        }
        else // Enemy
        {
            if(State == UnitState.Alive)
            {
                CurrentHp -= damage;
                if(CurrentHp <= 0)
                {
                    if(isDot || isCrit)
                    {
                        CurrentHp = 0;
                        State = UnitState.Dead;
                    }
                    else
                    {
                        State = UnitState.Corpse;
                        CurrentHp = EnemyData.CorpseHp;
                        CorpseTimer = EnemyData.CorpseDecayTurns;
                        ClearAllEffects();
                    }
                }
            }
            else if(State == UnitState.Corpse)
            {
                CurrentHp -= damage;
                if (CurrentHp <= 0)
                {
                    CurrentHp = 0;
                    State = UnitState.Dead;
                }
            }
        }


        return State;
    }

    // 회복
    public void Heal(int amount)
    {
        if (!IsAlive)
            return;
       
        CurrentHp += amount;
        if(CurrentHp > MaxHp)
            CurrentHp = MaxHp;


        // DeathsDoor 상태에서 힐 받으면 alive로 복귀
        if (State == UnitState.DeathsDoor && CurrentHp > 0)
            State = UnitState.Alive;
    }
    
    public void Kill()
    {
        State = UnitState.Dead;
        CurrentHp = 0;
    }


    // 에블라
    public void AddEbla(int amount)
    {
        if (UnitType != CombatUnitType.Nikke)
            return;
        Ebla = Mathf.Clamp(Ebla + amount, 0, MaxEbla);
    }

    // 스탯 재계산
    // Phase2 StatusEffectManager 연동 시 버프/디버프 합산 후 호출
    public void RecalculateStats()
    {
        StatBlock stats = BaseStats;
        for (int i=0; i< ActiveEffects.Count; ++i)
        {
            stats = stats.Apply(ActiveEffects[i].Data.StatModifier);
        }
        CurrentStats = stats;
    }

    private static IReadOnlyList<SkillData> BuildSkillList(NikkeInstance instance)
    {
        return instance.GetActiveSkills();
    }

    public bool IsStunned
    {
        get
        {
            for(int i=0; i< ActiveEffects.Count; ++i)
            {
                if (ActiveEffects[i].Data.EffectType == StatusEffectType.Stun)
                    return true;
            }
            return false;
        }
    }

    public ActiveStatusEffect FindEffect(StatusEffectData data)
    {
        for (int i = 0; i < m_ActiveEffects.Count; ++i)
        {
            if (m_ActiveEffects[i].Data == data)
                return m_ActiveEffects[i];
        }
        return null;
    }
    public void RemoveEffect(StatusEffectData data)
    {
        if (data == null)
            return;
        for (int i = m_ActiveEffects.Count - 1; i >= 0; --i)
        {
            if (m_ActiveEffects[i].Data == data)
            {
                RemoveEffectAt(i);
                return;
            }
        }
    }
    public void ClearAllEffects()
    {
        m_ActiveEffects.Clear();
        RecalculateStats();
    }
    public ActiveStatusEffect FindEffectByType(StatusEffectType type)
    {
        for(int i=0; i < m_ActiveEffects.Count; ++i)
        {
            if (m_ActiveEffects[i].Data.EffectType == type)
                return m_ActiveEffects[i];
        }
        return null;
    }
    public void DecrementGuardTurns()
    {
        GuardTurnsRemaining--;
    }
}