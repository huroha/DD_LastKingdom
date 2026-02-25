# Last Kingdom Core - Strategic Plan

## Executive Summary
니케 세계관을 기반으로 한 Darkest Dungeon 모작 프로젝트. 2D 턴제 RPG로 왕국(영지) 관리 → 던전 탐험 → 귀환의 핵심 루프를 구현한다. Unity 6 + URP 2D로 개발하며, 데이터 주도 설계(ScriptableObject)를 핵심 아키텍처로 채택하여 1인 개발 3개월 목표에 맞춘 단계적 구현 전략을 수립한다.

## Current State
- Unity 프로젝트 미생성 상태
- 아트 에셋 미준비 (2D 일러스트 PNG 기반 계획)
- 개발자 Unity 초급, C++ 숙련, DirectX 2D 경험 있음, C# 학습 필요
- 기획서 초안 완료 (본 문서)

## 기술 스택 결정

### 렌더 파이프라인: URP (Universal Render Pipeline)
- **이유**: 2D 프로젝트에 최적, Post Processing 지원, 향후 포팅 시에도 유리
- 2D Renderer를 사용하며 2D Light, Shadow 활용 가능
- Color Grading 등 Post Processing으로 위험발각 시각 연출 가능

### 입력 시스템: New Input System
- **이유**: PC 키보드/마우스 입력 처리에 적합, Action Map 기반으로 깔끔한 입력 추상화

### UI: UI Toolkit 또는 UGUI
- **권장**: UGUI (Canvas 기반) — 초급자에게 더 직관적, 튜토리얼/레퍼런스 풍부
- UI Toolkit은 Unity 6에서 런타임 지원이 개선되었으나 학습 곡선 고려 시 UGUI 권장

### 애니메이션: Sprite Animation (Animator + Animation Clip)
- DirectX 2D FlipBook 방식과 유사하게 Sprite Sheet → Animation Clip으로 처리
- Unity의 Animator Controller + State Machine으로 상태 전환 관리
- **대안**: Spine/DragonBones 같은 스켈레탈 애니메이션은 에셋 제작 부담으로 제외

### 데이터: ScriptableObject 중심 + JSON 보조
- 게임 데이터 (영웅, 스킬, 몬스터, 아이템): ScriptableObject
- 세이브 데이터: JSON 직렬화 (Newtonsoft JSON 또는 Unity JsonUtility)

## Proposed Solution

### 아키텍처 개요
```
[GameManager] ─── Singleton, 전역 상태 관리
    ├── [SceneFlowManager] ─── 씬 전환 (Town ↔ Dungeon ↔ Combat)
    ├── [DataManager] ─── SO 로딩, 세이브/로드
    ├── [AudioManager] ─── BGM/SFX 관리
    │
    ├── [Town System]
    │   ├── BuildingManager ─── 건물 업그레이드, UI 연결
    │   ├── RosterManager ─── 영웅 풀 관리, 모집
    │   └── ShopManager ─── 보급품 구매
    │
    ├── [Dungeon System]
    │   ├── DungeonMapProvider ─── 맵 제공 (수동맵 → 추후 절차적 생성 교체)
    │   ├── RoomManager ─── 방/복도 이벤트 처리
    │   ├── DetectionSystem ─── 위험발각 수치 관리
    │   └── CampSystem ─── 야영 처리
    │
    └── [Combat System]
        ├── TurnManager ─── 스피드 기반 행동 순서
        ├── PositionManager ─── 4슬롯 포지션 관리
        ├── SkillExecutor ─── 스킬 실행, 데미지 계산
        ├── StatusEffectManager ─── 상태이상/DOT 처리
        └── EblaSystem ─── 에블라 수치, Affliction/Virtue 발동
```

### 씬 구조
```
Scenes/
├── BootScene.unity          # 초기화, 매니저 생성
├── TitleScene.unity         # 타이틀, 세이브 슬롯
├── TownScene.unity          # 왕국 (건물, 영웅 관리)
├── DungeonScene.unity       # 던전 탐험 (복도 이동, 방 진입)
└── CombatScene.unity        # 전투 (또는 DungeonScene 내 오버레이)
```

> **설계 판단**: 전투를 별도 씬으로 분리 vs 던전 씬 내 오버레이 — Darkest Dungeon 원작은 같은 씬 내에서 전환하지만, 초급자 기준으로 씬 분리가 디버깅/관리에 유리함. **Phase 1에서는 씬 분리**, Phase 3에서 통합 검토.

## Implementation Phases

