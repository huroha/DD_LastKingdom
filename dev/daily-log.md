# Last Kingdom - Daily Log

---
# Daily Work Log

---

## Day 1 — 2026-02-25

### 완료 작업
- Unity 6 프로젝트 생성 (URP 2D)
- 폴더 구조 생성
- 필수 패키지 설치 (TextMeshPro, Input System, Newtonsoft JSON)
- CLAUDE.md 작성
- `Singleton<T>` 구현
- `EventBus` 구현

### 주요 결정
- EventBus: `Dictionary<Type, Delegate>` 구조로 초기 구현

---

## Day 2 — 2026-02-26

### 완료 작업
- `EventBus` 개선: `Dictionary<Type, List<Delegate>>`로 변경, Publish 시 복사본 순회
- `GameManager` 구현: GameState enum, delegate 직접 선언, event, 상태 전환 로그
- `SceneFlowManager` 구현: Async 씬 로딩, 전환별 페이드 속도 분리, FadeCanvas 코드 생성
- DebugBootstrap으로 콘솔 동작 확인

### 주요 결정 및 컨벤션 확립
- `var` 사용 금지 — 명시적 타입 선언
- `m_PascalCase` 멤버 변수 접두사 (CLAUDE.md 반영)
- `++i` 전위 연산자 사용
- `delegate void XxxHandler(...)` 직접 선언 — `Action<T>` 대신
- Dungeon↔Combat 씬 전환은 0.05f 빠른 페이드 (원작 DD 구조)

### Obsidian 노트
- `Day1 - EventBus.md` 업데이트 (List 구조 반영)
- `Day2 - GameManager.md` 생성
- `Day2 - SceneFlowManager.md` 생성


---

## Day 3 — 2026-02-27

### 완료 작업
- `StatBlock.cs` — 스탯 구조체 (ResistanceBlock 포함)
- `NikkeData.cs` — 니케 캐릭터 SO (HeroData에서 변경, NikkeClass/Manufacturer/ElementType/SquadData 포함)
- `SkillData.cs` — 스킬 SO (UsablePositions bool[4], TargetPositions bool[4], OnHitEffects)
- `SquadData.cs` — 스쿼드 SO (신규 추가)
- `EnemyData.cs` — 적 SO (DropTable 포함)
- `StatusEffectData.cs` — 상태이상 SO (Phase 2 태스크 선행 완료)
- 테스트 에셋 생성 시작 (킬로/크라운/랩처/랩처엘리트/스킬5종/출혈)

### 주요 결정
- `HeroData` → `NikkeData` 로 네이밍 변경 (세계관 반영)
- 속성(Element)은 스킬이 아닌 캐릭터에 귀속 (원작 니케 구조)
- 크리티컬 패시브 효과 NikkeData에 배치 (`m_OnCritSelfEffects`, `m_OnReceiveCritSelfEffects`)
- DropTable에 경험치 제외 — 경험치는 던전 탐험 완료 시 별도 지급

### Obsidian 노트
- `Day3 - ScriptableObject & Data Layer.md` 생성

---

## Day 4 — 2026-03-02

