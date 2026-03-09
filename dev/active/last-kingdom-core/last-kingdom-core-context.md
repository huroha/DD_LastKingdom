# Last Kingdom Core - Context & Decisions

## Status
- Phase: Phase 1.6 진행 중 (CombatScene 통합 테스트)
- Progress: 22 / 65 tasks complete (34%)
- Last Updated: 2026-03-07

## Key Files

### 프로젝트 구조 (현재 상태)
```
Assets/
├── Scripts/
│   ├── Managers/
│   │   ├── GameManager.cs          # 전역 싱글톤, 게임 상태 관리 ✅
│   │   ├── SceneFlowManager.cs     # 씬 전환 (Town↔Dungeon↔Combat) ✅
│   │   ├── AudioManager.cs         # BGM/SFX 관리
│   │   └── DataManager.cs          # SO 로딩, 세이브/로드 중재
│   ├── Data/                        # ScriptableObject 정의
│   │   ├── StatBlock.cs            # 스탯 구조체 (ResistanceBlock 포함) ✅
│   │   ├── NikkeData.cs            # 니케 캐릭터 데이터 ✅
│   │   ├── SkillData.cs            # 스킬 데이터 ✅
│   │   ├── EnemyData.cs            # 적 데이터 ✅
│   │   ├── StatusEffectData.cs     # 상태이상 데이터 ✅
│   │   ├── SquadData.cs            # 스쿼드 그룹 데이터 ✅
│   │   ├── RoomData.cs             # 방 타입/이벤트 데이터
│   │   ├── BuildingData.cs         # 건물 업그레이드 데이터
│   │   ├── TrinketData.cs          # 장신구 데이터
│   │   └── SupplyItemData.cs       # 보급품 데이터
│   ├── Combat/
│   │   ├── CombatUnit.cs           # 런타임 유닛 (Pure C#) ✅
│   │   ├── ActiveStatusEffect.cs   # 런타임 상태이상 인스턴스 (Pure C#) ✅
│   │   ├── CombatEvent.cs          # 전투 EventBus 이벤트 타입 선언 ✅
│   │   ├── CombatState.cs          # 전투 FSM 상태 enum ✅
│   │   ├── SkillResult.cs          # 스킬 실행 결과 구조체 ✅
│   │   ├── EnemyAction.cs          # 적 행동 결정 구조체 ✅
│   │   ├── TurnManager.cs          # SPD 기반 행동 순서 (Pure C#) ✅
│   │   ├── PositionSystem.cs       # 4슬롯 포지션 관리 ✅
│   │   ├── SkillExecutor.cs        # 스킬 실행, 데미지 계산 ✅
│   │   ├── StatusEffectManager.cs  # DOT/Debuff 틱 처리
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
│   │   ├── RosterManager.cs        # 니케 풀 관리
│   │   └── ShopManager.cs          # 보급품 구매
│   ├── UI/
│   │   ├── CombatHUD.cs            # 전투 UI
│   │   ├── TownUI.cs               # 마을 UI
│   │   ├── DungeonUI.cs            # 던전 UI
│   │   ├── InventoryUI.cs          # 인벤토리/장비 UI
│   │   └── NikkeInfoPanel.cs       # 니케 상세 정보
│   ├── VFX/
│   │   ├── CombatFeedback.cs       # 히트스탑/쉐이크/플래시
│   │   └── EblaVFX.cs              # 에블라 시각 연출
│   └── Utils/
│       ├── Singleton.cs            # 제네릭 싱글톤 베이스 ✅
│       ├── ObjectPool.cs           # 오브젝트 풀
│       └── EventBus.cs             # 전역 이벤트 시스템 ✅
├── ScriptableObjects/
│   ├── Nikkes/                     # 니케 SO 에셋 ✅
│   ├── Skills/                     # 스킬 SO 에셋 ✅
│   ├── Enemies/                    # 적 SO 에셋 ✅
│   ├── StatusEffects/              # 상태이상 SO 에셋 ✅
│   ├── Squads/                     # 스쿼드 SO 에셋 ✅
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
│   ├── Nikkes/                     # 니케 일러스트/스프라이트
│   ├── Enemies/                    # 적 스프라이트
│   ├── Dungeon/                    # 던전 배경/타일
│   ├── Town/                       # 마을 배경/건물
│   ├── UI/                         # UI 스프라이트
│   └── VFX/                        # 이펙트 스프라이트
├── Audio/
│   ├── BGM/
│   └── SFX/
├── Animations/
│   ├── Nikkes/                     # 니케 Animator Controller + Clips
│   └── Enemies/                    # 적 Animator Controller + Clips
└── Resources/
    └── GameConfig/                 # 런타임 로드 필요한 설정
```

