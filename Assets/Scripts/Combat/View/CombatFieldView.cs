using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class CombatFieldView : MonoBehaviour
{
    [SerializeField] private Transform[] m_NikkeSlots;      // Inspector에서 연결
    [SerializeField] private Transform[] m_EnemySlots;
    [SerializeField] private float m_MoveDuration = 0.3f;
    [SerializeField] private float m_UnitScale = 0.33f;
    [SerializeField] private float m_LargeUnitScale = 0.5f;

    [SerializeField] private CombatHUD m_CombatHUD;

    [SerializeField] private float m_PopScale = 1.1f;
    [SerializeField] private float m_PopDuration = 0.2f;
    private Dictionary<CombatUnit, Coroutine> m_PopCoroutine;
    private Dictionary<CombatUnit, Vector3> m_PopOriginalScales;

    // CombatUnit -> 해당 유닛의 SpriteRenderer 맵핑
    private Dictionary<CombatUnit, UnitView> m_UnitViews;
    // 중복 코루틴 방지용
    private Dictionary<CombatUnit, Coroutine> m_MoveCoroutines;
    // 시체 관리용
    private Dictionary<CombatUnit, SpriteRenderer> m_CorpseViews;

    private int m_SortingCount = 10;

    // 이동 후 패턴사용
    public bool IsMoving => m_MoveCoroutines.Count > 0;
    // 포커싱중 이동 금지
    private bool m_FocusLocked;
    public struct UnitView
    {
        public SpriteRenderer Renderer;
        public Animator Animator;
        public UnitAnimBridge AnimBridge;
    }

    private void Awake()
    {
        m_PopCoroutine = new Dictionary<CombatUnit, Coroutine>();
        m_PopOriginalScales = new Dictionary<CombatUnit, Vector3>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Subscribe<UnitMovedEvent>(OnUnitMoved);
        EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);

    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Unsubscribe<UnitMovedEvent>(OnUnitMoved);
        EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
    }

    private void OnBattleStarted(BattleStartedEvent e)
    {
        if(m_UnitViews != null)
        {
            foreach (KeyValuePair<CombatUnit, UnitView> pair in m_UnitViews)
                Destroy(pair.Value.Renderer.gameObject);
        }

        m_UnitViews = new Dictionary<CombatUnit, UnitView>();
        m_MoveCoroutines = new Dictionary<CombatUnit, Coroutine>();
        m_CorpseViews = new Dictionary<CombatUnit, SpriteRenderer>();

        for (int i=0; i<e.Nikkes.Count; ++i)
        {
            CombatUnit unit = e.Nikkes[i];
            Vector3 pos = GetSlotPosition(unit);
            UnitView view = CreateUnitView(unit, pos);
            m_UnitViews[unit] = view;
        }

        for (int i=0; i<e.Enemies.Count; ++i)
        {
            CombatUnit unit = e.Enemies[i];
            Vector3 pos = GetSlotPosition(unit);
            UnitView view = CreateUnitView(unit, pos);
            m_UnitViews[unit] = view;
        }
    }
    private void OnUnitMoved(UnitMovedEvent e)
    {
        if (m_FocusLocked)
            return;
        MoveAllToCurrentSlots();
    }
    private void OnUnitDied(UnitDiedEvent e)
    {
        if(m_MoveCoroutines.ContainsKey(e.Unit))
            StopCoroutine(m_MoveCoroutines[e.Unit]);
        m_MoveCoroutines.Remove(e.Unit);

        // Corpse -> Dead : 시체 제거
        if(m_CorpseViews.ContainsKey(e.Unit))
        {
            Destroy(m_CorpseViews[e.Unit].gameObject);
            m_CorpseViews.Remove(e.Unit);
            MoveAllToCurrentSlots();
            return;
        }

        if (!m_UnitViews.TryGetValue(e.Unit, out UnitView view))
            return;
        m_UnitViews.Remove(e.Unit);

        // 적이고 시체 스프라이트가 있으면 교체, 없으면 제거?
        if(e.Unit.UnitType == CombatUnitType.Enemy && e.Unit.EnemyData.CorpseSprite != null)
        {
            if (e.Unit.State == UnitState.Corpse)
            {
                view.Renderer.sprite = e.Unit.EnemyData.CorpseSprite;
                m_CorpseViews[e.Unit] = view.Renderer;
            }
            else
            {
                Destroy(view.Renderer.gameObject);
                MoveAllToCurrentSlots();
            }
        }
        else
        {
            Destroy(view.Renderer.gameObject);
            MoveAllToCurrentSlots();
        }
    }

    private UnitView CreateUnitView(CombatUnit unit, Vector3 pos)
    {
        GameObject go;
        SpriteRenderer sr;
        float scale = unit.SlotSize == 2 ? m_LargeUnitScale : m_UnitScale;
        if(unit.UnitType == CombatUnitType.Nikke && unit.NikkeData.CombatPrefab != null)
        {
            go = Instantiate(unit.NikkeData.CombatPrefab);
            go.name = unit.UnitName;
            sr = go.GetComponent<SpriteRenderer>();
        }
        else
        {
            go = new GameObject(unit.UnitName);
            sr = go.AddComponent<SpriteRenderer>();
            if (unit.UnitType == CombatUnitType.Nikke)
                sr.sprite = unit.NikkeData.CombatIdleSprite;
            else
                sr.sprite = unit.EnemyData.Sprite;
        }
        go.AddComponent<BoxCollider2D>();
        if(unit.UnitType == CombatUnitType.Enemy)
        {
            UnitHoverHandler hoverHandler = go.AddComponent<UnitHoverHandler>();
            CombatUnit captured = unit;
            hoverHandler.Initialize(
                () => m_CombatHUD.ShowEnemyInfo(captured),
                () => m_CombatHUD.HideEnemyInfo());
        }
        go.transform.SetParent(transform);
        go.transform.position = pos;
        float scaleOffset =  unit.UnitType == CombatUnitType.Nikke ? unit.NikkeData.ScaleOffset : unit.EnemyData.ScaleOffset;
        go.transform.localScale = Vector3.one * scale * scaleOffset;
        sr.sortingOrder = m_SortingCount - unit.SlotIndex;

        Animator animator = go.GetComponent<Animator>();
        if(animator == null)
        {
            RuntimeAnimatorController animCtrl = null;
            if (unit.UnitType == CombatUnitType.Enemy)
                animCtrl = unit.EnemyData.CombatAnimator;
            if(animCtrl != null)
            {
                animator = go.AddComponent<Animator>();
                animator.runtimeAnimatorController = animCtrl;
            }
        }
        UnitAnimBridge animBridge = go.GetComponent<UnitAnimBridge>();
        if (animBridge == null && animator != null)
            animBridge = go.AddComponent<UnitAnimBridge>();

        UnitView view;
        view.Renderer = sr;
        view.Animator = animator;
        view.AnimBridge = animBridge;
        return view;
    }
    private Transform[] GetSlots(CombatUnitType type)
    {
        if (type == CombatUnitType.Nikke)
            return m_NikkeSlots;
        else
            return m_EnemySlots;
    }

    public Vector3 GetSlotPosition(CombatUnit unit)
    {
        Transform[] unitSlots = GetSlots(unit.UnitType);
        if (unit.SlotSize == 2 && unit.SlotIndex + 1 < unitSlots.Length)
            return (unitSlots[unit.SlotIndex].position + unitSlots[unit.SlotIndex + 1].position) / 2f;
        return unitSlots[unit.SlotIndex].position;
    }

    public void MoveAllToCurrentSlots()
    {
        foreach(KeyValuePair<CombatUnit, UnitView> pair in m_UnitViews)
        {
            CombatUnit unit = pair.Key;
            UnitView view = pair.Value;

            Vector3 targetPos = GetSlotPosition(unit);
            if(m_MoveCoroutines.ContainsKey(unit))
                StopCoroutine(m_MoveCoroutines[unit]);
            m_MoveCoroutines[unit] = StartCoroutine(LerpToPosition(unit,view.Renderer, targetPos));
        }

        foreach (KeyValuePair<CombatUnit, SpriteRenderer> pair in m_CorpseViews)
        {
            CombatUnit unit = pair.Key;
            SpriteRenderer view = pair.Value;
            Vector3 targetPos = GetSlotPosition(unit);
            if (m_MoveCoroutines.ContainsKey(unit))
                StopCoroutine(m_MoveCoroutines[unit]);
            m_MoveCoroutines[unit] = StartCoroutine(LerpToPosition(unit, view, targetPos));
        }

    }
    private IEnumerator LerpToPosition(CombatUnit unit, SpriteRenderer view , Vector3 target)
    {
        Vector3 startPos = view.transform.position;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / m_MoveDuration;
            view.transform.position = Vector3.Lerp(startPos, target, t);
            yield return null;
        }
        view.transform.position = target;
        m_MoveCoroutines.Remove(unit);

    }

    public UnitView GetView(CombatUnit unit)
    {
        if (m_UnitViews.TryGetValue(unit, out UnitView view))
            return view;
        if (m_CorpseViews.TryGetValue(unit, out SpriteRenderer sr))
        {
            UnitView corpseView;
            corpseView.Renderer = sr;
            corpseView.Animator = null;
            corpseView.AnimBridge = null;
            return corpseView;
        }
        return default(UnitView);
    }
    public void GetAllLivingUnits(List<CombatUnit> result)
    {
        result.Clear();
        foreach(CombatUnit unit in m_UnitViews.Keys)
            result.Add(unit);
    }

    public void SetFocusLock(bool locked)
    {
        m_FocusLocked = locked;
    }
    public void PlayPopScale(CombatUnit unit)
    {
        if (!m_UnitViews.TryGetValue(unit, out UnitView view))
            return;
        StopPopScale(unit);
        Vector3 originalScale = view.Renderer.transform.localScale;
        m_PopOriginalScales[unit] = originalScale;
        m_PopCoroutine[unit] = StartCoroutine(PopRoutine(unit, view.Renderer, originalScale));
    }

    public void StopPopScale(CombatUnit unit)
    {
        if (!m_PopCoroutine.ContainsKey(unit))
            return;
        StopCoroutine(m_PopCoroutine[unit]);
        m_PopCoroutine.Remove(unit);
        if(m_UnitViews.TryGetValue(unit, out UnitView view) && m_PopOriginalScales.ContainsKey(unit))
            view.Renderer.transform.localScale = m_PopOriginalScales[unit];
        m_PopOriginalScales.Remove(unit);
    }

    private IEnumerator PopRoutine(CombatUnit unit, SpriteRenderer renderer, Vector3 originalScale)
    {
        Vector3 peakScale = originalScale * m_PopScale;
        float half = m_PopDuration * 0.5f;
        float elapsed = 0f;
        while(elapsed < half)
        {
            elapsed += Time.deltaTime;
            renderer.transform.localScale = Vector3.Lerp(originalScale, peakScale, elapsed / half);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            renderer.transform.localScale = Vector3.Lerp(peakScale, originalScale, elapsed / half);
            yield return null;
        }

        renderer.transform.localScale = originalScale;
        m_PopCoroutine.Remove(unit);
        m_PopOriginalScales.Remove(unit);
    }
}
