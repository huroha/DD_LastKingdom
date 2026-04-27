using UnityEngine;
using UnityEngine.InputSystem;
public class UnitRightClickHandler : MonoBehaviour
{
    public delegate void RightClickedHandler();
    private RightClickedHandler m_OnRightClicked;

    public void Initialize(RightClickedHandler onRightClicked)
    {
        m_OnRightClicked = onRightClicked;
    }

    private void OnMouseOver()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            m_OnRightClicked?.Invoke();
    }
}
