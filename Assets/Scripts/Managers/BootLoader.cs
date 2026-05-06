using UnityEngine;

public class BootLoader : MonoBehaviour
{
    private void Start()
    {
        GameManager.Instance.ChangeState(GameState.Title);
    }
}
