using UnityEditor;
using UnityEngine;

/// <summary>
/// Xem thử các panel dựng bằng code (bảng kết quả, menu Pause) mà không cần chơi hết bài.
/// Bấm Play trong một màn chơi (Man1..Man5) rồi chọn menu.
/// </summary>
public static class UiPreviewMenu
{
    private const string Root = "Tools/BeatRun/Xem thử UI/";

    [MenuItem(Root + "Bảng kết quả - 3 sao")] private static void Win3() => EndGameBase.PreviewResultPanel(3);
    [MenuItem(Root + "Bảng kết quả - 2 sao")] private static void Win2() => EndGameBase.PreviewResultPanel(2);
    [MenuItem(Root + "Bảng kết quả - 1 sao")] private static void Win1() => EndGameBase.PreviewResultPanel(1);
    [MenuItem(Root + "Bảng kết quả - thua (0 sao)")] private static void Lose() => EndGameBase.PreviewResultPanel(0);

    [MenuItem(Root + "Menu Pause")]
    private static void Pause()
    {
        var s = Object.FindFirstObjectByType<Scenes>();
        if (s == null) { Debug.LogWarning("[UI] Scene không có component Scenes."); return; }
        s.ShowOptionPanel();
    }

    // Các mục chỉ bấm được khi đang Play
    [MenuItem(Root + "Bảng kết quả - 3 sao", true)]
    [MenuItem(Root + "Bảng kết quả - 2 sao", true)]
    [MenuItem(Root + "Bảng kết quả - 1 sao", true)]
    [MenuItem(Root + "Bảng kết quả - thua (0 sao)", true)]
    [MenuItem(Root + "Menu Pause", true)]
    private static bool InPlay() => Application.isPlaying;
}
