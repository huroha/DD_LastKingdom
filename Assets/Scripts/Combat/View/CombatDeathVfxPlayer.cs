using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatDeathVfxPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatFieldView m_FieldView;
    [SerializeField] private CombatHUD m_CombatHUD;
    [SerializeField] private CombatHpBarController m_HpBarController;

    [Header("Death Overlay")]
    [SerializeField] private Sprite m_DeathOverlaySprite;
    [SerializeField] private float m_DeathOverlayScale = 1f;
    [SerializeField] private float m_DeathOverlayScaleLarge = 1.5f;
    [SerializeField] private float m_DeathPopStartMultiplier = 2f;
    [SerializeField] private float m_DeathPopDuration = 0.2f;
    [SerializeField] private bool m_NikkeOverlayFlipX = false;
    [SerializeField] private bool m_EnemyOverlayFlipX = true;
    [SerializeField] private float m_DeathFadeInDuration = 0.4f;
    [SerializeField] private float m_DeathHoldDuration = 0.4f;

    [Header("Corpse Death VFX")]
    [SerializeField] private Sprite m_CorpseVfxSprite;
    [SerializeField] private float m_CorpseVfxScale = 3.5f;
    [SerializeField] private float m_CorpseVfxScaleLarge = 4f;
    [SerializeField] private float m_CorpseVfxStartMultiplier = 0.9f;

    private enum DeathVfxKind { None, Normal, Corpse }
    private HashSet<CombatUnit> m_DyingUnits;

    private void Awake()
    {
        m_DyingUnits = new HashSet<CombatUnit>();
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        if (m_DyingUnits != null)
            m_DyingUnits.Clear();
    }

    public Coroutine Play(CombatUnit unit, UnitState prev, UnitState result, bool isHit, float fadeOutDuration)
    {
        DeathVfxKind kind = GetVfxKind(unit, prev, result, isHit);
        if (kind == DeathVfxKind.Normal)
            return StartCoroutine(DeathVfxRoutine(unit, fadeOutDuration));
        if (kind == DeathVfxKind.Corpse)
            return StartCoroutine(CorpseVfxRoutine(unit, fadeOutDuration));
        return null;
    }

    private IEnumerator ScalePopIn(Transform target, Vector3 startScale, Vector3 endScale, float duration)
    {
        target.localScale = startScale;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            float eased = 1f - (1f - k) * (1f - k);
            target.localScale = Vector3.Lerp(startScale, endScale, eased);
            yield return null;
        }
        target.localScale = endScale;
    }
    private IEnumerator DeathFadeSequence(CombatFieldView.UnitView view, SpriteRenderer overlay, float fadeOutDuration, bool trackOverlayLayer)
    {
        // Phase 1: white → black tint
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
        float phase1Alpha = view.Renderer.color.a;
        view.Renderer.color = new Color(0f, 0f, 0f, phase1Alpha);

        // Phase 2: hold
        yield return new WaitForSeconds(m_DeathHoldDuration);

        // Phase 3: alpha fade-out
        Color mainStart = view.Renderer.color;
        float t2 = 0f;
        while (t2 < fadeOutDuration)
        {
            t2 += Time.deltaTime;
            float k = 1f - (t2 / fadeOutDuration);
            view.Renderer.color = new Color(mainStart.r, mainStart.g, mainStart.b, mainStart.a * k);
            overlay.color = new Color(1f, 1f, 1f, k);
            if (trackOverlayLayer)
            {
                overlay.sortingOrder = view.Renderer.sortingOrder + 5;
                overlay.gameObject.layer = view.Renderer.gameObject.layer;
            }
            yield return null;
        }

        view.Renderer.color = new Color(0f, 0f, 0f, 0f);
    }
    private IEnumerator DeathVfxRoutine(CombatUnit unit, float fadeOutDuration)
    {
        if (m_DyingUnits.Contains(unit)) yield break;
        m_DyingUnits.Add(unit);

        m_HpBarController.Hide(unit);
        m_CombatHUD.HideTicker(unit);

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
        view.DeathOverlay.gameObject.layer = view.Renderer.gameObject.layer;
        view.DeathOverlay.flipX = flipX;
        view.DeathOverlay.color = Color.white;

        // Phase 0: Pop-in
        Vector3 startScale = Vector3.one * baseScale * m_DeathPopStartMultiplier;
        Vector3 endScale = Vector3.one * 1.5f;
        yield return ScalePopIn(view.DeathOverlay.transform, startScale, endScale, m_DeathPopDuration);

        // Phase 1 + 2 + 3
        yield return DeathFadeSequence(view, view.DeathOverlay, fadeOutDuration, false);

        // DeathOverlay 정리
        view.DeathOverlay.color = new Color(1f, 1f, 1f, 0f);
        view.DeathOverlay.sprite = null;
        view.DeathOverlay.transform.localScale = Vector3.one;
        view.DeathOverlay.flipX = false;

        m_DyingUnits.Remove(unit);
    }
    private static DeathVfxKind GetVfxKind(CombatUnit target, UnitState prev, UnitState result, bool isHit)
    {
        if (!isHit)
            return DeathVfxKind.None;
        if (target.UnitType == CombatUnitType.Nikke)
            return (prev == UnitState.DeathsDoor && result == UnitState.Dead) ? DeathVfxKind.Normal : DeathVfxKind.None;
        if (prev == UnitState.Alive && (result == UnitState.Corpse || result == UnitState.Dead))
            return DeathVfxKind.Normal;
        if (prev == UnitState.Corpse && result == UnitState.Dead)
            return DeathVfxKind.Corpse;
        return DeathVfxKind.None;

    }

    private IEnumerator CorpseVfxRoutine(CombatUnit unit, float fadeOutDuration)
    {
        if (m_DyingUnits.Contains(unit)) yield break;
        m_DyingUnits.Add(unit);

        m_HpBarController.Hide(unit);
        m_CombatHUD.HideTicker(unit);

        CombatFieldView.UnitView view = m_FieldView.GetView(unit);
        if (view.Renderer == null)
        {
            m_DyingUnits.Remove(unit);
            yield break;
        }

        // 동적 overlay 생성 (corpse view는 DeathOverlay가 null)
        GameObject overlayGo = new GameObject("CorpseVfxOverlay");
        overlayGo.transform.SetParent(view.Renderer.transform, false);
        overlayGo.transform.localPosition = new Vector3(0f, 2f, 0f);
        overlayGo.transform.localRotation = Quaternion.identity;

        SpriteRenderer overlay = overlayGo.AddComponent<SpriteRenderer>();
        overlay.sprite = m_CorpseVfxSprite;
        overlay.sortingLayerID = view.Renderer.sortingLayerID;
        overlay.sortingOrder = view.Renderer.sortingOrder + 5;
        overlay.gameObject.layer = view.Renderer.gameObject.layer;
        overlay.color = Color.white;

        // Phase 0: Pop-in
        float baseScale = unit.SlotSize >= 2 ? m_CorpseVfxScaleLarge : m_CorpseVfxScale;
        Vector3 endScale = Vector3.one * baseScale;
        Vector3 startScale = endScale * m_CorpseVfxStartMultiplier;
        yield return ScalePopIn(overlay.transform, startScale, endScale, m_DeathPopDuration);

        // Phase 1 + 2 + 3
        yield return DeathFadeSequence(view, overlay, fadeOutDuration, true);

        // 최종 정리
        Destroy(overlayGo);

        m_DyingUnits.Remove(unit);
    }
}
