using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public enum SurpriseType
{
    None,
    PlayerSurprise,
    EnemySurprise
}


public class CombatStateMachine : MonoBehaviour
{
    [Header("Test Setup")]
    [SerializeField] private NikkeData[] m_TestNikkes;
    [SerializeField] private EnemyData[] m_TestEnemies;

    [Header("Settings")]
    [SerializeField] private float m_EnemyActionDelay = 0.5f;

    [Header("UI References")]
    [SerializeField] private SkillSelectPanel m_SkillSelectPanel;
    [SerializeField] private TargetSelectPanel m_TargetSelectPanel;

    [SerializeField] private int m_EblaFreeRounds = 4;
    [SerializeField] private int m_EblaRoundMultiplier = 1;

    [SerializeField] private CombatFieldView m_FieldView;
    [SerializeField] private CombatHUD m_CombatHUD;

    // 패널티 에블라 수치
    private const int PASS_EBLA_PENALTY = 5;
    private const int ALLY_DEATH_EBLA = 20;

    // 전투 시스템
    private TurnManager     m_TurnManager;
    private PositionSystem  m_PositionSystem;
    private SkillExecutor   m_SkillExecutor;
    private EnemyAI         m_EnemyAI;
    

    // FSM 상태
    private CombatState     m_CurrentState;
    private CombatUnit      m_ActiveUnit;
    private SkillData       m_SelectedSkill;
    private CombatUnit      m_SelectedTarget;

    // 플레이어 입력 플래그
    private bool m_SkillConfirmed;
    private bool m_TargetConfirmed;
    private bool m_MoveRequested;
    private bool m_MoveConfirmed;

    // 외부 읽기용 프로퍼티
    public CombatState CurrentState => m_CurrentState;
    public CombatUnit ActiveUnit => m_ActiveUnit;
    public PositionSystem PositionSystem => m_PositionSystem;

    public IReadOnlyList<CombatUnit> TurnOrder => m_TurnManager?.TurnOrder;
    public int CurrentTurnIndex => m_TurnManager?.CurrentTurnIndex ?? 0;

    // UI 이벤트
    public delegate void StateChangeHandler(CombatState newState);
    public event StateChangeHandler OnStateChanged;

    private void Start()
    {
        if(null != m_TestNikkes && m_TestNikkes.Length > 0)
        {
            StartTestBattle();
        }
    }

