# Last Kingdom Core - Task Checklist

## Status Legend
- [ ] Not started
- [🔄] In progress
- [✅] Complete
- [❌] Blocked
- [⏭️] Skipped

## Progress Summary
0 / 65 tasks complete (0%)

---

## Phase 1: Foundation & Prototype (3주)
> **Goal**: 턴제 전투 프로토타입 — Greybox 상태에서 핵심 전투 루프 동작 확인

### 1.1 프로젝트 셋업
- [ ] Unity 6 프로젝트 생성 (URP 2D 템플릿)
  - Details: Unity Hub에서 "2D (URP)" 템플릿 선택, 프로젝트명 "LastKingdom"
  - Acceptance: URP 2D Renderer 확인, 샘플 씬 60fps 동작
  - Size: S
  - Dependencies: Unity 6 설치

- [ ] 폴더 구조 생성
  - Details: Context 문서의 폴더 구조 그대로 생성
  - Acceptance: Scripts/, Data/, Prefabs/, Scenes/, Art/, Audio/ 등 전체 구조 확인
  - Size: S
  - Dependencies: 프로젝트 생성

- [ ] 필수 패키지 설치
  - Details: TextMeshPro, Input System, Newtonsoft JSON (com.unity.nuget.newtonsoft-json)
  - Acceptance: Package Manager에서 설치 확인
  - Size: S
  - Dependencies: 프로젝트 생성

- [ ] CLAUDE.md + dev/README.md 작성
  - Details: 프로젝트 표준, 코딩 컨벤션, 네이밍 규칙 정의
  - Acceptance: 문서 존재 및 아키텍처 패턴 명시
  - Size: S
  - Dependencies: 없음

### 1.2 기반 시스템
- [ ] Generic Singleton 베이스 클래스
  - File: `Assets/Scripts/Utils/Singleton.cs`
  - Details: MonoBehaviour 상속, DontDestroyOnLoad, 중복 방지
  - Acceptance: 여러 매니저에서 상속 사용 가능
  - Size: S
  - Dependencies: 없음

- [ ] EventBus (전역 이벤트 시스템)
  - File: `Assets/Scripts/Utils/EventBus.cs`
  - Details: static C# event/Action 기반, 타입 안전한 이벤트 발행/구독
  - Acceptance: 이벤트 발행 → 구독자 수신 테스트
  - Size: M
  - Dependencies: 없음

- [ ] GameManager 구현
  - File: `Assets/Scripts/Managers/GameManager.cs`
  - Details: 게임 상태(Title/Town/Dungeon/Combat) 관리, Singleton
  - Acceptance: 상태 전환 로그 출력 확인
  - Size: M
  - Dependencies: Singleton.cs

- [ ] SceneFlowManager 구현
  - File: `Assets/Scripts/Managers/SceneFlowManager.cs`
  - Details: Async 씬 로딩, 페이드 전환, 로딩 화면
  - Acceptance: Town→Combat 씬 전환 동작, 페이드 확인
  - Size: M
  - Dependencies: GameManager

### 1.3 데이터 레이어
- [ ] HeroData ScriptableObject 정의
  - File: `Assets/Scripts/Data/HeroData.cs`
  - Details: 이름, 클래스(enum), 기본스탯(HP/ATK/DEF/SPD/ACC/DODGE/CRIT), 스킬 슬롯[4], canVirtue 플래그
  - Acceptance: Inspector에서 데이터 입력 가능, CreateAssetMenu 동작
  - Size: M
  - Dependencies: 없음

- [ ] SkillData ScriptableObject 정의
  - File: `Assets/Scripts/Data/SkillData.cs`
  - Details: 이름, 사용가능위치 bool[4], 타겟범위 bool[4], 타겟타입, 기본데미지, 명중률, 크리티컬, 효과 리스트
  - Acceptance: Inspector에서 직관적 편집 가능
  - Size: M
  - Dependencies: StatusEffectData (기본 구조만)

- [ ] EnemyData ScriptableObject 정의
  - File: `Assets/Scripts/Data/EnemyData.cs`
  - Details: 이름, 스탯, 스킬 리스트, 드롭 테이블, 스프라이트
  - Acceptance: Inspector에서 데이터 입력 가능
  - Size: M
  - Dependencies: SkillData

