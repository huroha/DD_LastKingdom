using UnityEngine;

public class UnitAnimBridge : MonoBehaviour
{
    public delegate void AnimEventHandler();

    private AnimEventHandler m_OnHitFrame;
    private AnimEventHandler m_OnAttackEnd;

    public void SetCallbacks(AnimEventHandler onHitFrame, AnimEventHandler onAttackEnd)
    {
        m_OnHitFrame = onHitFrame;
        m_OnAttackEnd = onAttackEnd;
    }
    public void OnHitFrame()
    {
        m_OnHitFrame?.Invoke();
    }
    public void OnAttackEnd()
    {
        m_OnAttackEnd?.Invoke();
    }
    public void ClearCallbacks()
    {
        m_OnAttackEnd = null;
        m_OnHitFrame = null;
    }
}

