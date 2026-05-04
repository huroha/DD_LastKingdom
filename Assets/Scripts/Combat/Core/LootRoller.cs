using UnityEngine;
using System.Collections.Generic;

public static class LootRoller
{
    private const int RelicTypeCount  = 4;

    public static CombatResult Roll(List<EnemyData> defeated)
    {
        if (defeated == null || defeated.Count == 0)
            return new CombatResult(0, 0, 0, 0, new int[RelicTypeCount], System.Array.Empty<LootItem>());

        int totalCredit = 0;
        int totalBattleData = 0;
        int totalCore = 0;
        int totalGems = 0;
        int[] relicAmounts = new int[RelicTypeCount];

        for (int i=0; i<defeated.Count; ++i)
        {
            DropTable t = defeated[i].DropTable;
            totalCredit += Random.Range(t.MinCredit, t.MaxCredit + 1);
            totalBattleData += Random.Range(t.MinBattleData, t.MaxBattleData + 1);
            totalCore += Random.Range(t.MinCore, t.MaxCore + 1);
            totalGems += Random.Range(t.MinGems, t.MaxGems + 1);

            if (t.Relics == null) continue;
            for (int j=0; j < t.Relics.Length; ++j)
            {
                RelicDrop rd = t.Relics[j];
                relicAmounts[(int)rd.Type] += Random.Range(rd.Min, rd.Max + 1);
            }
        }

        return new CombatResult(totalCredit, totalBattleData, totalCore, totalGems, relicAmounts, System.Array.Empty<LootItem>());

    }
}
