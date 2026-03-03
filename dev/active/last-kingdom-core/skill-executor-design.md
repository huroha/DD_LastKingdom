# SkillExecutor 설계 문서

> Phase 1.4 (SkillExecutor, CombatStateMachine, EnemyAI) + Phase 1.5 (전투 UI) + Phase 1.6 (테스트 씬)
> 작성일: 2026-03-03
> 확정된 결정: Decision #16~#23 (context.md 참고)

---

## 1. 데미지 공식 (확정값 반영)

### 1.1 명중 판정

```
HitChance = (UserACC + SkillAccuracyMod) - TargetDODGE
```

- **클램프**: 0% ~ 100% (제한 없음 — Decision #17)
- **판정**: `float roll = Random.Range(0f, 100f); bool isHit = roll < HitChance;`

### 1.2 데미지 계산

```
BaseDamage = Random.Range(minDamage, maxDamage + 1)
RawDamage  = (int)(BaseDamage * skill.DamageMultiplier)
FinalDamage = (int)(RawDamage * (1f - target.CurrentStats.defense / 100f))
FinalDamage = Mathf.Max(FinalDamage, 0)
```

- **minDamage/maxDamage**: StatBlock 범위 데미지 (Decision #16)
- **defense**: % 기반 PROT 감소 0~100 (Decision #16)

### 1.3 크리티컬 판정

명중 성공 시에만 수행.

```
CritChance = user.CurrentStats.critChance + skill.CritMod
bool isCrit = Random.Range(0f, 100f) < CritChance
```

**크리티컬 효과** (Decision #17):
- 데미지 × 1.5배
- 적 에블라 +15
- 파티원 전체 에블라 -5
- NikkeData.OnCritSelfEffects 적용 (공격자)
- NikkeData.OnReceiveCritSelfEffects 적용 (피격자)

### 1.4 힐 처리 (Decision #18)

`skill.HealAmount > 0`이면 힐 스킬.

```
HealAmount = skill.HealAmount
if isCrit: HealAmount = (int)(HealAmount * 1.5f)
target.Heal(HealAmount)
```

- 힐은 명중 판정 없음 (항상 성공)
- 크리티컬은 적용됨

### 1.5 에블라 피해

```
if skill.EblaDamage > 0:
    target.AddEbla(skill.EblaDamage)
```

힐 스킬에도 에블라 감소 별도 적용:
```
if skill.EblaHealAmount > 0:
    target.AddEbla(-skill.EblaHealAmount)
```

---

## 2. 스킬 실행 파이프라인

### 2.1 전체 흐름

```
1. ValidateSkill      -- 사용 가능 여부 (위치, 상태, 유효 타겟)
2. ResolveTargets     -- 최종 타겟 리스트 결정
3. 각 타겟에 대해 반복:
   3a. RollHit        -- 명중 판정 (힐은 생략)
   3b. CalcDamage     -- 데미지/힐량 계산
   3c. RollCrit       -- 크리티컬 판정 + 데미지 보정
   3d. ApplyDamage    -- CombatUnit.TakeDamage() 또는 Heal()
   3e. ApplyEbla      -- 에블라 피해/회복 적용
   3f. ApplyOnHit     -- 상태이상 저항 판정 후 적용
   3g. ApplyCrit      -- 크리티컬 추가 효과
   3h. ApplyMove      -- 사용자/대상 위치 이동
4. BuildResult        -- SkillResult 구조체 반환
5. PublishEvents      -- EventBus 이벤트 발행
```

### 2.2 SkillResult / TargetResult 구조체

파일: `Assets/Scripts/Combat/SkillResult.cs`

```csharp
public struct SkillResult
{
    public CombatUnit           User;
    public SkillData            Skill;
    public TargetResult[]       TargetResults;
}

public struct TargetResult
{
    public CombatUnit           Target;
    public bool                 IsHit;
    public bool                 IsCrit;
    public int                  DamageDealt;
    public int                  HealAmount;
    public int                  EblaDamageDealt;
    public int                  EblaHealAmount;
    public UnitState            ResultState;
    public StatusEffectData[]   AppliedEffects;
    public StatusEffectData[]   ResistedEffects;
}
```

### 2.3 상태이상 저항 판정

명중 성공 시 `skill.OnHitEffects[]`를 순회.

| StatusEffectType | ResistanceBlock 필드 |
|------------------|---------------------|
| Bleed            | resistance.bleed    |
| Poison           | resistance.poison   |
| Disease          | resistance.disease  |
| Stun             | resistance.stun     |
| Debuff           | resistance.debuff   |
| Buff             | 항상 적용 (저항 없음) |
| Guard            | 항상 적용            |
| Mark             | 항상 적용            |

스택 처리:
- `IsStackable == true`: `CurrentStacks` 증가 (`MaxStack` 제한)
- `IsStackable == false`: `RemainingTurns` 갱신 (더 긴 쪽으로)

### 2.4 ValidateSkill 상세

```csharp
public bool ValidateSkill(CombatUnit user, SkillData skill)
{
    if (!user.IsAlive) return false;
    if (!m_PositionSystem.CanUseSkill(user, skill)) return false;

    // RequiredState: Awakened 스킬은 Phase 1에서 항상 false
    if (skill.RequiredState == SkillRequiredState.Awakened) return false;

    List<CombatUnit> validTargets = m_PositionSystem.GetValidTargets(user, skill);
    if (validTargets.Count == 0) return false;

    return true;
}
```

### 2.5 ResolveTargets 상세

| TargetType  | 처리                                      |
|-------------|-------------------------------------------|
| EnemySingle | selectedTarget (외부에서 전달)             |
| EnemyMulti  | GetValidTargets() 전체 (TargetPositions 기반) |
| EnemyAll    | 살아있는 모든 적 (TargetPositions 무시)    |
| AllySingle  | selectedTarget                            |
| AllyMulti   | GetValidTargets() 전체                    |
| AllyAll     | 살아있는 모든 아군                         |
| Self        | user 자신                                 |

Execute 시그니처:
```csharp
public SkillResult Execute(CombatUnit user, SkillData skill, CombatUnit selectedTarget = null)
```

---

## 3. SkillExecutor 클래스 구조

파일: `Assets/Scripts/Combat/SkillExecutor.cs` (Pure C#)

```csharp
public class SkillExecutor
{
    private PositionSystem m_PositionSystem;

    // 크리티컬 에블라 상수 (Decision #17)
    private const int CRIT_EBLA_TO_ENEMY      = 15;
    private const int CRIT_EBLA_PARTY_HEAL    = -5;
    private const float CRIT_DAMAGE_MULTI     = 1.5f;

    public SkillExecutor(PositionSystem positionSystem)
    {
        m_PositionSystem = positionSystem;
    }

    // --- Public API ---
    public bool ValidateSkill(CombatUnit user, SkillData skill);
    public SkillResult Execute(CombatUnit user, SkillData skill, CombatUnit selectedTarget = null);

    // --- Internal Pipeline ---
    private List<CombatUnit> ResolveTargets(CombatUnit user, SkillData skill, CombatUnit selectedTarget);
    private bool RollHit(CombatUnit user, CombatUnit target, SkillData skill);
    private int CalcDamage(CombatUnit user, CombatUnit target, SkillData skill);
    private bool RollCrit(CombatUnit user, SkillData skill);
    private void ApplyOnHitEffects(CombatUnit target, SkillData skill,
                                   List<StatusEffectData> applied, List<StatusEffectData> resisted);
    private float GetResistance(CombatUnit target, StatusEffectType effectType);
    private void ApplyCritEffects(CombatUnit user, CombatUnit target,
                                  List<CombatUnit> allNikkes);
    private void ApplyPositionMove(CombatUnit user, SkillData skill, CombatUnit target);
}
```

---

## 4. CombatStateMachine FSM 설계

### 4.1 상태 enum

파일: `Assets/Scripts/Combat/CombatState.cs`

```csharp
public enum CombatState
{
    BattleStart,
    TurnStart,
    PlayerSelectSkill,
    PlayerSelectTarget,
    EnemyDecide,
    ExecuteSkill,
    TurnEnd,
    CheckBattleEnd,
    Victory,
    Defeat
}
```

### 4.2 상태 전이 다이어그램

```
BattleStart
    |
    v
TurnStart <---------------------------------------------+
    |                                                    |
    +-- [Nikke] --> PlayerSelectSkill                   |
    |                  |                                 |
    |                  v                                 |
    |              PlayerSelectTarget                    |
    |                  |  (취소 시 PlayerSelectSkill)    |
    |                  v                                 |
    |              ExecuteSkill <--+                     |
    +-- [Enemy] --> EnemyDecide --+                     |
                                   |                    |
                                   v                    |
                               TurnEnd                  |
                                   |                    |
                                   v                    |
                           CheckBattleEnd               |
                               |                        |
                 +-------------|------------+           |
                 |                          |           |
            [전투 계속] -----------------> TurnStart    |
                                                       (라운드 종료 시 RoundStart 내부 처리)
                 |
            [적 전멸] --> Victory
                 |
            [아군 전멸] --> Defeat
```

> RoundStart는 TurnManager 내부에서 처리. FSM은 TurnManager.StartNextTurn()이 null을 반환할 때만 별도 처리.

### 4.3 각 상태 OnEnter 처리

**BattleStart**
- CombatUnit 리스트 생성
- PositionSystem.Initialize() 호출
- TurnManager.Initialize() 호출
- EventBus.Publish(BattleStartedEvent)
- 전이 → TurnStart

**TurnStart**
- TurnManager.StartNextTurn() 호출
- null 반환 시 → CheckBattleEnd
- Nikke이면 → PlayerSelectSkill
- Enemy이면 → EnemyDecide

**PlayerSelectSkill**
- UI에 사용 가능 스킬 표시 (ValidateSkill 기반)
- 플레이어 입력 대기 (Update 루프에서 상태 유지)

**PlayerSelectTarget**
- EnemySingle/AllySingle: 타겟 하이라이트, 클릭 대기
- EnemyMulti/EnemyAll/AllyMulti/AllyAll/Self: 자동 → 즉시 ExecuteSkill
- 취소(ESC/우클릭) → PlayerSelectSkill

**EnemyDecide**
- EnemyAI.DecideAction() 호출
- 0.3초 딜레이 (코루틴)
- 전이 → ExecuteSkill

**ExecuteSkill**
- SkillExecutor.Execute() 호출
- EventBus.Publish(SkillExecutedEvent)
- Dead 유닛 → PositionSystem.RemoveUnit() 즉시 처리
- Phase 1: 0.5초 연출 딜레이 (코루틴)
- 전이 → TurnEnd

**TurnEnd**
- TurnManager.EndCurrentTurn()
- Phase 2: DOT 틱, 효과 만료, 에블라 처리
- 전이 → CheckBattleEnd

**CheckBattleEnd**
```
hasActiveEnemy = false
for each enemy:
    if State == Alive or DeathsDoor or Corpse → hasActiveEnemy = true

if (!hasActiveEnemy) → Corpse들 Dead 처리 후 Victory
if (모든 Nikke Dead) → Defeat
else → TurnStart
```

### 4.4 CombatStateMachine MonoBehaviour 구조

파일: `Assets/Scripts/Combat/CombatStateMachine.cs`

```csharp
public class CombatStateMachine : MonoBehaviour
{
    // 소유하는 Pure C# 시스템
    private TurnManager     m_TurnManager;
    private PositionSystem  m_PositionSystem;
    private SkillExecutor   m_SkillExecutor;
    private EnemyAI         m_EnemyAI;

    // FSM 상태
    private CombatState     m_CurrentState;
    private CombatUnit      m_CurrentUnit;
    private SkillData       m_SelectedSkill;
    private CombatUnit      m_SelectedTarget;

    // 유닛 리스트
    private List<CombatUnit> m_AllUnits;
    private List<CombatUnit> m_NikkeUnits;
    private List<CombatUnit> m_EnemyUnits;

    // Phase 1 테스트용 (Inspector 할당)
    [SerializeField] private NikkeData[]  m_TestNikkes;
    [SerializeField] private EnemyData[]  m_TestEnemies;

    // UI 참조 (Inspector 할당)
    [SerializeField] private CombatHUD          m_CombatHUD;
    [SerializeField] private SkillSelectPanel   m_SkillSelectPanel;
    [SerializeField] private TargetSelectPanel  m_TargetSelectPanel;
}
```

---

## 5. EnemyAI 기초 설계

파일: `Assets/Scripts/Combat/EnemyAI.cs` (Pure C#)

### Phase 1: 랜덤 AI

```csharp
public class EnemyAI
{
    private PositionSystem  m_PositionSystem;
    private SkillExecutor   m_SkillExecutor;

    public EnemyAI(PositionSystem positionSystem, SkillExecutor skillExecutor)
    {
        m_PositionSystem = positionSystem;
        m_SkillExecutor  = skillExecutor;
    }

    public EnemyAction DecideAction(CombatUnit enemy)
    {
        List<SkillData> usableSkills = new List<SkillData>();
        for (int i = 0; i < enemy.Skills.Count; ++i)
        {
            if (m_SkillExecutor.ValidateSkill(enemy, enemy.Skills[i]))
                usableSkills.Add(enemy.Skills[i]);
        }

        if (usableSkills.Count == 0)
            return EnemyAction.Pass;

        SkillData selectedSkill = usableSkills[Random.Range(0, usableSkills.Count)];

        List<CombatUnit> validTargets = m_PositionSystem.GetValidTargets(enemy, selectedSkill);
        CombatUnit selectedTarget = null;

        if (selectedSkill.TargetType == TargetType.EnemySingle
         || selectedSkill.TargetType == TargetType.AllySingle)
        {
            if (validTargets.Count > 0)
                selectedTarget = validTargets[Random.Range(0, validTargets.Count)];
        }

        return new EnemyAction(selectedSkill, selectedTarget);
    }
}
```

### Phase 2 확장 (참고)
- 낮은 HP 타겟 집중 (Death's Door 우선)
- Supporter 우선 타겟
- 자가 힐 조건부 사용
- EnemyData에 AI 프로필 enum 추가 (Aggressive / Defensive / Random)

---

## 6. 새 CombatEvent 추가

기존 `Assets/Scripts/Combat/CombatEvent.cs`에 추가:

```csharp
public struct BattleStartedEvent
{
    public List<CombatUnit> Nikkes;
    public List<CombatUnit> Enemies;
    public BattleStartedEvent(List<CombatUnit> nikkes, List<CombatUnit> enemies)
    {
        Nikkes  = nikkes;
        Enemies = enemies;
    }
}

public struct BattleEndedEvent
{
    public bool IsVictory;
    public BattleEndedEvent(bool isVictory) { IsVictory = isVictory; }
}

public struct SkillExecutedEvent
{
    public SkillResult Result;
    public SkillExecutedEvent(SkillResult result) { Result = result; }
}

public struct UnitStateChangedEvent
{
    public CombatUnit   Unit;
    public UnitState    OldState;
    public UnitState    NewState;
    public UnitStateChangedEvent(CombatUnit unit, UnitState oldState, UnitState newState)
    {
        Unit     = unit;
        OldState = oldState;
        NewState = newState;
    }
}
```

---

## 7. 전투 UI 기초 (Phase 1.5)

### Canvas 구조

```
Canvas (Screen Space - Overlay)
├── TopPanel
│   └── TurnOrderBar             # 행동 순서 초상화 리스트
├── NikkePanel (하단 좌측)
│   └── NikkeSlot[0~3]           # HP바 + 이름
├── EnemyPanel (상단 우측)
│   └── EnemySlot[0~3]           # HP바 + 이름
├── SkillPanel (하단 중앙)
│   ├── SkillButton[0~3]         # 스킬 버튼
│   └── PassButton               # 턴 패스
└── InfoPanel
    └── CombatLogText            # 전투 로그
```

### UI 파일 역할

**CombatHUD.cs** (MonoBehaviour):
- EventBus 구독 OnEnable/OnDisable:
  - BattleStartedEvent, TurnStartedEvent, SkillExecutedEvent, UnitDiedEvent, BattleEndedEvent
- HP바 갱신, 행동 순서 초상화, 현재 턴 하이라이트

**SkillSelectPanel.cs** (MonoBehaviour):
- PlayerSelectSkill 상태에서 활성화
- ValidateSkill 기반 버튼 비활성화
- 클릭 콜백 → FSM으로 전달

**TargetSelectPanel.cs** (MonoBehaviour):
- PlayerSelectTarget 상태에서 활성화
- 유효 타겟 하이라이트
- ESC/우클릭 → 스킬 선택 복귀

### UI-FSM 통신: delegate 콜백

```csharp
public delegate void SkillSelectedHandler(SkillData skill);
public delegate void TargetSelectedHandler(CombatUnit target);
public delegate void CancelHandler();

// FSM이 UI에 콜백 전달
m_SkillSelectPanel.Show(m_CurrentUnit, OnSkillSelected, OnPass);
m_TargetSelectPanel.Show(validTargets, OnTargetSelected, OnCancel);
```

---

## 8. CombatScene 구성 (Phase 1.6)

### 씬 계층

```
CombatScene
├── Main Camera (2D, Orthographic)
├── CombatManager (GameObject)
│   └── CombatStateMachine (MonoBehaviour)
├── BattleBackground (SpriteRenderer placeholder)
├── UnitPositions
│   ├── NikkePos0~3 (Transform)
│   └── EnemyPos0~3 (Transform)
├── Canvas → CombatHUD
└── EventSystem
```

### Phase 1 테스트 시나리오

1. 기본 전투 흐름: 킬로/크라운 vs 랩처/랩처엘리트
2. MISS 발생 확인 (높은 DODGE)
3. 크리티컬 + 에블라 효과 확인
4. Death's Door 진입 → deathBlow 판정
5. Corpse 전환 → 추가 피격 → Dead
6. DOT 사망 (isDot=true → 즉시 Dead)
7. Victory / Defeat 로그

---

## 9. 구현 순서

### Phase 1.4

| Step | 내용 | 파일 | Size |
|------|------|------|------|
| 1 | SkillResult, TargetResult 구조체 | `Combat/SkillResult.cs` | S |
| 2 | EnemyAction 구조체 | `Combat/EnemyAction.cs` | S |
| 3 | CombatState enum | `Combat/CombatState.cs` | S |
| 4 | CombatEvent.cs에 4개 이벤트 추가 | `Combat/CombatEvent.cs` | S |
| 5 | **SkillExecutor** | `Combat/SkillExecutor.cs` | **L** |
| 6 | **EnemyAI** | `Combat/EnemyAI.cs` | M |
| 7 | **CombatStateMachine** | `Combat/CombatStateMachine.cs` | **XL** |

### Phase 1.5 (전투 UI)

| Step | 내용 | 파일 | Size |
|------|------|------|------|
| 8 | CombatHUD | `UI/CombatHUD.cs` | L |
| 9 | SkillSelectPanel | `UI/SkillSelectPanel.cs` | M |
| 10 | TargetSelectPanel | `UI/TargetSelectPanel.cs` | M |

### Phase 1.6 (테스트 씬)

| Step | 내용 | Size |
|------|------|------|
| 11 | CombatScene 구성 + 통합 테스트 | L |

---

## 10. 수정 필요 사항 (기존 코드)

### 즉시 수정 (버그)

| 파일 | 문제 | 수정 방법 |
|------|------|-----------|
| `TurnManager.cs:86` | `CompareBySPD` 내 `Random.Range` → 정렬 불안정 | Sort 전 각 유닛에 tiebreaker float 값 미리 할당, comparator에서 그 값 비교 |
| `PositionSystem.cs` 주석 | "SlotIndex 1=최전방" 오기 | 주석을 "SlotIndex 0=최전방"으로 수정 |

### SkillExecutor 구현 전 수정

| 파일 | 문제 | 수정 방법 |
|------|------|-----------|
| `CombatUnit.cs:59` | `Skills = data.Skills` → 7개 전부 참조 | 생성자에 `SkillData[] selectedSkills` 파라미터 추가 |
| `SkillData.cs` | 확장 필드 8개 미추가 | Decision #22 참고하여 필드 추가 |
| `CombatEvent.cs` | 새 이벤트 4개 미추가 | 섹션 6 참고 |

### Phase 4에서 수정 (성능)

| 파일 | 문제 |
|------|------|
| `EventBus.cs:Publish` | 매 호출마다 `new List<Delegate>()` GC 할당 |
| `PositionSystem.cs:GetValidTargets` | 매 호출마다 `new List<CombatUnit>()` GC 할당 |

---

## 11. 코딩 컨벤션 체크리스트

- namespace 사용 안 함
- var 사용 안 함 (명시적 타입)
- 멤버 변수 `m_PascalCase`
- delegate 직접 선언 (Action 대신)
- for 루프 `++i`
- public 필드 금지 (`[SerializeField] private` + property)
- LINQ 사용 금지 (전투 루프)
- SkillExecutor, EnemyAI = Pure C#
- CombatStateMachine = MonoBehaviour
- EventBus 구독은 OnEnable/OnDisable 쌍
- 매직 넘버는 const로 정의
