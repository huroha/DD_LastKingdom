using UnityEngine;

[System.Serializable]
public struct ResistanceBlock
{
    [Range(0f, 100f)] public float stun;        // 기절 저항
    [Range(0f, 100f)] public float move;        // 이동 저항
    [Range(0f, 100f)] public float poison;      // 중독 저항
    [Range(0f, 100f)] public float disease;     // 질병 저항
    [Range(0f, 100f)] public float bleed;       // 출혈 저항
    [Range(0f, 100f)] public float debuff;      // 디버프 저항
    [Range(0f, 100f)] public float trap;        // 함정 해제율
}

[System.Serializable]
public struct StatBlock
{
    [Header("체력")]
    public int maxHp;            

    [Header("공격")]
    public int minDamage;                       // 최소 피해
    public int maxDamage;                       // 최대 피해
    public int accuracyMod;                     // 명중 보정 (스킬 명중률 가산)
    [Range(0f, 100f)] public float critChance;   // 치명타 확률

    [Header("방어")]
    [Range(0f, 100f)] public float defense;     // 방어율 - 들어오는 피해를 % 감소
    public int dodge;                           // 회피 (명중 판정에서 차감)
    [Range(0f, 100f)] public float deathBlowResist; // 죽음의 일격 저항 - 빈사에서 생존확률
    
    [Header("속도")]
    public int speed;

    [Header("이동")]
    public int moveRange;                       // 이동 가능 칸 수 기본 1

    [Header("저항력")]
    public ResistanceBlock resistance;

}
