using System.Text;
using UnityEngine;

public static class TooltipHelper
{
    private static readonly Vector3[] s_Corners = new Vector3[4];


    public const string TAG_DEBUFF_OPEN = "<color=#BF1313>";
    public const string TAG_NORMAL_OPEN = "<color=#DBDBD0>";
    public const string TAG_BLEED = "<color=#BF1313>";
    public const string TAG_STUN = "<color=#CCA452>";
    public const string TAG_POISON = "<color=#BCC042>";
    public const string TAG_DEBUFF = "<color=#BF6210>";
    public const string TAG_HEAL = "<color=#87C042>";
    public const string TAG_SKILLTYPE = "<color=#807056>";
    public const string TAG_COLOR_CLOSE = "</color>";


    public const string STAT_DAMAGE = "피해";
    public const string STAT_ACCURACY = "명중";
    public const string STAT_CRIT = "치명타율";
    public const string STAT_DEFENCE = "방어율";
    public const string STAT_DODGE = "회피";
    public const string STAT_SPEED = "속도";

    public const string STAT_RES_STUN = "기절 저항";
    public const string STAT_RES_MOVE = "이동 저항";
    public const string STAT_RES_POISON = "중독 저항";
    public const string STAT_RES_DISEASE = "질병 저항";
    public const string STAT_RES_BLEED = "출혈 저항";
    public const string STAT_RES_DEBUFF = "디버프 저항";


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
    public static string GetEffectColorTag(StatusEffectType effectType)
    {
        if (effectType == StatusEffectType.Stun)
            return TAG_STUN;
        else if (effectType == StatusEffectType.Poison)
            return TAG_POISON;
        else if (effectType == StatusEffectType.Debuff)
            return TAG_DEBUFF;
        else if (effectType == StatusEffectType.Bleed)
            return TAG_BLEED;
        else
            return TAG_NORMAL_OPEN;
    }

    public static void AppendStatBlock(StringBuilder sb, StatBlock mod, int turns = 0)
    {
        if (mod.damageMultiplier != 0)
            AppendStatPercent(sb, STAT_DAMAGE, (int)mod.damageMultiplier, turns);
        if (mod.accuracyMod != 0)
            AppendStat(sb, STAT_ACCURACY, mod.accuracyMod, turns);
        if (mod.defense != 0)
            AppendStatPercent(sb, STAT_DEFENCE, (int)mod.defense, turns);
        if (mod.dodge != 0)
            AppendStat(sb, STAT_DODGE, mod.dodge, turns);
        if (mod.speed != 0)
            AppendStat(sb, STAT_SPEED, mod.speed, turns);
        if (mod.critChance != 0f)
            AppendStat(sb, STAT_CRIT, (int)mod.critChance, turns);

        if (mod.resistance.stun != 0)
            AppendStatPercent(sb, STAT_RES_STUN, (int)mod.resistance.stun, turns);
        if (mod.resistance.move != 0)
            AppendStatPercent(sb, STAT_RES_MOVE, (int)mod.resistance.move, turns);
        if (mod.resistance.poison != 0)
            AppendStatPercent(sb, STAT_RES_POISON, (int)mod.resistance.poison, turns);
        if (mod.resistance.disease != 0)
            AppendStatPercent(sb, STAT_RES_DISEASE, (int)mod.resistance.disease, turns);
        if (mod.resistance.bleed != 0)
            AppendStatPercent(sb, STAT_RES_BLEED, (int)mod.resistance.bleed, turns);
        if (mod.resistance.debuff != 0)
            AppendStatPercent(sb, STAT_RES_DEBUFF, (int)mod.resistance.debuff, turns);
    }
}
