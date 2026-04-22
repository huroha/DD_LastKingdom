using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatFocusController : MonoBehaviour
{
    [Header("Focus")]
    [SerializeField] private float m_FocusScale;
    [SerializeField] private float m_FocusOutDuration;
    [SerializeField] private float m_NikkeFocusLayoutScale;
    [SerializeField] private float m_EnemyFocusLayoutScale;
    [SerializeField] private float m_EnemyFocusMinXMargin;

    [Header("Focus Points")]
    [SerializeField] private Transform m_NikkeFocusPoint;
    [SerializeField] private Transform m_EnemyFocusPoint;

    [Header("Camera")]
    [SerializeField] private Camera m_Camera;
    [SerializeField] private float m_FocusFOV;

    [Header("Blur")]
    [SerializeField] private FocusBlurController m_BlurController;
    [SerializeField] private float m_BlurStrength;
    [SerializeField] private int m_FocusSortingOrder;

    [Header("BG Tilt")]
    [SerializeField] private Transform m_BgTransform;
    [SerializeField] private float m_BgTiltAngle;
    [SerializeField] private float m_BgTiltDuration;

    [Header("References")]
    [SerializeField] private CombatFieldView m_FieldView;
    [SerializeField] private CombatHUD m_CombatHUD;

    // BG Tilt
    private Coroutine m_BgTiltCoroutine;
    private float m_CurrentBgTiltZ;

    // Focus 캐시 (Drift에서도 접근 필요)
    private Dictionary<CombatUnit, Vector3> m_OriginalScales;
    private Dictionary<CombatUnit, Vector3> m_OriginalPositions;
    private Dictionary<CombatUnit, Vector3> m_DriftedPositions;
    private Dictionary<CombatUnit, int> m_OriginalSortingOrders;
    private Dictionary<CombatUnit, CombatFieldView.UnitView> m_ViewCache;
    private List<CombatUnit> m_AllLivingBuffer;
    private HashSet<CombatUnit> m_FocusBuffer;
    private List<CombatUnit> m_NikkeFocusBuffer;
    private List<CombatUnit> m_EnemyFocusBuffer;
    private CombatUnit m_FocusUser;
    private float m_OriginalFOV;
    private int m_FocusLayer;
    private int m_DefaultLayer;

    // Drift, Director에서 접근할 프로퍼티
    public IReadOnlyCollection<CombatUnit> FocusBuffer => m_FocusBuffer;
    public Transform NikkeFocusPoint => m_NikkeFocusPoint;
    public Transform EnemyFocusPoint => m_EnemyFocusPoint;
    public Camera FocusCamera => m_Camera;
    public float FocusOutDuration => m_FocusOutDuration;
    private void Awake()
    {
        m_OriginalScales = new Dictionary<CombatUnit, Vector3>();
        m_AllLivingBuffer = new List<CombatUnit>();
        m_FocusBuffer = new HashSet<CombatUnit>();
        m_ViewCache = new Dictionary<CombatUnit, CombatFieldView.UnitView>();
        m_OriginalSortingOrders = new Dictionary<CombatUnit, int>();
        m_OriginalPositions = new Dictionary<CombatUnit, Vector3>();
        m_DriftedPositions = new Dictionary<CombatUnit, Vector3>();
        m_NikkeFocusBuffer = new List<CombatUnit>();
        m_EnemyFocusBuffer = new List<CombatUnit>();

        m_FocusLayer = LayerMask.NameToLayer("FocusForeground");
        m_DefaultLayer = LayerMask.NameToLayer("Default");
    }

    // public
    public void SetupFocus(CombatUnit user, List<CombatUnit> targets)
    {
        // 포커스 대상 구성
        m_FocusBuffer.Clear();
        m_FocusBuffer.Add(user);
        for (int i = 0; i < targets.Count; ++i)
        {
            if (targets[i] != user)
                m_FocusBuffer.Add(targets[i]);
        }
        m_FocusUser = user;
    }

    public IEnumerator FocusIn(bool skipTilt = false)
    {
        m_CombatHUD.SetHpBarsVisible(false);
        m_FieldView.GetAllLivingUnits(m_AllLivingBuffer);

        m_OriginalScales.Clear();
        m_OriginalPositions.Clear();
        m_OriginalSortingOrders.Clear();
        m_ViewCache.Clear();

        if (!skipTilt)
            StartBgTilt(m_FocusUser.UnitType);

        foreach (CombatUnit unit in m_AllLivingBuffer)
        {
            m_FieldView.StopPopScale(unit);
            CombatFieldView.UnitView view = m_FieldView.GetView(unit);
            CacheUnitView(unit, view);
        }
        foreach (CombatUnit unit in m_FocusBuffer)
        {
            if (m_ViewCache.ContainsKey(unit))
                continue;
            CombatFieldView.UnitView view = m_FieldView.GetView(unit);
            if (view.Renderer == null)
                continue;
            CacheUnitView(unit, view);
        }

        m_NikkeFocusBuffer.Clear();
        m_EnemyFocusBuffer.Clear();
        foreach (CombatUnit unit in m_FocusBuffer)
        {
            if (unit.UnitType == CombatUnitType.Nikke)
                m_NikkeFocusBuffer.Add(unit);
            else
                m_EnemyFocusBuffer.Add(unit);
        }
        Vector3 screenCenter = (m_NikkeFocusPoint.position + m_EnemyFocusPoint.position) * 0.5f;

        bool allSameTeam = m_NikkeFocusBuffer.Count == 0 || m_EnemyFocusBuffer.Count == 0;

        if (allSameTeam)
        {
            List<CombatUnit> units = m_NikkeFocusBuffer.Count > 0 ? m_NikkeFocusBuffer : m_EnemyFocusBuffer;
            AssignFocusPositions(units, screenCenter, units == m_NikkeFocusBuffer ? m_NikkeFocusLayoutScale :
            m_EnemyFocusLayoutScale);
        }
        else
        {
            AssignFocusPositions(m_NikkeFocusBuffer, m_NikkeFocusPoint.position, m_NikkeFocusLayoutScale);
            AssignFocusPositions(m_EnemyFocusBuffer, m_EnemyFocusPoint.position, m_EnemyFocusLayoutScale, screenCenter.x +
            m_EnemyFocusMinXMargin);
        }

        m_BlurController.SetBlurStrength(m_BlurStrength);
        m_OriginalFOV = m_Camera.fieldOfView;
        m_Camera.fieldOfView = m_FocusFOV;



        yield break;
    }
    public IEnumerator FocusOut()
    {
        StopBgTilt();

        m_DriftedPositions.Clear();
        foreach (CombatUnit unit in m_FocusBuffer)
            m_DriftedPositions[unit] = m_ViewCache[unit].Renderer.transform.position;

        int snapCount = 0;
        CombatFieldView.UnitView[] snapViews = new CombatFieldView.UnitView[m_FocusBuffer.Count];
        Vector3[] snapScales = new Vector3[m_FocusBuffer.Count];
        Vector3[] snapFrom = new Vector3[m_FocusBuffer.Count];
        Vector3[] snapTo = new Vector3[m_FocusBuffer.Count];

        foreach (CombatUnit unit in m_FocusBuffer)
        {
            snapViews[snapCount] = m_ViewCache[unit];
            snapScales[snapCount] = m_OriginalScales[unit];
            snapFrom[snapCount] = m_DriftedPositions[unit];
            snapTo[snapCount] = m_OriginalPositions[unit];
            ++snapCount;
        }

        float elapsed = 0f;
        while (elapsed < m_FocusOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / m_FocusOutDuration;

            for (int i = 0; i < snapCount; ++i)
            {
                snapViews[i].Renderer.transform.localScale = Vector3.Lerp(
                    snapScales[i] * m_FocusScale, snapScales[i], t);
                snapViews[i].Renderer.transform.position = Vector3.Lerp(
                    snapFrom[i], snapTo[i], t);
            }

            m_Camera.fieldOfView = Mathf.Lerp(m_FocusFOV, m_OriginalFOV, t);
            yield return null;
        }

        foreach (CombatUnit unit in m_FocusBuffer)
        {
            CombatFieldView.UnitView view = m_ViewCache[unit];
            view.Renderer.transform.localScale = m_OriginalScales[unit];
            view.Renderer.transform.position = m_OriginalPositions[unit];
            view.Renderer.sortingOrder = m_OriginalSortingOrders[unit];
            view.Renderer.gameObject.layer = m_DefaultLayer;
            if (view.DeathOverlay != null)
            {
                view.DeathOverlay.sortingOrder = m_OriginalSortingOrders[unit] + 1;
                view.DeathOverlay.gameObject.layer = m_DefaultLayer;
            }
        }

        m_Camera.fieldOfView = m_OriginalFOV;
        m_BlurController.SetBlurStrength(0f);
        m_CombatHUD.SetHpBarsVisible(true);
    }
    // 헬퍼
    private void AssignFocusPositions(List<CombatUnit> units, Vector3 focusCenter, float layoutScale, float minX = float.MinValue)
    {
        Vector3[] slotPositions = new Vector3[units.Count];
        Vector3 layoutCenter = Vector3.zero;
        for (int i = 0; i < units.Count; ++i)
        {
            slotPositions[i] = m_FieldView.GetSlotPosition(units[i]);
            layoutCenter += slotPositions[i];
        }
        layoutCenter /= units.Count;

        float xShift = 0f;
        if (minX > float.MinValue)
        {
            for (int i = 0; i < units.Count; ++i)
            {
                Vector3 slotOffset = slotPositions[i] - layoutCenter;
                float candidateX = focusCenter.x + slotOffset.x * layoutScale;
                xShift = Mathf.Max(xShift, minX - candidateX);
            }
        }
        for (int i = 0; i < units.Count; ++i)
        {
            CombatUnit unit = units[i];
            CombatFieldView.UnitView view = m_ViewCache[unit];
            Vector3 slotOffset = slotPositions[i] - layoutCenter;
            Vector3 pos = focusCenter + slotOffset * layoutScale;
            pos.x += xShift;
            view.Renderer.transform.localPosition = m_BgTransform.InverseTransformPoint(pos);
            view.Renderer.transform.localScale = m_OriginalScales[unit] * m_FocusScale;
            view.Renderer.sortingOrder = m_FocusSortingOrder;
            view.Renderer.gameObject.layer = m_FocusLayer;
            if (view.DeathOverlay != null)
            {
                view.DeathOverlay.sortingOrder = m_FocusSortingOrder + 1;
                view.DeathOverlay.gameObject.layer = m_FocusLayer;
            }
        }
    }
    private void CacheUnitView(CombatUnit unit, CombatFieldView.UnitView view)
    {
        m_ViewCache[unit] = view;
        m_OriginalScales[unit] = view.Renderer.transform.localScale;
        m_OriginalPositions[unit] = view.Renderer.transform.position;
        m_OriginalSortingOrders[unit] = view.Renderer.sortingOrder;
    }

    // Bg Tilt관련
    private void StartBgTilt(CombatUnitType attackerType)
    {
        float targetAngle = (attackerType == CombatUnitType.Nikke) ? -m_BgTiltAngle : m_BgTiltAngle;
        CoroutineHelper.Restart(this, ref m_BgTiltCoroutine, BgTiltRoutine(m_CurrentBgTiltZ, targetAngle));
    }

    private void StopBgTilt()
    {
        CoroutineHelper.Restart(this, ref m_BgTiltCoroutine, BgTiltRoutine(m_CurrentBgTiltZ, 0f));
    }

    private IEnumerator BgTiltRoutine(float from, float to)
    {
        float elapsed = 0f;
        Vector3 euler = m_BgTransform.localEulerAngles;
        while (elapsed < m_BgTiltDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / m_BgTiltDuration;
            m_CurrentBgTiltZ = Mathf.Lerp(from, to, t);
            euler.z = m_CurrentBgTiltZ;
            m_BgTransform.localEulerAngles = euler;
            yield return null;
        }
        m_CurrentBgTiltZ = to;
        euler.z = to;
        m_BgTransform.localEulerAngles = euler;
        m_BgTiltCoroutine = null;
    }
}
