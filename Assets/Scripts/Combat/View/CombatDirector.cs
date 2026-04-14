using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class CombatDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatFieldView m_FieldView;
    [SerializeField] private CombatFeedback m_Feedback;
    [SerializeField] private DamagePopupPool m_PopupPool;
    [SerializeField] private CombatHUD m_CombatHUD;
    [SerializeField] private CombatFocusController m_FocusController;
    [SerializeField] private CombatDriftController m_DriftController;


    [Header("Timing")]
    [SerializeField] private float m_PopupDuration = 0.6f;
    [SerializeField] private float m_StatusPopupDelay = 0.3f;
    [SerializeField] private float m_PostSequenceDelay = 0.2f;
    [SerializeField] private float m_HitDuration = 0.15f;


    // 색상 상수
    private static readonly Color COLOR_DAMAGE_RED = new Color(0.75f, 0.07f, 0.07f);
    private static readonly Color COLOR_CRIT = new Color(0.9f, 0.53f, 0.1f);
    private static readonly Color COLOR_HEAL = new Color(0.53f, 0.75f, 0.26f);
    private static readonly Color COLOR_MISS = new Color(0.7f, 0.7f, 0.7f);
    private static readonly Color COLOR_POISON = new Color(0.73f, 0.75f, 0.25f);
    private static readonly Color COLOR_STUN = new Color(0.8f, 0.64f, 0.32f);
    private static readonly Color COLOR_DEBUFF = new Color(0.75f, 0.38f, 0.06f);
    private static readonly Color COLOR_RESIST = new Color(0.7f, 0.7f, 0.7f);
    private static readonly Color COLOR_DEATHDOOR = new Color(0.6f, 0f, 0f);

    // 팝업 텍스트
    private const string TRIGGER_ATTACK = "Attack";
    private const string TRIGGER_HIT = "Hit";
    private const string TRIGGER_DEATH = "Death";

    private const string POPUP_DODGE = "회피";
    private const string POPUP_RESIST = "저항";
    private const string POPUP_CRIT = "치명타!\n";
    private const string POPUP_DEATHS_DOOR = "죽음의 문턱!";

    private const float CRIT_POPUP_SCALE = 1.15f;

    // duration 필드
    private WaitForSecondsRealtime m_WaitPopup;
    private WaitForSecondsRealtime m_WaitStatusPopup;
    private WaitForSecondsRealtime m_WaitPostSequence;
    private WaitForSecondsRealtime m_WaitHit;


    // Animation Event 동기화 플래그
    private bool m_AttackEndReceived;
    private StringBuilder m_PopupTextBuilder;


    private void Awake()
    {
        m_PopupTextBuilder = new StringBuilder();
        m_WaitPopup = new WaitForSecondsRealtime(m_PopupDuration);
        m_WaitStatusPopup = new WaitForSecondsRealtime(m_StatusPopupDelay);
        m_WaitPostSequence = new WaitForSecondsRealtime(m_PostSequenceDelay);
        m_WaitHit = new WaitForSecondsRealtime(m_HitDuration);

    }
    public Coroutine PlaySkillSequence(CombatUnit user, SkillData skill, List<CombatUnit> targets, SkillResult result)
    {
        return StartCoroutine(SkillSequence(user, skill, targets, result));
    }
    private IEnumerator SkillSequence(CombatUnit user, SkillData skill, List<CombatUnit> targets, SkillResult result)
    {
        m_FocusController.SetupFocus(user, targets);
        m_FieldView.SetFocusLock(true);
        // 2. Focus In
        yield return m_FocusController.FocusIn();
        m_DriftController.StartDrift(user,skill);

        // 3. 공격 모션
        if (result.TargetResults != null && result.TargetResults.Length > 0)
        {
            if (targets.Count > 1)
                ProcessHitBatch(result.TargetResults, skill);
            else
                ProcessSingleHit(result.TargetResults[0], skill);
        }
        CombatFieldView.UnitView userView = m_FieldView.GetView(user);

        if (user.UnitType == CombatUnitType.Nikke && user.NikkeData.AttackSprite != null)
        {
            if (userView.Animator != null)
                userView.Animator.enabled = false;
            userView.Renderer.sprite = user.NikkeData.AttackSprite;
            yield return m_WaitPopup;
            if (userView.Animator != null)
                userView.Animator.enabled = true;
            else
                userView.Renderer.sprite = user.NikkeData.CombatIdleSprite;
        }
        else if (userView.Animator != null && HasParameter(userView.Animator, TRIGGER_ATTACK))
        {
            m_AttackEndReceived = true;
            if (userView.AnimBridge != null)
            {
                userView.AnimBridge.SetCallbacks(null, OnAttackEnd);
                m_AttackEndReceived = false;
            }
            userView.Animator.SetTrigger(TRIGGER_ATTACK);
            yield return m_WaitPopup;
            while (!m_AttackEndReceived) yield return null;
        }
        else
        {
            yield return m_WaitPopup;
        }
        m_DriftController.StopDrift();

        // 8.콜백 정리
        if (userView.AnimBridge != null)
            userView.AnimBridge.ClearCallbacks();

        // FocusOut
        yield return m_FocusController.FocusOut();
        m_FieldView.SetFocusLock(false);
        m_FieldView.MoveAllToCurrentSlots();

        // 상태이상 팝업
        if (result.TargetResults != null)
        {
            for (int i = 0; i < result.TargetResults.Length; ++i)
            {
                TargetResult tr = result.TargetResults[i];
                Vector3 pos = m_FieldView.GetSlotPosition(tr.Target);
                bool isNikke = tr.Target.UnitType == CombatUnitType.Nikke;
                if (tr.AppliedEffects != null)
                {
                    for (int j = 0; j < tr.AppliedEffects.Length; ++j)
                    {
                        m_PopupPool.SpawnEffect(pos, isNikke, tr.AppliedEffects[j].EffectName,
                                                GetEffectColor(tr.AppliedEffects[j].EffectType));
                        yield return m_WaitStatusPopup;
                    }
                }
                if (tr.ResistedEffects != null)
                {
                    for (int j = 0; j < tr.ResistedEffects.Length; ++j)
                    {
                        m_PopupPool.SpawnEffect(pos, isNikke, POPUP_RESIST, COLOR_RESIST);
                        yield return m_WaitStatusPopup;
                    }
                }
                if (tr.PreviousState == UnitState.Alive && tr.ResultState == UnitState.DeathsDoor)
                {
                    m_PopupPool.SpawnEffect(pos, isNikke, POPUP_DEATHS_DOOR, COLOR_DEATHDOOR);
                    yield return m_WaitStatusPopup;
                }
            }
        }
        // 10. 후딜레이
        yield return m_WaitPostSequence;
    }

    private void ProcessSingleHit(TargetResult targetResult, SkillData skill)
    {
        ProcessOneTarget(targetResult, skill);
        m_Feedback.PlayHitStop(targetResult.IsCrit);
    }
    private void ProcessHitBatch(TargetResult[] results, SkillData skill)
    {
        bool anyCrit = false;
        for (int i = 0; i < results.Length; ++i)
        {
            ProcessOneTarget(results[i], skill);
            if (results[i].IsHit && results[i].IsCrit) anyCrit = true;
        }
        m_Feedback.PlayHitStop(anyCrit);
    }
    private void SpawnDamagePopup(CombatUnit target, TargetResult result, SkillData skill)
    {
        CombatFieldView.UnitView view = m_FieldView.GetView(target);
        Vector3 pos = view.Renderer.transform.position;
        bool isNikke = target.UnitType == CombatUnitType.Nikke;

        if (skill.MaxHeal > 0)
        {
            m_PopupPool.SpawnDamage(pos, isNikke, result.HealAmount.ToString(), COLOR_HEAL);
        }
        else
        {
            m_PopupTextBuilder.Clear();
            if (result.IsCrit)
                m_PopupTextBuilder.Append(POPUP_CRIT);
            m_PopupTextBuilder.Append(result.DamageDealt);
            string text = m_PopupTextBuilder.ToString();
            Color color = result.IsCrit ? COLOR_CRIT : COLOR_DAMAGE_RED;
            float scale = result.IsCrit ? CRIT_POPUP_SCALE : 1f;
            m_PopupPool.SpawnDamage(pos, isNikke, text, color, scale);
        }
    }
    private Color GetEffectColor(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed:
                return COLOR_DAMAGE_RED;
            case StatusEffectType.Poison:
                return COLOR_POISON;
            case StatusEffectType.Stun:
                return COLOR_STUN;
            default:
                return COLOR_DEBUFF;
        }
    }

    public Coroutine PlayDotTick(CombatUnit unit, int damage, StatusEffectType type)
    {
        return StartCoroutine(DotTickRoutine(unit, damage, type));
    }
    private IEnumerator DotTickRoutine(CombatUnit unit, int damage, StatusEffectType type)
    {
        CombatFieldView.UnitView view = m_FieldView.GetView(unit);
        bool isNikke = unit.UnitType == CombatUnitType.Nikke;
        if (view.Renderer == null)
            yield break;
        m_PopupPool.SpawnEffect(m_FieldView.GetSlotPosition(unit), isNikke, damage.ToString(), GetEffectColor(type));
        yield return m_WaitPopup;
    }

    private void OnAttackEnd()
    {
        m_AttackEndReceived = true;
    }

    private void ProcessOneTarget(TargetResult result, SkillData skill)
    {
        CombatFieldView.UnitView targetView = m_FieldView.GetView(result.Target);
        if (result.IsHit)
        {
            if (targetView.Animator != null && HasParameter(targetView.Animator, TRIGGER_HIT))
            {
                targetView.Animator.SetTrigger(TRIGGER_HIT);
                if (result.ResultState == UnitState.Dead || result.ResultState == UnitState.Corpse)
                    targetView.Animator.SetTrigger(TRIGGER_DEATH);
            }
            else
            {
                StartCoroutine(HitSpriteRoutine(result.Target, targetView , result.PreviousState));
            }
            SpawnDamagePopup(result.Target, result, skill);
        }
        else
        {
            StartCoroutine(HitSpriteRoutine(result.Target, targetView, result.PreviousState));
            bool isNikke = result.Target.UnitType == CombatUnitType.Nikke;
            m_PopupPool.SpawnEffect(m_FieldView.GetSlotPosition(result.Target), isNikke, POPUP_DODGE, COLOR_MISS);
        }
    }
    private IEnumerator HitSpriteRoutine(CombatUnit unit, CombatFieldView.UnitView view, UnitState previousState)
    {
        Sprite hitSprite = null;
        Sprite idleSprite = null;

        if (unit.UnitType == CombatUnitType.Nikke)
        {
            hitSprite = unit.NikkeData.HitSprite;
            idleSprite = unit.NikkeData.CombatIdleSprite;
        }
        else
        {
            if (previousState == UnitState.Corpse)
            {
                hitSprite = unit.EnemyData.CorpseSprite;
                idleSprite = unit.EnemyData.CorpseSprite;
            }
            else
            {
                hitSprite = unit.EnemyData.HitSprite;
                idleSprite = unit.EnemyData.Sprite;
            }
        }

        if (hitSprite != null)
        {
            if (view.Animator != null)
                view.Animator.enabled = false;
            view.Renderer.sprite = hitSprite;
            yield return m_WaitHit;
            if (view.Animator != null)
                view.Animator.enabled = true;
            else
                view.Renderer.sprite = idleSprite;
        }
    }
    private bool HasParameter(Animator animator, string paramName)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; ++i)
        {
            if (parameters[i].name == paramName)
                return true;
        }
        return false;
    }

}