## Key Decisions

### 1. 렌더 파이프라인: URP 2D (2026-02-25)
- **Rationale**: 2D 프로젝트에 최적. Post Processing 내장 지원
- **Alternatives**: Built-in RP (Post Processing 별도 패키지 필요), HDRP (2D에 과함)
- **Trade-offs**: URP 2D Renderer는 3D 기능 제한적이나 본 프로젝트에선 불필요

### 2. 입력 시스템: New Input System (2026-02-25)
- **Rationale**: PC 키보드/마우스 입력 처리에 적합, Action Map 기반 입력 추상화
- **Alternatives**: Old Input Manager (간단하지만 확장성 낮음)
- **Trade-offs**: 학습 곡선이 약간 있지만 장기적 이점이 큼

### 3. UI: UGUI (Canvas 기반) (2026-02-25)
- **Rationale**: 초급자에게 직관적, 레퍼런스 풍부, Inspector에서 시각적 편집
- **Alternatives**: UI Toolkit (코드 기반, 웹 스타일 레이아웃)
- **Trade-offs**: UGUI는 Canvas 재구축 비용이 있지만 본 프로젝트 규모에서는 문제없음

### 4. 전투 씬 분리 (2026-02-25) — Phase 3에서 오버레이로 전환 예정
- **Rationale**: 디버깅 용이, 메모리 격리, 관리 쉬움
- **Alternatives**: Additive Scene Loading, 같은 씬 내 UI 오버레이
- **Trade-offs**: 씬 전환 시 로딩 발생하지만 Async + 페이드 연출로 커버
- **Phase 3 전환 예정**: 원작 DD와 동일하게 DungeonScene 내 CombatPanel 오버레이로 변경. CombatScene은 전투 단독 테스트 용도로 유지 가능. Decision #25(패널 오버레이 방식)와 일관성 유지

### 5. 애니메이션: Sprite Animation (FlipBook 스타일) (2026-02-25)
- **Rationale**: 개발자의 DirectX 2D 경험과 일치. PNG 에셋 준비 가능
- **Alternatives**: Spine 2D (부드러운 보간, 에셋 제작 복잡), Unity 2D Animation (리깅 필요)
- **Trade-offs**: 프레임 수 많으면 메모리 증가, Sprite Atlas로 최적화

### 6. 세이브 방식: JSON + 자동저장 (2026-02-25)
- **Rationale**: 영구 사망 게임에서 세이브 스캠 방지 필수. 핵심 시점 자동저장
- **Alternatives**: Binary Serialization (보안 좋지만 디버깅 어려움), PlayerPrefs (소규모만)
- **Trade-offs**: JSON은 사용자가 파일 수정 가능 → 암호화/체크섬 추가 고려

### 7. 에블라 수치 범위 0~200 2단계 구조 (2026-02-25)
- **확정**: 0~100 정상 구간 → 100 도달 시 Affliction(기본)/Virtue(특정 영웅) 발동 → 100~200 위험 구간 → 200 도달 시 영구 사망
- **Rationale**: 원작 DD의 스트레스 시스템과 동일한 2단계 구조. 100에서 디버프, 200에서 사망이라는 명확한 위험 곡선
- **게임플레이 효과**: 100 이후에도 계속 던전을 진행할지 퇴각할지의 리스크 판단 요소

