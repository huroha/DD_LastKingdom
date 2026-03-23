# EblaSystem Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 에블라 수치 임계값 판정 시스템 구현 — 100 도달 시 Affliction 스탯 디버프, 200 도달 시 영구 사망

**Architecture:** EblaSystem은 Pure C# 클래스로 CombatStateMachine이 소유. 모든 에블라 변경은 EblaSystem.ModifyEbla()를 경유하여 임계값 판정을 중앙 관리. Affliction 디버프는 기존 ActiveEffects 파이프라인(StatusEffectData SO)을 활용.

**Tech Stack:** Unity 6, C#, ScriptableObject, EventBus

**Spec:** `docs/superpowers/specs/2026-03-23-ebla-system-design.md`

---

## File Map

| 파일 | 작업 | 책임 |
|------|------|------|
| `Assets/Scripts/Data/StatBlock.cs` | Modify | Apply 메서드 추가 (StatModifier 합산) |
| `Assets/Scripts/Combat/CombatUnit.cs` | Modify | EblaState enum/프로퍼티, RecalculateStats 확장, TakeDamage에서 AddEbla 제거 |
| `Assets/Scripts/Systems/EblaSystem.cs` | Create | 에블라 임계값 판정 (100=Affliction, 200=영구사망) |
| `Assets/Scripts/Combat/CombatEvent.cs` | Modify | AfflictionTriggeredEvent, PermanentDeathEvent 추가 |
| `Assets/Scripts/Combat/SkillExecutor.cs` | Modify | EblaSystem 참조 주입, AddEbla→ModifyEbla 전환, DeathsDoor 에블라 외부 처리 |
| `Assets/Scripts/Combat/CombatStateMachine.cs` | Modify | EblaSystem 소유/생성, AddEbla→ModifyEbla 전환, PermanentDeath 처리 |
| `Assets/ScriptableObjects/StatusEffects/Affliction_Debuff.asset` | Create (Inspector) | Affliction 디버프 SO 에셋 |

---

## Task 1: StatBlock.Apply 메서드 추가

**Files:**
- Modify: `Assets/Scripts/Data/StatBlock.cs`

RecalculateStats()에서 ActiveEffects의 StatModifier를 합산할 때 사용할 메서드.

- [ ] **Step 1: StatBlock에 Apply 메서드 추가**

```csharp
// StatBlock struct 내부에 추가
public StatBlock Apply(StatBlock modifier)
{
    StatBlock result;
    result.maxHp = maxHp + modifier.maxHp;
    result.minDamage = minDamage + modifier.minDamage;
    result.maxDamage = maxDamage + modifier.maxDamage;
    result.accuracyMod = accuracyMod + modifier.accuracyMod;
    result.critChance = critChance + modifier.critChance;
    result.defense = defense + modifier.defense;
    result.dodge = dodge + modifier.dodge;
    result.deathBlowResist = deathBlowResist + modifier.deathBlowResist;
    result.speed = speed + modifier.speed;
    result.moveRange = moveRange + modifier.moveRange;

    result.resistance.stun = resistance.stun + modifier.resistance.stun;
    result.resistance.move = resistance.move + modifier.resistance.move;
    result.resistance.poison = resistance.poison + modifier.resistance.poison;
    result.resistance.disease = resistance.disease + modifier.resistance.disease;
    result.resistance.bleed = resistance.bleed + modifier.resistance.bleed;
    result.resistance.debuff = resistance.debuff + modifier.resistance.debuff;
    result.resistance.trap = resistance.trap + modifier.resistance.trap;

    return result;
}
```

- [ ] **Step 2: 저장 후 Unity 컴파일 확인**

Unity Editor에서 컴파일 에러 없음 확인.

- [ ] **Step 3: 커밋**

```
feat: StatBlock에 Apply 메서드 추가 (StatModifier 합산용)
```

---

## Task 2: CombatUnit — EblaState enum 및 RecalculateStats 확장

**Files:**
- Modify: `Assets/Scripts/Combat/CombatUnit.cs`

- [ ] **Step 1: EblaState enum 선언**

CombatUnit.cs 파일 상단, `UnitState` enum 아래에 추가:

```csharp
public enum EblaState
{
    Normal,
    Afflicted
    // 추후 Virtuous 추가 예정 (Phase 3)
}
```

- [ ] **Step 2: CombatUnit에 EblaState 프로퍼티 추가**

`Ebla` 프로퍼티 아래에 추가:

```csharp
public EblaState EblaState { get; private set; }

public void SetEblaState(EblaState state)
{
    EblaState = state;
}
```

- [ ] **Step 3: RecalculateStats() 확장**

기존 placeholder를 교체:

```csharp
public void RecalculateStats()
{
    StatBlock stats = BaseStats;
    for (int i = 0; i < ActiveEffects.Count; ++i)
    {
        stats = stats.Apply(ActiveEffects[i].Data.StatModifier);
    }
    CurrentStats = stats;
}
```

- [ ] **Step 4: 저장 후 Unity 컴파일 확인**

- [ ] **Step 5: 커밋**

```
feat: CombatUnit에 EblaState 추가, RecalculateStats에서 ActiveEffects StatModifier 합산
```

---

## Task 3: 이벤트 struct 추가

**Files:**
- Modify: `Assets/Scripts/Combat/CombatEvent.cs`

- [ ] **Step 1: 이벤트 2종 추가**

CombatEvent.cs 하단에 추가:

```csharp
public struct AfflictionTriggeredEvent
{
    public CombatUnit Unit;
    public AfflictionTriggeredEvent(CombatUnit unit) { Unit = unit; }
}

public struct PermanentDeathEvent
{
    public CombatUnit Unit;
    public PermanentDeathEvent(CombatUnit unit) { Unit = unit; }
}
```

- [ ] **Step 2: 저장 후 Unity 컴파일 확인**

- [ ] **Step 3: 커밋**

```
feat: AfflictionTriggeredEvent, PermanentDeathEvent 이벤트 추가
```

---

## Task 4: EblaSystem 클래스 구현

**Files:**
- Create: `Assets/Scripts/Systems/EblaSystem.cs`

핵심 클래스. ModifyEbla()에서 수치 변경 + 100/200 임계값 판정.

- [ ] **Step 1: EblaSystem 클래스 뼈대 작성**

```csharp
using UnityEngine;

public class EblaSystem
{
    private StatusEffectData m_AfflictionDebuff;

    public EblaSystem(StatusEffectData afflictionDebuff)
    {
        m_AfflictionDebuff = afflictionDebuff;
    }

    // 에블라 수치 변경 + 임계값 판정
    // 반환값: true = 200 도달로 사망
    public bool ModifyEbla(CombatUnit unit, int amount)
    {
        // Nikke가 아니면 무시
        // Dead면 무시
        // previousEbla 저장
        // unit.AddEbla(amount) 호출
        // 200 도달 체크 → Kill + PermanentDeathEvent, return true
        // 100 도달 체크 → Affliction 발동
        // 100 미만 복귀 체크 → Affliction 해제
        // return false
    }

    private void TriggerAffliction(CombatUnit unit)
    {
        // EblaState 변경
        // ActiveEffects에 Affliction 디버프 추가
        // RecalculateStats
        // 이벤트 발행
    }

    private void RemoveAffliction(CombatUnit unit)
    {
        // ActiveEffects에서 Affliction 디버프 제거
        // EblaState 변경
        // RecalculateStats
    }
}
```

- [ ] **Step 2: ModifyEbla 구현**

```csharp
public bool ModifyEbla(CombatUnit unit, int amount)
{
    if (unit.UnitType != CombatUnitType.Nikke)
        return false;
    if (!unit.IsAlive)
        return false;

    int previousEbla = unit.Ebla;
    unit.AddEbla(amount);

    // 200 도달 — 영구 사망 (최우선)
    if (unit.Ebla >= 200)
    {
        unit.Kill();
        EventBus.Publish(new PermanentDeathEvent(unit));
        return true;
    }

    // 100 도달 — Affliction 발동
    if (previousEbla < 100 && unit.Ebla >= 100)
    {
        TriggerAffliction(unit);
    }
    // 100 미만 복귀 — Affliction 해제
    else if (previousEbla >= 100 && unit.Ebla < 100)
    {
        RemoveAffliction(unit);
    }

    return false;
}
```

- [ ] **Step 3: TriggerAffliction / RemoveAffliction 구현**

```csharp
private void TriggerAffliction(CombatUnit unit)
{
    unit.SetEblaState(EblaState.Afflicted);
    unit.ActiveEffects.Add(new ActiveStatusEffect(m_AfflictionDebuff));
    unit.RecalculateStats();
    EventBus.Publish(new AfflictionTriggeredEvent(unit));
}

private void RemoveAffliction(CombatUnit unit)
{
    for (int i = unit.ActiveEffects.Count - 1; i >= 0; --i)
    {
        if (unit.ActiveEffects[i].Data == m_AfflictionDebuff)
        {
            unit.ActiveEffects.RemoveAt(i);
            break;
        }
    }
    unit.SetEblaState(EblaState.Normal);
    unit.RecalculateStats();
}
```

