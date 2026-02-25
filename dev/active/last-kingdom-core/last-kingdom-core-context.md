# Last Kingdom Core - Context & Decisions

## Status
- Phase: Pre-development (Planning)
- Progress: 0 / 0 tasks complete
- Last Updated: 2025-02-25

## Key Files

### 프로젝트 구조 (신규 생성 예정)
```
Assets/
├── Scripts/
│   ├── Managers/
│   │   ├── GameManager.cs          # 전역 싱글톤, 게임 상태 관리
│   │   ├── SceneFlowManager.cs     # 씬 전환 (Town↔Dungeon↔Combat)
│   │   ├── AudioManager.cs         # BGM/SFX 관리
│   │   └── DataManager.cs          # SO 로딩, 세이브/로드 중재
│   ├── Data/                        # ScriptableObject 정의
│   │   ├── HeroData.cs             # 영웅 클래스 데이터
│   │   ├── SkillData.cs            # 스킬 데이터
│   │   ├── EnemyData.cs            # 적 데이터
│   │   ├── StatusEffectData.cs     # 상태이상 데이터
│   │   ├── RoomData.cs             # 방 타입/이벤트 데이터
│   │   ├── BuildingData.cs         # 건물 업그레이드 데이터
│   │   ├── TrinketData.cs          # 장신구 데이터
│   │   └── SupplyItemData.cs       # 보급품 데이터
│   ├── Combat/
│   │   ├── TurnManager.cs          # 스피드 기반 행동 순서
│   │   ├── PositionSystem.cs       # 4슬롯 포지션 관리
│   │   ├── SkillExecutor.cs        # 스킬 실행, 데미지 계산
│   │   ├── StatusEffectManager.cs  # DOT/Debuff 틱 처리
│   │   ├── CombatUnit.cs           # 전투 유닛 기본 클래스
│   │   └── CombatStateMachine.cs   # 전투 FSM
│   ├── Systems/
│   │   ├── EblaSystem.cs           # 에블라 수치 관리
│   │   ├── DetectionSystem.cs      # 위험발각 시스템
│   │   ├── InventorySystem.cs      # 인벤토리 관리
│   │   ├── TrinketSystem.cs        # 장신구 장착/스탯보정
│   │   ├── SaveSystem.cs           # JSON 직렬화/역직렬화
│   │   └── QuestSystem.cs          # 퀘스트 목표 추적
│   ├── Dungeon/
│   │   ├── IDungeonMapProvider.cs  # 던전 맵 제공 인터페이스
│   │   ├── ManualDungeonMapProvider.cs # 수동 맵 (SO 기반)
│   │   ├── DungeonNavigator.cs     # 이동, 방 진입
│   │   ├── CampSystem.cs           # 야영 처리
│   │   └── ExplorationEventManager.cs # 함정/골동품/이벤트
│   ├── Town/
│   │   ├── BuildingManager.cs      # 건물 업그레이드
│   │   ├── RosterManager.cs        # 영웅 풀 관리
│   │   └── ShopManager.cs          # 보급품 구매
│   ├── UI/
│   │   ├── CombatHUD.cs            # 전투 UI
│   │   ├── TownUI.cs               # 마을 UI
│   │   ├── DungeonUI.cs            # 던전 UI
│   │   ├── InventoryUI.cs          # 인벤토리/장비 UI
│   │   └── HeroInfoPanel.cs        # 영웅 상세 정보
│   ├── VFX/
│   │   ├── CombatFeedback.cs       # 히트스탑/쉐이크/플래시
│   │   └── EblaVFX.cs              # 에블라 시각 연출
│   └── Utils/
│       ├── Singleton.cs            # 제네릭 싱글톤 베이스
│       ├── ObjectPool.cs           # 오브젝트 풀
│       └── EventBus.cs             # 전역 이벤트 시스템
├── ScriptableObjects/
│   ├── Heroes/                     # 영웅 SO 에셋
│   ├── Skills/                     # 스킬 SO 에셋
│   ├── Enemies/                    # 적 SO 에셋
│   ├── StatusEffects/              # 상태이상 SO 에셋
│   ├── Buildings/                  # 건물 SO 에셋
│   ├── Trinkets/                   # 장신구 SO 에셋
│   ├── Rooms/                      # 방 타입 SO 에셋
│   └── Supplies/                   # 보급품 SO 에셋
├── Prefabs/
│   ├── Combat/                     # 전투 유닛 프리팹
│   ├── Dungeon/                    # 던전 방/복도 프리팹
│   ├── UI/                         # UI 프리팹
│   └── VFX/                        # 이펙트 프리팹
├── Scenes/
│   ├── BootScene.unity
│   ├── TitleScene.unity
│   ├── TownScene.unity
│   ├── DungeonScene.unity
│   └── CombatScene.unity
├── Art/
│   ├── Heroes/                     # 영웅 일러스트/스프라이트
│   ├── Enemies/                    # 적 스프라이트
│   ├── Dungeon/                    # 던전 배경/타일
│   ├── Town/                       # 마을 배경/건물
│   ├── UI/                         # UI 스프라이트
│   └── VFX/                        # 이펙트 스프라이트
├── Audio/
│   ├── BGM/
│   └── SFX/
├── Animations/
│   ├── Heroes/                     # 영웅 Animator Controller + Clips
│   └── Enemies/                    # 적 Animator Controller + Clips
└── Resources/
    └── GameConfig/                 # 런타임 로드 필요한 설정
```