### Phase 1: Foundation & Prototype (3주)
**Goal**: Unity 프로젝트 셋업 + 턴제 전투 핵심 루프 동작 확인 (Greybox)
**Deliverable**: 4명의 영웅이 적과 턴제 전투를 수행하는 플레이 가능한 프로토타입

**Tasks**:
- [ ] Unity 6 프로젝트 생성 (URP 2D 템플릿) - Size: S
- [ ] 폴더 구조 셋업 (Scripts/, Prefabs/, SO/, Scenes/, Art/, Audio/) - Size: S
- [ ] CLAUDE.md, dev/README.md 프로젝트 표준 문서 작성 - Size: S
- [ ] GameManager 싱글톤 + SceneFlowManager 기초 구현 - File: `Assets/Scripts/Managers/GameManager.cs` - Size: M
- [ ] HeroData ScriptableObject 정의 (이름, 클래스, 스탯, 스킬 슬롯) - File: `Assets/Scripts/Data/HeroData.cs` - Size: M
- [ ] SkillData ScriptableObject 정의 (데미지, 타겟 범위, 사용 위치, 효과) - File: `Assets/Scripts/Data/SkillData.cs` - Size: M
- [ ] EnemyData ScriptableObject 정의 - File: `Assets/Scripts/Data/EnemyData.cs` - Size: M
- [ ] TurnManager 구현 (스피드 기반 행동 순서 정렬) - File: `Assets/Scripts/Combat/TurnManager.cs` - Size: L
- [ ] PositionSystem 구현 (4슬롯 배열, 위치 이동) - File: `Assets/Scripts/Combat/PositionSystem.cs` - Size: M
- [ ] 기초 전투 UI (HP바, 스킬 버튼 4개, 행동 순서 표시) - File: `Assets/Scripts/UI/CombatHUD.cs` - Size: L
- [ ] 전투 루프 완성: 스킬 선택 → 타겟 선택 → 실행 → 턴 종료 - Size: XL
- [ ] 테스트 데이터 SO 3~4개 생성 (영웅 2종, 적 2종, 스킬 4~5종) - Size: M

### Phase 2: Core Systems (4주)
**Goal**: 마을↔던전↔전투 풀 루프 + 핵심 시스템 전체 구현
**Deliverable**: 한 사이클(마을 출발→던전 탐험→전투→귀환)이 동작하는 빌드

**Tasks**:
- [ ] 에블라 시스템 구현 (수치 0~200, Affliction/Virtue 분기) - File: `Assets/Scripts/Systems/EblaSystem.cs` - Size: L
- [ ] 상태이상 시스템 (DOT, Debuff 스택, 턴 종료 틱) - File: `Assets/Scripts/Combat/StatusEffectManager.cs` - Size: L
- [ ] StatusEffectData SO 정의 - File: `Assets/Scripts/Data/StatusEffectData.cs` - Size: M
- [ ] 절차적 던전 생성 → **수동 맵 + IDungeonMapProvider 인터페이스** - File: `Assets/Scripts/Dungeon/DungeonGenerator.cs` - Size: L (XL→L 축소)
- [ ] RoomData SO (방 타입, 가중치, 이벤트) - File: `Assets/Scripts/Data/RoomData.cs` - Size: M
- [ ] 던전 이동 시스템 (복도 이동, 방 진입/이벤트 트리거) - File: `Assets/Scripts/Dungeon/DungeonNavigator.cs` - Size: L
- [ ] 위험발각 시스템 (수치 관리, 4단계 확률 테이블) - File: `Assets/Scripts/Systems/DetectionSystem.cs` - Size: L
- [ ] 마을 시스템 기초 (건물 3종: 대장장이/길드/병원) - File: `Assets/Scripts/Town/BuildingManager.cs` - Size: L
- [ ] BuildingData SO (레벨, 비용, 효과) - File: `Assets/Scripts/Data/BuildingData.cs` - Size: M
- [ ] 영웅 로스터 관리 (모집, 해고, 상태 확인) - File: `Assets/Scripts/Town/RosterManager.cs` - Size: L
- [ ] 보급품 구매 시스템 (횃불, 음식, 삽, 열쇠) - File: `Assets/Scripts/Town/ShopManager.cs` - Size: M
- [ ] 인벤토리 시스템 (탐험 물자 + 전리품) - File: `Assets/Scripts/Systems/InventorySystem.cs` - Size: L
- [ ] 장신구 시스템 (장착/해제, 스탯 보정) - File: `Assets/Scripts/Systems/TrinketSystem.cs` - Size: M
- [ ] 영구 사망 처리 (세이브에서 제거, 스페어바디 서사) - Size: M
- [ ] 세이브 시스템 v1 (JSON 직렬화, 자동저장) - File: `Assets/Scripts/Systems/SaveSystem.cs` - Size: XL
- [ ] 야영 시스템 (HP/에블라 회복) - File: `Assets/Scripts/Dungeon/CampSystem.cs` - Size: M
- [ ] 탐험 이벤트 (골동품 상호작용, 복도 함정) - File: `Assets/Scripts/Dungeon/ExplorationEventManager.cs` - Size: L
- [ ] 퀘스트 목표 시스템 (보스 처치, 수집, 탐험률) - File: `Assets/Scripts/Systems/QuestSystem.cs` - Size: M

