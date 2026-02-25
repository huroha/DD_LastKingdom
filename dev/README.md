# dev/ - Development Task Management

## Structure
```
dev/
├── README.md              # 이 파일
├── active/                # 진행 중인 태스크
│   └── [task-name]/
│       ├── [task-name]-plan.md
│       ├── [task-name]-context.md
│       └── [task-name]-tasks.md
├── completed/             # 완료된 태스크 (active/에서 이동)
└── archived/              # 보류/취소된 태스크
```

## Workflow
1. 새 기능 요청 → `dev/active/[task-name]/` 디렉토리에 plan, context, tasks 3개 파일 생성
2. 구현 진행 → tasks.md의 체크리스트 업데이트, context.md에 결정사항 기록
3. 완료 → `dev/completed/`로 이동

## Task Naming
- kebab-case 사용 (예: `turn-based-combat`, `ebla-system`, `town-buildings`)
- 명확하고 구체적으로 (❌ `fix-bug` → ✅ `combat-damage-calculation-fix`)

## Task Sizes
- **S** (1-2h): 단일 파일 수정, 간단한 기능
- **M** (2-4h): SO 정의, 단일 시스템 컴포넌트
- **L** (4-8h): 시스템 구현, 복잡한 UI
- **XL** (1-2d): 핵심 시스템, 여러 파일 연동

## Commit Convention
```
feat: 새 기능 추가
fix: 버그 수정
refactor: 리팩토링 (기능 변화 없음)
data: SO/데이터 에셋 추가/수정
ui: UI 관련 변경
art: 아트/에셋 추가
audio: 오디오 에셋 추가
docs: 문서 수정
chore: 빌드/설정 변경
```