## Key Decisions

### 1. 렌더 파이프라인: URP 2D (2025-02-25)
- **Rationale**: 2D 프로젝트 + Android 포팅 목표에 최적. Post Processing 내장 지원
- **Alternatives**: Built-in RP (Post Processing 별도 패키지 필요), HDRP (2D에 과함)
- **Trade-offs**: URP 2D Renderer는 3D 기능 제한적이나 본 프로젝트에선 불필요

### 2. 입력 시스템: New Input System (2025-02-25)
- **Rationale**: PC + Android 멀티플랫폼. Action Map으로 입력 추상화
- **Alternatives**: Old Input Manager (간단하지만 멀티플랫폼 대응 어려움)
- **Trade-offs**: 학습 곡선이 약간 있지만 장기적 이점이 큼

### 3. UI: UGUI (Canvas 기반) (2025-02-25)
- **Rationale**: 초급자에게 직관적, 레퍼런스 풍부, Inspector에서 시각적 편집
- **Alternatives**: UI Toolkit (코드 기반, 웹 스타일 레이아웃)
- **Trade-offs**: UGUI는 Canvas 재구축 비용이 있지만 본 프로젝트 규모에서는 문제없음

### 4. 전투 씬 분리 (2025-02-25)
- **Rationale**: 디버깅 용이, 메모리 격리, 초급자에게 관리 쉬움
- **Alternatives**: Additive Scene Loading, 같은 씬 내 UI 오버레이
- **Trade-offs**: 씬 전환 시 로딩 발생하지만 Async + 페이드 연출로 커버

### 5. 애니메이션: Sprite Animation (FlipBook 스타일) (2025-02-25)
- **Rationale**: 개발자의 DirectX 2D 경험과 일치. PNG 에셋 준비 가능
- **Alternatives**: Spine 2D (부드러운 보간, 에셋 제작 복잡), Unity 2D Animation (리깅 필요)
- **Trade-offs**: 프레임 수 많으면 메모리 증가, Sprite Atlas로 최적화

### 6. 세이브 방식: JSON + 자동저장 (2025-02-25)
- **Rationale**: 영구 사망 게임에서 세이브 스캠 방지 필수. 핵심 시점 자동저장
- **Alternatives**: Binary Serialization (보안 좋지만 디버깅 어려움), PlayerPrefs (소규모만)
- **Trade-offs**: JSON은 사용자가 파일 수정 가능 → 암호화/체크섬 추가 고려

