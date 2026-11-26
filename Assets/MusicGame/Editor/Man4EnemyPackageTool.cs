using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Đóng gói bộ kẻ địch Màn 4 (Mother Maw + quái con MedusaHead + scene test + script) thành 1 file .unitypackage
/// để gửi cho thành viên khác import vào project của họ.
/// </summary>
public static class Man4EnemyPackageTool
{
    private static readonly string[] Paths =
    {
        "Assets/Scenes/LONG/ManChoi/Tile_Man4_Man5/MotherMaw",          // sprite, clip, controller, prefab
        "Assets/Scenes/LONG/ManChoi/Tile_Man4_Man5/MedusaHead.png",     // sheet quái con (đã cắt 19 frame)
        "Assets/MusicGame/Scripts/TTAM/CaveMawSpawner.cs",
        "Assets/MusicGame/Scripts/TTAM/CountdownCave.cs",
        // script nền mà spawner cần, chưa có trên nhánh khác (đã biên dịch thử trên Feature/Long)
        "Assets/MusicGame/Scripts/TTAM/RhythmCountdown.cs",
        "Assets/MusicGame/Scripts/TTAM/SongPause.cs",
        "Assets/MusicGame/Scripts/TTAM/AnimatorSafe.cs",
        "Assets/MusicGame/Scripts/TTAM/RuntimeBossHpBar.cs",
        "Assets/MusicGame/Scripts/TTAM/GameplaySfx.cs",
        "Assets/MusicGame/Scripts/TTAM/AudioPrefs.cs",
        "Assets/MusicGame/Scripts/TTAM/RhythmEvents.cs",
        "Assets/MusicGame/Scripts/TTAM/MissExplosionFeedback.cs",
        "Assets/MusicGame/Data/Level4.asset",
        "Assets/MusicGame/Editor/SpriteAnimPreviewWindow.cs",
        "Assets/Scenes/TTAM/Man4_Test.unity",
        // KHÔNG gói EndGameCave.cs: nó cần EndGameBase / LevelFlow / PlayerDataManager mới của nhánh Feature/Ntam,
        // nhánh khác import vào sẽ lỗi biên dịch. Scene test vẫn chạy, chỉ thiếu bảng kết quả.
    };

    /// <summary>Danh sách đường dẫn (dùng cho cả cách lấy bằng git checkout).</summary>
    public static string[] PackagePaths => (string[])Paths.Clone();

    [MenuItem("Tools/BeatRun/Đóng gói kẻ địch Màn 4 (.unitypackage)")]
    private static void Export()
    {
        var missing = Paths.Where(p => !File.Exists(p) && !Directory.Exists(p)).ToArray();
        if (missing.Length > 0)
        {
            EditorUtility.DisplayDialog("Đóng gói Màn 4", "Thiếu:\n" + string.Join("\n", missing), "OK");
            return;
        }

        string file = EditorUtility.SaveFilePanel("Lưu gói kẻ địch Màn 4", "", "Man4_Enemy_MotherMaw.unitypackage", "unitypackage");
        if (string.IsNullOrEmpty(file)) return;

        // Không kéo theo dependency: scene test dùng lại hệ thống chung (ComboSystem, MidiReader...) đã có sẵn trong repo nhóm
        AssetDatabase.ExportPackage(Paths, file, ExportPackageOptions.Recurse);
        EditorUtility.RevealInFinder(file);
        Debug.Log("[Man4] Đã đóng gói: " + file);
    }
}
