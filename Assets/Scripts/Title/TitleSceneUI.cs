using UnityEngine;
using UnityEngine.UI;

public class TitleSceneUI : MonoBehaviour
{
    [SerializeField] private Button m_StartButton;

    private void Awake()
    {
        m_StartButton.onClick.AddListener(OnStartClicked);
    }
    private void OnStartClicked()
    {
        GameManager.Instance.ChangeState(GameState.Town);
    }
}
