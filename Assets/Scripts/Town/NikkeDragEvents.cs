using UnityEngine;
using UnityEngine.UI;
public static class NikkeDragEvents
{
    public enum Source { Card, Slot}
    public delegate void DragStateHandler(Source source);
    public static event DragStateHandler OnDragStarted;
    public static event DragStateHandler OnDragEnded;

    private static GameObject m_Ghost;
    private static Image m_GhostImage;
    private static RectTransform m_GhostRT;

    public static void BeginGhost(Canvas rootCanvas, Sprite sprite, Vector2 size, Vector3 worldPos)
    {
        if (m_Ghost == null)
        {
            m_Ghost = new GameObject("DragGhost");
            m_GhostImage = m_Ghost.AddComponent<Image>();
            m_GhostImage.raycastTarget = false;
            m_GhostRT = m_Ghost.GetComponent<RectTransform>();
        }
        m_Ghost.transform.SetParent(rootCanvas.transform, false);
        m_Ghost.transform.SetAsLastSibling();
        m_GhostImage.sprite = sprite;
        m_GhostRT.sizeDelta = size;
        m_Ghost.transform.position = worldPos;
        m_Ghost.SetActive(true);
    }
    public static void UpdateGhost(Vector3 worldPos)
    {
        m_Ghost.transform.position = worldPos;
    }
    public static void EndGhost()
    {
        m_Ghost.SetActive(false);
    }
    public static void RaiseDragStarted(Source source)
    {
        OnDragStarted?.Invoke(source);
    }
    public static void RaiseDragEnded(Source source)
    {
        OnDragEnded?.Invoke(source);
    }
}
