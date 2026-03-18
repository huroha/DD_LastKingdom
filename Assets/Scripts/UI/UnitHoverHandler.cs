using UnityEngine;

public class UnitHoverHandler : MonoBehaviour
{
    public delegate void HoverHandler();

    private HoverHandler m_OnEnter;
    private HoverHandler m_OnExit;

    public void Initialize(HoverHandler onEnter, HoverHandler onExit)
    {
        m_OnEnter = onEnter;
        m_OnExit = onExit;
    }

    private void OnMouseEnter()
    {
        m_OnEnter?.Invoke();
    }

    private void OnMouseExit()
    {
        m_OnExit?.Invoke();
    }

}

