using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Chuyển scene qua màn Loading.
/// Dùng: SceneLoader.Load("Level1");  thay cho SceneManager.LoadScene("Level1");
/// </summary>
public static class SceneLoader
{
    /// <summary>Tên scene Loading (phải có trong Build Settings).</summary>
    public const string LoadingSceneName = "Loading";

    /// <summary>Scene đích mà màn Loading sẽ nạp.</summary>
    public static string TargetScene { get; private set; }

    public static void Load(string targetScene)
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("SceneLoader: targetScene rỗng!");
            return;
        }

        TargetScene = targetScene;
        SceneManager.LoadScene(LoadingSceneName);
    }
}