### 8. 던전 생성: 수동 맵 우선 + 인터페이스 추상화 (2026-02-25)
- **확정**: IDungeonMapProvider 인터페이스 정의, 초기에는 ManualDungeonMapProvider(SO 기반 수동 맵)로 구현. 추후 ProceduralDungeonMapProvider로 교체 가능
- **Rationale**: 절차적 생성은 XL 사이즈 태스크이므로 MVP에서 제외. 인터페이스 추상화로 교체 비용 최소화
- **Trade-offs**: 수동 맵은 리플레이 가치가 낮지만, 핵심 루프 검증에 충분

### 9. 플랫폼: PC(Windows) 전용 (2026-02-25)
- **확정**: Android 포팅 스코프에서 제외
- **Rationale**: 3개월 1인 개발에서 멀티플랫폼은 비현실적. PC에 집중하여 품질 확보
- **비고**: URP 2D 기반이므로 향후 포팅 시 기술적 장벽은 낮음

### 10. HeroData → NikkeData 로 변경 (2026-02-27)
- **확정**: 영웅 데이터 클래스명을 세계관에 맞게 NikkeData로 변경
- **추가된 필드**: NikkeClass, Manufacturer, ElementType, SquadData, 스킬 슬롯 7개(4개 선택), StatGrowthPerLevel
- **Rationale**: 원작 니케 세계관 반영. 스킬 7개 중 4개 선택 시스템으로 커스텀 여지 확보

### 11. CombatUnit Pure C# 클래스 (2026-03-02)
- **확정**: CombatUnit은 MonoBehaviour가 아닌 Pure C# 클래스
- **Rationale**: 데이터/로직은 Pure C# 클래스 원칙. Ebla와 ActiveEffects를 별도 컴포넌트로 분리하지 않고 CombatUnit에 통합
- **기존 계획 변경**: EblaHandler.cs, StatusEffectHolder.cs 별도 컴포넌트 → CombatUnit 내 통합 필드로

### 12. UnitState 4단계 사망 판정 (2026-03-02)
- **확정**: Alive / DeathsDoor / Corpse / Dead
- **Nikke 사망**: HP 0 → DeathsDoor 진입, 추가 피해 시 deathBlowResist 판정 → 실패 시 Dead
- **Enemy 사망**: 일반 피해 HP 0 → Corpse(별도 CorpseHp 보유, 일반 스킬 타겟 가능), DOT 피해 HP 0 → 즉시 Dead
- **Corpse 제거**: 어떤 피해든 HP 0 → 즉시 Dead (deathBlow 없음)
- **Rationale**: 원작 DD의 Death's Door + 시체 시스템을 니케 세계관에 맞게 재설계

### 13. 전투 간 Nikke 상태 유지 (2026-03-02)
- **확정**: Nikke의 HP, Ebla, ActiveEffects는 전투 종료 후에도 유지
- **Rationale**: 던전 내 이동 턴마다 상태이상 소모. 충분히 이동하지 않으면 다음 전투에도 디버프 유지 → 리스크 관리 요소
- **구현**: Nikke 생성자에 currentHp, ebla, activeEffects 파라미터로 주입. Enemy는 매 전투 새 인스턴스 생성

### 15. SlotIndex 0-based (2026-03-02)
- **확정**: SlotIndex는 0-based. index 0 = 최전방
- **Rationale**: 배열 접근 시 `-1` 변환이 불필요해 off-by-one 버그 가능성 제거
- **UI 표시**: UI에서 사람이 읽는 번호가 필요할 때만 `SlotIndex + 1` 변환
- **CombatUnitTest**: 생성자 호출 시 슬롯 번호 0, 1, 2, 3으로 전달

### 14. TurnManager Non-Singleton (2026-03-02)
- **확정**: TurnManager는 Singleton이 아닌 Pure C# 클래스. Combat/ 폴더에 위치
- **Rationale**: 전투 중에만 존재하고 CombatStateMachine만 사용. DontDestroyOnLoad 불필요
- **규칙 확립**: Managers/ 폴더 = DontDestroyOnLoad 싱글톤 전용. 이름에 Manager가 붙어도 전투 전용이면 Combat/에 위치

