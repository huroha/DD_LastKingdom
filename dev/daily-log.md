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

---

## Day 9 — 2026-03-09 (Phase1 버그 수정 + CombatFieldView)

### 완료 작업
- **카메라/씬 설정**: Orthographic Size 5.4 (PPU 100 기준 공식), 배경 이미지 정합
- **UI 설정**: Canvas 레이어 순서(Hierarchy), HP Bar 9-Slice 적용
- **phase1-review-fixes.md FIX-01~09 전부 수정 완료**
- **CombatFieldView.cs 구현**: World Space 유닛 뷰 동적 생성 + Lerp 이동 + 사망 처리

### phase1-review-fixes 수정 내역
| 항목 | 파일 | 내용 |
|------|------|------|
| FIX-01 | PositionSystem | GetAllUnits 필터 IsAlive로 변경 (Corpse 승리 판정 제외) |
| FIX-02 | CombatUnit, CombatStateMachine | IsStunned 프로퍼티 + 스턴 턴 스킵 + RemoveStun() |
| FIX-03 | SkillExecutor | target 이동 전 resistance.move 저항 판정 추가 |
| FIX-04 | SkillExecutor | target 이동을 IsHit == true일 때만 적용 |
| FIX-05 | CombatUnit | DeathsDoor 전환 시 AddEbla(18) |
| FIX-06 | SkillExecutor | ApplyCritEffects user.UnitType 분기 (적 크리 시 파티 에블라 증가) |
| FIX-07 | CombatUnit, EnemyData, PositionSystem, CombatStateMachine | 시체 자동 소멸 (CorpseTimer, CorpseDecayTurns, GetCorpses, TickCorpseTimers) |
| FIX-08 | CombatStateMachine | SurpriseType enum + StartBattle 파라미터 추가 (Phase 2 분기점) |
| FIX-09 | CombatStateMachine | 패스 시 AddEbla(PASS_EBLA_PENALTY) |

### CombatFieldView 구현
- World Space SpriteRenderer 기반 (Canvas UI 아님)
- `OnBattleStarted`: 유닛 GameObject 동적 생성, 슬롯 위치 배치
- `OnUnitMoved`: MoveAllToCurrentSlots → LerpToPosition 코루틴
- `OnUnitDied`: 코루틴 중단 + Destroy + 딕셔너리 제거
- 슬롯 앵커: CombatField 하위 빈 Transform (NikkeSlots/EnemySlots)

### /simplify 리뷰 수정 (10개)
- null 체크 순서 역전 버그 (StateMachine)
- ApplyCritEffects 에블라 이중 적용 + 잘못된 상수 버그
- LerpToPosition 최종 위치 미확정
- OnUnitDied 코루틴 미정지 후 Destroy
- OnBattleStarted 이전 뷰 오브젝트 누수
- m_EnemySlots 네이밍 오타
- PASS_EBLA_PENALTY 오타
- gameObject 변수명 충돌 → go
- 매직 넘버 0.3f → m_UnitScale SerializeField

### 현재 상태
- 아군 이동 연출 동작 확인 (Lerp 슬라이딩)
- 내일 전체 전투 플로우 추가 테스트 예정

### Obsidian 노트
- `Day9 - Phase1 버그수정 & CombatFieldView.md` 생성

---

## Day 10 — 2026-03-10 (CombatScene 테스트 & UI 보완)

### 완료 작업
- **EblaBar NullReferenceException 수정**: Inspector에서 Cells 배열 직접 할당
- **Ebla Bar 계산 수정**: `/ 10` (floor) → `Mathf.CeilToInt(/ 10f)` (ceil)
- **Ebla UI 실시간 갱신**: CombatHUD + NikkeInfoPanel에 TurnEndedEvent 구독 추가
- **모든 스킬 타겟 선택 강제**: `NeedsTargetSelection()` 제거, 원작 DD 방식 통일
- **EnemyAll/AllyAll UI 처리**: `GetValidTargets()`에서 All 타입은 전원 반환, 아무 유닛 클릭으로 발동
- **moveRange 스탯 추가**: `StatBlock.moveRange` + `GetValidMoveTargets()` 범위 확장
- **적 시체 스프라이트**: `EnemyData.CorpseSprite` + `m_CorpseViews` 딕셔너리 (Corpse/Dead 분리 처리)
- **ProcessDeadUnits Corpse 분기**: Corpse 전환 시 UnitDiedEvent 즉시 발행 (RemoveUnit 없이)
- **NikkeInfoPanel 보완**: `m_CurrentUnit` 추적 + TurnEndedEvent HP/Ebla 갱신
- **UnitClickHandler**: 적 스프라이트 클릭 시 콘솔 정보 출력 (전투 중 언제든 사용 가능)

