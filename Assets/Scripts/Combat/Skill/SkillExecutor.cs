using UnityEngine;
using System.Collections.Generic;

public struct AttackPreview
{
    public float HitChance;
    public float CritChance;
    public int MinDamage;
    public int MaxDamage;
}
public class SkillExecutor
{
    private PositionSystem m_PositionSystem;
    private EblaSystem m_EblaSystem;

    private StatusEffectData m_DeathsDoorDebuff;
    private StatusEffectData m_DeathsDoorRecovery;

    private const int CRIT_EBLA_TO_ENEMY = 5;
    private const int CRIT_EBLA_PARTY_HEAL = -3;
    private const float CRIT_DAMAGE_MULTI = 1.5f;

    // 리스트 버퍼들
    private List<CombatUnit> m_TargetsBuffer = new List<CombatUnit>();
    private List<CombatUnit> m_AllNikkesBuffer = new List<CombatUnit>();
    private List<StatusEffectData> m_AppliedBuffer = new List<StatusEffectData>();
    private List<StatusEffectData> m_ResistedBuffer = new List<StatusEffectData>();
    private List<CombatUnit> m_ActualTargetBuffer = new List<CombatUnit>();
    private List<StatusEffectData> m_AllyAppliedBuffer = new List<StatusEffectData>();
    private List<AllyEffectResult> m_AllyResultList = new List<AllyEffectResult>();

    public SkillExecutor(PositionSystem positionSystem, EblaSystem eblaSystem, StatusEffectData deathsDoorDebuff, StatusEffectData deathsDoorRecovery)
    {
        m_PositionSystem = positionSystem;
        m_EblaSystem = eblaSystem;
        m_DeathsDoorDebuff = deathsDoorDebuff;
        m_DeathsDoorRecovery = deathsDoorRecovery;
    }

    public bool ValidateSkill(CombatUnit user, BaseSkillData skill)
    {
        // 유저가 살아있지 않으면 false
        if (!user.IsAlive)
            return false;

        // 현재 위치에서 스킬 사용이 불가능하면 false
        if (!m_PositionSystem.CanUseSkill(user, skill))
            return false;
        // 각성 스킬이면 flase
        if (skill is SkillData ns && ns.RequiredState == SkillRequiredState.Awakened)
            return false;
        m_PositionSystem.GetValidTargets(user, skill, m_TargetsBuffer);
        if (m_TargetsBuffer.Count == 0)
            return false;

        return true;
    }

    public SkillResult Execute(CombatUnit user, SkillData skill, int skillLevel, CombatUnit selectedTarget = null)
    {
        // ValidateSkill 실패 시 빈 SkillResult 반환 (early return)
        if (!ValidateSkill(user, skill))
            return new SkillResult();

        SkillResult finalResult = new SkillResult();
        // 스킬 레밸
        SkillLevelData ld = skill.GetLevelData(skillLevel);

        // ResolveTargets로 최종 타겟 리스트 결정
        ResolveTargets(user, skill, selectedTarget, m_TargetsBuffer);
        List<CombatUnit> targets = m_TargetsBuffer;

        // 크리티컬 파티 에블라용
        bool allNikkesFetched = false;

        m_ActualTargetBuffer.Clear();
        for (int i = 0; i < targets.Count; ++i)
        {
            CombatUnit actual = ResolveGuardTarget(targets[i], skill, targets);
            if (!m_ActualTargetBuffer.Contains(actual))
                m_ActualTargetBuffer.Add(actual);
        }
        // TargetResult 배열 선언 (targets.Count 크기)
        TargetResult[] result = new TargetResult[m_ActualTargetBuffer.Count];


        // for 루프: 각 타겟에 대해
        for (int i = 0; i < result.Length; ++i)
        {
            m_AppliedBuffer.Clear();
            m_ResistedBuffer.Clear();

            CombatUnit actualTarget = m_ActualTargetBuffer[i];

            // [명중 판정]
            result[i].IsHit = skill.IsAllyTargeting || RollHit(user, actualTarget, ld.accuracyMod);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Skill] {user.UnitName} → {actualTarget.UnitName} | Hit:{result[i].IsHit}");
#endif
            result[i].PreviousState = actualTarget.State;
            if (result[i].IsHit)
                ApplyHit(user, actualTarget, skill, ld, ref result[i], ref allNikkesFetched);

            // [위치 이동] ApplyPositionMove 호출 (명중 여부 무관)
            ApplyPositionMove(user, skill, actualTarget, result[i].IsHit);

            // TargetResult 채우기
            result[i].Target = actualTarget;
            result[i].ResultState = actualTarget.State;
            result[i].AppliedEffects = new StatusEffectData[m_AppliedBuffer.Count];
            for (int j = 0; j < m_AppliedBuffer.Count; ++j)
                result[i].AppliedEffects[j] = m_AppliedBuffer[j];
            result[i].ResistedEffects = new StatusEffectData[m_ResistedBuffer.Count];
            for (int j = 0; j < m_ResistedBuffer.Count; ++j)
                result[i].ResistedEffects[j] = m_ResistedBuffer[j];
        }