- [ ] 테스트 데이터 SO 에셋 생성
  - Details: 영웅 2종 (킬로, 크라운), 적 2종, 스킬 4~5종 SO 에셋 파일 생성
  - Acceptance: 전투 테스트에 충분한 데이터
  - Size: M
  - Dependencies: HeroData, SkillData, EnemyData

### 1.4 전투 시스템 핵심
- [ ] CombatUnit 클래스
  - File: `Assets/Scripts/Combat/CombatUnit.cs`
  - Details: 런타임 유닛 데이터 (현재HP, 현재스탯, 위치 슬롯, 살아있음 여부)
  - Acceptance: SO 데이터로부터 인스턴스 생성 가능
  - Size: M
  - Dependencies: HeroData, EnemyData

- [ ] TurnManager 구현
  - File: `Assets/Scripts/Combat/TurnManager.cs`
  - Details: 모든 유닛 SPD 기반 내림차순 정렬, 현재 턴 유닛 추적, 턴 시작/종료 이벤트
  - Acceptance: 4명 영웅 + 3명 적 → 올바른 행동 순서 정렬 확인
  - Size: L
  - Dependencies: CombatUnit, EventBus

- [ ] PositionSystem 구현
  - File: `Assets/Scripts/Combat/PositionSystem.cs`
  - Details: 아군 슬롯[4] + 적 슬롯[4], 위치 이동/교환, 스킬 사용 가능 여부 판정
  - Acceptance: 유닛 위치 변경, 스킬 위치 제약 동작
  - Size: M
  - Dependencies: CombatUnit

- [ ] SkillExecutor 구현
  - File: `Assets/Scripts/Combat/SkillExecutor.cs`
  - Details: 스킬 선택 → 타겟 유효성 검증 → 데미지 계산(ATK vs DEF, ACC vs DODGE, CRIT) → 적용
  - Acceptance: 스킬 사용 → HP 감소 → 사망 판정 동작
  - Size: L
  - Dependencies: CombatUnit, PositionSystem, SkillData

- [ ] CombatStateMachine 구현
  - File: `Assets/Scripts/Combat/CombatStateMachine.cs`
  - Details: 상태 = PlayerSelectSkill → PlayerSelectTarget → Execute → EnemyTurn → CheckResult → 반복 / Victory / Defeat
  - Acceptance: 전투 시작부터 승리/패배까지 풀 플로우 동작
  - Size: XL
  - Dependencies: TurnManager, PositionSystem, SkillExecutor

- [ ] 적 AI 기초 (랜덤 행동)
  - File: `Assets/Scripts/Combat/EnemyAI.cs`
  - Details: 사용 가능한 스킬 중 랜덤 선택, 유효 타겟 중 랜덤 선택
  - Acceptance: 적 턴에 자동으로 스킬 사용
  - Size: M
  - Dependencies: CombatUnit, SkillExecutor

### 1.5 전투 UI
- [ ] 전투 HUD 기초
  - File: `Assets/Scripts/UI/CombatHUD.cs`
  - Details: 영웅/적 HP바 (Slider), 행동 순서 초상화 리스트, 현재 턴 하이라이트
  - Acceptance: HP 변화 실시간 반영, 행동 순서 시각적 확인
  - Size: L
  - Dependencies: CombatUnit, TurnManager

- [ ] 스킬 선택 UI
  - File: `Assets/Scripts/UI/SkillSelectPanel.cs`
  - Details: 현재 유닛의 스킬 4개 버튼 표시, 위치 제약에 따라 비활성화
  - Acceptance: 스킬 버튼 클릭 → 타겟 선택 모드 진입
  - Size: M
  - Dependencies: CombatHUD, PositionSystem

- [ ] 타겟 선택 UI
  - File: `Assets/Scripts/UI/TargetSelectPanel.cs`
  - Details: 유효 타겟 하이라이트, 클릭으로 타겟 확정
  - Acceptance: 유효 타겟만 선택 가능, 선택 시 스킬 실행
  - Size: M
  - Dependencies: SkillSelectPanel, SkillExecutor

