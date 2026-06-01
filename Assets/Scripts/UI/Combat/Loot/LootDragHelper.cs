using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class LootDragHelper
{
    // OnBeginDrag 시각: 아이콘/수량 숨기고 고스트 표시
    public static void BeginVisual(Image icon, TextMeshProUGUI quantity, Vector2 screenPos)
    {
        Sprite sprite = icon.sprite;
        icon.enabled = false;
        quantity.enabled = false;
        LootDragGhost.Instance.Show(sprite, screenPos);
    }

    public static void Move(Vector2 screenPos)
    {
        LootDragGhost.Instance.Move(screenPos);
    }

    // OnEndDrag 시각: 고스트 숨기고, 드래그가 살아있으면 복원 후 상태 초기화
    public static void EndVisual(Image icon, TextMeshProUGUI quantity)
    {
        LootDragGhost.Instance.Hide();
        if (LootDragState.IsDragging)
        {
            icon.enabled = true;
            quantity.enabled = true;
        }
        LootDragState.Clear();
    }
}
