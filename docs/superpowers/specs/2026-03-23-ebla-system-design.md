# EblaSystem Design

## Overview
전투 중 에블라 수치 임계값 판정 시스템. 에블라 100 도달 시 Affliction 발동(스탯 디버프), 200 도달 시 영구 사망 처리.

## Scope (Phase 2)
- Affliction 발동/해제 + 스탯 디버프
- 영구 사망 처리 + PermanentDeathEvent 발행
- RecalculateStats() 확장 (ActiveEffects StatModifier 합산)

## Scope 외 (Phase 3)
- Virtue 발동 + Awakened 스킬 해금 (최종 전투 전용 니케만 해당)
- Affliction 랜덤 행동 (명령 거부, 아군 공격 등)
- 에블라 시각 연출 (EblaVFX)
- 니케별/클래스별 다른 Affliction 종류

---

## Architecture

### 소유 구조
EblaSystem은 Pure C# 클래스. CombatStateMachine이 소유.

```
CombatStateMachine.cs
├── TurnManager        (Pure C#)
├── PositionSystem     (Pure C#)
├── SkillExecutor      (Pure C#)
├── EnemyAI            (Pure C#)
└── EblaSystem         (Pure C#) ← 신규
```

### EblaSystem 클래스
- File: `Assets/Scripts/Systems/EblaSystem.cs`
- Pure C# 클래스 (MonoBehaviour 아님)
- 생성자: `EblaSystem(StatusEffectData afflictionDebuff)` — Affliction 디버프 SO 주입
- CombatStateMachine에서 `[SerializeField]`로 SO를 Inspector에서 할당 후 주입

### 핵심 메서드

```
ModifyEbla(CombatUnit unit, int amount) → bool (200 도달로 사망했으면 true 반환)
├── int previousEbla = unit.Ebla
├── unit.AddEbla(amount) 호출 (AddEbla 내부에서 0~200 클램프 유지)
├── 200 도달 체크 (unit.Ebla >= 200) — 최우선 처리
│   ├── unit.Kill()
│   ├── EventBus.Publish(PermanentDeathEvent)
│   └── return true
├── 100 도달 체크 (previousEbla < 100 && unit.Ebla >= 100)
│   ├── unit.SetEblaState(EblaState.Afflicted)
│   ├── unit.ActiveEffects.Add(AfflictionDebuff SO)
│   ├── unit.RecalculateStats()
│   └── EventBus.Publish(AfflictionTriggeredEvent)
├── 100 미만 복귀 체크 (previousEbla >= 100 && unit.Ebla < 100)
│   ├── ActiveEffects에서 AfflictionDebuff 제거
│   ├── unit.SetEblaState(EblaState.Normal)
│   └── unit.RecalculateStats()
└── return false
```

### 반환값 활용
`ModifyEbla()`가 true를 반환하면 호출자(SkillExecutor/CSM)가 `PositionSystem.RemoveUnit()` + `UnitDiedEvent` 발행을 처리한다. `PermanentDeathEvent`는 EblaSystem이 발행하고, `UnitDiedEvent`는 호출자가 발행 — 두 이벤트 모두 발생하여 UI/애니메이션(UnitDiedEvent 구독)과 SaveSystem(PermanentDeathEvent 구독)이 각각 반응할 수 있다.

---

## Data Changes

### CombatUnit 변경
- `EblaState` enum 추가: `Normal / Afflicted` (추후 `Virtuous` 확장)
- `public EblaState EblaState { get; private set; }` 프로퍼티 추가
- `public void SetEblaState(EblaState state)` 메서드 추가 (EblaSystem만 호출하는 의도, 캡슐화)
- `AddEbla()` 메서드 유지 (EblaSystem이 내부에서 호출). 0~200 클램프는 AddEbla() 내부에서 유지 (단일 책임)
- `RecalculateStats()` 확장: ActiveEffects의 StatModifier 합산

### RecalculateStats() 확장
```
RecalculateStats()
├── CurrentStats = BaseStats (초기화)
└── for each ActiveEffect in ActiveEffects
    └── CurrentStats += effect.Data.StatModifier (필드별 합산)
```

StatBlock이 struct이므로 필드별 덧셈 연산 필요. StatBlock에 Apply 메서드 추가.
- StatBlock 내 모든 수치 필드 합산 (minDamage, maxDamage, defense, accuracyMod, dodge, speed, critChance, deathBlowResist, moveRange)
- ResistanceBlock 필드도 합산 (bleed, poison, disease, stun, debuff, move) — Affliction SO에서 0으로 두면 영향 없음

### Affliction 디버프 SO
- 에셋: `Assets/ScriptableObjects/StatusEffects/Affliction_Debuff.asset`
- EffectType: Debuff
- Duration: -1 (영구 — EblaSystem이 직접 관리)
- StatModifier: ACC -10, DODGE -5, SPD -1 (밸런스 조정 가능)
- IsStackable: false

### 이벤트 (CombatEvent.cs 추가)
```csharp
public struct AfflictionTriggeredEvent { public CombatUnit Unit; }
public struct PermanentDeathEvent { public CombatUnit Unit; }
```

---

## Integration Points

### 기존 AddEbla() 호출 → EblaSystem.ModifyEbla() 전환