### 1.6 테스트 씬
- [ ] CombatScene 구성
  - Details: 전투 배경 스프라이트(placeholder), 유닛 배치 위치, Canvas + HUD, CombatManager 오브젝트
  - Acceptance: Play 시 전투 시작, 스킬 사용, 승리/패배까지 플레이 가능
  - Size: L
  - Dependencies: 위 모든 전투 시스템

---

## Phase 2: Core Systems (4주)
> **Goal**: 마을↔던전↔전투 풀 루프 구현

### 2.1 에블라 & 상태이상
- [ ] EblaSystem 구현
  - File: `Assets/Scripts/Systems/EblaSystem.cs`
  - Details: 유닛별 에블라 수치(0~200). 0~100: 정상 구간. 100 도달: Affliction(기본)/Virtue(canVirtue 영웅) 발동 → 디버프 or 버프+일러스트 변경+특수 스킬. 100~200: 위험 구간(Affliction 디버프 유지). 200 도달: **영구 사망 발동**
  - Acceptance: 에블라 100 → Affliction 스탯 디버프 적용 / Virtue 영웅은 일러스트 변경 + 특수 스킬. 에블라 200 → 해당 영웅 즉시 사망 + 세이브에서 영구 제거
  - Size: L
  - Dependencies: CombatUnit, HeroData, SaveSystem(사망 처리)

- [ ] StatusEffectData SO 정의
  - File: `Assets/Scripts/Data/StatusEffectData.cs`
  - Details: 이름, 타입(DOT/Buff/Debuff), 지속턴, 틱데미지, 스탯보정 값, 스택 가능 여부
  - Size: M
  - Dependencies: 없음

- [ ] StatusEffectManager 구현
  - File: `Assets/Scripts/Combat/StatusEffectManager.cs`
  - Details: 유닛별 효과 리스트, 턴 종료 시 틱 처리, 만료 제거, 스택 관리
  - Acceptance: 독(DOT) 3턴 부여 → 매 턴 종료 데미지 → 3턴 후 제거
  - Size: L
  - Dependencies: StatusEffectData, CombatUnit

### 2.2 던전 시스템
- [ ] IDungeonMapProvider 인터페이스 + ManualDungeonMapProvider 구현
  - File: `Assets/Scripts/Dungeon/IDungeonMapProvider.cs`, `Assets/Scripts/Dungeon/ManualDungeonMapProvider.cs`
  - Details: IDungeonMapProvider 인터페이스 정의(GetMap() → DungeonMap 반환). ManualDungeonMapProvider는 SO로 정의된 수동 맵을 로드. 추후 ProceduralDungeonMapProvider로 교체 가능한 구조
  - Acceptance: 수동 맵 SO 3개 생성, DungeonNavigator가 인터페이스를 통해 맵 데이터 수신
  - Size: L
  - Dependencies: RoomData

- [ ] DungeonMapData SO 정의 (수동 맵 데이터)
  - File: `Assets/Scripts/Data/DungeonMapData.cs`
  - Details: 방 리스트, 연결 정보(그래프 edge), 시작방/보스방 지정, 복도 이벤트 슬롯
  - Acceptance: Inspector에서 수동으로 맵 구조 편집 가능
  - Size: M
  - Dependencies: RoomData

- [ ] RoomData SO 정의
  - File: `Assets/Scripts/Data/RoomData.cs`
  - Details: 방 타입(전투/보급/보물/보스/빈방), 가중치, 이벤트 리스트, 적 풀
  - Size: M
  - Dependencies: EnemyData

- [ ] DungeonNavigator 구현
  - File: `Assets/Scripts/Dungeon/DungeonNavigator.cs`
  - Details: 현재 위치 추적, 이동 가능 방향 표시, 복도 이동 중 이벤트 트리거, 방 진입 시 이벤트 실행
  - Acceptance: 맵에서 방 이동 → 전투방 진입 시 전투 씬 전환
  - Size: L
  - Dependencies: DungeonGenerator, SceneFlowManager