### 발견/수정한 버그
| 버그 | 원인 | 수정 |
|------|------|------|
| EblaBar NullReferenceException | Cells 배열 미할당 | Inspector 직접 채움 |
| Ebla 5 → 0칸 표시 | 정수 나눗셈 floor | CeilToInt로 변경 |
| 에블라 UI 미갱신 | TurnEndedEvent 미구독 | 구독 추가 |
| Self 스킬 자동 발동 | NeedsTargetSelection Self 미처리 | 메서드 제거 |
| Corpse 공격 시 KeyNotFoundException | Remove 후 재접근 | view 로컬 캐싱 |
| Corpse HP 0 표시 | ProcessDeadUnits Corpse 미처리 | Corpse 분기 추가 |
| CorpseHp=0 Division by Zero | 분모 0 | Mathf.Max(..., 1) 방어 코드 |
| 불필요한 using | 자동완성 오염 | NUnit, System.Net 제거 |

### 현재 상태
- CombatScene 전투 루프 대부분 동작 확인
- 내일 승리/패배 전체 플로우 + CorpseDecayTurns 동작 테스트 예정

### Obsidian 노트
- `Day10 - CombatScene 테스트 & UI 보완.md` 생성

---

## Day 11 — 2026-03-13 (CombatScene 버그수정 & 타겟 하이라이트)

### 완료 작업
- **니케 사망 시 HPBar 미숨김 수정**: `RefreshNikkeSlots()`에서 unit == null 시 HPBar/Name `SetActive(false)` 누락 → 추가
- **NikkeInfoPanel null 체크 추가**: `m_CombatStateMachine` 미연결 시 `ValidateSkill()` NPE → null guard 추가
- **아군 사망 시 파티원 에블라 증가**: `ALLY_DEATH_EBLA = 20`, `ApplyAllyDeathEbla()` 추가 (ProcessDeadUnits에서 Nikke 사망 시 호출)
- **타겟 선택 하이라이트 미리보기 구현** (TargetSelectPanel 신규 기능)
  - 스킬 선택 시 유효 타겟 슬롯 dim 이미지 표시
  - 호버 시 실제 피격 슬롯 bright 전환 (Single vs Multi/All 분기)
  - EventTrigger 기반 호버 이벤트 코드 등록
  - `Show()` 시그니처에 `SkillData skill` 추가

### 발견/수정한 버그
| 버그 | 원인 | 수정 |
|------|------|------|
| 니케 사망 HPBar value 0인 채 잔존 | `RefreshNikkeSlots()` SetActive 누락 | `SetActive(false)` 추가 |
| NikkeInfoPanel NPE | `m_CombatStateMachine` null 상태 | null guard 추가 |
| 스킬 미선택 호버 NPE | `m_ValidTargets == null` 상태 호버 발화 | `OnButtonHoverEnter/Exit` null guard |
| 하이라이트 이미지 초기 활성화 | Inspector active 기본값 | `Awake()` HideAllHighlights + Inspector 비활성화 |
| 타겟 클릭 후 NPE | `Hide()`에서 RefreshHighlights → null 접근 | HideAllHighlights 교체 + m_ValidTargets = null 선행 |
| 미사용 변수 | `OnUnitDied`의 `int index` 미사용 | 제거 |
| using 잔존 오염 | Unity.VisualScripting 불필요 import | 제거 |
| 메서드명 오타 | `ReslovePreviewTargets` | `ResolvePreviewTargets` 수정 |

### 현재 상태
- Phase 1.6 진행 중 — CombatScene 전투 루프 안정화
- StatusEffectManager(Bleed/Poison 틱) 미구현 — Phase 2 태스크 유지

### Obsidian 노트
- `Day11 - CombatScene 버그수정 & 타겟 하이라이트.md` 생성

---

## Day 12 — 2026-03-16 (TurnTicker / ActiveTurnBar / Large Enemy 시스템)

