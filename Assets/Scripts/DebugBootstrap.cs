using UnityEngine;

public class DebugBootstrap : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== 1. 초기화 확인 ===");
        Debug.Log("[DebugBootstrap] CurrentState : " + GameManager.Instance.CurrentState);

        Debug.Log("=== 2. 상태 전환 확인 ===");
        GameManager.Instance.ChangeState(GameState.Town);

        Debug.Log("=== 3. 동일 상태 경고 확인 ===");
        GameManager.Instance.ChangeState(GameState.Town);

        Debug.Log("=== 4. 빠른 페이드 전환 확인 ===");
        GameManager.Instance.ChangeState(GameState.Combat);

    }
}