## Component Architecture

### CombatUnit (전투 유닛) — Pure C# 클래스
```
CombatUnit (Pure C#)
├── 유닛 식별: CombatUnitType, UnitName, SlotIndex
├── SO 참조: NikkeData 또는 EnemyData
├── 런타임 상태: UnitState, IsAlive, CurrentHp, MaxHp
├── 스탯: BaseStats, CurrentStats (버프/디버프 반영)
├── 에블라: Ebla (Nikke 전용, 0~200)
└── 상태이상: ActiveEffects List<ActiveStatusEffect>
```

> 씬 시각 표현(SpriteRenderer, Animator)은 별도 MonoBehaviour에서 CombatUnit을 참조하는 구조로 Phase 3에서 연결 예정

### CombatManager (전투 관리)
```
GameObject "CombatManager"
└── CombatStateMachine.cs (MonoBehaviour) — 전투 FSM
      ├── TurnManager        (Pure C# 멤버) — 행동 순서 정렬/실행
      ├── PositionSystem     (Pure C# 멤버) — 4슬롯 x 2팀 관리
      ├── SkillExecutor      (Pure C# 멤버) — 스킬 실행 파이프라인
      └── StatusEffectManager(Pure C# 멤버) — DOT/Debuff 틱 처리
```

## ScriptableObject Data

### NikkeData.asset (구 HeroData)
```csharp
- m_NikkeName: string
- m_NikkeClass: NikkeClass (Attacker / Supporter / Defender)
- m_Manufacturer: Manufacturer (Pilgrim / Elysion / Missilis / Tetra / Abnormal)
- m_Element: ElementType (Fire / Water / Wind / Iron / Electric)
- m_Squad: SquadData
- m_CanVirtue: bool
- m_BaseStats: StatBlock
- m_MaxLevel: int
- m_ExpThresholds: int[]
- m_StatGrowthPerLevel: StatBlock
- m_Skills: SkillData[7]  (7개 중 4개 선택)
- m_PortraitSprite: Sprite
- m_VirtuePortraitSprite: Sprite
- m_OnCritSelfEffects: StatusEffectData[]
- m_OnReceiveCritSelfEffects: StatusEffectData[]
```

### SkillData.asset (Decision #22, #23에서 확장)
```csharp
- m_SkillName: string
- m_Description: string
- m_SkillIcon: Sprite
- m_SkillType: SkillType (Melee / Ranged)
- m_RequiredState: SkillRequiredState (None / Awakened)
- m_UsablePositions: bool[4]
- m_TargetPositions: bool[4]
- m_TargetType: TargetType (EnemySingle / EnemyMulti / EnemyAll / AllySingle / AllyMulti / AllyAll / Self)
- m_DamageMultiplier: float
- m_AccuracyMod: int
- m_CritMod: float
- m_HealAmount: int              # HP 회복량
- m_EblaDamage: int              # 에블라 피해량
- m_EblaHealAmount: int          # 에블라 감소량
- m_MoveUserAmount: int          # 사용자 위치 이동 (양수=후방, 음수=전방)
- m_MoveTargetAmount: int        # 대상 강제 이동 (양수=후방, 음수=전방)
- m_IsGuard: bool                # 호위 스킬 여부
- m_IsMark: bool                 # 마크 부여 여부
- m_MarkDamageBonus: float       # 마크 대상 추가 피해 배율
- m_OnHitEffects: StatusEffectData[]
```

### EnemyData.asset
```csharp
- m_EnemyName: string
- m_EnemyType: EnemyType (Normal / Elite / Boss)
- m_Element: ElementType
- m_BaseStats: StatBlock
- m_Skills: SkillData[]
- m_CorpseHp: int           # 시체 전환 시 HP
- m_DropTable: DropTable
- m_Sprite: Sprite
```

### StatusEffectData.asset
```csharp
- m_EffectName: string
- m_Icon: Sprite
- m_EffectType: StatusEffectType (Bleed / Poison / Disease / Stun / Buff / Debuff / Guard / Mark)
- m_Duration: int
- m_TickDamage: int
- m_StatModifier: StatBlock
- m_IsStackable: bool
- m_MaxStack: int
```

