# Combat 폴더 구조 리팩토링 계획

## 목표
- Combat 폴더를 역할별 하위 폴더로 분리
- 큰 파일(CombatHUD, CombatDirector)의 책임을 분리
- 메서드 순서를 논리적으로 정렬

## 폴더 구조

```
Assets/Scripts/Combat/
├── Core/           # FSM, 유닛, 이벤트, 상태
├── Skill/          # 스킬 실행, 결과
├── Turn/           # 턴 관리, 위치, 상태이상
├── AI/             # 적 AI
├── View/           # 비주얼, 연출, 카메라
└── UI/             # 전투 UI 패널
```

## 작업 순서

### Phase 1: 폴더 생성 + 기존 파일 이동 (분리 없이)
파일 내용 변경 없이 폴더만 정리.

| 파일 | 이동 위치 |
|------|-----------|
| CombatStateMachine.cs | Core/ |
| CombatState.cs | Core/ |
| CombatEvent.cs | Core/ |
| CombatUnit.cs | Core/ |
| SkillExecutor.cs | Skill/ |
| SkillResult.cs | Skill/ |
| ActiveStatusEffect.cs | Skill/ |
| TurnManager.cs | Turn/ |
| PositionSystem.cs | Turn/ |
| StatusEffectManager.cs | Turn/ |
| EnemyAI.cs | AI/ |
| EnemyAction.cs | AI/ |
| CombatFieldView.cs | View/ |
| CombatDirector.cs | View/ |
| CombatCameraTilt.cs | View/ |
| UnitAnimBridge.cs | View/ |

### Phase 2: CombatDirector 분리
CombatDirector(573줄)에서 독립 연출 단위를 분리.

**CombatFocusController.cs (View/)**
- FocusIn, FocusOut, AssignFocusPositions
- m_FocusScale, m_FocusOutDuration, m_FocusPoints, m_Camera, m_FocusFOV 관련 필드
- m_OriginalScales, m_OriginalPositions, m_OriginalSortingOrders, m_ViewCache 등 캐시
- m_BlurController 참조

**CombatDriftController.cs (View/)**
- StartDrift, StopDrift, DriftRoutine
- m_DriftSpeed, m_AllyTargetDirBuffer 관련 필드

CombatDirector에 남는 것:
- SkillSequence (메인 연출 코루틴)
- ProcessSingleHit, ProcessHitBatch, ProcessOneTarget
- SpawnDamagePopup, GetEffectColor
- DotTickRoutine
- HitSpriteRoutine
- CombatFocusController, CombatDriftController를 SerializeField로 참조

### Phase 3: CombatHUD 분리
CombatHUD(812줄)에서 독립 UI 단위를 분리.

**CombatTurnTickerDisplay.cs (UI/)**
- CacheTickerAnimators, SetTickerCount, RefreshTurnTickers
- HideAllTickers, HideOneTicker, ShowAllTickersAnimated
- ShowTickersAnimated, GetTickerGroup, TickerAnimTimer
- TickerGroup struct, 관련 필드

**CombatTurnBarDisplay.cs (UI/)**
- SnapTurnBar
- m_ActiveTurnBar, m_LargeActiveTurnBar
- m_NikkeBarAnchor, m_EnemyBarAnchor, m_LargeEnemyBarAnchors
- m_CurrentTurnUnit

CombatHUD에 남는 것:
- 이벤트 허브 (OnBattleStarted, OnTurnStarted 등)
- RefreshHpBar, RefreshNikkeSlots, RefreshEnemySlots
- UpdateEblaBar
- EnemyInfo, Highlight, Announce
- Tilt 연동
- 분리된 컴포넌트를 SerializeField로 참조하고 이벤트에서 위임

### Phase 4: UI 폴더 이동
기존 Assets/Scripts/UI/에서 전투 관련 UI를 Combat/UI/로 이동.

| 파일 | 이동 위치 |
|------|-----------|
| CombatHUD.cs | Combat/UI/ |
| TargetSelectPanel.cs | Combat/UI/ |
| SkillSelectPanel.cs | Combat/UI/ |
| CombatTurnTickerDisplay.cs | Combat/UI/ (신규) |
| CombatTurnBarDisplay.cs | Combat/UI/ (신규) |

### Phase 5: 메서드 순서 정렬
모든 파일에 통일된 메서드 순서 적용:

1. SerializeField
2. Private 필드
3. Public 프로퍼티
4. Unity Lifecycle (Awake → OnEnable → OnDisable → OnDestroy)
5. Public API
6. Event Handlers
7. Private Helpers
8. Coroutines
