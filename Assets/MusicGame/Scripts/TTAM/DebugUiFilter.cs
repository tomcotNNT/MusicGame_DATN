using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ẩn các nút chỉ dùng để test (ví dụ nút Skip bài) trong bản build phát hành.
/// Trong Editor và Development Build các nút vẫn hiện như cũ.
/// Nút được nhận diện theo hàm gắn trong OnClick, nên không cần sửa scene.
/// </summary>
public static class DebugUiFilter
{
    private static readonly string[] DebugMethods = { "SkipSong" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (Debug.isDebugBuild)
            return;

        SceneManager.sceneLoaded += (scene, mode) => HideDebugButtons();
        HideDebugButtons();
    }

    private static void HideDebugButtons()
    {
        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            int count = b.onClick.GetPersistentEventCount();

            for (int i = 0; i < count; i++)
            {
                if (System.Array.IndexOf(DebugMethods, b.onClick.GetPersistentMethodName(i)) >= 0)
                {
                    b.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }
}
