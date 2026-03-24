using UnityEngine;






[System.Serializable]
public struct ResistanceBlock
{
    [Range(0f, 500f)] public float stun;        // 기절 저항
    [Range(0f, 500f)] public float move;        // 이동 저항
    [Range(0f, 500f)] public float poison;      // 중독 저항
    [Range(0f, 500f)] public float disease;     // 질병 저항
    [Range(0f, 500f)] public float bleed;       // 출혈 저항
    [Range(0f, 500f)] public float debuff;      // 디버프 저항
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

    [Header("배율 보정")]
    public float damageMultiplier;              // 피해 배율 보정
    public float eblaMultiplier;                

    public StatBlock Apply(StatBlock modifier)
    {
        StatBlock result;
        result.maxHp        = maxHp + modifier.maxHp;
        result.minDamage    = minDamage + modifier.minDamage;
        result.maxDamage    = maxDamage + modifier.maxDamage;
        result.accuracyMod  = accuracyMod + modifier.accuracyMod;
        result.critChance   = critChance + modifier.critChance;
        result.defense      = defense + modifier.defense;
        result.dodge        = dodge + modifier.dodge;
        result.deathBlowResist = deathBlowResist + modifier.deathBlowResist;
        result.speed        = speed + modifier.speed;
        result.moveRange    = moveRange + modifier.moveRange;

        result.resistance.stun      = resistance.stun + modifier.resistance.stun;
        result.resistance.move      = resistance.move + modifier.resistance.move;
        result.resistance.poison    = resistance.poison + modifier.resistance.poison;
        result.resistance.disease   = resistance.disease + modifier.resistance.disease;
        result.resistance.bleed     = resistance.bleed + modifier.resistance.bleed;
        result.resistance.debuff    = resistance.debuff + modifier.resistance.debuff;
        result.resistance.trap      = resistance.trap + modifier.resistance.trap;
        result.damageMultiplier     = damageMultiplier + modifier.damageMultiplier;
        result.eblaMultiplier       = eblaMultiplier + modifier.eblaMultiplier;

        return result;
    }

}