- [ ] DetectionSystem (위험발각)
  - File: `Assets/Scripts/Systems/DetectionSystem.cs`
  - Details: 전역 수치 0~100, 25단위 4단계. 전투 턴 초과 시 증가, 루팅/보급품으로 감소, 100 도달 시 강제 퇴각
  - Acceptance: 수치 변화 → 확률 테이블 교체 → 100 도달 시 퇴각 트리거
  - Size: L
  - Dependencies: EventBus

- [ ] ExplorationEventManager 구현
  - File: `Assets/Scripts/Dungeon/ExplorationEventManager.cs`
  - Details: 복도 이벤트(함정/아이템/전투), 골동품 상호작용(결과 테이블), 확률 기반 트리거
  - Acceptance: 복도 이동 시 랜덤 이벤트 발생, 골동품 상호작용 결과 적용
  - Size: L
  - Dependencies: DetectionSystem, InventorySystem

- [ ] CampSystem 구현
  - File: `Assets/Scripts/Dungeon/CampSystem.cs`
  - Details: 캠프파이어 아이템 소모, HP 회복량 결정, 에블라 감소, 야영 스킬 선택
  - Acceptance: 야영 → HP/에블라 변화 확인
  - Size: M
  - Dependencies: CombatUnit, EblaSystem

### 2.3 마을 시스템
- [ ] BuildingData SO 정의
  - File: `Assets/Scripts/Data/BuildingData.cs`
  - Details: 건물이름, 레벨별 데이터(비용, 효과, 해금조건), 최대레벨
  - Size: M
  - Dependencies: 없음

- [ ] BuildingManager 구현
  - File: `Assets/Scripts/Town/BuildingManager.cs`
  - Details: 대장장이(무기/방어구 업글), 길드(스킬 업글), 병원(에블라 감소), 업그레이드 비용 처리
  - Acceptance: 골드 소비 → 건물 레벨업 → 효과 적용
  - Size: L
  - Dependencies: BuildingData, DataManager

- [ ] RosterManager 구현
  - File: `Assets/Scripts/Town/RosterManager.cs`
  - Details: 영웅 풀 관리, 마차를 통한 신규 영입(랜덤 생성), 파티 편성, 영구 사망 처리
  - Acceptance: 새 영웅 영입, 파티 4명 선택, 사망 영웅 제거
  - Size: L
  - Dependencies: HeroData, SaveSystem

- [ ] ShopManager 구현
  - File: `Assets/Scripts/Town/ShopManager.cs`
  - Details: 보급품 리스트(횃불/음식/삽/열쇠), 가격, 구매 → 인벤토리 추가
  - Acceptance: 골드로 보급품 구매 → 인벤토리에 추가
  - Size: M
  - Dependencies: InventorySystem

### 2.4 인벤토리 & 장비
- [ ] InventorySystem 구현
  - File: `Assets/Scripts/Systems/InventorySystem.cs`
  - Details: 보급품 인벤토리 (던전 탐험용) + 전리품 인벤토리 (귀환 시 정산), 슬롯 제한
  - Acceptance: 아이템 추가/제거/사용, 슬롯 초과 시 경고
  - Size: L
  - Dependencies: SupplyItemData SO

- [ ] TrinketSystem 구현
  - File: `Assets/Scripts/Systems/TrinketSystem.cs`
  - Details: 장신구 SO 정의, 장착(슬롯 2개)/해제, 스탯 보정 적용
  - Acceptance: 장신구 장착 → 스탯 변화, 해제 → 원복
  - Size: M
  - Dependencies: TrinketData SO, CombatUnit

### 2.5 세이브 & 퀘스트
- [ ] SaveSystem v1 구현
  - File: `Assets/Scripts/Systems/SaveSystem.cs`
  - Details: JSON 직렬화(영웅 상태, 마을 레벨, 골드, 인벤토리), 자동저장 시점(던전 입장, 전투 시작, 영웅 사망, 귀환), Application.persistentDataPath 사용
  - Acceptance: 저장 → 게임 종료 → 재시작 시 상태 복원
  - Size: XL
  - Dependencies: 모든 상태 관리 시스템

- [ ] QuestSystem 구현
  - File: `Assets/Scripts/Systems/QuestSystem.cs`
  - Details: 퀘스트 타입(보스처치/아이템수집/탐험률), 완료 판정, 보상 지급
  - Acceptance: 보스 처치 → 퀘스트 완료 → 보상 획득
  - Size: M
  - Dependencies: EventBus