- [ ] **Step 4: 저장 후 Unity 컴파일 확인**

- [ ] **Step 5: 커밋**

```
feat: EblaSystem 구현 — ModifyEbla, Affliction 발동/해제, 영구 사망
```

---

## Task 5: CombatUnit.TakeDamage에서 DeathsDoor 에블라 제거

**Files:**
- Modify: `Assets/Scripts/Combat/CombatUnit.cs`

TakeDamage() 내부의 `AddEbla(DEATHS_DOOR_EBLA)` 호출을 제거. DeathsDoor 에블라는 이제 호출자(SkillExecutor)가 State 비교 후 EblaSystem 경유로 처리.

- [ ] **Step 1: TakeDamage에서 AddEbla 제거**

`CombatUnit.cs`의 TakeDamage 메서드에서 아래 줄 제거:

```csharp
// 제거할 줄 (현재 line 118):
AddEbla(DEATHS_DOOR_EBLA);
```

DeathsDoor 진입 시 `State = UnitState.DeathsDoor`만 설정하고, 에블라 추가는 하지 않음.

- [ ] **Step 2: DEATHS_DOOR_EBLA 상수를 public const로 변경**

SkillExecutor에서 참조해야 하므로:

```csharp
// private → public
public const int DEATHS_DOOR_EBLA = 18;
```

- [ ] **Step 3: 저장 후 Unity 컴파일 확인**

- [ ] **Step 4: 커밋**

```
refactor: TakeDamage에서 DeathsDoor 에블라 제거 — 호출자에서 EblaSystem 경유로 처리
```

---

## Task 6: SkillExecutor — EblaSystem 연동

**Files:**
- Modify: `Assets/Scripts/Combat/SkillExecutor.cs`

EblaSystem 참조 주입. 모든 AddEbla → ModifyEbla 전환. DeathsDoor 에블라 외부 처리 추가.

- [ ] **Step 1: 생성자에 EblaSystem 추가**

```csharp
private EblaSystem m_EblaSystem;

public SkillExecutor(PositionSystem positionSystem, EblaSystem eblaSystem)
{
    m_PositionSystem = positionSystem;
    m_EblaSystem = eblaSystem;
}
```

- [ ] **Step 2: Execute() 내 에블라 처리 변경**

Execute() 메서드의 for 루프 내부, `[에블라]` 섹션을 변경:

기존:
```csharp
if (skill.EblaDamage > 0)
    targets[i].AddEbla(skill.EblaDamage);
if (skill.EblaHealAmount > 0)
    targets[i].AddEbla(-skill.EblaHealAmount);
```

변경:
```csharp
if (skill.EblaDamage > 0)
    m_EblaSystem.ModifyEbla(targets[i], skill.EblaDamage);
if (skill.EblaHealAmount > 0)
    m_EblaSystem.ModifyEbla(targets[i], -skill.EblaHealAmount);
```

- [ ] **Step 3: Execute() 내 DeathsDoor 에블라 처리 추가**

TakeDamage 호출 직후, 에블라 처리 전에 DeathsDoor 진입 체크 추가:

```csharp
// 기존 TakeDamage 호출 후:
result[i].PreviousState = targets[i].State;
targets[i].TakeDamage(damage);
result[i].DamageDealt = damage;

// DeathsDoor 진입 체크 추가 (이 줄 추가):
if (result[i].PreviousState == UnitState.Alive && targets[i].State == UnitState.DeathsDoor)
    m_EblaSystem.ModifyEbla(targets[i], CombatUnit.DEATHS_DOOR_EBLA);
```

- [ ] **Step 4: ApplyCritEffects 내 에블라 변경**

기존 `AddEbla` 호출을 모두 `m_EblaSystem.ModifyEbla`로 교체:

```csharp
// 기존: target.AddEbla(CRIT_EBLA_TO_ENEMY);
// 변경:
m_EblaSystem.ModifyEbla(target, CRIT_EBLA_TO_ENEMY);

// 기존: allNikkes[i].AddEbla(CRIT_EBLA_PARTY_HEAL);
// 변경:
m_EblaSystem.ModifyEbla(allNikkes[i], CRIT_EBLA_PARTY_HEAL);

// 기존: allNikkes[i].AddEbla(5);
// 변경:
m_EblaSystem.ModifyEbla(allNikkes[i], 5);
```