### 완료 작업
- **TurnTicker 구현**: 라운드 내 행동할 유닛에 Image UI 표시. RoundStartedEvent로 전원 show, TurnEndedEvent/UnitDiedEvent로 해당 유닛 hide
- **ActiveTurnBar 구현**: 현재 턴 유닛 위치에 snap되는 펄스 애니메이션 바. Unity Animator 루프 클립(TurnBarPulse) 제작, TurnStartedEvent로 위치 이동, TurnEndedEvent로 hide. RectTransform.position anchor 배열(m_NikkeBarAnchor / m_EnemyBarAnchor) 방식
- **Large Enemy(2슬롯) 시스템 전체 구현**:
  - `EnemyData.m_SlotSize` 필드 추가 (기본값 1)
  - `CombatUnit.SlotSize` 프로퍼티 추가 (Nikke=1, Enemy=EnemyData.SlotSize)
  - `PositionSystem.Initialize`: size-2 유닛 slots[SlotIndex], slots[SlotIndex+1] 동일 참조
  - `PositionSystem.MoveLargeUnit` 전면 재작성: displaced/targetSlots 배열 방식, size-2 displaced 유닛 swap 확장(재귀), IsFirstOccurrence 헬퍼
  - `PositionSystem.Move` forward shift 버그 수정: `slots[i-1] != slots[i]` 조건으로 size-2 SlotIndex 중복 갱신 방지
  - `CombatFieldView`: size-2 시 두 슬롯 중점 위치, m_LargetUnitScale 적용
  - `CombatHUD`: m_LargeEnemyHpBars/TurnTickers/BarAnchors 3개, OnUnitMoved에 HideAllTickers+RefreshTurnTickers 추가, SetTickerVisible/SnapTurnBar/RefreshHpBar size-2 분기
  - `TargetSelectPanel`: m_LargeEnemyButtons/Highlights 3개, RefreshButtons isAnchor 체크, OnTargetButtonClicked Hide() 선행 후 콜백
- **LargeEnemySlot 2개→3개**: 4칸 슬롯에서 size-2 유닛 시작 위치가 0, 1, 2 세 곳 가능 — 모든 SlotIndex/2 → SlotIndex 직접 사용으로 변경

### 발견/수정한 버그
| 버그 | 원인 | 수정 |
|------|------|------|
| Large pull -1 잘못된 결과 | displaced/targetSlots 방향 오류 | MoveLargeUnit 전면 재작성 |
| Move +2 넉백 시 size-2 유닛 겹침 | forward shift에서 SlotIndex 중복 갱신 | `slots[i-1] != slots[i]` 조건 추가 |
| AABB에서 B -1 시 실패 | numDisplaced=1인데 displaced가 size-2 | extendedSteps 재귀 호출로 swap 처리 |
| pull -2 시 2번째 displaced 미처리 | 단일 blocker만 처리 | numDisplaced 루프로 전체 처리 |
| TargetSelectPanel Large 버튼 갱신 번쩍 | Hide() 전에 콜백 → UnitMovedEvent | Hide() 먼저, 콜백 나중 |
| TurnTicker 이동 후 위치 갱신 안됨 | OnUnitMoved에 ticker 갱신 없음 | HideAllTickers + RefreshTurnTickers 추가 |
| 3번째 LargeHighlight 초기 활성 | LargeEnemySlot 배열 크기 불일치 | 슬롯 수 2개→3개로 통일 |
| Ally 스킬 버튼 클릭 안됨 | RefreshButtons에서 NikkeButton interactable 미설정 | ValidTargets.Contains() 체크 추가 |

### /simplify 리뷰 수정
- `HideAllTickers()` 메서드 추출 (코드 중복 제거)
- `IsFirstOccurrence()` static 헬퍼 추출 (MoveLargeUnit 내 중복 로직)
- `m_PreviewTargets` List 캐싱 (매 호버마다 new List 방지)
- `m_LargeEnemyNames` 미사용 배열 제거

### 현재 상태
- Large Enemy(2슬롯) 이동/전투/UI 전체 동작 확인
- Phase 1.6 계속 진행 중

### Obsidian 노트
- `Day12 - Large Enemy & 이동 시스템.md` 생성

---

## Day 13 — 2026-03-17