### 16. 데미지 공식: 범위 데미지 + PROT 방식 (2026-03-03)
- **확정**: StatBlock의 `atk` → `minDamage / maxDamage` 범위로 변경
- **데미지 공식**: `BaseDmg = Random(minDamage, maxDamage)` → `RawDmg = BaseDmg * SkillMultiplier` → `FinalDmg = RawDmg * (1 - defense/100)`
- **defense**: % 기반 감소율 (DD 원작의 PROT). 0~100 범위
- **Rationale**: DD 원작과 동일한 방식. 매 공격마다 변동성이 있어 전투에 긴장감 부여. PROT % 방식은 밸런스 조절이 직관적

### 17. 명중/크리티컬 공식 (2026-03-03)
- **명중 공식**: `HitChance = (UserACC + SkillAccuracyMod) - TargetDODGE`
- **명중 클램프**: 0% ~ 100% (제한 없음. DD 원작의 5%/95%와 다르게 극단적 빌드 허용)
- **크리티컬 공식**: `CritChance = baseCrit + SkillCritMod` → roll 판정 (명중 성공 시에만)
- **크리티컬 효과**: 데미지 1.5배 + 적 에블라 +15, 파티원 전체 에블라 -5
- **크리티컬 패시브**: NikkeData의 OnCritSelfEffects, OnReceiveCritSelfEffects 적용
- **Rationale**: 크리티컬이 단순 데미지 보너스가 아닌 전술적 의미(에블라 관리)를 가짐

### 18. 힐 스킬: 별도 m_HealAmount 필드 (2026-03-03)
- **확정**: SkillData에 `m_HealAmount` 필드 별도 추가
- **Rationale**: DamageMultiplier 음수 방식보다 데미지+힐 동시 스킬 표현 가능. DD 원작도 힐량이 별도 수치
- **힐 판정**: 명중 판정 없음 (항상 성공). 크리티컬 판정은 수행 (크리 시 힐량 1.5배)

### 19. 다중 타겟: EnemyMulti/AllyMulti 추가 (2026-03-03)
- **확정**: TargetType에 `EnemyMulti`, `AllyMulti` 추가
- **동작**: TargetPositions bool[4] 기반으로 해당 포지션의 모든 유닛 타격
- **예시**: TargetPositions [true, true, false, false] + EnemyMulti = 포지션 0-1 동시 공격
- **기존 EnemyAll/AllyAll**: 전체 유닛 대상 (TargetPositions 무시)
- **Rationale**: DD 원작의 "포지션 1-2만 공격" 패턴 표현 필수

### 20. SPD 고정값 유지 (2026-03-03)
- **확정**: TurnManager의 SPD 정렬에 랜덤 요소 추가하지 않음
- **동일 SPD 처리**: Sort 전에 tiebreaker 값 미리 할당 (comparator 내 Random.Range 제거)
- **Rationale**: 행동 순서 예측 가능성 유지. DD 원작(SPD+Random 1~8)과 다른 선택