| 위치 | 현재 | 변경 |
|------|------|------|
| CombatStateMachine.HandlePlayerTurn (패스) | `m_ActiveUnit.AddEbla(PASS_EBLA_PENALTY)` | `m_EblaSystem.ModifyEbla(m_ActiveUnit, PASS_EBLA_PENALTY)` |
| CombatStateMachine.ApplyAllyDeathEbla | `nikkes[i].AddEbla(ALLY_DEATH_EBLA)` | `m_EblaSystem.ModifyEbla(nikkes[i], ALLY_DEATH_EBLA)` |
| CombatStateMachine.ApplyPostBattleEbla | `nikkes[i].AddEbla(total)` | `m_EblaSystem.ModifyEbla(nikkes[i], total)` |
| SkillExecutor (에블라 데미지/힐) | `targets[i].AddEbla(...)` | `m_EblaSystem.ModifyEbla(targets[i], ...)` |
| SkillExecutor.ApplyCritEffects | `target.AddEbla(...)`, `allNikkes[i].AddEbla(...)` | `m_EblaSystem.ModifyEbla(...)` |
| CombatUnit.TakeDamage (DeathsDoor) | `AddEbla(DEATHS_DOOR_EBLA)` | TakeDamage에서 제거. 호출자(SE/CSM)가 State 비교 후 `EblaSystem.ModifyEbla()` |
| StatusEffectManager DOT 틱 (Phase 2 후속) | 미구현 | DOT 틱 후 DeathsDoor 진입 시 `EblaSystem.ModifyEbla()`. SE 참조 주입 필요 |

### SkillExecutor 변경
- 생성자에 EblaSystem 참조 추가: `SkillExecutor(PositionSystem, EblaSystem)`
- Execute() 내 에블라 관련 코드를 EblaSystem.ModifyEbla() 호출로 교체

### CombatStateMachine 변경
- `m_EblaSystem` 멤버 추가
- `[SerializeField] private StatusEffectData m_AfflictionDebuff` 추가 (Inspector 할당)
- `StartBattle()`에서 `m_EblaSystem = new EblaSystem(m_AfflictionDebuff)` 생성
- SkillExecutor 생성 시 EblaSystem 전달
- `ProcessDeadUnits()`에서 PermanentDeath 이벤트도 처리

### CombatUnit.TakeDamage의 DEATHS_DOOR_EBLA 처리
현재 TakeDamage 내부에서 `AddEbla(DEATHS_DOOR_EBLA)`를 직접 호출. EblaSystem 경유가 필요하나, CombatUnit은 EblaSystem을 모름.

**해결 방식**: State 비교로 DeathsDoor 진입 감지
1. TakeDamage()에서 `AddEbla(DEATHS_DOOR_EBLA)` 제거
2. SkillExecutor.Execute()에서: `TargetResult.PreviousState`(이미 존재)와 TakeDamage 후 State를 비교
3. `PreviousState == Alive && target.State == DeathsDoor`이면 → `m_EblaSystem.ModifyEbla(target, DEATHS_DOOR_EBLA)` 호출
4. ModifyEbla가 true 반환(200 도달)하면 → 해당 유닛도 사망 처리에 포함

**처리 순서 (SkillExecutor.Execute 내부)**:
```
1. TakeDamage(damage)
2. DeathsDoor 진입 체크 → EblaSystem.ModifyEbla(DEATHS_DOOR_EBLA)
3. 에블라 데미지 적용 → EblaSystem.ModifyEbla(skill.EblaDamage)
4. 크리티컬 에블라 효과
5. TargetResult 채우기 (최종 State 반영)
```
→ ProcessDeadUnits()는 TargetResult의 최종 State를 보므로 에블라 200 사망도 정상 감지.

**DOT 데미지 경로** (StatusEffectManager 구현 시):
StatusEffectManager가 DOT 틱으로 TakeDamage() 호출 후 동일하게 State 비교 → DeathsDoor 진입 시 EblaSystem.ModifyEbla() 호출. StatusEffectManager도 EblaSystem 참조를 주입받아야 함.

| 위치 | DeathsDoor 에블라 처리 |
|------|------|
| SkillExecutor.Execute | PreviousState vs State 비교 → EblaSystem.ModifyEbla() |
| StatusEffectManager (Phase 2 후속) | DOT 틱 후 State 비교 → EblaSystem.ModifyEbla() |

---

## Design Decisions Summary

| 결정 | 선택 | 이유 |
|------|------|------|
| Affliction 디버프 방식 | StatusEffectData SO | 기존 ActiveEffects 파이프라인 활용 |
| Affliction 종류 | 단일 (모든 니케 동일) | Phase 2 스코프 최소화, Decision #29 |
| 판정 시점 | EblaSystem.ModifyEbla() 중앙 관리 | CombatUnit은 데이터 홀더로 유지 |
| 소유 구조 | CSM 소유 Pure C# | Decision #14 전투 전용 패턴 |
| Virtue | Phase 2 미구현 | 최종 전투 전용 니케만 해당, Phase 3 |
| 200 영구 사망 | 전투 내 즉시 사망 + PermanentDeathEvent | SaveSystem 연동은 추후 |
| RecalculateStats | ActiveEffects StatModifier 합산 | StatusEffectManager 없이도 동작 |
