using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayFromBoot
{
    private const string PrevSceneKey = "PlayFromBoot_PrevScene";

    static PlayFromBoot()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EditorPrefs.SetString(PrevSceneKey, SceneManager.GetActiveScene().path);
            EditorSceneManager.OpenScene("Assets/Scenes/BootScene.unity");
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            string prev = EditorPrefs.GetString(PrevSceneKey, "");
            if (prev != "")
                EditorSceneManager.OpenScene(prev);
        }
    }
}