        IReadOnlyList<StatusEffectData> selfEffects = ld.onSelfEffects ?? System.Array.Empty<StatusEffectData>();
        for (int i = 0; i < selfEffects.Count; ++i)
        {
            StatusEffectData effect = selfEffects[i];
            ActiveStatusEffect existing = user.FindEffect(effect);
            if (existing != null)
            {
                if (effect.IsStackable)
                { if (existing.CurrentStacks < effect.MaxStack) existing.CurrentStacks++; }
                else
                    existing.RemainingTurns = Mathf.Max(existing.RemainingTurns, effect.Duration);
            }
            else
                user.AddEffect(new ActiveStatusEffect(effect));
        }

        if (selfEffects.Count > 0)
            user.RecalculateStats();

        // 아군에게 버프나 디버프 부여
        ApplyAllyEffects(user, skill, ld);
        if (m_AllyResultList.Count > 0)
            finalResult.AllyResults = m_AllyResultList.ToArray();
        // SkillResult 반환 (User, Skill, TargetResults 채워서)
        finalResult.User = user;
        finalResult.Skill = skill;
        finalResult.TargetResults = result;
        if (selfEffects.Count > 0)
            finalResult.SelfAppliedEffects = selfEffects;
        return finalResult;
    }

    public SkillResult ExecuteEnemy(CombatUnit user, EnemySkillData skill, CombatUnit selectedTarget = null)
    {
        if (!ValidateSkill(user, skill))
            return new SkillResult();

        SkillResult finalResult = new SkillResult();
        bool allNikkesFetched = false;

        ResolveTargets(user, skill, selectedTarget, m_TargetsBuffer);

        m_ActualTargetBuffer.Clear();
        for (int i = 0; i < m_TargetsBuffer.Count; ++i)
        {
            CombatUnit actual = ResolveGuardTarget(m_TargetsBuffer[i], skill, m_TargetsBuffer);
            if (!m_ActualTargetBuffer.Contains(actual))
                m_ActualTargetBuffer.Add(actual);
        }

        TargetResult[] result = new TargetResult[m_ActualTargetBuffer.Count];

        for (int i = 0; i < result.Length; ++i)
        {
            m_AppliedBuffer.Clear();
            m_ResistedBuffer.Clear();

            CombatUnit actualTarget = m_ActualTargetBuffer[i];
            result[i].IsHit = skill.IsAllyTargeting || RollHit(user, actualTarget, skill.AccuracyMod);

            result[i].PreviousState = actualTarget.State;       // 시체 sprite 오류 방지
            if (result[i].IsHit)
            {
                result[i].PreviousHp = actualTarget.CurrentHp;

                bool wasBlocked = false;
                if (!skill.IsAllyTargeting)
                {
                    ActiveStatusEffect blockEffect = actualTarget.FindEffectByType(StatusEffectType.Block);
                    if (blockEffect != null)
                    {
                        result[i].WasBlocked = true;
                        wasBlocked = true;
                        blockEffect.CurrentStacks--;
                        if (blockEffect.CurrentStacks <= 0)
                            actualTarget.RemoveEffect(blockEffect.Data);
                    }
                }

                if (!wasBlocked)
                {
                    int damage = Random.Range(skill.MinDamage, skill.MaxDamage + 1);
                    float defence = (actualTarget.State == UnitState.Corpse) ? 0f : actualTarget.CurrentStats.defense;
                    damage = Mathf.Max((int)(damage * (1f - defence / 100f)), 0);

                    result[i].IsCrit = RollCrit(user, skill.CritMod);
                    if (result[i].IsCrit)
                        damage = (int)(damage * CRIT_DAMAGE_MULTI);

                    actualTarget.TakeDamage(damage, isCrit: result[i].IsCrit);
                    result[i].DamageDealt = damage;

                    if (skill.EblaDamage > 0)
                        m_EblaSystem.ModifyEbla(actualTarget, skill.EblaDamage);

                    if (actualTarget.IsAlive)
                        ApplyOnHitEffects(actualTarget, skill.OnHitEffects ?? System.Array.Empty<StatusEffectData>(),
                                          m_AppliedBuffer, m_ResistedBuffer, result[i].IsCrit);

                    if (result[i].IsCrit)
                    {
                        if (!allNikkesFetched)
                        {
                            m_PositionSystem.GetAllUnits(CombatUnitType.Nikke, m_AllNikkesBuffer);
                            allNikkesFetched = true;
                        }
                        ApplyCritEffects(user, actualTarget, m_AllNikkesBuffer);
                    }
                }
            }

            ApplyPositionMove(user, skill, actualTarget, result[i].IsHit);

            result[i].Target = actualTarget;
            result[i].ResultState = actualTarget.State;
            result[i].AppliedEffects = new StatusEffectData[m_AppliedBuffer.Count];
            for (int j = 0; j < m_AppliedBuffer.Count; ++j)
                result[i].AppliedEffects[j] = m_AppliedBuffer[j];
            result[i].ResistedEffects = new StatusEffectData[m_ResistedBuffer.Count];
            for (int j = 0; j < m_ResistedBuffer.Count; ++j)
                result[i].ResistedEffects[j] = m_ResistedBuffer[j];
        }

        // onSelfEffects
        IReadOnlyList<StatusEffectData> selfEffects = skill.OnSelfEffects ?? System.Array.Empty<StatusEffectData>();
        for (int i = 0; i < selfEffects.Count; ++i)
        {
            StatusEffectData effect = selfEffects[i];
            ActiveStatusEffect existing = user.FindEffect(effect);
            if (existing != null)
            {
                if (effect.IsStackable)
                { if (existing.CurrentStacks < effect.MaxStack) existing.CurrentStacks++; }
                else
                    existing.RemainingTurns = Mathf.Max(existing.RemainingTurns, effect.Duration);
            }
            else
                user.AddEffect(new ActiveStatusEffect(effect));
        }
        if (selfEffects.Count > 0)
            user.RecalculateStats();

        // onAllyEffects
        IReadOnlyList<StatusEffectData> allyEffects = skill.OnAllyEffects ?? System.Array.Empty<StatusEffectData>();
        if (allyEffects.Count > 0)
        {
            m_PositionSystem.GetAllTargetable(user.UnitType, m_AllNikkesBuffer);
            m_AllyResultList.Clear();
            for (int i = 0; i < m_AllNikkesBuffer.Count; ++i)
            {
                CombatUnit unit = m_AllNikkesBuffer[i];
                m_AllyAppliedBuffer.Clear();
                for (int j = 0; j < allyEffects.Count; ++j)
                {
                    StatusEffectData effect = allyEffects[j];
                    ActiveStatusEffect existing = unit.FindEffect(effect);
                    if (existing != null)
                    {
                        if (effect.IsStackable && existing.CurrentStacks < effect.MaxStack)
                            existing.CurrentStacks++;
                        else
                            existing.RemainingTurns = Mathf.Max(existing.RemainingTurns, effect.Duration);
                    }
                    else
                        unit.AddEffect(new ActiveStatusEffect(effect));
                    m_AllyAppliedBuffer.Add(effect);
                }
                unit.RecalculateStats();
                if (m_AllyAppliedBuffer.Count > 0)
                {
                    AllyEffectResult allyResult = new AllyEffectResult();
                    allyResult.Unit = unit;
                    allyResult.AppliedEffects = m_AllyAppliedBuffer.ToArray();
                    m_AllyResultList.Add(allyResult);
                }
            }
            if (m_AllyResultList.Count > 0)
                finalResult.AllyResults = m_AllyResultList.ToArray();
        }

        finalResult.User = user;
        finalResult.Skill = skill;
        finalResult.TargetResults = result;
        if (selfEffects.Count > 0)
            finalResult.SelfAppliedEffects = selfEffects;
        return finalResult;
    }
    // 타겟 리스트 결정
    private void ResolveTargets(CombatUnit user, BaseSkillData skill, CombatUnit selectedTarget, List<CombatUnit> result)
    {
        result.Clear();
        // TargetType이 EnemyAll이면: 살아있는 모든 적 반환 (TargetPositions 무시)
        if (skill.TargetType == TargetType.EnemyAll)
        {
            CombatUnitType enemyType = (user.UnitType == CombatUnitType.Nikke) ? CombatUnitType.Enemy : CombatUnitType.Nikke;
            m_PositionSystem.GetAllTargetable(enemyType, result);
            return;
        }
        // TargetType이 AllyAll이면: 살아있는 모든 아군 반환 (TargetPositions 무시)
        else if (skill.TargetType == TargetType.AllyAll)
        {
            m_PositionSystem.GetAllTargetable(user.UnitType, result);
            return;
        }
        else if (skill.TargetType == TargetType.EnemySingle || skill.TargetType == TargetType.AllySingle)
        {
            if (selectedTarget != null)
                result.Add(selectedTarget);
            return;
        }
        else
            m_PositionSystem.GetValidTargets(user, skill, result);
        // TargetType이 EnemySingle 또는 AllySingle이면: selectedTarget을 리스트에 담아 반환
        // TargetType이 EnemyMulti, AllyMulti, Self이면: GetValidTargets 결과 반환
    }

    private float CalcHitChance(CombatUnit attacker, CombatUnit target, int accuracyMod)
    {
        float dodge = (target.State == UnitState.Corpse) ? 0f : target.CurrentStats.dodge;
        float result = (attacker.CurrentStats.accuracyMod + accuracyMod) - dodge;
        return result;
    }

    // 명중 판정: roll < (user.CurrentStats.accuracyMod + skill.AccuracyMod) - target.CurrentStats.dodge
    private bool RollHit(CombatUnit user, CombatUnit target, int accuracyMod)
    {
        float hitChance = CalcHitChance(user, target, accuracyMod);
        float roll = Random.Range(0f, 100f);
        return roll < hitChance;
    }


    private (int min, int max) CalcDamageRange(CombatUnit attacker, CombatUnit target, SkillLevelData ld)
    {
        float defence = (target.State == UnitState.Corpse) ? 0f : target.CurrentStats.defense;
        float damageMul = 1f + attacker.CurrentStats.damageMultiplier / 100f;
        int rawMin = (int)(attacker.CurrentStats.minDamage * ld.damageMultiplier * damageMul);
        int rawMax = (int)(attacker.CurrentStats.maxDamage * ld.damageMultiplier * damageMul);
        int min = Mathf.Max((int)(rawMin * (1f - defence / 100f)), 0);
        int max = Mathf.Max((int)(rawMax * (1f - defence / 100f)), 0);

        return (min, max);
    }
    // 데미지 계산: BaseDamage -> RawDamage -> finalDamage (defense % 감소)
    private int CalcDamage(CombatUnit user, CombatUnit target, SkillData skill, SkillLevelData ld)
    {
        (int min, int max) range = CalcDamageRange(user, target, ld);
        int finalDamage = Random.Range(range.min, range.max + 1);
        if (skill.MarkBonus && target.FindEffectByType(StatusEffectType.Mark) != null)
        {
            finalDamage = (int)(finalDamage * (1f + skill.MarkDamageBonus));
        }

        return finalDamage;
    }
    private float CalcCritChance(CombatUnit attacker, float critMod)
    {
        float critchance = attacker.CurrentStats.critChance + critMod;
        return critchance;
    }
    // 크리티컬 판정: roll < user.CurrentStats.critChance + skill.CritMod
    private bool RollCrit(CombatUnit user, float critMod)
    {
        float critchance = CalcCritChance(user, critMod);
        return Random.Range(0f, 100f) < critchance;
    }


    // 상태이상 저항 판정 -> target.ActiveEffects에 실제 추가 + applied/resisted 분류
    private void ApplyOnHitEffects(CombatUnit target, IReadOnlyList<StatusEffectData> effects,
                                   List<StatusEffectData> applied,
                                   List<StatusEffectData> resisted,
                                   bool isCrit)
    {
        float roll = 0f;

        for (int i = 0; i < effects.Count; ++i)
        {
            StatusEffectData effect = effects[i];
            float resistance = GetResistance(target, effect.EffectType);
            float effectiveResist = Mathf.Max(0f, resistance - (effect.BaseApplyRate - 100f));
            roll = Random.Range(0f, 100f);
            if (roll < effectiveResist)  // 저항 성공
                resisted.Add(effect);

            else
            {
                ActiveStatusEffect existing = null;

                if (effect.EffectType.IsDot())
                {
                    int duration = isCrit ? Mathf.CeilToInt(effect.Duration * 1.5f) : effect.Duration;
                    ActiveStatusEffect existingDot = target.FindEffectByType(effect.EffectType);
                    if (existingDot != null)
                    {
                        existingDot.AccumulatedTickDamage += effect.TickDamage;
                        existingDot.RemainingTurns = Mathf.Max(existingDot.RemainingTurns, duration);
                    }
                    else
                    {
                        target.AddEffect(new ActiveStatusEffect(effect, duration));
                    }
                }
                else
                {
                    existing = target.FindEffect(effect);
                    if (existing != null)
                    {
                        if (effect.IsStackable)
                        {
                            if (existing.CurrentStacks < effect.MaxStack)
                                existing.CurrentStacks++;
                        }
                        else
                        {
                            existing.RemainingTurns = Mathf.Max(existing.RemainingTurns, effect.Duration);
                        }
                    }
                    else
                    {
                        target.AddEffect(new ActiveStatusEffect(effect));
                    }
                }
                applied.Add(effect);
            }
        }
        if (applied.Count > 0)
            target.RecalculateStats();
    }

    // Buff/Guard/Mark -> 0 반환 (항상 적용), 나머지 -> resistance 블록 필드 반환
    private float GetResistance(CombatUnit target, StatusEffectType effectType)
    {
        // effectType이 Buff, Guard, Mark이면 0 반환 (항상 적용)
        switch (effectType)
        {
            case StatusEffectType.Burn:
                return target.CurrentStats.resistance.bleed;
            case StatusEffectType.Poison:
                return target.CurrentStats.resistance.poison;
            case StatusEffectType.Disease:
                return target.CurrentStats.resistance.disease;
            case StatusEffectType.Stun:
                return target.CurrentStats.resistance.stun;
            case StatusEffectType.Debuff:
                return target.CurrentStats.resistance.debuff;
            default:
                return 0;
        }
    }

    // 크리티컬 추가 효과: 적 에블라+15, 파티원 에블라-5, OnCritSelfEffects 적용
    private void ApplyCritEffects(CombatUnit user, CombatUnit target,
                                  List<CombatUnit> allNikkes)
    {

        if (user.UnitType == CombatUnitType.Nikke)
        {
            if (target.UnitType == CombatUnitType.Enemy)
                m_EblaSystem.ModifyEbla(target, CRIT_EBLA_TO_ENEMY);
            for (int i = 0; i < allNikkes.Count; ++i)
                m_EblaSystem.ModifyEbla(allNikkes[i], CRIT_EBLA_PARTY_HEAL);
        }
        else
        {
            if (target.UnitType == CombatUnitType.Nikke)
            {
                m_EblaSystem.ModifyEbla(target, CRIT_EBLA_TO_ENEMY);
                for (int i = 0; i < allNikkes.Count; ++i)
                    m_EblaSystem.ModifyEbla(allNikkes[i], 3);
            }
        }


        if (user.NikkeData != null)
        {
            for (int i = 0; i < user.NikkeData.OnCritSelfEffects.Count; ++i)
            {
                user.AddEffect(new ActiveStatusEffect(user.NikkeData.OnCritSelfEffects[i]));
            }
        }
        if (target.NikkeData != null)
        {
            for (int i = 0; i < target.NikkeData.OnReceiveCritSelfEffects.Count; ++i)
            {
                target.AddEffect(new ActiveStatusEffect(target.NikkeData.OnReceiveCritSelfEffects[i]));
            }
        }
    }

    // skill.MoveUserAmount -> user 이동, skill.MoveTargetAmount -> target 이동
    private void ApplyPositionMove(CombatUnit user, BaseSkillData skill, CombatUnit target, bool isHit)
    {
        if (skill.MoveUserAmount != 0)
        {
            m_PositionSystem.Move(user, skill.MoveUserAmount);
        }
        if (skill.MoveTargetAmount != 0 && isHit)
        {
            if (target.State == UnitState.Corpse || target.State == UnitState.Dead)
                return;
            float roll = Random.Range(0f, 100f);
            if (roll >= target.CurrentStats.resistance.move)
            {
                m_PositionSystem.Move(target, skill.MoveTargetAmount);
            }
        }
    }

    //  Enemy Info 용도
    public AttackPreview PreviewAttack(CombatUnit attacker, SkillData skill, int skillLevel, CombatUnit target)
    {
        AttackPreview preview = new AttackPreview();
        SkillLevelData ld = skill.GetLevelData(skillLevel);
        if (attacker == null)
            return preview;


        float hitChance = CalcHitChance(attacker, target, ld.accuracyMod);
        float critChance = CalcCritChance(attacker, ld.critMod);
        (int min, int max) range = CalcDamageRange(attacker, target, ld);
        preview.HitChance = hitChance;
        preview.CritChance = critChance;
        preview.MinDamage = range.min;
        preview.MaxDamage = range.max;

        return preview;
    }

    private void ApplyDeathsDoorDebuff(CombatUnit unit)
    {
        if (m_DeathsDoorDebuff == null) return;

        // recovery 제거 (있다면)
        unit.RemoveEffect(m_DeathsDoorRecovery);

        // debuff 부여
        unit.AddEffect(new ActiveStatusEffect(m_DeathsDoorDebuff));
        unit.RecalculateStats();
    }
    private void ApplyDeathsDoorRecovery(CombatUnit unit)
    {
        if (m_DeathsDoorDebuff == null) return;

        // debuff 제거
        unit.RemoveEffect(m_DeathsDoorDebuff);

        // recovery 부여
        if (m_DeathsDoorRecovery != null)
            unit.AddEffect(new ActiveStatusEffect(m_DeathsDoorRecovery));

        unit.RecalculateStats();
    }
    private CombatUnit ResolveGuardTarget(CombatUnit target, BaseSkillData skill, List<CombatUnit> originalTargets)
    {
        CombatUnit guardian = target.GuardedBy;
        if (skill.IsAllyTargeting || skill.BypassGuard) return target;
        else if (guardian == null || !guardian.IsAlive || guardian.IsStunned) return target;
        else if (originalTargets.Contains(guardian)) return target;
        else return guardian;
    }
    private void ApplyHit(CombatUnit user, CombatUnit target, SkillData skill, SkillLevelData ld, ref TargetResult result, ref bool allNikkesFetched)
    {
        result.PreviousHp = target.CurrentHp;

        // 직접 공격만 Block 소모 — 힐·아군 스킬은 통과
        if (ld.maxHeal <= 0 && !skill.IsAllyTargeting)
        {
            ActiveStatusEffect blockEffect = target.FindEffectByType(StatusEffectType.Block);
            if (blockEffect != null)
            {
                result.WasBlocked = true;
                blockEffect.CurrentStacks--;
                if (blockEffect.CurrentStacks <= 0)
                    target.RemoveEffect(blockEffect.Data);
                return;
            }
        }

        int damage = 0;
        if (ld.minHeal > 0)
            result.HealAmount = Random.Range(ld.minHeal, ld.maxHeal + 1);
        else
            damage = CalcDamage(user, target, skill, ld);

        result.IsCrit = RollCrit(user, ld.critMod);
        if (result.IsCrit)
        {
            damage = (int)(damage * CRIT_DAMAGE_MULTI);
            result.HealAmount = (int)(result.HealAmount * CRIT_DAMAGE_MULTI);
        }

        if (ld.maxHeal > 0)
        {
            UnitState preHealState = target.State;
            target.Heal(result.HealAmount);
            if (preHealState == UnitState.DeathsDoor && target.State == UnitState.Alive)
                ApplyDeathsDoorRecovery(target);
        }
        else
        {
            result.PreviousState = target.State;
            target.TakeDamage(damage, isCrit: result.IsCrit);
            result.DamageDealt = damage;

            if (result.PreviousState == UnitState.Alive && target.State == UnitState.DeathsDoor)
            {
                m_EblaSystem.ModifyEbla(target, CombatUnit.DEATHS_DOOR_EBLA);
                ApplyDeathsDoorDebuff(target);
            }
        }

        if (ld.eblaDamage > 0)
            m_EblaSystem.ModifyEbla(target, ld.eblaDamage);
        if (ld.eblaHealAmount > 0)
            m_EblaSystem.ModifyEbla(target, -ld.eblaHealAmount);

        if (target.IsAlive)
            ApplyOnHitEffects(target, ld.onHitEffects ?? System.Array.Empty<StatusEffectData>(), m_AppliedBuffer, m_ResistedBuffer, result.IsCrit);

        if (result.IsCrit)
        {
            if (!allNikkesFetched)
            {
                m_PositionSystem.GetAllUnits(CombatUnitType.Nikke, m_AllNikkesBuffer);
                allNikkesFetched = true;
            }
            ApplyCritEffects(user, target, m_AllNikkesBuffer);
        }

        if (skill.IsGuard)
        {
            user.Protecting?.SetGuardedBy(null, 0);
            user.SetProtecting(target);
            target.SetGuardedBy(user, skill.GuardDuration);
        }
        else if (skill.IsForceGuard)
        {
            target.Protecting?.SetGuardedBy(null, 0);
            target.SetProtecting(user);
            user.SetGuardedBy(target, skill.GuardDuration);
        }
    }
    private void ApplyAllyEffects(CombatUnit user, SkillData skill, SkillLevelData ld)
    {
        m_AllyResultList.Clear();
        if ((ld.onAllyEffects == null || ld.onAllyEffects.Length == 0) && ld.allyEblaAmount == 0)
            return;
        m_PositionSystem.GetAllTargetable(user.UnitType, m_AllNikkesBuffer);

        for (int i = 0; i < m_AllNikkesBuffer.Count; ++i)
        {
            CombatUnit unit = m_AllNikkesBuffer[i];
            if (skill.ExcludeAllyEffect && unit == user) continue;

            m_AllyAppliedBuffer.Clear();

            if (ld.allyEblaAmount != 0)
                m_EblaSystem.ModifyEbla(unit, ld.allyEblaAmount);

            if (ld.onAllyEffects != null && ld.onAllyEffects.Length > 0)
            {
                for (int j = 0; j < ld.onAllyEffects.Length; ++j)
                {
                    StatusEffectData effect = ld.onAllyEffects[j];
                    ActiveStatusEffect existing = unit.FindEffect(effect);
                    if (existing != null)
                    {
                        if (effect.IsStackable && existing.CurrentStacks < effect.MaxStack)
                            existing.CurrentStacks++;
                        else
                            existing.RemainingTurns = Mathf.Max(existing.RemainingTurns, effect.Duration);
                    }
                    else
                        unit.AddEffect(new ActiveStatusEffect(effect));
                    m_AllyAppliedBuffer.Add(effect);
                }
                unit.RecalculateStats();
            }

            if (m_AllyAppliedBuffer.Count > 0)
            {
                AllyEffectResult allyResult = new AllyEffectResult();
                allyResult.Unit = unit;
                allyResult.AppliedEffects = m_AllyAppliedBuffer.ToArray();
                m_AllyResultList.Add(allyResult);
            }
        }
    }
}