    // 이벤트 구독
    private void OnEnable()
    {
        EventBus.Subscribe<RoundEndedEvent>(OnRoundEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<RoundEndedEvent>(OnRoundEnded);
    }
    // 이벤트 함수
    private void OnRoundEnded(RoundEndedEvent e)
    {
        TickCorpseTimers();
    }


    public bool ValidateSkill(CombatUnit unit, SkillData skill)
    {
        return m_SkillExecutor.ValidateSkill(unit, skill);
    }

    // 전투 시작
    private void StartTestBattle()
    {
        List<CombatUnit> nikkes = new List<CombatUnit>();

        // 니케 순환하면서 데이터 채우기
        for(int i=0; i<m_TestNikkes.Length; ++i)
        {
            NikkeData data = m_TestNikkes[i];
            if (data == null)
                continue;
            nikkes.Add(new CombatUnit(data, i, data.BaseStats.maxHp, 0, null));
        }

        // 적 순환하면서 데이터 채우기
        List<CombatUnit> enemies = new List<CombatUnit>();
        int slotIndex = 0;
        for (int i=0; i<m_TestEnemies.Length; ++i)
        {
            EnemyData data = m_TestEnemies[i];
            if (data == null)
                continue;
            CombatUnit unit = new CombatUnit(data, slotIndex);
            enemies.Add(unit);
            slotIndex += unit.SlotSize;
        }

        // 채운 데이터를 넘겨준다.
        StartBattle(nikkes, enemies);
    }
    public void StartBattle(List<CombatUnit> nikkes, List<CombatUnit> enemies, SurpriseType surprise = SurpriseType.None)
    {
        // 시스템 인스턴스 생성
        m_PositionSystem = new PositionSystem();
        m_TurnManager = new TurnManager();
        m_SkillExecutor = new SkillExecutor(m_PositionSystem);
        m_EnemyAI = new EnemyAI(m_PositionSystem, m_SkillExecutor);

        m_PositionSystem.Initialize(nikkes, enemies);

        List<CombatUnit> allUnits = new List<CombatUnit>();
        allUnits.AddRange(nikkes);
        allUnits.AddRange(enemies);

        m_TurnManager.Initialize(allUnits);

        EventBus.Publish(new BattleStartedEvent(nikkes, enemies));

        // TODO Phase 2: surprise 타입에 따라 기습 처리
        // SurpriseType.PlayerSurprise → 적 전체 1라운드 스킵
        // SurpriseType.EnemySurprise → 아군 위치 셔플 + 1라운드 스킵

        StartCoroutine(RunBattle());
    }



    // 코루틴
    private IEnumerator RunBattle()
    {
        SetState(CombatState.BattleStart);
        yield return null;

        while (true)
        {
            SetState(CombatState.TurnStart);
            m_ActiveUnit = m_TurnManager.StartNextTurn();

            if (m_ActiveUnit == null)
                break;
            if (m_CombatHUD != null)
                while (m_CombatHUD.IsTickerAnimating)
                    yield return null;

            EventBus.Publish(new TurnStartedEvent(m_ActiveUnit));

            // 스턴 체크
            if (m_ActiveUnit.IsStunned)
            {
                RemoveStun(m_ActiveUnit);
                SetState(CombatState.TurnEnd);
                m_TurnManager.EndCurrentTurn();
                yield return null;
                continue;
            }

            Debug.Log($"[Turn] Round {m_TurnManager.RoundNumber} — {m_ActiveUnit.UnitName} ({m_ActiveUnit.UnitType}, Slot{ m_ActiveUnit.SlotIndex}) HP: { m_ActiveUnit.CurrentHp}/{ m_ActiveUnit.MaxHp}");

            if (m_ActiveUnit.UnitType == CombatUnitType.Nikke)
                yield return StartCoroutine(HandlePlayerTurn());
            else
                yield return StartCoroutine(HandleEnemyTurn());

            SetState(CombatState.TurnEnd);
            m_TurnManager.EndCurrentTurn();
            yield return null;

            // 이동 애니메이션 완료 대기
            if (m_FieldView != null)
                while (m_FieldView.IsMoving)
                    yield return null;

            SetState(CombatState.CheckBattleEnd);
            if (m_PositionSystem.GetAllUnits(CombatUnitType.Enemy).Count == 0)
            {
                ApplyPostBattleEbla();
                SetState(CombatState.Victory);
                EventBus.Publish(new BattleEndedEvent(true));
                yield break;
            }
            if (m_PositionSystem.GetAllUnits(CombatUnitType.Nikke).Count == 0)
            {
                SetState(CombatState.Defeat);
                EventBus.Publish(new BattleEndedEvent(false));
                yield break;
            }

        }
    }


    private IEnumerator HandlePlayerTurn()
    {
        bool turnHandled = false;
        m_MoveRequested = false;  // 전에 사용됬던거 사용 방지
        m_MoveConfirmed = false;
        while (!turnHandled)
        {
            SetState(CombatState.PlayerSelectSkill);
            m_SkillConfirmed = false;
            m_SelectedSkill = null;
            m_SkillSelectPanel.Show(m_ActiveUnit, OnSkillSelected, OnSkillPass, OnMoveRequested);

            while (!m_SkillConfirmed)
                yield return null;

            // Move 요청된 경우
            if (m_MoveRequested)
            {
                m_MoveRequested = false;
                List<CombatUnit> moveTargets = GetValidMoveTargets();
                if (moveTargets.Count == 0)
                    continue; // 이동 가능 슬롯 없으면 다시 스킬 선택

                SetState(CombatState.PlayerSelectMoveTarget);
                m_MoveConfirmed = false;
                m_SelectedTarget = null;
                m_TargetSelectPanel.Show(moveTargets,m_SelectedSkill ,OnMoveTargetSelected, OnMoveCancel);

                while (!m_MoveConfirmed)
                    yield return null;

                if (m_SelectedTarget != null)
                    turnHandled = true;
                continue;
            }


            // 패스 선택 시
            if (m_SelectedSkill == null)
            {
                m_ActiveUnit.AddEbla(PASS_EBLA_PENALTY);
                turnHandled = true;
                continue;
            }

            

            // 타겟 선택
            SetState(CombatState.PlayerSelectTarget);
            m_TargetConfirmed = false;
            m_SelectedTarget = null;
            List<CombatUnit> validTargets = m_PositionSystem.GetValidTargets(m_ActiveUnit, m_SelectedSkill);
            m_TargetSelectPanel.Show(validTargets,m_SelectedSkill, OnTargetSelected, OnTargetCancel);

            while (!m_TargetConfirmed)
                yield return null;

            if (m_SelectedTarget != null)
                turnHandled = true;
            // 취소면 루프 처음으로 돌아가 스킬 재선택
        }

        // 패스가 아닐 때만 실행
        if (m_SelectedSkill != null)
        {
            SetState(CombatState.ExecuteSkill);
            SkillResult result = m_SkillExecutor.Execute(m_ActiveUnit, m_SelectedSkill, m_SelectedTarget);
            EventBus.Publish(new SkillExecutedEvent(result));
            ProcessDeadUnits(result);
            yield return null;
        }
    }
    private IEnumerator HandleEnemyTurn()
    {
        SetState(CombatState.EnemyDecide);
        yield return new WaitForSeconds(m_EnemyActionDelay);

        EnemyAction action = m_EnemyAI.DecideAction(m_ActiveUnit);
        SetState(CombatState.ExecuteSkill);
        if(!action.IsPass)
        {
            SkillResult result = m_SkillExecutor.Execute(m_ActiveUnit, action.Skill, action.Target);
            EventBus.Publish(new SkillExecutedEvent(result));
            ProcessDeadUnits(result);
        }

        yield return new WaitForSeconds(m_EnemyActionDelay);

    }

    // 헬퍼

    private void TickCorpseTimers()
    {
        List<CombatUnit> corpses = m_PositionSystem.GetCorpses(CombatUnitType.Enemy);
        for (int i=0; i<corpses.Count; ++i)
        {
            corpses[i].CorpseTimer--;
            if (corpses[i].CorpseTimer <=0)
            {
                corpses[i].Kill();
                m_PositionSystem.RemoveUnit(corpses[i]);
                EventBus.Publish(new UnitDiedEvent(corpses[i]));
            }
        }
    }

    // 스턴제거
    private void RemoveStun(CombatUnit unit)
    {
        for (int i = unit.ActiveEffects.Count - 1; i >= 0; --i)
        {
            if (unit.ActiveEffects[i].Data.EffectType == StatusEffectType.Stun)
            {
                unit.ActiveEffects.RemoveAt(i);
                break;
            }
        }
    }


    // 죽은 유닛 이벤트 발생
    private void ProcessDeadUnits(SkillResult result)
    {
        if (result.TargetResults == null)
            return;
        for (int i =0; i<result.TargetResults.Length; ++i)
        {
            CombatUnit target = result.TargetResults[i].Target;
            if(target != null)
            {
                if (target.State == UnitState.Dead)
                {
                    m_PositionSystem.RemoveUnit(target);
                    EventBus.Publish(new UnitDiedEvent(target));
                    if (target.UnitType == CombatUnitType.Nikke)
                        ApplyAllyDeathEbla();
                }
                else if (target.State == UnitState.Corpse && result.TargetResults[i].PreviousState == UnitState.Alive)
                    EventBus.Publish(new UnitDiedEvent(target));
            }
        }
    }
    private void ApplyAllyDeathEbla()
    {
        List<CombatUnit> nikkes = m_PositionSystem.GetAllUnits(CombatUnitType.Nikke);
        for(int i=0; i<nikkes.Count; ++i)
            nikkes[i].AddEbla(ALLY_DEATH_EBLA);
    }

    // State 설정
    private void SetState(CombatState newState)
    { 
        m_CurrentState = newState;
        if (OnStateChanged != null)
            OnStateChanged(newState);
        // OnstateChanged?.Invoke(newState)와 동일함.
        Debug.Log($"[FSM] {newState}");

    }

    // 전투 후반부 에블라 패널티
    private void ApplyPostBattleEbla()
    {
        int roundCheck = m_TurnManager.RoundNumber;
        if (m_EblaFreeRounds > roundCheck)
            return;
        int total = 0;
        for(int i=m_EblaFreeRounds +1; i<=roundCheck; ++i)
        {
            total += i * m_EblaRoundMultiplier;
        }

        List<CombatUnit> nikkes = m_PositionSystem.GetAllUnits(CombatUnitType.Nikke);
        for(int i=0; i<nikkes.Count; ++i)
        {
            nikkes[i].AddEbla(total);
        }

    }

    private void OnSkillSelected(SkillData skill) 
    {
        m_SelectedSkill = skill;
        m_SkillConfirmed = true;
    }
    private void OnSkillPass() 
    {
        m_SelectedSkill = null;
        m_SkillConfirmed = true;
    }
    private void OnTargetSelected(CombatUnit target)
    {
        m_SelectedTarget = target;
        m_TargetConfirmed = true;
    }
    private void OnTargetCancel() 
    {
        m_SelectedTarget = null;
        m_TargetConfirmed = true;
    }

    private void OnMoveCancel()
    {
        m_SelectedTarget = null;
        m_MoveConfirmed = true;
    }

    private void OnMoveRequested()
    {
        m_MoveRequested = true;
        m_SkillConfirmed = true; // while(!m_SkillConfirmed) 루프 탈출용
    }

    private void OnMoveTargetSelected(CombatUnit target)
    {
        int steps = target.SlotIndex - m_ActiveUnit.SlotIndex;
        m_PositionSystem.Move(m_ActiveUnit, steps);
        EventBus.Publish(new UnitMovedEvent(m_ActiveUnit, target));
        m_SelectedTarget = target;
        m_MoveConfirmed = true;
    }

    private List<CombatUnit> GetValidMoveTargets()
    {
        List<CombatUnit> targets = new List<CombatUnit>();
        int currentSlot = m_ActiveUnit.SlotIndex;
        int range = m_ActiveUnit.CurrentStats.moveRange;

        for(int i=1; i<=range; ++i)
        {
            CombatUnit forward = m_PositionSystem.GetUnit(CombatUnitType.Nikke, currentSlot - i);
            CombatUnit backward = m_PositionSystem.GetUnit(CombatUnitType.Nikke, currentSlot + i);

            if (forward != null)
                targets.Add(forward);
            if (backward != null)
                targets.Add(backward);
        }
        return targets;

    }



}
