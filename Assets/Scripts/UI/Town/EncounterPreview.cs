using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EncounterPreview : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private Transform m_EnemyContainer;
    [SerializeField] private Image m_EnemyIconPrefab; //  적 아이콘용 단순 image prefab

    public void Show(EncounterData encounter)
    {
        Clear();
        m_NameText.text = encounter.EncounterName;

        IReadOnlyList<EnemyData> enemies = encounter.Enemies;
        for (int i = 0; i < enemies.Count; ++i)
        {
            if (enemies[i] == null) continue;
            Image icon = Instantiate(m_EnemyIconPrefab, m_EnemyContainer);
            icon.sprite = enemies[i].Sprite;
        }
    }
    public void Clear()
    {
        m_NameText.text = string.Empty;
        for (int i= m_EnemyContainer.childCount -1; i >= 0; --i)
            Destroy(m_EnemyContainer.GetChild(i).gameObject);
    }
}
