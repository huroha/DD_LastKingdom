using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SettlementLineView : MonoBehaviour
{
    [SerializeField] private Image m_Icon;
    [SerializeField] private TextMeshProUGUI m_QuantityText;

    [Header("Effect")]
    [SerializeField] private Image m_SparkleImage;
    [SerializeField] private float m_SparklePeakScale = 1.2f;
    [SerializeField] private float m_SparkleDuration = 0.4f;


    public void Setup(Sprite icon, int quantity, bool playEffect = true)
    {

        if (m_Icon != null) m_Icon.sprite = icon;
        if (m_QuantityText != null)
        {
            if (quantity > 0)
                m_QuantityText.SetText(quantity.ToString());
            else
                m_QuantityText.gameObject.SetActive(false);
        }
        if (m_SparkleImage != null)
        {
            if (playEffect)
            {
                m_SparkleImage.gameObject.SetActive(true);
                StartCoroutine(SparkleRoutine());
            }
            else
                m_SparkleImage.gameObject.SetActive(false);
        }
    }
    private IEnumerator SparkleRoutine()
    {
        yield return StartCoroutine(
            CoroutineHelper.PopScale(m_SparkleImage.transform, 0.3f, m_SparklePeakScale, 0f, m_SparkleDuration));
        m_SparkleImage.gameObject.SetActive(false);
    }

}
