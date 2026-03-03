using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;


public class SceneFlowManager : Singleton<SceneFlowManager>
{
    private const string SceneName_Title = "TitleScene";        // 오타를 IDE에서 바로 알 수 있도록 const 상수선언
    private const string SceneName_Town = "TownScene";
    private const string SceneName_Dungeon = "DungeonScene";
    private const string SceneName_Combat = "CombatScene";

    [SerializeField] private float m_FadeDuration = 0.5f;   // private 임에도 Unity Inspector에 노출해줌 수정가능 다른곳에서 접근불가
    [SerializeField] private float m_FastFadeDuration = 0.05f;   

    private CanvasGroup m_FadeCanvasGroup;
    private bool m_IsTransitioning;

    protected override void Awake()
    {
        base.Awake();
        CreateFadeCanvas();
        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;  // OnGameStateChanged 이벤트가 발생하면 내 OnGameStateChanged 메서드를 불러줘 라고 등록
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState previous, GameState current)
    {
        string sceneName = GetSceneName(current);
        if (sceneName == null)
            return;

        float duration = GetFadeDuration(previous, current);
        StartCoroutine(LoadSceneWithFade(sceneName,duration));
    }

    private string GetSceneName(GameState state)
    {
        switch (state)
        {
            case GameState.Title:       return SceneName_Title;
            case GameState.Town:        return SceneName_Town;
            case GameState.Dungeon:     return SceneName_Dungeon;
            case GameState.Combat:      return SceneName_Combat;
            default:                    return null;        
        }
    }

    private float GetFadeDuration(GameState previous, GameState current)
    {
        if (previous == GameState.Dungeon && current == GameState.Combat)
            return m_FastFadeDuration;
        if (previous == GameState.Combat && current == GameState.Dungeon)
            return m_FastFadeDuration;

        return m_FadeDuration;
    }
    private IEnumerator LoadSceneWithFade(string sceneName, float fadeDuration)     // 일반 메서드는 호출되면 끝날때까지 한 번에 실행
    {                                                                               // 코루틴은 yield return을 만나면 그 자리에서 일시정지하고 다음 프레임에 이어서 실행함.
        if (m_IsTransitioning)
            yield break;            // 이미 전환중이면 코루틴 바로 종료
        m_IsTransitioning = true;

        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));    // Fade가 완전히 끝날 때까지 대기

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);  // 씬 로딩 작업을 담당하는 객체
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)           
            yield return null;

        operation.allowSceneActivation = true;      
        yield return operation;


        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        m_IsTransitioning = false;
    }
    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        m_FadeCanvasGroup.alpha = from;
        m_FadeCanvasGroup.blocksRaycasts = true;        // 페이드 중 화면 버튼 등이 클릭되지 않게 막기

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            m_FadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;                          // 러프로 부드럽게 변환
        }
        m_FadeCanvasGroup.alpha = to;
        m_FadeCanvasGroup.blocksRaycasts = (to > 0f);   // 차단된거 다시 해제
    }
    private void CreateFadeCanvas()
    {
        GameObject canvasObject = new GameObject("FadeCanvas"); // 빈 오브젝트를 자식으로 붙인다.
        canvasObject.transform.SetParent(transform);

        Canvas canvas = canvasObject.AddComponent<Canvas>();        // Canvas 컴포넌트 추가
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;      // 모든 오브젝트 위에 UI를 그림
        canvas.sortingOrder = 999;                              // 다른 Canvas보다 항상 맨 위에 그려짐

        m_FadeCanvasGroup = canvasObject.AddComponent<CanvasGroup>();   // Canvas 전체의투명도와 입력을 한번에 제어하는 컴포넌트
        m_FadeCanvasGroup.alpha = 0f;
        m_FadeCanvasGroup.blocksRaycasts = false;                       // 평상시 클릭 통과
        m_FadeCanvasGroup.interactable = false;                         // Canvas 내 버튼 등의 상호작용 비활성화 ->Fade 전용이기 때문

        GameObject imageObject = new GameObject("FadeImage");               // 이미지 오브젝트 생성
        imageObject.transform.SetParent(canvasObject.transform, false);     // 부모의 위치/크기에 맞게 로컬 Transform 유지 설정

        Image image = imageObject.AddComponent<Image>();                    // 색 지정
        image.color = Color.black;

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;                                       // 부모 전체를 채움 (0,0) (1,1)
        rect.sizeDelta = Vector2.zero;                                      // 앵커 기준 추가크기 없음 화면 크기와 일치
        rect.anchoredPosition = Vector2.zero;                               // 중심점 오프셋 x
    }

}