---

## Phase 3: Content & Polish (3주)
> **Goal**: 아트 통합, 연출, UI 완성, 콘텐츠 채우기

### 3.1 아트 & 애니메이션
- [ ] 영웅 스프라이트 통합 + Animator Controller
  - Details: PNG 스프라이트 → Sprite Sheet → Animation Clip (Idle/Attack/Hit/Death), Animator Controller에 State Machine 구성
  - Size: XL
  - Dependencies: 아트 에셋 준비

- [ ] 적 스프라이트 통합 + Animator Controller
  - Details: 적 종류별 Idle/Attack/Hit/Death 애니메이션
  - Size: XL
  - Dependencies: 아트 에셋 준비

- [ ] CombatFeedback 연출 시스템
  - File: `Assets/Scripts/VFX/CombatFeedback.cs`
  - Details: 히트스탑(Time.timeScale 조절), 카메라 흔들림(Cinemachine Impulse 또는 커스텀), 피격 플래시(SpriteRenderer color)
  - Acceptance: 공격 시 화면 흔들림 + 히트스탑 체감
  - Size: L
  - Dependencies: 전투 시스템

- [ ] 에블라 시각 연출
  - File: `Assets/Scripts/VFX/EblaVFX.cs`
  - Details: URP Volume Profile의 Color Grading 조절(에블라 높을수록 어두운 톤), 영웅 대사 텍스트 팝업
  - Acceptance: 에블라 증가 → 화면 색조 변화 확인
  - Size: L
  - Dependencies: EblaSystem, URP Post Processing

- [ ] 위험발각 시각 연출
  - Details: 단계별 Color Grading / Vignette 변화
  - Size: M
  - Dependencies: DetectionSystem, URP Post Processing

### 3.2 UI 완성
- [ ] 마을 UI 전체
  - File: `Assets/Scripts/UI/TownUI.cs`
  - Details: 건물 선택 화면, 업그레이드 패널, 영웅 로스터, 파티 편성, 영웅 상세 정보
  - Size: XL
  - Dependencies: BuildingManager, RosterManager

- [ ] 던전 UI 전체
  - File: `Assets/Scripts/UI/DungeonUI.cs`
  - Details: 미니맵(던전 그래프 시각화), 인벤토리 패널, 위험발각 게이지, 야영 UI
  - Size: L
  - Dependencies: DungeonNavigator, DetectionSystem

- [ ] 전투 HUD 완성
  - Details: 에블라 바 추가, 상태이상 아이콘 표시, 데미지 숫자 팝업, 버프/디버프 표시
  - Size: L
  - Dependencies: EblaSystem, StatusEffectManager

- [ ] 인벤토리 & 장비 UI
  - File: `Assets/Scripts/UI/InventoryUI.cs`
  - Details: 장신구 장착/해제, 보급품 관리, 전리품 확인
  - Size: L
  - Dependencies: InventorySystem, TrinketSystem

### 3.3 콘텐츠
- [ ] 영웅 클래스 4~6종 데이터 완성
  - Details: 클래스별 고유 스킬 4개, 기본 스탯 차별화, SO 에셋 생성
  - Size: XL
  - Dependencies: HeroData, SkillData

- [ ] 몬스터 다양화 (던전별 3~5종)
  - Details: 던전 3개 x 적 3~5종 = 9~15종 EnemyData SO
  - Size: L
  - Dependencies: EnemyData

- [ ] 건물 업그레이드 트리 데이터
  - Details: 건물 3종 x 레벨 3~5 데이터 입력
  - Size: L
  - Dependencies: BuildingData

- [ ] Virtue 시스템 완성
  - Details: Virtue 전용 영웅 일러스트 교체, 특수 스킬 전용 UI, Virtue 중 기본 스킬 사용 불가 처리
  - Size: L
  - Dependencies: EblaSystem, HeroData

- [ ] 하이퍼푸드 창고 최종 목표 흐름
  - Details: 창고 업그레이드 완료 → 최종 던전 해금 → 최종 보스 → 엔딩
  - Size: M
  - Dependencies: BuildingManager, QuestSystem