- [ ] **Step 5: 저장 후 Unity 컴파일 확인**

컴파일 에러 예상: CombatStateMachine에서 SkillExecutor 생성자 호출이 아직 변경되지 않아 에러 발생 가능. Task 7에서 수정.

- [ ] **Step 6: 커밋**

```
feat: SkillExecutor에 EblaSystem 연동 — AddEbla→ModifyEbla 전환, DeathsDoor 에블라 외부 처리
```

---

## Task 7: CombatStateMachine — EblaSystem 소유 및 연동

**Files:**
- Modify: `Assets/Scripts/Combat/CombatStateMachine.cs`

EblaSystem 인스턴스 생성/소유. 기존 AddEbla 직접 호출을 ModifyEbla로 전환.

- [ ] **Step 1: SerializeField 및 멤버 추가**

CSM 클래스 상단 필드 영역에 추가:

```csharp
[Header("Ebla System")]
[SerializeField] private StatusEffectData m_AfflictionDebuff;

private EblaSystem m_EblaSystem;
```

- [ ] **Step 2: StartBattle()에서 EblaSystem 생성 및 SkillExecutor에 전달**

```csharp
// 기존:
m_SkillExecutor = new SkillExecutor(m_PositionSystem);

// 변경:
m_EblaSystem = new EblaSystem(m_AfflictionDebuff);
m_SkillExecutor = new SkillExecutor(m_PositionSystem, m_EblaSystem);
```

- [ ] **Step 3: HandlePlayerTurn — 패스 에블라 변경 + 200 사망 처리**

기존:
```csharp
m_ActiveUnit.AddEbla(PASS_EBLA_PENALTY);
turnHandled = true;
```

변경:
```csharp
if (m_EblaSystem.ModifyEbla(m_ActiveUnit, PASS_EBLA_PENALTY))
{
    m_PositionSystem.RemoveUnit(m_ActiveUnit);
    EventBus.Publish(new UnitDiedEvent(m_ActiveUnit));
}
turnHandled = true;
```

- [ ] **Step 4: ApplyAllyDeathEbla — 아군 사망 에블라 변경 + 200 사망 처리**

기존:
```csharp
nikkes[i].AddEbla(ALLY_DEATH_EBLA);
```

변경 — **연쇄 사망 방지**: 아군 사망 에블라로 다른 니케가 200 도달해도 추가 연쇄 에블라는 발생하지 않음. 한 번의 사망 이벤트에 대해 1회만 처리.
```csharp
private void ApplyAllyDeathEbla()
{
    List<CombatUnit> nikkes = m_PositionSystem.GetAllUnits(CombatUnitType.Nikke);
    // 사망 처리를 위해 역순 순회
    for (int i = nikkes.Count - 1; i >= 0; --i)
    {
        if (m_EblaSystem.ModifyEbla(nikkes[i], ALLY_DEATH_EBLA))
        {
            m_PositionSystem.RemoveUnit(nikkes[i]);
            EventBus.Publish(new UnitDiedEvent(nikkes[i]));
            // 연쇄 에블라 없음 — 이 루프에서 추가 ApplyAllyDeathEbla 호출하지 않음
        }
    }
}
```

- [ ] **Step 5: ApplyPostBattleEbla — 후반전 에블라 변경 + 200 사망 처리**

기존:
```csharp
nikkes[i].AddEbla(total);
```

변경 — 승리 직후 에블라로 사망 가능 (의도된 동작: 전투는 이겼지만 니케를 잃을 수 있음):
```csharp
private void ApplyPostBattleEbla()
{
    int roundCheck = m_TurnManager.RoundNumber;
    if (m_EblaFreeRounds > roundCheck)
        return;
    int total = 0;
    for (int i = m_EblaFreeRounds + 1; i <= roundCheck; ++i)
    {
        total += i * m_EblaRoundMultiplier;
    }

    List<CombatUnit> nikkes = m_PositionSystem.GetAllUnits(CombatUnitType.Nikke);
    for (int i = nikkes.Count - 1; i >= 0; --i)
    {
        if (m_EblaSystem.ModifyEbla(nikkes[i], total))
        {
            m_PositionSystem.RemoveUnit(nikkes[i]);
            EventBus.Publish(new UnitDiedEvent(nikkes[i]));
        }
    }
}
```

