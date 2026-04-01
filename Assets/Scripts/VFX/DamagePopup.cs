using UnityEngine;
using TMPro;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro m_Text;
    [SerializeField] private float m_FloatSpeed = 1f;
    [SerializeField] private float m_Duration = 0.8f;
    [SerializeField] private Vector3 m_Offset;

    private Coroutine m_FloatRoutine;

    public void Show(Vector3 worldPosition, string text, Color color, float scale = 1f)
    {
        m_Text.text = text;
        m_Text.color = color;
        transform.position = worldPosition + m_Offset;
        transform.localScale = Vector3.one * scale;
        gameObject.SetActive(true);

        if(m_FloatRoutine != null )
            StopCoroutine(m_FloatRoutine);
        
        m_FloatRoutine = StartCoroutine(FloatUp());
    }   
    
    private IEnumerator FloatUp()
    {
        float elapsed = 0f;
        while(elapsed < m_Duration)
        {
            transform.position += Vector3.up * m_FloatSpeed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }
    private void OnDisable()
    {
        if(m_FloatRoutine != null )
            StopCoroutine(m_FloatRoutine);
    }

}
