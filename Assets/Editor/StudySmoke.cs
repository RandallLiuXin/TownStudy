using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class StudySmoke
{
    static StudySmoke()
    {
        if (SessionState.GetBool("StudySmoke.Active", false))
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
        }
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/AssetGallery.unity");
        SessionState.SetBool("StudySmoke.Active", true);
        SessionState.SetInt("StudySmoke.Errors", 0);
        SessionState.SetFloat("StudySmoke.Deadline", (float)EditorApplication.timeSinceStartup + 45);
        EditorApplication.EnterPlaymode();
    }
    static void OnLog(string text, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            SessionState.SetInt("StudySmoke.Errors", SessionState.GetInt("StudySmoke.Errors", 0) + 1);
    }
    static void Tick()
    {
        if (EditorApplication.isPlaying && Time.timeSinceLevelLoad >= 5)
        {
            int errors = SessionState.GetInt("StudySmoke.Errors", 0);
            int missing = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            File.WriteAllText("Reports/play-smoke.txt", "Play duration >= 5 seconds\nErrors=" + errors + "\nMissing components=" + missing + "\nActive cameras=" + Camera.allCamerasCount);
            SessionState.SetBool("StudySmoke.Active", false);
            EditorApplication.Exit(errors == 0 && missing == 0 && Camera.allCamerasCount > 0 ? 0 : 1);
        }
        else if (EditorApplication.timeSinceStartup > SessionState.GetFloat("StudySmoke.Deadline", 0))
        {
            File.WriteAllText("Reports/play-smoke.txt", "Timeout entering or running Play mode");
            SessionState.SetBool("StudySmoke.Active", false);
            EditorApplication.Exit(2);
        }
    }
}