### Phase 3: Content, Art & Polish (3주)
**Goal**: 아트 통합, 애니메이션, 연출, 사운드, UI 완성
**Deliverable**: 시각적으로 완성도 있는 플레이 가능 빌드

**Tasks**:
- [ ] 2D 영웅 스프라이트 통합 + Animator Controller 셋업 - Size: XL
- [ ] 전투 애니메이션 (공격/피격/사망 FlipBook 스타일) - Size: XL
- [ ] 히트스탑, 화면 흔들림, 피격 플래시 연출 - File: `Assets/Scripts/VFX/CombatFeedback.cs` - Size: L
- [ ] 에블라 시각 연출 (Post Processing Color Grading 변화, 대사 트리거) - Size: L
- [ ] 위험발각 시각 연출 (Post Processing 단계별 변화) - Size: M
- [ ] 마을 UI 완성 (건물 선택, 영웅 관리, 장비 화면) - Size: XL
- [ ] 던전 UI 완성 (미니맵, 인벤토리, 위험발각 게이지) - Size: L
- [ ] 전투 HUD 완성 (에블라 바 추가, 상태이상 아이콘) - Size: L
- [ ] BGM/SFX 통합 + AudioManager - File: `Assets/Scripts/Managers/AudioManager.cs` - Size: L
- [ ] 건물 업그레이드 트리 전체 데이터 입력 - Size: L
- [ ] 몬스터 다양화 (3~5 던전별 적 종류) - Size: L
- [ ] 영웅 클래스 4~6종 데이터 + 스킬 셋 완성 - Size: XL
- [ ] Virtue 전용 영웅 일러스트 변경 + 특수 스킬 시스템 - Size: L
- [ ] 하이퍼푸드 창고 최종 업그레이드 → 최종 던전 해금 흐름 - Size: M

### Phase 4: Optimization, QA & Build (2주)
**Goal**: 성능 최적화, 버그 수정, PC 빌드 안정화
**Deliverable**: PC(Windows) 릴리즈 빌드

**Tasks**:
- [ ] 프로파일링 (CPU/GPU/Memory) 및 병목 제거 - Size: L
- [ ] GC Allocation 최소화 (전투 루프 내 0 할당 목표) - Size: L
- [ ] Object Pooling (전투 이펙트, UI 요소, 던전 오브젝트) - Size: L
- [ ] 세이브 스캠 방지 검증 (자동저장 시점 테스트) - Size: M
- [ ] 밸런스 패스 (스킬 수치, 에블라 축적률, 위험발각 테이블) - Size: L
- [ ] 엣지 케이스 처리 (전투 중 사망, 전멸, 에블라 200 사망, 씬 전환 중 상태) - Size: L
- [ ] 로컬라이제이션 훅 (한국어 기본, 영어 확장 가능) - Size: M
- [ ] 최종 QA 플레이스루 (마을→던전→전투→귀환 5회 이상) - Size: L

## System Architecture
- **Design Pattern**: Singleton (Managers) + Observer (이벤트 시스템) + State Machine (전투 FSM, 게임 플로우) + Strategy (스킬 효과)
- **Core Components**: ScriptableObject (데이터), MonoBehaviour (런타임 로직), Pure C# (유틸리티, 데이터 구조)
- **Data Flow**: SO → Manager가 로드 → 런타임 인스턴스 생성 → 이벤트로 UI 갱신
- **Event System**: C# Action/event 기반 (UnityEvent는 Inspector 바인딩 필요 시만)

## Risk Assessment

### High Risk
- **1인 개발 3개월은 매우 빡빡함** — Mitigation: MVP 우선, Phase 1-2에 집중. Phase 3-4는 축소 가능. 핵심 루프(전투+던전+마을) 우선 완성
- **Unity + C# 학습 곡선** — Mitigation: C++/DirectX 경험 활용, C#은 C++과 유사한 부분 많음. MonoBehaviour 라이프사이클과 SerializeField 중점 학습
- **절차적 던전 생성 복잡도** — Mitigation: ✅ 수동 맵으로 시작, IDungeonMapProvider 인터페이스로 추상화하여 추후 절차적 생성기로 교체 가능