### 7. 에블라 수치 범위 0~200 2단계 구조 (2025-02-25)
- **확정**: 0~100 정상 구간 → 100 도달 시 Affliction(기본)/Virtue(특정 영웅) 발동 → 100~200 위험 구간 → 200 도달 시 영구 사망
- **Rationale**: 원작 Darkest Dungeon의 스트레스 시스템과 동일한 2단계 구조. 100에서 디버프, 200에서 사망이라는 명확한 위험 곡선 제공
- **게임플레이 효과**: 100 이후에도 계속 던전을 진행할지 퇴각할지의 리스크 판단 요소

### 8. 던전 생성: 수동 맵 우선 + 인터페이스 추상화 (2025-02-25)
- **확정**: IDungeonMapProvider 인터페이스를 정의하고, 초기에는 ManualDungeonMapProvider(SO 기반 수동 맵)로 구현. 추후 ProceduralDungeonMapProvider로 교체 가능
- **Rationale**: 절차적 생성은 XL 사이즈 태스크이므로 MVP에서 제외. 인터페이스 추상화로 교체 비용 최소화
- **Trade-offs**: 수동 맵은 리플레이 가치가 낮지만, 핵심 루프 검증에 충분

### 9. 플랫폼: PC(Windows) 전용 (2025-02-25)
- **확정**: Android 포팅 스코프에서 제외
- **Rationale**: 3개월 1인 개발에서 멀티플랫폼은 비현실적. PC에 집중하여 품질 확보
- **비고**: URP 2D 기반이므로 향후 포팅 시 기술적 장벽은 낮음

## Component Architecture

### CombatUnit (전투 유닛)
```
GameObject "HeroUnit_Kilo"
├── SpriteRenderer — 영웅 스프라이트
├── Animator — FlipBook 애니메이션
├── CombatUnit.cs — HP, 스탯, 위치 슬롯
├── EblaHandler.cs — 개별 에블라 수치
└── StatusEffectHolder.cs — 상태이상 리스트
```

### CombatManager (전투 관리)
```
GameObject "CombatManager"
├── TurnManager.cs — 행동 순서 정렬/실행
├── PositionSystem.cs — 4슬롯 x 2팀 관리
├── SkillExecutor.cs — 스킬 실행 파이프라인
└── CombatStateMachine.cs — 전투 FSM (PlayerTurn/EnemyTurn/Victory/Defeat)
```

## ScriptableObject Data

### HeroData.asset
```csharp
- heroName: string
- heroClass: HeroClass (enum)
- baseStats: StatBlock (HP, ATK, DEF, SPD, ACC, DODGE, CRIT)
- skillSlots: SkillData[4]
- trinketSlots: int (기본 2)
- canVirtue: bool (Virtue 가능 영웅 여부)
- virtueSkill: SkillData (Virtue 전용 스킬)
- portraitNormal: Sprite
- portraitVirtue: Sprite (Virtue 시 변경)
- portraitAffliction: Sprite
```

### SkillData.asset
```csharp
- skillName: string
- description: string
- usablePositions: bool[4] (사용 가능 위치)
- targetPositions: bool[4] (공격 가능 범위)
- targetType: TargetType (Single/Multi/Self/Party)
- baseDamage: float
- accuracy: float
- critModifier: float
- effects: StatusEffectData[] (부가 효과)
- eblaInflict: float (적에게 에블라 부여량)
- eblaSelf: float (자신 에블라 변화)
- animationClip: AnimationClip
- sfx: AudioClip
```

## Testing Notes
- **Phase 1 테스트**: CombatScene에서 전투 루프 수동 테스트
- **Phase 2 테스트**: 풀 사이클 플레이스루 (마을→던전→전투→귀환)
- **EditMode Tests**: 데미지 계산, 스피드 정렬, 에블라 수치 로직
- **PlayMode Tests**: 전투 FSM 전환, 씬 로딩, 세이브/로드

## Known Issues
- Unity 6 + C# 학습 곡선으로 Phase 1 예상 기간 초과 가능성
- 아트 에셋 미확보 — 프로토타입은 플레이스홀더 사용
- 절차적 던전 생성은 후순위 — 수동 맵으로 시작, IDungeonMapProvider 인터페이스로 추후 교체 예정
