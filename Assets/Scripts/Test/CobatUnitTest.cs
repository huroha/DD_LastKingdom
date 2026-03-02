using UnityEngine;
using System.Collections.Generic;

public class CombatUnitTest : MonoBehaviour
{
    [SerializeField] private NikkeData m_TestNikke;
    [SerializeField] private EnemyData m_TestEnemy;

    private void Start()
    {
        TestNikke();
        TestEnemy();
    }

    private void TestNikke()
    {
        CombatUnit nikke = new CombatUnit(m_TestNikke, 0, m_TestNikke.BaseStats.maxHp, 0, null);
        Debug.Log($"[Nikke] {nikke.UnitName} | HP:{nikke.CurrentHp}/{nikke.MaxHp} | State:{nikke.State}");

        // DeathsDoor 진입 확인
        nikke.TakeDamage(nikke.MaxHp);
        Debug.Log($"[Nikke] 데미지 후 | HP:{nikke.CurrentHp} | State:{nikke.State}");

        // DeathsDoor에서 추가 피해
        UnitState result = nikke.TakeDamage(10);
        Debug.Log($"[Nikke] DeathBlow 결과 | State:{nikke.State}");

        // Heal 테스트 (State가 Dead가 아닐 경우를 위해 별도 유닛)
        CombatUnit nikke2 = new CombatUnit(m_TestNikke, 1, 1, 50, null);
        nikke2.Heal(999);
        Debug.Log($"[Nikke2] 힐 후 | HP:{nikke2.CurrentHp}/{nikke2.MaxHp} | Ebla:{nikke2.Ebla}");

        // AddEbla 클램프 확인
        nikke2.AddEbla(300);
        Debug.Log($"[Nikke2] AddEbla(300) 후 | Ebla:{nikke2.Ebla} (최대 200이어야 함)");
    }

    private void TestEnemy()
    {
        // 일반 데미지 → Corpse
        CombatUnit enemy = new CombatUnit(m_TestEnemy, 0);
        enemy.TakeDamage(enemy.MaxHp);
        Debug.Log($"[Enemy] 일반 사망 | State:{enemy.State} | HP:{enemy.CurrentHp} (CorpseHp여야 함)");

        // Corpse → Dead
        enemy.TakeDamage(enemy.CurrentHp);
        Debug.Log($"[Enemy] 시체 제거 | State:{enemy.State}");

        // DOT 데미지 → 즉시 Dead
        CombatUnit enemy2 = new CombatUnit(m_TestEnemy, 1);
        enemy2.TakeDamage(enemy2.MaxHp, true);
        Debug.Log($"[Enemy2] DOT 사망 | State:{enemy2.State} (Corpse 없이 Dead여야 함)");
    }
}