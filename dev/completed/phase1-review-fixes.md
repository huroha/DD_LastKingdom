# Phase 1 설계 검토 - 수정 목록 (DD 원작 대비)

- 검토일: 2026-03-07
- 대상: Phase 1 전체 (테스트 제외)
- 기준: Darkest Dungeon 원작 핵심 메커니즘

---

## 치명적 문제 (전투 흐름에 직접 영향)

### FIX-01. 시체(Corpse)가 전투 승리를 막음 [난이도: S]

**현상**: `PositionSystem.GetAllUnits()`가 `State != Dead`로 필터링하여 Corpse 유닛도 포함됨. `CombatStateMachine.RunBattle()`의 승리 조건 `GetAllUnits(Enemy).Count == 0`에서 Corpse가 카운트되어 적이 전부 시체여도 전투가 끝나지 않음.

**DD 원작**: 시체는 슬롯만 차지. 살아있는 적(Alive)이 없으면 전투 승리.

**수정 방향**:
- `PositionSystem.GetAllUnits()`의 필터를 `IsAlive` (Alive || DeathsDoor)로 변경
- 또는 승리 판정 전용 메서드 `GetAliveUnits()` 추가
- 주의: Corpse가 슬롯을 차지하는 것 자체는 유지 (위치 시스템에서 시체 뒤 유닛 타겟 불가)

**관련 파일**: `PositionSystem.cs:39-49`, `CombatStateMachine.cs:142-154`

---

### FIX-02. 스턴(Stun) 처리 로직 부재 [난이도: M]

**현상**: `StatusEffectType.Stun`과 저항 판정은 존재하지만, 스턴된 유닛의 턴을 스킵하는 로직이 없음. `TurnManager.AdvanceToNextAliveUnit()`은 `IsAlive`만 확인.

**DD 원작**: 스턴된 유닛은 턴 시작 시 "스턴 해제"로 턴 소비. 스턴 해제 후 일시적 스턴 저항 +50% 증가 (중복 스턴 방지).

**수정 방향**:
- `CombatUnit`에 `IsStunned` 프로퍼티 추가 (ActiveEffects에서 Stun 탐색)
- `CombatStateMachine.RunBattle()` 턴 시작 후 스턴 체크:
  - 스턴 상태면 → 스턴 해제 + 스턴 저항 임시 증가 + 턴 종료 (스킬 사용 불가)
  - 정상이면 → 기존 HandlePlayerTurn/HandleEnemyTurn 진행
- 스턴 저항 임시 증가분은 다음 라운드 시작 시 초기화 (또는 StatusEffect로 처리)

**관련 파일**: `CombatUnit.cs`, `CombatStateMachine.cs:118-157`, `TurnManager.cs`

---

### FIX-03. 위치 이동(Knockback/Pull)의 저항 판정 누락 [난이도: S]

**현상**: `SkillExecutor.ApplyPositionMove()`에서 `m_MoveTargetAmount`를 무조건 적용. `ResistanceBlock`에 `move` 저항값이 존재하지만 사용하지 않음.

**DD 원작**: 대상 강제 이동은 Move Resist로 저항 가능. 사용자 자신의 이동은 저항 불가.

**수정 방향**:
- `ApplyPositionMove()`에서 target 이동 전 `target.CurrentStats.resistance.move` 기반 저항 판정 추가
- 저항 성공 시 이동하지 않음
- user 이동(m_MoveUserAmount)은 저항 판정 없이 그대로 적용

**관련 파일**: `SkillExecutor.cs:300-306`

---

### FIX-04. 대상 이동이 빗나가도 적용됨 [난이도: S]

**현상**: `ApplyPositionMove()`가 for 루프 내에서 `result[i].IsHit` 여부와 무관하게 호출됨 (SkillExecutor:121).
- 사용자(user) 이동: DD 원작도 빗나가도 이동 (Lunge 등) → 정상
- 대상(target) 이동: 빗나갔는데 넉백되면 안 됨

**DD 원작**: 사용자 이동은 미스여도 발생. 대상 강제 이동은 명중 시에만.

**수정 방향**:
- `ApplyPositionMove()` 호출을 분리:
  - `m_MoveUserAmount`: 명중 여부 무관 (루프 밖, 또는 루프 내 무조건 1회)
  - `m_MoveTargetAmount`: `result[i].IsHit == true`일 때만 적용
- 또는 `ApplyPositionMove`에 `bool isHit` 파라미터 추가

**관련 파일**: `SkillExecutor.cs:120-121, 300-306`

---

## 중요 문제 (DD 핵심 메커니즘 누락/불일치)

### FIX-05. Death's Door 진입 시 에블라 미증가 [난이도: S]

**현상**: `CombatUnit.TakeDamage()`에서 Alive → DeathsDoor 전환 시 `AddEbla()` 호출 없음.

**DD 원작**: Death's Door 진입 시 본인 스트레스 +18.

**수정 방향**:
- `TakeDamage()` 내 DeathsDoor 전환 직후 `AddEbla(DEATHS_DOOR_EBLA)` 호출
- 상수 정의 필요 (DD 원작 기준 18, 조정 가능)
- 파티원 스트레스 증가는 Phase 2 EblaSystem에서 처리 가능

**관련 파일**: `CombatUnit.cs:96-103`

---

### FIX-06. 크리티컬 에블라 효과 방향 오류 [난이도: S]

**현상**: `ApplyCritEffects()`가 공격자(user)의 UnitType을 구분하지 않음.
- 적이 니케를 크리티컬 → `target(니케).AddEbla(+15)` + `allNikkes.AddEbla(-5)`
- 결과: **적이 크리티컬 쳐도 파티 에블라가 감소**하는 모순