### 3.4 오디오
- [ ] AudioManager 구현 + BGM/SFX 통합
  - File: `Assets/Scripts/Managers/AudioManager.cs`
  - Details: BGM 전환(씬별), SFX 재생, AudioMixer 볼륨 조절
  - Size: L
  - Dependencies: 오디오 에셋

---

## Phase 4: Optimization & QA (2주)
> **Goal**: 성능, 안정성, 빌드

- [ ] 프로파일링 (CPU/GPU/Memory)
  - Details: Unity Profiler로 병목 확인, 전투 씬/던전 씬 중점
  - Size: L

- [ ] GC Allocation 최소화
  - Details: 전투 루프 내 매 프레임 할당 0B 목표, string 연산/LINQ/boxing 제거
  - Size: L

- [ ] Object Pooling 적용
  - File: `Assets/Scripts/Utils/ObjectPool.cs`
  - Details: 데미지 팝업, 이펙트, 상태이상 아이콘 등 풀링
  - Size: L

- [ ] Sprite Atlas 구성
  - Details: 캐릭터/UI/던전 별도 Atlas, Draw Call 최적화
  - Size: M

- [ ] 세이브 스캠 방지 검증
  - Details: 자동저장 시점 테스트, 프로세스 강제종료 후 복원 확인
  - Size: M

- [ ] 밸런스 패스
  - Details: 스킬 수치, 에블라 축적률, 위험발각 테이블, 건물 비용 전체 조정
  - Size: L

- [ ] 엣지 케이스 처리
  - Details: 전투 중 전멸, 에블라 200 사망, 씬 전환 중 상태 유지, 세이브 파일 손상
  - Size: L

- [⏭️] ~~Android 빌드 테스트~~ (스코프 제외)
  - Details: PC(Windows) 전용으로 결정. 향후 포팅 시 별도 마일스톤
  - Size: -
  - Dependencies: -

- [ ] 최종 QA 플레이스루
  - Details: 마을→던전→전투→귀환 5사이클 이상, 영구사망/세이브로드/업그레이드 전체 확인
  - Size: L

---

## Build & Deployment Checklist
- [ ] PC (Windows) 빌드 정상 동작
- [ ] 퍼포먼스 프로파일링 완료 (60fps 유지)
- [ ] GC 할당 최소화 확인
- [ ] Missing Reference 없음
- [ ] 모든 씬 빌드 세팅에 포함
- [ ] Quality Settings 적절 설정
- [ ] 입력 테스트 (키보드 + 마우스)
- [ ] 오디오 믹스 밸런스

## QA Checklist
- [ ] 전투 풀 루프 (시작→스킬사용→승리/패배)
- [ ] 에블라 100 도달 → Affliction 발동 (디버프 적용)
- [ ] Virtue 영웅 에블라 100 → Virtue 발동 + 일러스트 변경 + 특수 스킬 전용
- [ ] 에블라 200 도달 → 영구 사망 발동 → 세이브에서 제거
- [ ] 영구 사망 → 로스터에서 제거 → 세이브 반영
- [ ] 던전 생성 → 이동 → 전투 → 보스 → 귀환
- [ ] 위험발각 100 → 강제 퇴각
- [ ] 건물 업그레이드 → 효과 적용
- [ ] 보급품 구매 → 던전 내 사용
- [ ] 야영 → HP/에블라 회복
- [ ] 세이브 → 종료 → 로드 → 상태 복원
- [ ] 하이퍼푸드 창고 완성 → 최종 던전 해금
- [ ] 최종 보스 처치 → 엔딩

## Notes
- Phase 1 완료 후 반드시 플레이테스트하여 전투 감 확인
- C# 학습이 필요한 부분: 코루틴, async/await, SerializeField, ScriptableObject, Event/Action, LINQ 기초
- ✅ 절차적 던전 생성은 후순위 — IDungeonMapProvider 인터페이스로 추상화, 수동 맵으로 시작
- ✅ 에블라 확정: 100=Affliction/Virtue, 200=영구사망
- ✅ Android 포팅 제외 — PC(Windows) 전용
- 아트 에셋이 준비 안 되면 Unity Asset Store 무료 에셋이나 AI 생성 이미지로 대체