### 완료 작업
- `EnemyData.cs` — `CorpseHp` 필드 추가
- `CombatUnit.cs` — 런타임 유닛 클래스 (Pure C#)
- `ActiveStatusEffect.cs` — 런타임 상태이상 인스턴스
- `CombatEvent.cs` — 전투 EventBus 이벤트 타입 선언 (struct)
- `TurnManager.cs` — SPD 기반 턴 순서 관리
- `CombatUnitTest.cs` — SO 에셋 연동 테스트
- `PositionSystem.cs` — 위치 시스템 (슬롯 관리, 스킬 위치 판정, 포지션 앞당김)

### 주요 결정
- UnitState: Alive / DeathsDoor / Corpse / Dead 4단계
- Nikke 죽음: HP 0 → DeathsDoor → 추가 피해 → deathBlowResist 판정
- Enemy 죽음: 일반 피해 → Corpse(별도 HP), DOT 피해 → 즉시 Dead
- Corpse: 일반 스킬 타겟 가능, DOT도 적용, HP 0 되면 즉시 Dead
- Nikke 생성자에 currentHp/ebla/activeEffects 주입 — 전투 간 상태 유지
- 적도 Heal 가능 (Nikke 전용 제한 제거)
- TurnManager: Pure C#, Singleton 아님, Combat/ 폴더
- SPD 동점 시 Nikke 우선, 같은 타입 동점 시 랜덤
- SlotIndex 1 = 최전방 (적과 인접), 시각 반전(4→3→2→1)은 UI 담당
- 슬롯 배열 가변 크기 — `new CombatUnit[팀 인원 수]`
- PositionSystem.GetValidTargets는 위치 후보만 반환, Single/All 구분은 SkillExecutor 담당
- RemoveUnit: 제거 후 뒤 유닛 전체 한 칸 앞당김 + SlotIndex 갱신

### 발견/수정한 버그
- CombatUnit 생성자에서 NikkeData/EnemyData 미할당
- TurnManager BuildTurnOrder() 내 Sort가 for 루프 안에 위치
- PositionSystem RemoveUnit: null 처리만 했을 경우 빈 슬롯 구멍 발생 → 앞당김 로직 추가

### Obsidian 노트
- `Day4 - CombatUnit.md` 생성
- `Day4 - TurnManager.md` 생성
- `Day4 - PositionSystem.md` 생성

## Day 5 — 2026-03-03 (설계 & 코드 정비)

### 작업 내용
- planner/plan-reviewer 에이전트로 전체 설계 검토 완료
- `skill-executor-design.md` 작성 (SkillExecutor + FSM + UI 전체 설계)
- Decision #16~#26 확정 및 context.md 기록

### 코드 변경
- **TurnManager**: Sort 내 `Random.Range` → TieBreaker float 방식으로 수정 (정렬 불안정 버그픽스)
- **PositionSystem**: 주석 오기 수정 (SlotIndex 1=최전방 → 0=최전방)
- **CombatUnit**: `TurnOrderTieBreaker` 추가, `BuildSkillList` (선택 4개 스킬 보장)
- **SkillData**: `TargetType`에 EnemyMulti/AllyMulti 추가, 확장 필드 8개 추가
- **StatusEffectType**: Guard/Mark 추가
- **CombatEvent**: BattleStartedEvent, BattleEndedEvent, SkillExecutedEvent, UnitStateChangedEvent 추가

### 확정된 결정
- 데미지 공식: 범위 데미지(minDamage/maxDamage) + PROT % 방어
- 명중/크리티컬 공식 확정 (클램프 없음)
- 힐 스킬: 별도 m_HealAmount 필드
- 게임 루프 상세 흐름 확정
- 씬 구조: 기존 5개 유지, 새 화면은 패널 오버레이
- Phase 3에서 CombatScene → DungeonScene 내 CombatPanel 오버레이 전환 예정
- Quirk 시스템 Phase 2 구현 확정

---

## Day 7 — 2026-03-05 (전투 FSM + UI 구현)

### 완료 작업
- `EnemyAI.cs` — Pure C#, 랜덤 스킬 행동, 순환 순회
- `CombatStateMachine.cs` — 코루틴 기반 FSM, 전투 전체 흐름 제어
- `CombatHUD.cs` — EventBus 구독, HP/에블라 바, 턴 순서 텍스트 (StringBuilder)
- `SkillSelectPanel.cs` — Show/Hide 패턴, 스킬 버튼 4개 + Pass 버튼
- `TargetSelectPanel.cs` — Show/Hide 패턴, 타겟 버튼 8개 + Cancel 버튼
- CombatScene 구성 (Canvas 계층, Inspector 연결)

### 주요 결정
- 코루틴 기반 FSM 채택 (Update switch 방식 대신) — 선형 흐름이 시나리오처럼 읽힘
- UI-FSM 통신: 이벤트 방식 → Show() + delegate 콜백 방식으로 전환 (PassButton/Cancel 흐름 지원)
- ApplyPostBattleEbla(): FreeRounds 이후 라운드 누적합 × Multiplier — 복도 귀환 후 유지

### 발생/수정한 버그
- Korean font 경고: LiberationSans 한글 미지원 → TMP Font Asset 교체
- IndexOutOfRange (SkillSelectPanel): Skills.Count 기준 순회 → SkillButtons.Length 기준으로 수정
- 클로저 캡처 버그 예방: 루프 내 `int index = i` 로컬 복사

### /simplify 리뷰 수정
- `OnUnitDied` HP 바 중복 초기화 → `RefreshHpBar()` 호출로 통합
- `RefreshTurnOrder` string += → StringBuilder
- `RefreshButtons` 중복 null 분기 → ternary 단일화

### Obsidian 노트
- `Day7 - CombatStateMachine & EnemyAI.md` 생성
- `Day7 - Combat UI.md` 생성

---

## Day 6 — 2026-03-04 (SkillExecutor 구현)

### 작업 내용
- SkillExecutor.cs 전체 구현 완료

### 구현한 메서드
- `ValidateSkill` — 4단계 검증 (IsAlive, CanUseSkill, Awakened, 유효타겟)
- `Execute` — 전체 스킬 실행 파이프라인
- `ResolveTargets` — TargetType별 타겟 리스트 결정
- `RollHit` — 명중 판정
- `CalcDamage` — 데미지 계산 (BaseDamage → RawDamage → FinalDamage)
- `RollCrit` — 크리티컬 판정
- `ApplyOnHitEffects` — 상태이상 저항 판정 + ActiveEffects 적용
- `GetResistance` — StatusEffectType별 저항값 반환
- `ApplyCritEffects` — 크리티컬 추가 효과 (에블라, OnCritSelfEffects)
- `ApplyPositionMove` — 위치 이동

### 발견 및 수정한 버그
| 버그 | 원인 | 수정 |
|------|------|------|
| IsCrit 항상 false | RollCrit 결과를 result[i].IsCrit에 저장 안 함 | `result[i].IsCrit = RollCrit(...)` |
| 상태이상 타겟 간 누적 오염 | applied/resisted 리스트를 루프 밖에 선언 | 루프 안으로 이동 |
| DamageDealt 미기록 | TakeDamage 후 TargetResult에 저장 안 함 | `result[i].DamageDealt = damage` |

### /simplify 리뷰 후 수정 사항
- `using Unity.VisualScripting` 불필요한 import 제거
- `skill.HealAmount > 0` 3번 반복 → `bool isHealSkill` 추출
- `allNikkes` 무조건 fetch → 크리티컬 발생 시에만 lazy-fetch
- `AllyAll` 분기 간소화 (`user.UnitType` 직접 사용)
- `PositionSystem.GetUnit()` 불필요한 `int index` 변수 제거
- `CombatEvent.cs` 파라미터명 오타 수정 (`oldstate` → `oldState`)

---

## Day 8 — 2026-03-06 (CombatScene 통합 테스트)

### 완료 작업
- CombatScene Hierarchy 구성 및 Inspector 연결
- HP 슬라이더 색상/Fill Rect 수정
- SkillSelectPanel / TargetSelectPanel 초기화 버그 수정
- 명중률 0% 문제 → SO 에셋 accuracyMod 값 조정
- 빈 슬롯 UI 숨김 처리 (OnBattleStarted SetActive)
- TargetSelectPanel 버튼 가시성 분리 (SetActive vs interactable)
- **위치 이동 기능 추가**: Move 버튼, PlayerSelectMoveTarget 상태, GetValidMoveTargets()
- `UnitMovedEvent` 추가 → CombatHUD/TargetSelectPanel 갱신 연동

### 발견/수정한 버그
| 버그 | 원인 | 수정 |
|------|------|------|
| Cancel NullReferenceException | Show() 전 클릭 가능 | null 체크 + Awake SetActive(false) |
| 명중 항상 Miss | accuracyMod 수치 부족 | SO 에셋 수치 조정 |
| Move 후 다음 턴 오작동 | m_MoveRequested 미리셋 | HandlePlayerTurn 시작에 플래그 리셋 |
| 이동 후 버튼 사라짐 | Hide()가 SetActive 호출 | interactable만 제어하도록 변경 |
| BattleStartedEvent 이중 발화 | Awake + OnEnable 둘 다 구독 | OnEnable/OnDisable 제거 |

### /simplify 리뷰 수정
- `OnDestroy` public → private
- `OnUnitMoved` 비활성 시 early return 추가
- `RefreshNikkeSlots()` 미사용 `nikkes` 변수 제거

### 현재 상태
- Phase 1.6 진행 중 — 전투 루프/이동 기능 동작 확인, 승리/패배 전체 플로우 검증 필요

### Obsidian 노트
- `Day8 - CombatScene 통합 테스트.md` 생성
