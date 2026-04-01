using UnityEngine;
using System.Collections.Generic;

public class DamagePopupPool : MonoBehaviour
{
    [SerializeField] private DamagePopup m_Prefab;
    [SerializeField] private int m_InitialSize = 8;

    private List<DamagePopup> m_Pool;

    private void Awake()
    {
        m_Pool = new List<DamagePopup>();
        for(int i=0; i< m_InitialSize; ++i)
        {
            DamagePopup popup = Instantiate(m_Prefab, this.transform);
            popup.gameObject.SetActive(false);
            m_Pool.Add(popup);
        }
    }
    public DamagePopup Spawn(Vector3 position, string text, Color color, float scale = 1f)
    {

        DamagePopup popup = null;
        for(int i=0; i<m_Pool.Count; ++i)
        {
            if(!m_Pool[i].gameObject.activeSelf)
            {
                popup = m_Pool[i];
                break;
            }
        }

        if(popup == null)
        {
            popup = Instantiate(m_Prefab);
            m_Pool.Add(popup);
        }
        popup.Show(position, text, color, scale);
        return popup;
    }

}
