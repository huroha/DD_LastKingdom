using UnityEngine;

public class UnitClickHandler : MonoBehaviour
{
    // delegate .action ¼±¾ð
    public delegate void ClickedHandler();

    private ClickedHandler m_OnClicked;

    public void Initialize(ClickedHandler onClicked)
    {
        m_OnClicked = onClicked;
    }
    private void OnMouseDown()
    {
        m_OnClicked?.Invoke();
    }
}