### 21. Move 방향 규약 (2026-03-03)
- **확정**: `steps > 0` = 후방 이동 (index 증가), `steps < 0` = 전방 이동 (index 감소)
- **기준**: SlotIndex 0 = 최전방 (Decision #15와 일관)
- **Rationale**: index 증가 = 뒤로 물러남이 배열 구조와 직관적으로 일치

### 22. SkillData 확장 필드 (2026-03-03)
- **확정**: 아래 필드를 SkillData.cs에 추가
- **추가 필드**:
  - `m_HealAmount` (int): HP 회복량. 별도 필드로 데미지+힐 동시 스킬 표현 가능
  - `m_EblaDamage` (int): 에블라(스트레스) 피해량. HP 대신/추가로 에블라 피해
  - `m_EblaHealAmount` (int): 에블라 감소량. 힐 스킬과 조합 가능
  - `m_MoveUserAmount` (int): 사용자 위치 이동량. Lunge류 스킬 (양수=후방, 음수=전방)
  - `m_MoveTargetAmount` (int): 대상 강제 이동량. Knockback/Pull (양수=후방, 음수=전방)
  - `m_IsGuard` (bool): 호위 스킬 여부. Defender가 아군 대신 피격
  - `m_IsMark` (bool): 마크 부여 여부. 마크된 적에게 추가 피해
  - `m_MarkDamageBonus` (float): 마크 대상 추가 피해 배율
- **시체 타겟**: 별도 필드 없음. 모든 스킬이 Corpse 타겟 가능
- **StatusEffectType 확장**: Guard, Mark 추가
- **Riposte/Stealth**: Phase 1에서는 미추가. 필요 시 Phase 2에서 StatusEffectType 확장
- **Rationale**: DD 원작의 핵심 전투 메커니즘(스트레스 공격, 위치 이동, 호위, 표식) 모작에 필수

### 23. SkillData 확장 후 전체 필드 정리 (2026-03-03)
```csharp
SkillData.cs 최종 필드 목록:
- m_SkillName: string
- m_Description: string
- m_SkillIcon: Sprite
- m_SkillType: SkillType (Melee / Ranged)
- m_RequiredState: SkillRequiredState (None / Awakened)
- m_UsablePositions: bool[4]
- m_TargetPositions: bool[4]
- m_TargetType: TargetType (EnemySingle / EnemyMulti / EnemyAll / AllySingle / AllyMulti / AllyAll / Self)
- m_DamageMultiplier: float
- m_AccuracyMod: int
- m_CritMod: float
- m_HealAmount: int              // 신규
- m_EblaDamage: int              // 신규
- m_EblaHealAmount: int          // 신규
- m_MoveUserAmount: int          // 신규
- m_MoveTargetAmount: int        // 신규
- m_IsGuard: bool                // 신규
- m_IsMark: bool                 // 신규
- m_MarkDamageBonus: float       // 신규
- m_OnHitEffects: StatusEffectData[]
```

### 24. 핵심 게임 루프 상세 흐름 확정 (2026-03-03)
- **확정**: 전체 게임 루프 흐름
```
마을 → [출정] → 던전 선택 + 파티 편성 → [원정 준비] → 보급품 구매 → [출정]
→ 던전 (복도 이벤트/함정/파밍 + 방 전투) → [목표 달성 시 귀환 버튼 활성화]
→ [귀환] → 정산 화면 (골동품→Gold, 재료, 경험치, 특성 지급) → [마을 복귀] → 마을
```
- **던전 목표 예시**: 던전 N% 탐사 / 방 전투 100% 완료 / 목표물 N개 수집
- **귀환 버튼**: 목표 달성 전까지 비활성화 (QuestSystem 연동)
- **정산 보상**: 골동품 → Gold 자동 치환 / 업그레이드 재료 / 경험치 / 특성(Quirk)

### 25. 씬 구조 확정 — 패널 오버레이 방식 (2026-03-03)
- **확정**: 씬 구조는 기존 유지. 새 화면들은 패널 오버레이로 처리
```
BootScene → TitleScene → TownScene ↔ DungeonScene ↔ CombatScene
```
- **TownScene 내 패널**: 던전 선택 + 파티 편성 패널 / 보급품 구매 패널
- **DungeonScene 내 패널**: 정산 화면 패널
- **Rationale**: 1인 개발 규모에 적합. 원작 DD와 동일한 방식. 씬 전환 없이 즉각적 전환. 씬 수 증가 방지

### 26. Quirk(특성) 시스템 도입 확정 (2026-03-03)
- **확정**: Quirk 시스템 구현. 정산 화면에서 보상으로 지급
- **시점**: Phase 2에서 구현
- **설계 방향**: 긍정/부정 특성이 정산 보상으로 랜덤 지급. NikkeData 런타임에 QuirkData[] 필드 필요
- **Rationale**: 정산 보상 목록에 "특성 지급"이 명시됨. 리플레이 가치의 핵심 요소

### 27. 후반전 에블라 유지 (2026-03-07)
- **확정**: `ApplyPostBattleEbla()` 유지 (FreeRounds 초과 시 라운드 기반 에블라 부여)
- **Rationale**: DD 원작에서는 장기전 시 적 난입(Reinforcement)이 페널티. 본 프로젝트는 난입 미구현 대체 메커니즘으로 라운드 기반 에블라 채택
- **DD와 차이**: 원작은 개별 이벤트 기반 스트레스만 존재. 본 프로젝트는 추가로 전투 길이 페널티 부여

### 28. DOT 틱 타이밍: 턴 시작 시 (2026-03-07)
- **확정**: StatusEffectManager의 DOT(출혈/중독/질병) 틱은 해당 유닛의 **턴 시작 시** 처리
- **처리 순서**: 턴 시작 → DOT 틱 → 스턴 체크 → 행동 (또는 스턴 해제)
- **Rationale**: DD 원작과 동일. 스턴 중에도 DOT 피해를 받음. 스턴+출혈 콤보의 전술적 가치 보존

### 29. Affliction: 스탯 디버프만 (확장 가능) (2026-03-07)
- **확정**: Phase 2 EblaSystem의 Affliction은 스탯 디버프만 구현
- **확장 가능성**: 랜덤 행동(명령 거부, 아군 공격, 강제 패스, 파티 에블라 발언)은 인터페이스/전략 패턴으로 확장점만 마련
- **구현 방향**: Affliction 발동 시 StatBlock 디버프 적용. 행동 개입 로직은 추후 AfflictionBehavior 클래스로 분리 가능
- **Rationale**: 1인 개발 스코프 관리. 스탯 디버프만으로도 Affliction의 위험성 전달 가능

### 30. 원정 상태 관리: ExpeditionManager 신설 (2026-03-07)
- **확정**: DontDestroyOnLoad 싱글톤 `ExpeditionManager` 신설
- **위치**: `Assets/Scripts/Managers/ExpeditionManager.cs`
- **책임**: 원정(출정~귀환) 동안의 파티 상태(HP/Ebla/ActiveEffects), 보급품, 던전 진행도, 전리품 보유
- **생명주기**: TownScene에서 출정 시 생성/초기화 → 던전/전투 씬에서 유지 → 귀환 정산 후 초기화
- **전투 연동**: CombatStateMachine.StartBattle() 시 ExpeditionManager에서 파티 상태 읽기 → 전투 종료 시 결과 상태를 ExpeditionManager에 반영
- **Rationale**: GameManager 비대화 방지. 원정 단위로 데이터 응집. SaveSystem 자동저장 단위와 일치

### 31. 인벤토리 분리: 3개 독립 시스템 (2026-03-07)
- **확정**: 보급품 / 전리품 / 장신구를 각각 독립 시스템으로 분리
- **SupplyInventory**: 보급품 관리 (소모, 스택). 던전 내 사용
- **LootInventory**: 전리품 관리 (골동품/보석). 귀환 시 정산(Gold 변환)
- **TrinketSystem**: 장신구 장착/해제, 스탯 보정 (기존 tasks.md의 별도 태스크 유지)
- **Rationale**: 세 카테고리의 동작 규칙이 거의 겹치지 않음. DD 원작과 동일한 구조. 통합 시 분기 코드가 오히려 복잡

## Testing Notes
- **Phase 1 테스트**: CombatScene에서 전투 루프 수동 테스트
- **Phase 2 테스트**: 풀 사이클 플레이스루 (마을→던전→전투→귀환)
- **단위 테스트**: CombatUnitTest.cs로 SO → CombatUnit 인스턴스 생성 및 TakeDamage/Heal/AddEbla 검증 ✅

## Known Issues
- 아트 에셋 미확보 — 프로토타입은 플레이스홀더 사용
- 절차적 던전 생성은 후순위 — 수동 맵으로 시작, IDungeonMapProvider 인터페이스로 추후 교체 예정
- RecalculateStats()는 현재 placeholder — Phase 2 StatusEffectManager 구현 시 ActiveEffects 합산 로직 추가 필요
