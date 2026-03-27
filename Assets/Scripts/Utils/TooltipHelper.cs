using System.Text;
using UnityEngine;

public static class TooltipHelper
{
    private static readonly Vector3[] s_Corners = new Vector3[4];


    public const string TAG_DEBUFF_OPEN = "<color=#BF1313>";
    public const string TAG_NORMAL_OPEN = "<color=#DBDBD0>";
    public const string TAG_BUFF_OPEN = "<color=#FF4444>";
    public const string TAG_COLOR_CLOSE = "</color>";


    public const string STAT_DAMAGE = "피해";
    public const string STAT_ACCURACY = "명중";
    public const string STAT_CRIT = "치명타율";
    public const string STAT_DEFENCE = "방어율";
    public const string STAT_DODGE = "회피";
    public const string STAT_SPEED = "속도";


    public static void ClampToScreen(RectTransform rectTransform)
    {
        rectTransform.GetWorldCorners(s_Corners);

        float offsetX = 0f;
        float offsetY = 0f;

        // 오른쪽 빠져나감
        if (s_Corners[2].x > Screen.width)
            offsetX = Screen.width - s_Corners[2].x;
        if (s_Corners[0].x < 0f)
            offsetX = -s_Corners[0].x;
        if (s_Corners[1].y > Screen.height)
            offsetY = Screen.height - s_Corners[1].y;
        if (s_Corners[0].y < 0f)
            offsetY = -s_Corners[0].y;

        rectTransform.anchoredPosition += new Vector2(offsetX, offsetY);
    }
    public static void AppendStat(StringBuilder sb, string statName, int value, int turns = 0)
    {
        sb.Append('\n');
        sb.Append(statName);
        sb.Append(' ');
        if (value > 0)
            sb.Append('+');
        sb.Append(value);
        if (turns > 0)
        {
            sb.Append('(');
            sb.Append(turns);
            sb.Append("차례)");
        }
    }
    public static void AppendStatPercent(StringBuilder sb, string statName, int value, int turns =0)
    {
        sb.Append('\n');
        sb.Append(statName);
        sb.Append(' ');
        if (value > 0)
            sb.Append('+');
        sb.Append(value);
        sb.Append('%');
        if (turns > 0)
        {
            sb.Append("(");
            sb.Append(turns);
            sb.Append("차례)");
        }
    }
    

}