**DD 원작**:
- 아군 크리 → 적 스트레스 증가, 파티 스트레스 감소
- 적 크리 → 피격 아군 스트레스 증가, 파티 스트레스 증가 (감소 아님)

**수정 방향**:
```
if (user.UnitType == Nikke)
    // 아군 크리: target.AddEbla(+15), allNikkes.AddEbla(-5)  (현재 로직)
else  // Enemy 크리
    // target(니케).AddEbla(+15), allNikkes.AddEbla(+5)  (파티 전체 에블라 증가)
```

**관련 파일**: `SkillExecutor.cs:275-297`

---

### FIX-07. 시체 자동 소멸 미구현 [난이도: M]

**현상**: Enemy Corpse가 생성되면 누군가 공격해서 HP 0으로 만들어야만 제거됨.

**DD 원작**: 시체는 1~3턴 후 자동 소멸하여 슬롯 확보.

**수정 방향**:
- `CombatUnit`에 `CorpseTimer` 필드 추가 (Corpse 전환 시 초기화)
- `CombatStateMachine` 턴 종료(TurnEnd) 시점에 모든 Corpse의 타이머 감소
- 타이머 0 도달 시 → `State = Dead`, `PositionSystem.RemoveUnit()` 호출
- `EnemyData`에 `m_CorpseDecayTurns` 필드 추가 (SO에서 설정 가능)

**관련 파일**: `CombatUnit.cs`, `EnemyData.cs`, `CombatStateMachine.cs`

---

### FIX-08. 기습/서프라이즈 라운드 미설계 [난이도: L]

**현상**: 전투 항상 정상 라운드로 시작. 기습 개념 없음.

**DD 원작**:
- 기습 성공 → 적 일부 1라운드 행동 불가 (랜덤 선택)
- 역기습 → 아군 위치 셔플 + 아군 일부 1라운드 행동 불가
- 기습 확률은 torch level, scouting, 특성 등에 의해 결정

**수정 방향**:
- `StartBattle()`에 `SurpriseType` enum 파라미터 추가 (None / PlayerSurprise / EnemySurprise)
- PlayerSurprise: 1라운드에서 적 일부 턴 스킵 (스턴과 유사하게 처리)
- EnemySurprise: 아군 위치 랜덤 셔플 + 1라운드 아군 일부 턴 스킵
- Phase 2 던전 시스템에서 SurpriseType 결정 로직 구현
- Phase 1에서는 CombatStateMachine에 분기점만 마련

**관련 파일**: `CombatStateMachine.cs:94-113`

---

### FIX-09. 패스(Pass) 시 에블라 페널티 없음 [난이도: S]

**현상**: `OnSkillPass()`에서 에블라 추가 없이 턴 종료.

**DD 원작**: 턴 패스 시 스트레스 +5.

**수정 방향**:
- `CombatStateMachine.HandlePlayerTurn()`에서 패스 확정 후 `m_ActiveUnit.AddEbla(PASS_EBLA_PENALTY)` 호출
- 상수 정의: `private const int PASS_EBLA_PENALTY = 5;` (조정 가능)

**관련 파일**: `CombatStateMachine.cs:198-202`

---

## 보완 권장 (Phase 2 이후 가능)

### FIX-10. 적 AI 가중치 부재

**현상**: `EnemyAI.DecideAction()`이 순수 랜덤 선택.

**DD 원작**: 적마다 스킬 우선순위/조건 테이블 보유. HP 낮은 대상 우선, 스트레스 공격 우선, 특정 위치 우선 등.

**수정 방향**: `EnemyData`에 스킬별 가중치 배열 또는 조건 테이블 추가. Phase 2에서 구현.

---

### FIX-11. Death's Door 회복 시 디버프 없음

**현상**: `Heal()`에서 DeathsDoor → Alive 전환 시 추가 처리 없음.

**DD 원작**: Death's Door 회복 시 Recovery debuff (ACC/DODGE 감소 등) 부여.

**수정 방향**: Phase 2 StatusEffectManager 구현 시 함께 처리. `Heal()` 내 DeathsDoor 탈출 시 Recovery StatusEffect 자동 부여.

---

### FIX-12. 후반전 에블라 공식 — 유지 확정 (Decision #27)

**결정**: 유지. DD 원작의 적 난입(Reinforcement) 대체 메커니즘.
**수정 불필요** — 현재 구현 그대로 사용.

---

## 수정 우선순위

| 순위 | 항목 | 난이도 | Phase 1.6 전 권장 |
|------|------|--------|-------------------|
| 1 | FIX-01 시체 승리 판정 | S | O |
| 2 | FIX-06 크리티컬 에블라 방향 | S | O |
| 3 | FIX-04 대상 이동 명중 조건 | S | O |
| 4 | FIX-03 Move 저항 판정 | S | O |
| 5 | FIX-05 Death's Door 에블라 | S | O |
| 6 | FIX-09 패스 에블라 페널티 | S | O |
| 7 | FIX-02 스턴 턴 스킵 | M | O |
| 8 | FIX-07 시체 자동 소멸 | M | 선택 |
| 9 | FIX-08 서프라이즈 라운드 | L | Phase 2 |
| ~~10~~ | ~~FIX-12 후반전 에블라~~ | - | 유지 확정 (수정 불필요) |
| 11 | FIX-10 적 AI 가중치 | M | Phase 2 |
| 12 | FIX-11 DD 회복 디버프 | S | Phase 2 |

---