- [ ] **Step 6: ProcessDeadUnits 후 에블라 200 사망 확인**

`ProcessDeadUnits()`는 TargetResult의 최종 State를 보므로 SkillExecutor 경로의 에블라 200 사망은 자동 감지됨. 별도 수정 불필요.
다만, SkillExecutor.Execute() 내에서 ModifyEbla가 true를 반환하는 경우(DeathsDoor 에블라 → 200 도달) unit.Kill()로 State가 Dead가 되므로, ProcessDeadUnits에서 정상 처리됨을 확인.

- [ ] **Step 7: 저장 후 Unity 컴파일 확인**

Task 6의 컴파일 에러가 이제 해소되어야 함. 전체 컴파일 에러 0 확인.

- [ ] **Step 8: 커밋**

```
feat: CombatStateMachine에 EblaSystem 소유 — 모든 에블라 변경 경로를 EblaSystem 경유로 전환, 200 사망 처리 포함
```

---

## Task 8: Affliction_Debuff SO 에셋 생성 및 테스트

**Files:**
- Create (Inspector): `Assets/ScriptableObjects/StatusEffects/Affliction_Debuff.asset`
- Modify (Inspector): CombatScene의 CombatManager 오브젝트 — m_AfflictionDebuff 할당

- [ ] **Step 1: Affliction_Debuff SO 에셋 생성**

Unity Editor에서:
1. `Assets/ScriptableObjects/StatusEffects/` 폴더 우클릭
2. Create → LastKingdom → Status Effect Data
3. 이름: `Affliction_Debuff`
4. Inspector에서 설정:
   - m_EffectName: "Affliction"
   - m_EffectType: Debuff
   - m_Duration: -1
   - m_TickDamage: 0
   - m_StatModifier: accuracyMod = -10, dodge = -5, speed = -1 (나머지 0)
   - m_IsStackable: false
   - m_MaxStack: 1

- [ ] **Step 2: CombatManager에 SO 할당**

CombatScene → CombatManager 오브젝트 → CombatStateMachine 컴포넌트:
- m_AfflictionDebuff 필드에 `Affliction_Debuff` SO 드래그 앤 드롭

- [ ] **Step 3: 플레이 테스트 — Affliction 발동**

1. 테스트 니케의 에블라를 90 정도로 시작하도록 `StartTestBattle()`의 ebla 파라미터를 임시 변경 (또는 에블라 데미지 스킬을 적이 사용하도록 설정)
2. 전투 중 에블라가 100에 도달하면:
   - Console에 `AfflictionTriggeredEvent` 관련 로그 확인 (EventBus 구독 로그 추가 가능)
   - 해당 니케의 CurrentStats가 BaseStats에서 ACC -10, DODGE -5, SPD -1만큼 변경됨 확인
   - EblaState가 Afflicted로 변경됨 확인

- [ ] **Step 4: 플레이 테스트 — 영구 사망 (에블라 200)**

1. 테스트 니케의 에블라를 190 이상으로 설정
2. 에블라가 200에 도달하면:
   - 해당 니케 즉시 사망 (UnitState.Dead)
   - Console에 `PermanentDeathEvent` 확인
   - 유닛이 포지션에서 제거됨 확인

- [ ] **Step 5: 플레이 테스트 — DeathsDoor 에블라**

1. HP가 낮은 니케가 피격으로 DeathsDoor에 진입
2. 에블라가 +18 증가하는지 확인 (기존 동작과 동일)

- [ ] **Step 6: 테스트용 임시 변경 원복**

StartTestBattle()의 ebla 파라미터를 0으로 복원.

- [ ] **Step 7: 커밋**

```
feat: Affliction_Debuff SO 에셋 생성, CombatManager 연결, EblaSystem 플레이 테스트 완료
```

---

## 구현 순서 요약

```
Task 1: StatBlock.Apply       ← 의존성 없음
Task 2: CombatUnit 변경       ← Task 1 필요 (RecalculateStats에서 Apply 사용)
Task 3: 이벤트 struct         ← 의존성 없음 (Task 1과 병렬 가능)
Task 4: EblaSystem 구현       ← Task 2, 3 필요
Task 5: TakeDamage 수정       ← 의존성 없음 (Task 4와 병렬 가능)
Task 6: SkillExecutor 연동    ← Task 4, 5 필요
Task 7: CSM 연동              ← Task 4, 6 필요
Task 8: SO 에셋 + 테스트      ← Task 7 완료 후
```