### Medium Risk
- **세이브 스캠 방지 설계** — Mitigation: 핵심 시점(전투 시작, 영웅 사망, 던전 입장/퇴장)에 자동저장, 수동 세이브 제한
- **에블라 시스템 밸런스** — Mitigation: SO 기반 데이터로 코드 수정 없이 수치 조정 가능

### Low Risk
- **아트 에셋 부족** — Mitigation: 프로토타입은 무료 에셋/플레이스홀더, 이후 AI 생성 일러스트 활용 가능
- **오디오** — Mitigation: 무료 사운드팩 활용, 후반 교체

## Performance Budget
- **Target Frame Rate**: 60 FPS (PC Windows)
- **Memory Budget**: 전체 500MB 이하 (2D 게임 기준 충분)
- **Draw Calls**: 배칭 활용, 전투 씬 50 이하, 던전 씬 100 이하
- **GC Allocation**: 전투 루프 내 0B/frame 목표 (Object Pooling, 캐싱)
- **씬 로딩**: Async Loading, 로딩 화면 2초 이내

## Success Metrics
- 핵심 루프(마을→던전→전투→귀환) 1사이클 완주 가능
- 60 FPS 유지 (PC)
- 에블라 시스템, 위험발각 시스템이 게임플레이에 유의미한 영향
- 영구 사망 + 자동저장이 긴장감 제공
- 4~6종 영웅 클래스로 파티 조합의 재미

## Dependencies
- **Packages**: TextMeshPro, New Input System, URP, Post Processing (URP 내장), Newtonsoft JSON (com.unity.nuget.newtonsoft-json)
- **Assets**: 2D 일러스트 (영웅, 적, 배경, UI), SFX/BGM
- **Platform**: PC (Windows) 전용

## Timeline
| Phase | 기간 | 핵심 산출물 |
|-------|------|------------|
| Phase 1: Foundation & Prototype | 3주 | 턴제 전투 프로토타입 |
| Phase 2: Core Systems | 4주 | 풀 게임 루프 |
| Phase 3: Content & Polish | 3주 | 아트/사운드 통합 빌드 |
| Phase 4: Optimization & QA | 2주 | 릴리즈 후보 빌드 |
| **Total** | **12주 (3개월)** | |

## 기획서 검토 피드백

### 👍 잘 설계된 부분
1. **핵심 루프가 명확함** — 마을→던전→귀환 사이클이 원작을 충실히 따르면서도 니케 세계관에 맞게 변형
2. **데이터 주도 설계 인지** — SO/JSON 기반 설계 방향이 1인 개발에 매우 적합
3. **위험발각 시스템** — 원작 횃불 시스템의 변형이 독창적 (강제 퇴각 메커니즘)
4. **에블라 시스템의 Virtue 변형** — 특정 영웅만 Virtue 상태 + 일러스트 변경 + 특수 스킬은 차별점이 됨

### ⚠️ 주의/보완이 필요한 부분
1. **스코프가 큼** — 12개 시스템을 3개월 내 혼자 구현은 도전적. **MVP 정의가 필수**
   - MVP: 전투 + 던전(수동 맵 3~4방) + 마을(건물 2종) + 세이브 + 영웅 3종
   - ✅ 절차적 던전 생성은 후순위, 수동 맵으로 시작 후 추후 교체 가능하도록 인터페이스 설계
2. **C# 학습 시간을 Phase 1에 포함해야 함** — Unity MonoBehaviour 라이프사이클, 코루틴, SerializeField, SO 패턴 등 1주 정도 학습 기간 고려
3. ✅ **에블라 수치 범위 확정** — 0~100: 정상 구간, 100 도달: Affliction/Virtue 발동(디버프/버프), 100~200: 위험 구간, 200 도달: 영구 사망. 원작의 2단계 구조를 그대로 채용
4. **전투-던전 씬 전환 방식** — 전투를 별도 씬으로 할지 오버레이로 할지 초기에 결정 필요. 프로토타입에서는 별도 씬 권장
5. ✅ **Android 포팅 제외** — PC(Windows) 전용으로 집중. 향후 포팅 가능성은 열어두되 현 단계에서는 스코프에서 제외
6. **장신구 시스템과 보급품 시스템의 인벤토리 통합** — 두 시스템이 같은 인벤토리를 공유할지 분리할지 설계 필요 (원작은 분리)
