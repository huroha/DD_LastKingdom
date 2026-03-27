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

    private const int CRIT_EBLA_TO_ENEMY = 15;
    private const int CRIT_EBLA_PARTY_HEAL = -5;
    private const float CRIT_DAMAGE_MULTI = 1.5f;

    // 리스트 버퍼들
    private List<CombatUnit> m_TargetsBuffer = new List<CombatUnit>();
    private List<CombatUnit> m_AllNikkesBuffer = new List<CombatUnit>();
    private List<StatusEffectData> m_AppliedBuffer = new List<StatusEffectData>();
    private List<StatusEffectData> m_ResistedBuffer = new List<StatusEffectData>();


    public SkillExecutor(PositionSystem positionSystem, EblaSystem eblaSystem, StatusEffectData deathsDoorDebuff, StatusEffectData deathsDoorRecovery)
    {
        m_PositionSystem = positionSystem;
        m_EblaSystem = eblaSystem;
        m_DeathsDoorDebuff = deathsDoorDebuff;
        m_DeathsDoorRecovery = deathsDoorRecovery;
    }

    public bool ValidateSkill(CombatUnit user, SkillData skill)
    {
        // 유저가 살아있지 않으면 false
        if (!user.IsAlive)
            return false;

        // 현재 위치에서 스킬 사용이 불가능하면 false
        if (!m_PositionSystem.CanUseSkill(user, skill))
            return false;
        // 각성 스킬이면 flase
        if (skill.RequiredState == SkillRequiredState.Awakened)
            return false;
        m_PositionSystem.GetValidTargets(user, skill,m_TargetsBuffer);
        if (m_TargetsBuffer.Count == 0)
            return false;

        return true;
    }

    public SkillResult Execute(CombatUnit user, SkillData skill, CombatUnit selectedTarget = null)
    {
        // ValidateSkill 실패 시 빈 SkillResult 반환 (early return)
        SkillResult finalResult;

        if (!ValidateSkill(user, skill))
            return new SkillResult();
        // ResolveTargets로 최종 타겟 리스트 결정
        ResolveTargets(user, skill, selectedTarget, m_TargetsBuffer);
        List<CombatUnit> targets = m_TargetsBuffer;

        // 크리티컬 파티 에블라용
        bool allNikkesFetched = false;
        //m_PositionSystem.GetAllUnits(CombatUnitType.Nikke);
        bool isHealSkill = skill.HealAmount > 0;
        // TargetResult 배열 선언 (targets.Count 크기)
        TargetResult[] result = new TargetResult[targets.Count];


        // for 루프: 각 타겟에 대해
        for (int i = 0; i < result.Length; ++i)
        {
            m_AppliedBuffer.Clear();
            m_ResistedBuffer.Clear();

            // [명중 판정]
            if (isHealSkill)
                result[i].IsHit = true;
            else
                result[i].IsHit = RollHit(user, targets[i], skill);
                
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Skill] {user.UnitName} → {targets[i].UnitName} | Hit:{result[i].IsHit}");
            #endif
            // [명중 성공 시]
            if (result[i].IsHit)
            {
                int damage = 0;
                // [데미지/힐 계산]
                if (skill.HealAmount > 0)
                    result[i].HealAmount = skill.HealAmount;
                else
                    damage = CalcDamage(user, targets[i], skill);

                // [크리티컬 판정] RollCrit 호출
                result[i].IsCrit = RollCrit(user, skill);
                if (result[i].IsCrit)
                {
                    // 크리티컬이면: 데미지 또는 힐량에 CRIT_DAMAGE_MULTI 적용 (int 캐스팅)
                    damage = (int)(damage * CRIT_DAMAGE_MULTI);
                    result[i].HealAmount = (int)(result[i].HealAmount * CRIT_DAMAGE_MULTI);
                }

                // [적용]
                // 힐 스킬이면: target.Heal(healAmount)
                if (skill.HealAmount > 0)
                {
                    UnitState preHealState = targets[i].State;
                    targets[i].Heal(result[i].HealAmount);
                    if (preHealState == UnitState.DeathsDoor && targets[i].State == UnitState.Alive)
                        ApplyDeathsDoorRecovery(targets[i]);
                }
                // 아니면: target.TakeDamage(damage)
                else
                {
                    result[i].PreviousState = targets[i].State;
                    targets[i].TakeDamage(damage);
                    result[i].DamageDealt = damage;

                    if (result[i].PreviousState == UnitState.Alive && targets[i].State == UnitState.DeathsDoor)
                    {
                        m_EblaSystem.ModifyEbla(targets[i], CombatUnit.DEATHS_DOOR_EBLA);
                        ApplyDeathsDoorDebuff(targets[i]);
                    }
                }
                // [에블라]
                // skill.EblaDamage > 0 이면: target.AddEbla(skill.EblaDamage)
                if (skill.EblaDamage > 0)
                    m_EblaSystem.ModifyEbla(targets[i], skill.EblaDamage);
                // skill.EblaHealAmount > 0 이면: target.AddEbla(-skill.EblaHealAmount)
                if (skill.EblaHealAmount > 0)
                    m_EblaSystem.ModifyEbla(targets[i], -skill.EblaHealAmount);


                // [상태이상] ApplyOnHitEffects 호출 (applied, resisted 리스트 준비)
                ApplyOnHitEffects(targets[i], skill, m_AppliedBuffer, m_ResistedBuffer, result[i].IsCrit);

                // [크리티컬 추가 효과] 크리티컬이면 ApplyCritEffects 호출
                if (result[i].IsCrit)
                {
                    if (!allNikkesFetched)
                    {
                        m_PositionSystem.GetAllUnits(CombatUnitType.Nikke, m_AllNikkesBuffer);
                        allNikkesFetched = true;
                    }
                    ApplyCritEffects(user, targets[i], m_AllNikkesBuffer);
                }
            }

            // [위치 이동] ApplyPositionMove 호출 (명중 여부 무관)
            ApplyPositionMove(user, skill, targets[i], result[i].IsHit);

            // TargetResult 채우기
            // - applied/resisted List → 배열로 변환 (for문 복사)
            // - results[i] 할당
            result[i].Target = targets[i];
            result[i].ResultState = targets[i].State;
            result[i].AppliedEffects = new StatusEffectData[m_AppliedBuffer.Count];
            for (int j = 0; j < m_AppliedBuffer.Count; ++j)
                result[i].AppliedEffects[j] = m_AppliedBuffer[j];
            result[i].ResistedEffects = new StatusEffectData[m_ResistedBuffer.Count];
            for (int j = 0; j < m_ResistedBuffer.Count; ++j)
                result[i].ResistedEffects[j] = m_ResistedBuffer[j];
        }

        // SkillResult 반환 (User, Skill, TargetResults 채워서)
        finalResult.User = user;
        finalResult.Skill = skill;
        finalResult.TargetResults = result;
        return finalResult;
    }

    // 타겟 리스트 결정
    private void ResolveTargets(CombatUnit user, SkillData skill, CombatUnit selectedTarget, List<CombatUnit> result)
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

    private float CalcHitChance(CombatUnit attacker, CombatUnit target, SkillData skill)
    {
        float dodge = (target.State == UnitState.Corpse) ? 0f : target.CurrentStats.dodge;
        float result = (attacker.CurrentStats.accuracyMod + skill.AccuracyMod) - dodge;
        return result;
    }

    // 명중 판정: roll < (user.CurrentStats.accuracyMod + skill.AccuracyMod) - target.CurrentStats.dodge
    private bool RollHit(CombatUnit user, CombatUnit target, SkillData skill)
    {
        float hitChance = CalcHitChance(user, target, skill);
        float roll = Random.Range(0f, 100f);
        return roll < hitChance;
    }


    private (int min, int max) CalcDamageRange(CombatUnit attacker, CombatUnit target, SkillData skill)
    {
        float defence = (target.State == UnitState.Corpse) ? 0f : target.CurrentStats.defense;
        float damageMul = 1f + attacker.CurrentStats.damageMultiplier / 100f;
        int rawMin = (int)(attacker.CurrentStats.minDamage * skill.DamageMultiplier * damageMul);
        int rawMax = (int)(attacker.CurrentStats.maxDamage * skill.DamageMultiplier * damageMul);
        int min = Mathf.Max((int)(rawMin * (1f - defence / 100f)), 0);
        int max = Mathf.Max((int)(rawMax * (1f - defence / 100f)), 0);

        return (min, max);
    }
    // 데미지 계산: BaseDamage → RawDamage → finalDamage (defense % 감소)
    private int CalcDamage(CombatUnit user, CombatUnit target, SkillData skill)
    {
        (int min, int max) range = CalcDamageRange(user, target, skill);
        int finalDamage = Random.Range(range.min, range.max + 1);

        return finalDamage;
    }
    private float CalcCritChance(CombatUnit attacker, SkillData skill)
    {
        float critchance = attacker.CurrentStats.critChance + skill.CritMod;
        return critchance;
    }
    // 크리티컬 판정: roll < user.CurrentStats.critChance + skill.CritMod
    private bool RollCrit(CombatUnit user, SkillData skill)
    {
        float critchance = CalcCritChance(user, skill);
        return Random.Range(0f, 100f) < critchance;
    }


    // 상태이상 저항 판정 → target.ActiveEffects에 실제 추가 + applied/resisted 분류
    private void ApplyOnHitEffects(CombatUnit target, SkillData skill,
                                   List<StatusEffectData> applied,
                                   List<StatusEffectData> resisted,
                                   bool isCrit)
    {
        float roll = 0f;
        // skill.OnHitEffects를 for 루프로 순회
        for (int i = 0; i < skill.OnHitEffects.Count; ++i)
        {
            StatusEffectData effect = skill.OnHitEffects[i];
            float resistance = GetResistance(target, effect.EffectType);
            roll = Random.Range(0f, 100f);

            //저항 성공
            if (roll < resistance)
                resisted.Add(effect);

            else
            {
                ActiveStatusEffect existing = null;

                if (effect.EffectType.IsDot())
                {
                    int duration = isCrit ? Mathf.CeilToInt(effect.Duration * 1.5f) : effect.Duration;
                    target.AddEffect(new ActiveStatusEffect(effect, duration));
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
    }

    // Buff/Guard/Mark → 0 반환 (항상 적용), 나머지 → resistance 블록 필드 반환
    private float GetResistance(CombatUnit target, StatusEffectType effectType)
    {
        // effectType이 Buff, Guard, Mark이면 0 반환 (항상 적용)
        switch (effectType)
        {
            case StatusEffectType.Bleed:
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
                    m_EblaSystem.ModifyEbla(allNikkes[i], 5);
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

    // skill.MoveUserAmount → user 이동, skill.MoveTargetAmount → target 이동
    private void ApplyPositionMove(CombatUnit user, SkillData skill, CombatUnit target, bool isHit)
    {
        if (skill.MoveUserAmount != 0)
        {
            if (m_PositionSystem.Move(user, skill.MoveUserAmount))
                EventBus.Publish(new UnitMovedEvent(user, user));
        }
        if (skill.MoveTargetAmount != 0 && isHit)
        {
            if (target.State == UnitState.Corpse || target.State == UnitState.Dead)
                return;
            float roll = Random.Range(0f, 100f);
            if (roll >= target.CurrentStats.resistance.move)
            {
                if (m_PositionSystem.Move(target, skill.MoveTargetAmount))
                    EventBus.Publish(new UnitMovedEvent(target, target));
            }
        }
    }

    //  Enemy Info 용도
    public AttackPreview PreviewAttack(CombatUnit attacker, SkillData skill, CombatUnit target)
    {
        AttackPreview preview = new AttackPreview();
        if (attacker == null)
            return preview;


        float hitChance = CalcHitChance(attacker, target, skill);
        float critChance = CalcCritChance(attacker, skill);
        (int min, int max) range = CalcDamageRange(attacker, target, skill);
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

}