### 완료 작업
- **PositionSystem 강제이동 버그 수정**
  - `Move()` size-1: 슬롯 수 기준 → 유닛 수 기준 cursor 방식으로 교체 (size-2 경로 통과 버그 수정)
  - `MoveLargeUnit()`: return false → 클램프 후 가능한 만큼 이동
  - `RemoveUnit()`: size-2 SlotIndex 이중 덮어씀 수정 (`slots[i-1] != slots[i]` 조건)
  - `GetCorpses()`: size-2 중복 추가 수정 (`Contains` → `SlotIndex == i` 패턴)
  - `GetAllUnits()`: size-2 중복 반환 수정 (`SlotIndex == i` 체크)
  - `CanUseSkill()`: size-2 모든 점유 슬롯 체크 (for 루프)
- **보스 다중 행동 시스템**
  - `EnemyData.m_ActionsPerRound` 필드 추가
  - `CombatUnit.ActionsPerRound` 프로퍼티 (Enemy: data 참조, Nikke: 1 고정)
  - `TurnManager.BuildTurnOrder()` 페이즈 분리 방식으로 전면 변경
  - `TurnManager.CurrentTurnIndex` 노출 → CombatStateMachine에 전달
- **CombatHUD Ticker 시스템 완성**
  - `TickerGroup { Image[] Tickers }` 구조체로 교체 (슬롯당 N개 ticker)
  - `SetTickerCount()`: diff 체크 + 애니메이션 없이 활성화
  - `RefreshTurnTickers()`: HideAllTickers 선행 + IsAlive 필터 + currentIndex+1 시작
  - `HideOneTicker()`: 뒤에서부터 숨김
  - `ShowAllTickersAnimated()` + `Ticker_Bounce.anim` pop-in 애니메이션
  - `IsTickerAnimating` 프로퍼티 + `TickerAnimTimer` 코루틴
- **ActiveTurnBar size-2 대응**: `m_LargeActiveTurnBar` 별도 Image 추가 + isLarge 분기
- **CombatStateMachine 타이밍 수정**
  - 이동 중 적 행동 버그: TurnEnd 후 `while (m_FieldView.IsMoving)` 대기
  - Ticker pop-in 후 전투 시작: `StartNextTurn()` 후 `while (m_CombatHUD.IsTickerAnimating)` 대기
  - `TurnStartedEvent` 발행을 TurnManager → CombatStateMachine으로 이전 (대기 후 발행)
  - `StartTestBattle()` slotIndex 카운터 방식으로 교체
  - `CombatHUD.OnBattleStarted()` size-2 이후 HP bar 인덱스 버그 수정

### 발견/수정한 버그
| 버그 | 원인 | 수정 |
|------|------|------|
| Ticker 위치 갱신 안됨 (시체 제거/이동 후) | 이전 슬롯 ticker 미hide | RefreshTurnTickers 앞 HideAllTickers() 선행 |
| Corpse 상태 ticker 표시 | TurnOrder에 Corpse 잔류 | `!unit.IsAlive` 필터 추가 |
| 이동 애니메이션 중 적 행동 | FieldView Lerp 완료 미대기 | IsMoving 폴링 추가 |
| Ticker 팝인 전 행동 시작 | StartNextTurn 전에 대기 → 라운드 전환 감지 불가 | StartNextTurn 후로 대기 이동 |
| Move hover NullReferenceException | Move 모드에서 m_CurrentSkill null | ResolvePreviewTargets null 체크 추가 |
| OnUnitMoved HideAllTickers 중복 | RefreshTurnTickers 내부에서 이미 호출 | 제거 |
| size-2 CorpseTimer 2회 감소 | GetCorpses 중복 반환 | SlotIndex == i 패턴 적용 |

### /simplify 리뷰 수정
- `GetCorpses()` O(n²) Contains → O(1) SlotIndex == i 패턴
- `BuildTurnOrder()` phase 지역 변수 → `m_PhaseBuffer` 멤버 변수 (GC 절감)
- `OnUnitMoved` 중복 `HideAllTickers()` 제거
- `using Unity.VisualScripting` 미사용 import 제거
- `ShowAllTickersAnimated()` IsAlive 필터 추가
- `m_ActiveTurnUnit` → `m_CombatStateMachine.ActiveUnit` 직접 참조

### 설계 확정
- Nikke는 항상 size-1, ActionsPerRound 항상 1 고정
- 아군 다중 행동은 InsertExtraTurn 스킬로만 (Phase 2)
- TurnStartedEvent 발행: TurnManager → CombatStateMachine (Ticker 대기 타이밍 확보)

### Obsidian 노트
- `Day13 - size-2 완성 & 다중행동 & Ticker 시스템.md` 생성
