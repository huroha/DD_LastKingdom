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

    [Header("Death Overlay")]
    [SerializeField] private Sprite m_DeathOverlaySprite;
    [SerializeField] private float m_DeathOverlayScale = 1f;        // SlotSize=1 기본 크기
    [SerializeField] private float m_DeathOverlayScaleLarge = 1.5f;
    [SerializeField] private float m_DeathPopStartMultiplier = 2f;  // 팝인 시작 배수
    [SerializeField] private float m_DeathPopDuration = 0.2f;       // 팝인 시간
    [SerializeField] private bool m_NikkeOverlayFlipX = false;
    [SerializeField] private bool m_EnemyOverlayFlipX = true;
    [SerializeField] private float m_DeathFadeInDuration = 0.25f;   // 기존 유지
    [SerializeField] private float m_DeathHoldDuration = 0.15f;     // 기존 유지
    [SerializeField] private float m_DotDeathFadeOutDuration = 0.25f;

    private HashSet<CombatUnit> m_DyingUnits;

    // 색상 상수
    private static readonly Color COLOR_DAMAGE_RED = new Color(0.75f, 0.07f, 0.07f);
    private static readonly Color COLOR_CRIT = new Color(0.9f, 0.53f, 0.1f);
    private static readonly Color COLOR_HEAL = new Color(0.53f, 0.75f, 0.26f);
    private static readonly Color COLOR_MISS = new Color(0.7f, 0.7f, 0.7f);
    private static readonly Color COLOR_POISON = new Color(0.73f, 0.75f, 0.25f);
    private static readonly Color COLOR_STUN = new Color(0.8f, 0.64f, 0.32f);
    private static readonly Color COLOR_DEBUFF = new Color(0.75f, 0.38f, 0.06f);
    private static readonly Color COLOR_BUFF = new Color(0f, 1f, 1f);
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
        m_DyingUnits = new HashSet<CombatUnit>();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (m_DyingUnits != null)
            m_DyingUnits.Clear();
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
        yield return m_FocusController.FocusIn(skill.IsAllyTargeting);
        m_DriftController.StartDrift(user, skill);

        // 3. 공격 모션
        if (result.TargetResults != null && result.TargetResults.Length > 0)
        {
            if (targets.Count > 1)
                ProcessHitBatch(result.TargetResults, skill);
            else
                ProcessSingleHit(result.TargetResults[0], skill);
        }

        // 사망 overlay 동작
        if (result.TargetResults != null)
        {
            for (int i = 0; i < result.TargetResults.Length; ++i)
            {
                TargetResult tr = result.TargetResults[i];
                if (ShouldPlayDeathVfx(tr.Target, tr.PreviousState, tr.ResultState, tr.IsHit))
                    StartCoroutine(DeathVfxRoutine(tr.Target, m_FocusController.FocusOutDuration));
            }
        }
        CombatFieldView.UnitView userView = m_FieldView.GetView(user);
        if (user.UnitType == CombatUnitType.Nikke && user.NikkeData.AttackSprite != null)
        {
            if (userView.Animator != null)
                userView.Animator.enabled = false;
            userView.Renderer.sprite = user.NikkeData.AttackSprite;
            yield return m_WaitPopup;
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
            userView.Animator.enabled = false;
        }
        else
        {
            yield return m_WaitPopup;
        }
        m_DriftController.StopDrift();

        // 8.콜백 정리
        if (userView.AnimBridge != null)
            userView.AnimBridge.ClearCallbacks();

        // ghostbar 준비
        for (int i = 0; i < result.TargetResults.Length; ++i)
        {
            TargetResult tr = result.TargetResults[i];
            if (!tr.IsHit)
                continue;
            if (tr.PreviousState != UnitState.Corpse && tr.Target.State == UnitState.Corpse)
                continue;   // alive -> corpse만 불통
            m_CombatHUD.PrepareHpGhost(tr.Target, tr.PreviousHp);
        }

        // FocusOut
        yield return m_FocusController.FocusOut();
        m_FieldView.SetFocusLock(false);
        m_FieldView.MoveAllToCurrentSlots();

        // bar 등장 직후 적용
        if (result.TargetResults != null)
        {
            for (int i = 0; i < result.TargetResults.Length; ++i)
            {
                if (!result.TargetResults[i].IsHit)
                    continue;
                m_CombatHUD.StartHpGhostDrain(result.TargetResults[i].Target);
            }
        }
        // 복귀
        yield return m_WaitPostSequence;
        RestoreSprites(user, userView, result.TargetResults);

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
                        if (tr.AppliedEffects[j].EffectType == StatusEffectType.Stun)
                            m_CombatHUD.ShowStunHalo(tr.Target);
                        m_PopupPool.SpawnEffect(pos, isNikke, tr.AppliedEffects[j].EffectName,
                                                GetEffectColor(tr.AppliedEffects[j].EffectType), tr.AppliedEffects[j].Icon);
                        yield return m_WaitStatusPopup;
                    }
                }
                if (tr.ResistedEffects != null)
                {
                    for (int j = 0; j < tr.ResistedEffects.Length; ++j)
                    {
                        m_PopupPool.SpawnEffect(pos, isNikke, POPUP_RESIST, COLOR_RESIST, tr.ResistedEffects[j].Icon);
                        yield return m_WaitStatusPopup;
                    }
                }
                if (tr.PreviousState == UnitState.Alive && tr.ResultState == UnitState.DeathsDoor)
                {
                    m_CombatHUD.PopupDeathsDoorHalo(tr.Target);
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
        if (targetResult.IsHit && !skill.IsAllyTargeting)
            m_Feedback.PlayCameraShake();
        if(!skill.IsAllyTargeting)
            m_Feedback.PlayHitStop(targetResult.IsCrit);
    }
    private void ProcessHitBatch(TargetResult[] results, SkillData skill)
    {
        bool anyCrit = false;
        bool anyHit = false;
        for (int i = 0; i < results.Length; ++i)
        {
            ProcessOneTarget(results[i], skill);
            if (results[i].IsHit)
            {
                if (results[i].IsCrit)
                    anyCrit = true;
                anyHit = true;
            }
        }
        if (anyHit && !skill.IsAllyTargeting)
            m_Feedback.PlayCameraShake();
        if(!skill.IsAllyTargeting)
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
        else if(result.DamageDealt > 0)
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
            case StatusEffectType.Buff:
                return COLOR_BUFF;
            default:
                return COLOR_DEBUFF;
        }
    }

    public Coroutine PlayDotTick(CombatUnit unit, int damage, StatusEffectType type, UnitState previousState, UnitState resultState)
    {
        return StartCoroutine(DotTickRoutine(unit, damage, type, previousState, resultState));
    }
    private IEnumerator DotTickRoutine(CombatUnit unit, int damage, StatusEffectType type, UnitState previousState, UnitState resultState)
    {
        CombatFieldView.UnitView view = m_FieldView.GetView(unit);
        bool isNikke = unit.UnitType == CombatUnitType.Nikke;
        if (view.Renderer == null)
            yield break;

        SetHitSprite(unit, view, previousState);

        // Death VFX 판정과 시작
        Coroutine deathCo = null;
        if (ShouldPlayDeathVfx(unit, previousState, resultState, true))
        {
            deathCo = StartCoroutine(DeathVfxRoutine(unit, m_DotDeathFadeOutDuration));
        }

        m_PopupPool.SpawnEffect(m_FieldView.GetSlotPosition(unit), isNikke, damage.ToString(), GetEffectColor(type));
        yield return m_WaitPopup;
        RestoreSingleSprite(unit, view);

        // DeathVfx가 팝업보다 길면 완료까지 대기
        if (deathCo != null)
            yield return deathCo;
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
            if(!skill.IsAllyTargeting)
                SetHitSprite(result.Target, targetView, result.PreviousState);
            SpawnDamagePopup(result.Target, result, skill);
        }
        else
        {
            SetHitSprite(result.Target, targetView, result.PreviousState);
            bool isNikke = result.Target.UnitType == CombatUnitType.Nikke;
            m_PopupPool.SpawnEffect(m_FieldView.GetSlotPosition(result.Target), isNikke, POPUP_DODGE, COLOR_MISS);
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

    private void SetHitSprite(CombatUnit unit, CombatFieldView.UnitView view, UnitState previousState)
    {
        Sprite hitSprite = null;

        if (unit.UnitType == CombatUnitType.Nikke)
            hitSprite = unit.NikkeData.HitSprite;
        else
        {
            if (previousState == UnitState.Corpse)
                hitSprite = unit.EnemyData.CorpseSprite;
            else
                hitSprite = unit.EnemyData.HitSprite;
        }
        if (hitSprite != null)
        {
            if (view.Animator != null)
                view.Animator.enabled = false;
            view.Renderer.sprite = hitSprite;
        }
    }
    private void RestoreSprites(CombatUnit user, CombatFieldView.UnitView userView, TargetResult[] results)
    {
        // 공격자 복귀
        RestoreSingleSprite(user, userView);
        // 피격자 복귀
        for (int i = 0; i < results.Length; ++i)
        {
            CombatFieldView.UnitView targetView = m_FieldView.GetView(results[i].Target);
            RestoreSingleSprite(results[i].Target, targetView);

            // Death 트리거는 별도 처리
            if (targetView.Animator != null
                && results[i].IsHit
                && (results[i].ResultState == UnitState.Dead || results[i].ResultState == UnitState.Corpse)
                && HasParameter(targetView.Animator, TRIGGER_DEATH))
            {
                targetView.Animator.SetTrigger(TRIGGER_DEATH);
            }
        }
    }
    private void RestoreSingleSprite(CombatUnit unit, CombatFieldView.UnitView view)
    {
        // 시체 pop in에서 재등장 예정
        if (unit.State == UnitState.Dead || unit.State == UnitState.Corpse)
            return;

        if (view.Animator != null)
            view.Animator.enabled = true;
        else if (unit.UnitType == CombatUnitType.Nikke)
            view.Renderer.sprite = unit.NikkeData.CombatIdleSprite;
        else
            view.Renderer.sprite = unit.EnemyData.Sprite;
    }

    // Stun관련
    public Coroutine PlayStunRecovery(CombatUnit unit, StatusEffectData stunResistBuff)
    {
        return StartCoroutine(StunRecoveryRoutine(unit, stunResistBuff));
    }
    private IEnumerator StunRecoveryRoutine(CombatUnit unit, StatusEffectData stunResistBuff)
    {
        m_CombatHUD.HideStunHalo(unit);
        Vector3 pos = m_FieldView.GetSlotPosition(unit);
        bool isNikke = unit.UnitType == CombatUnitType.Nikke;
        m_PopupPool.SpawnEffect(pos, isNikke, stunResistBuff.EffectName, GetEffectColor(stunResistBuff.EffectType), stunResistBuff.Icon);
        yield return m_WaitStatusPopup;
    }
    private static bool ShouldPlayDeathVfx(CombatUnit target, UnitState previousState, UnitState resultState, bool isHit)
    {
        if (!isHit)
            return false;
        if (target.UnitType == CombatUnitType.Nikke)
            return previousState == UnitState.DeathsDoor && resultState == UnitState.Dead;
        else
            return previousState == UnitState.Alive && (resultState == UnitState.Corpse || resultState == UnitState.Dead);
    }
    private IEnumerator DeathVfxRoutine(CombatUnit unit, float fadeOutDuration)
    {
        if (m_DyingUnits.Contains(unit)) yield break;
        m_DyingUnits.Add(unit);

        CombatFieldView.UnitView view = m_FieldView.GetView(unit);
        if (view.Renderer == null || view.DeathOverlay == null)
        {
            m_DyingUnits.Remove(unit);
            yield break;
        }

        // 기본 설정
        bool isNikke = unit.UnitType == CombatUnitType.Nikke;
        float baseScale = unit.SlotSize >= 2 ? m_DeathOverlayScaleLarge : m_DeathOverlayScale;
        bool flipX = isNikke ? m_NikkeOverlayFlipX : m_EnemyOverlayFlipX;

        view.DeathOverlay.sprite = m_DeathOverlaySprite;
        view.DeathOverlay.flipX = flipX;
        view.DeathOverlay.color = Color.white;   // alpha=1 즉시

        // Phase 0: Pop-in (큰 scale → base scale)
        Vector3 startScale = Vector3.one * baseScale * m_DeathPopStartMultiplier;
        Vector3 endScale = Vector3.one * 1.5f;
        view.DeathOverlay.transform.localScale = startScale;

        float t0 = 0f;
        while (t0 < m_DeathPopDuration)
        {
            t0 += Time.deltaTime;
            float k = t0 / m_DeathPopDuration;
            // EaseOut 느낌 (1 - (1-k)^2)
            float eased = 1f - (1f - k) * (1f - k);
            view.DeathOverlay.transform.localScale = Vector3.Lerp(startScale, endScale, eased);
            yield return null;
        }
        view.DeathOverlay.transform.localScale = endScale;

        // Phase 1: 메인 white → black (overlay는 유지)
        float t = 0f;
        while (t < m_DeathFadeInDuration)
        {
            t += Time.deltaTime;
            float k = t / m_DeathFadeInDuration;
            Color mainC = Color.Lerp(Color.white, Color.black, k);
            float mainAlpha = view.Renderer.color.a;
            view.Renderer.color = new Color(mainC.r, mainC.g, mainC.b, mainAlpha);
            yield return null;
        }
        float finalMainAlpha = view.Renderer.color.a;
        view.Renderer.color = new Color(0f, 0f, 0f, finalMainAlpha);

        // Phase 2: Hold
        yield return new WaitForSeconds(m_DeathHoldDuration);

        // Phase 3: 양쪽 alpha 1 → 0 (기존과 동일)
        Color mainStart = view.Renderer.color;
        float t2 = 0f;
        while (t2 < fadeOutDuration)
        {
            t2 += Time.deltaTime;
            float k = 1f - (t2 / fadeOutDuration);
            view.Renderer.color = new Color(mainStart.r, mainStart.g, mainStart.b, mainStart.a * k);
            view.DeathOverlay.color = new Color(1f, 1f, 1f, k);
            yield return null;
        }

        // 최종 상태 + 재사용 대비 원복
        view.Renderer.color = new Color(0f, 0f, 0f, 0f);
        view.DeathOverlay.color = new Color(1f, 1f, 1f, 0f);
        view.DeathOverlay.sprite = null;
        view.DeathOverlay.transform.localScale = Vector3.one;
        view.DeathOverlay.flipX = false;

        m_DyingUnits.Remove(unit);
    }
}
