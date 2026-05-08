using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EncounterPreview : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_QuestTypeText;
    [SerializeField] private TextMeshProUGUI m_DifficultyText;
    [SerializeField] private TextMeshProUGUI m_LengthText;
    [SerializeField] private TextMeshProUGUI m_DescriptionText;

    public void Show(EncounterData encounter)
    {
        if (encounter == null) return;
        Clear();
        m_DifficultyText.SetText(LabelText.GetDifficultyLabel(encounter.Difficulty));
        m_LengthText.SetText(LabelText.GetLengthLabel(encounter.Length));
        m_QuestTypeText.SetText(LabelText.GetQuestTypeLabel(encounter.QuestType));
        m_DescriptionText.text = encounter.Description;
    }
    public void Clear()
    {
        m_DifficultyText.text = string.Empty;
        m_LengthText.text = string.Empty;
        m_DescriptionText.text = string.Empty;
        m_QuestTypeText.text = string.Empty;
    }
}
