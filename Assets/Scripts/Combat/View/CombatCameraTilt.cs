using UnityEngine;
using System.Collections;

public class CombatCameraTilt : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera m_Camera;
    [SerializeField] private CombatHUD m_CombatHUD;
    [SerializeField] private CombatSlotPositionController m_SlotPositionController;

    [Header("Option")]
    [SerializeField] private float m_TiltAngle = 3f;
    [SerializeField] private float m_TiltDuration = 0.3f;

    private Coroutine m_TiltCoroutine;
    private bool m_IsTilted;
    private float m_CurrentTiltY;

    private void OnEnable()
    {
        EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
    }
    private void OnDisable()
    {
        EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
    }

    private void OnTurnStarted(TurnStartedEvent e)
    {
        if(e.Unit.UnitType == CombatUnitType.Enemy && m_IsTilted == false)
        {
            CoroutineHelper.Restart(this, ref m_TiltCoroutine, LerpTilt(m_CurrentTiltY, m_TiltAngle));
            m_IsTilted = true;
        }
        else if(e.Unit.UnitType == CombatUnitType.Nikke && m_IsTilted)
        {
            CoroutineHelper.Restart(this, ref m_TiltCoroutine, LerpTilt(m_CurrentTiltY, 0f));
            m_IsTilted = false;
        }
    }

    private IEnumerator LerpTilt(float from, float to)
    {
        float elapsed = 0f;
        Vector3 euler = m_Camera.transform.eulerAngles;

        while (elapsed < m_TiltDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / m_TiltDuration;
            m_CurrentTiltY = Mathf.Lerp(from, to, t);
            euler.y = m_CurrentTiltY;
            m_Camera.transform.eulerAngles = euler;
            m_SlotPositionController.UpdateSlotPositionsForTilt();
            yield return null;
        }

        m_CurrentTiltY = to;
        euler.y = to;
        m_Camera.transform.eulerAngles = euler;

        if (to == 0f)
            m_SlotPositionController.ResetSlotPositions();
        else
            m_SlotPositionController.UpdateSlotPositionsForTilt();

        m_TiltCoroutine = null;
    }
}
