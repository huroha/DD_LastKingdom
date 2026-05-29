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
    [SerializeField] private CombatHaloController m_HaloController;
    [SerializeField] private CombatDeathVfxPlayer m_DeathVfxPlayer;
    [SerializeField] private CombatHpBarController m_HpBarController;
    [SerializeField] private BgAttackOverlay m_BgAttackOverlay;
    [SerializeField] private CombatEffectPool m_EffectPool;

    [Header("Timing")]
    [SerializeField] private float m_PopupDuration = 0.6f;
    [SerializeField] private float m_StatusPopupDelay = 0.3f;
    [SerializeField] private float m_PostSequenceDelay = 0.2f;
    [SerializeField] private float m_HitDuration = 0.15f;
    [SerializeField] private float m_DotDeathFadeOutDuration = 0.25f;


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
    private static readonly Color COLOR_BLOCK = new Color(0.15f, 0.16f, 0.15f);
    private static readonly Color COLOR_DEATHDOOR = new Color(0.6f, 0f, 0f);

    // 팝업 텍스트
    private const string TRIGGER_ATTACK = "Attack";
    private const string TRIGGER_HIT = "Hit";
    private const string TRIGGER_DEATH = "Death";

    private const string POPUP_DODGE = "회피";
    private const string POPUP_RESIST = "저항";
    private const string POPUP_CRIT = "치명타!\n";
    private const string POPUP_DEATHS_DOOR = "죽음의 문턱!";
    private const string POPUP_BLOCK = "피해 차단됨";
    private const float CRIT_POPUP_SCALE = 1.15f;

    // duration 필드
    private WaitForSecondsRealtime m_WaitPopup;
    private WaitForSecondsRealtime m_WaitStatusPopup;
    private WaitForSecondsRealtime m_WaitPostSequence;
    private WaitForSecondsRealtime m_WaitHit;


    // Animation Event 동기화 플래그
    private bool m_AttackEndReceived;
    private StringBuilder m_PopupTextBuilder;

    //캐싱
    private Dictionary<int, HashSet<string>> m_AnimParamCache;

    private void Awake()
    {
        m_PopupTextBuilder = new StringBuilder();
        m_WaitPopup = new WaitForSecondsRealtime(m_PopupDuration);
        m_WaitStatusPopup = new WaitForSecondsRealtime(m_StatusPopupDelay);
        m_WaitPostSequence = new WaitForSecondsRealtime(m_PostSequenceDelay);
        m_WaitHit = new WaitForSecondsRealtime(m_HitDuration);
        m_AnimParamCache = new Dictionary<int, HashSet<string>>();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        m_AnimParamCache.Clear();
    }
    public Coroutine PlaySkillSequence(CombatUnit user, BaseSkillData skill, List<CombatUnit> targets, SkillResult result)
    {
        return StartCoroutine(SkillSequence(user, skill, targets, result));
    }
    private IEnumerator SkillSequence(CombatUnit user, BaseSkillData skill, List<CombatUnit> targets, SkillResult result)
    {
        m_FocusController.SetupFocus(user, targets, skill.SkillType);
        m_FieldView.SetFocusLock(true);

        if (!skill.IsAllyTargeting && result.TargetResults != null)
        {
            bool anyDamageHit = false;
            bool anyCrit = false;
            for (int i = 0; i < result.TargetResults.Length; ++i)
            {
                TargetResult tr = result.TargetResults[i];
                if (tr.IsHit && tr.DamageDealt > 0)
                {
                    anyDamageHit = true;
                    if (tr.IsCrit)
                        anyCrit = true;
                }
            }
            if (anyDamageHit)
            {
                bool targetIsNikke = user.UnitType == CombatUnitType.Enemy;
                m_BgAttackOverlay.Show(anyCrit, targetIsNikke);
            }
        }
        // 2. Focus In
        yield return m_FocusController.FocusIn(skill.IsAllyTargeting);
        m_DriftController.StartDrift(user, skill);

        // 3. 공격 모션
        if (result.TargetResults != null && result.TargetResults.Length > 0)
        {
            bool anyHit = false;
            for (int i = 0; i < result.TargetResults.Length; ++i)
            {
                if (!result.TargetResults[i].IsHit) continue;
                if (!anyHit)
                {
                    anyHit = true;
                    PlayAttackEffect(user, skill, targets);
                }
                PlayHitEffect(result.TargetResults[i].Target, skill);
            }
            if (targets.Count > 1)
                ProcessHitBatch(result.TargetResults, skill);
            else
                ProcessSingleHit(result.TargetResults[0], skill);
            if (skill.IsAllyTargeting && result.AllyResults != null)
            {
                for (int i = 0; i < result.AllyResults.Length; ++i)
                    PlayHitEffect(result.AllyResults[i].Unit, skill);
            }
        }

        // 사망 overlay 동작
        if (result.TargetResults != null)
        {
            for (int i = 0; i < result.TargetResults.Length; ++i)
            {
                TargetResult tr = result.TargetResults[i];
                m_DeathVfxPlayer.Play(tr.Target, tr.PreviousState, tr.ResultState, tr.IsHit, m_FocusController.FocusOutDuration);
            }
        }
        CombatFieldView.UnitView userView = m_FieldView.GetView(user);
        Sprite attackSprite = ResolveAttackSprite(user, skill);
        if (attackSprite != null)
        {
            if (userView.Animator != null)
                userView.Animator.enabled = false;
            userView.Renderer.sprite = attackSprite;
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
            m_HpBarController.PrepareGhost(tr.Target,tr.PreviousHp);
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
                m_HpBarController.StartGhostDrain(result.TargetResults[i].Target);
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
                            m_HaloController.ShowStunHalo(tr.Target);
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
                    m_HaloController.PopupDeathsDoorHalo(tr.Target);
                    m_PopupPool.SpawnEffect(pos, isNikke, POPUP_DEATHS_DOOR, COLOR_DEATHDOOR);
                    yield return m_WaitStatusPopup;
                }
            }
        }
        // OnSelf 효과 팝업
        if (result.SelfAppliedEffects != null)
        {
            Vector3 userPos = m_FieldView.GetSlotPosition(user);
            bool userIsNikke = user.UnitType == CombatUnitType.Nikke;
            for (int i = 0; i < result.SelfAppliedEffects.Count; ++i)
            {
                StatusEffectData effect = result.SelfAppliedEffects[i];
                m_PopupPool.SpawnEffect(userPos, userIsNikke, effect.EffectName,
                                        GetEffectColor(effect.EffectType), effect.Icon);
                yield return m_WaitStatusPopup;
            }
        }
        // AllyResults 순회 추가
        if (result.AllyResults != null)
        {
            for (int i = 0; i < result.AllyResults.Length; ++i)
            {
                CombatUnit ally = result.AllyResults[i].Unit;
                Vector3 pos = m_FieldView.GetSlotPosition(ally);
                bool isNikke = ally.UnitType == CombatUnitType.Nikke;
                StatusEffectData[] effects = result.AllyResults[i].AppliedEffects;
                for (int j = 0; j < effects.Length; ++j)
                {
                    m_PopupPool.SpawnEffect(pos, isNikke, effects[j].EffectName,
                                            GetEffectColor(effects[j].EffectType), effects[j].Icon);
                    yield return m_WaitStatusPopup;
                }
            }
        }

        // 10. 후딜레이
        yield return m_WaitPostSequence;
    }

    private void ProcessSingleHit(TargetResult targetResult, BaseSkillData skill)
    {
        ProcessOneTarget(targetResult, skill);
        if (targetResult.IsHit && !skill.IsAllyTargeting)
            m_Feedback.PlayCameraShake();
        if(!skill.IsAllyTargeting)
            m_Feedback.PlayHitStop(targetResult.IsCrit);
    }
    private void ProcessHitBatch(TargetResult[] results, BaseSkillData skill)
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
    private void SpawnDamagePopup(CombatUnit target, TargetResult result, BaseSkillData skill)
    {
        CombatFieldView.UnitView view = m_FieldView.GetView(target);
        Vector3 pos = view.Renderer.transform.position;
        bool isNikke = target.UnitType == CombatUnitType.Nikke;

        if (result.WasBlocked)
        {
            m_PopupPool.SpawnDamage(pos, isNikke, POPUP_BLOCK, COLOR_BLOCK);
        }
        else if (result.HealAmount > 0)
        {
            m_PopupPool.SpawnDamage(pos, isNikke, result.HealAmount.ToString(), COLOR_HEAL);
        }
        else if (result.DamageDealt > 0)
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
            case StatusEffectType.Burn:
                return COLOR_DAMAGE_RED;
            case StatusEffectType.Poison:
                return COLOR_POISON;
            case StatusEffectType.Stun:
                return COLOR_STUN;
            case StatusEffectType.Buff:
                return COLOR_BUFF;
            case StatusEffectType.Block:
                return COLOR_BLOCK;
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
        Coroutine deathCo = m_DeathVfxPlayer.Play(unit, previousState, resultState, true, m_DotDeathFadeOutDuration);

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

    private void ProcessOneTarget(TargetResult result, BaseSkillData skill)
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
    private Sprite ResolveAttackSprite(CombatUnit user, BaseSkillData skill)
    {
        if (skill.AttackSprite != null)
            return skill.AttackSprite;
        if (user.UnitType == CombatUnitType.Nikke && user.NikkeData.AttackSprite != null)
            return user.NikkeData.AttackSprite;
        return null;
    }
    private bool HasParameter(Animator animator, string paramName)
    {
        if (animator.runtimeAnimatorController == null) return false;
        int id = animator.runtimeAnimatorController.GetInstanceID();
        if (!m_AnimParamCache.TryGetValue(id, out HashSet<string> paramSet))
        {
            AnimatorControllerParameter[] ps = animator.parameters;
            paramSet = new HashSet<string>();
            for (int i = 0; i < ps.Length; ++i)
                paramSet.Add(ps[i].name);
            m_AnimParamCache[id] = paramSet;
        }
        return paramSet.Contains(paramName);
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
        m_HaloController.HideStunHalo(unit);
        Vector3 pos = m_FieldView.GetSlotPosition(unit);
        bool isNikke = unit.UnitType == CombatUnitType.Nikke;
        m_PopupPool.SpawnEffect(pos, isNikke, stunResistBuff.EffectName, GetEffectColor(stunResistBuff.EffectType), stunResistBuff.Icon);
        yield return m_WaitStatusPopup;
    }
    // Effect 관련
    private bool ResolveFlipX(CombatEffectData effect, CombatUnit target)
    {
        if (target.UnitType == CombatUnitType.Nikke)
            return effect.FlipXOnNikkeTarget;
        else
            return effect.FlipXOnEnemyTarget;
    }
    private IEnumerator ReturnAfter(Coroutine co, GameObject go, CombatEffectData effect)
    {
        yield return co;
        m_EffectPool.Return(go, effect);
    }
    private void PlayAttackEffect(CombatUnit user, BaseSkillData skill, List<CombatUnit> targets)
    {
        CombatEffectData effect = skill.AttackEffect;
        if (effect == null)
            return;
        CombatUnit refTarget = targets[0];
        CombatFieldView.UnitView userView = m_FieldView.GetView(user);
        int order = userView.Renderer.sortingOrder + 2;
        bool flipX = ResolveFlipX(effect, refTarget);

        if (skill.AttackMovement == EffectMovement.Static)
        {
            PlayAndReturn(effect, userView.Renderer.transform, order, flipX);
        }
        else if (skill.AttackMovement == EffectMovement.Projectile)
        {
            GameObject go = m_EffectPool.Borrow(effect);
            if (go == null) return;
            Vector3 from = userView.Renderer.transform.position;
            Vector3 to = m_FieldView.GetView(refTarget).Renderer.transform.position;
            Coroutine playCo = effect.Play(this, go, m_EffectPool.WorldRoot, order, flipX);
            StartCoroutine(ProjectileMove(go, from, to, skill.ProjectileSpeed, playCo, effect));
        }
    }
    private void PlayHitEffect(CombatUnit target, BaseSkillData skill)
    {
        CombatEffectData effect = skill.HitEffect;
        if (effect == null)
            return;
        CombatFieldView.UnitView view = m_FieldView.GetView(target);
        int order = view.Renderer.sortingOrder + 1;
        bool flipX = ResolveFlipX(effect, target);
        PlayAndReturn(effect, view.Renderer.transform, order, flipX);
    }
    private IEnumerator ProjectileMove(GameObject go, Vector3 from, Vector3 to, float speed, Coroutine playCo, CombatEffectData effect)
    {
        float dist = Vector3.Distance(from, to);
        if (speed <= 0f) speed = 1f;
        float duration = dist / speed;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            go.transform.position = Vector3.Lerp(from, to, k);
            yield return null;
        }
        go.transform.position = to;
        yield return playCo;
        m_EffectPool.Return(go, effect);
    }
    private void PlayAndReturn(CombatEffectData effect, Transform parent, int order, bool flipX)
    {
        GameObject go = m_EffectPool.Borrow(effect);
        if (go == null) return;
        Coroutine co = effect.Play(this, go, parent, order, flipX);
        StartCoroutine (ReturnAfter(co, go, effect));
    }
}
