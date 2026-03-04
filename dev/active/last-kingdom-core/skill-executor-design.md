# SkillExecutor 설계 문서

> Phase 1.4 (CombatStateMachine, EnemyAI) + Phase 1.5 (전투 UI) + Phase 1.6 (테스트 씬)
> 작성일: 2026-03-03
> 확정된 결정: Decision #16~#23 (context.md 참고)

---

## 1. CombatStateMachine FSM 설계

### 1.1 상태 전이 다이어그램

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

### 1.2 각 상태 OnEnter 처리

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

### 1.3 CombatStateMachine MonoBehaviour 구조

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

## 2. EnemyAI 기초 설계

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

## 3. 전투 UI 기초 (Phase 1.5)

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

## 4. CombatScene 구성 (Phase 1.6)

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

## 5. 구현 순서

### Phase 1.4 (남은 것)

| Step | 내용 | 파일 | Size |
|------|------|------|------|
| 1 | **EnemyAI** | `Combat/EnemyAI.cs` | M |
| 2 | **CombatStateMachine** | `Combat/CombatStateMachine.cs` | **XL** |

### Phase 1.5 (전투 UI)

| Step | 내용 | 파일 | Size |
|------|------|------|------|
| 3 | CombatHUD | `UI/CombatHUD.cs` | L |
| 4 | SkillSelectPanel | `UI/SkillSelectPanel.cs` | M |
| 5 | TargetSelectPanel | `UI/TargetSelectPanel.cs` | M |

### Phase 1.6 (테스트 씬)

| Step | 내용 | Size |
|------|------|------|
| 6 | CombatScene 구성 + 통합 테스트 | L |

---

## 6. Phase 4에서 수정 (성능)

| 파일 | 문제 |
|------|------|
| `EventBus.cs:Publish` | 매 호출마다 `new List<Delegate>()` GC 할당 |
| `PositionSystem.cs:GetValidTargets` | 매 호출마다 `new List<CombatUnit>()` GC 할당 |

---

## 7. 코딩 컨벤션 체크리스트

- namespace 사용 안 함
- var 사용 안 함 (명시적 타입)
- 멤버 변수 `m_PascalCase`
- delegate 직접 선언 (Action 대신)
- for 루프 `++i`
- public 필드 금지 (`[SerializeField] private` + property)
- LINQ 사용 금지 (전투 루프)
- EnemyAI = Pure C#
- CombatStateMachine = MonoBehaviour
- EventBus 구독은 OnEnable/OnDisable 쌍
- 매직 넘버는 const로 정의
