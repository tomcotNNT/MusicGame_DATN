using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Điều hướng chung cho mọi màn chơi (Man1, Man2, Man3...).
/// Mọi nút Replay / Next / Home trong màn chơi đều nên đi qua đây
/// để luật trừ lượt và khóa màn giống nhau ở tất cả các màn.
///
/// Quy ước tên scene: màn chơi "Man{N}", cốt truyện "StoryMan{N}".
///   Retry : trừ 1 lượt rồi chơi lại ngay màn hiện tại (không xem lại cốt truyện).
///   Next  : màn N+1 phải đã mở khóa, trừ 1 lượt, vào StoryMan{N+1}.
///   Back  : về scene chọn màn.
/// Không đủ điều kiện (hết lượt, màn khóa, thiếu scene) thì về scene chọn màn.
/// </summary>
public static class LevelFlow
{
    public const string SelectSongScene = "SelectSong";
    private const string GameplayPrefix = "Man";
    private const string StoryPrefix = "StoryMan";

    private static readonly Regex GameplayName = new Regex(@"^Man(\d+)$");

    /// <summary>Level của scene màn chơi đang mở (Man2 -> 2). Không phải màn chơi thì dùng level đã chọn.</summary>
    public static int CurrentLevelId
    {
        get
        {
            Match m = GameplayName.Match(SceneManager.GetActiveScene().name);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int id))
                return id;

            return SelectedLevelData.LevelId;
        }
    }

    public static void Retry()
    {
        int levelId = CurrentLevelId;
        string scene = GameplayPrefix + levelId;

        if (!CanLoad(scene))
        {
            Debug.LogError("[LevelFlow] Không load được scene '" + scene + "'.");
            BackToSelect();
            return;
        }

        if (!TryUseTurn())
            return;

        LoadScene(scene);
    }

    /// <summary>Màn kế tiếp đã mở khóa và có trong Build Settings (dùng để ẩn/hiện nút Next).</summary>
    public static bool IsNextLevelAvailable()
    {
        int nextId = CurrentLevelId + 1;

        PlayerDataManager data = PlayerDataManager.Instance;
        if (data != null && !data.IsLevelUnlocked(nextId))
            return false;

        return GetEntryScene(nextId) != null;
    }

    // Vào màn N: ưu tiên scene cốt truyện, không có thì vào thẳng màn chơi
    private static string GetEntryScene(int levelId)
    {
        string story = StoryPrefix + levelId;
        if (CanLoad(story)) return story;

        string gameplay = GameplayPrefix + levelId;
        if (CanLoad(gameplay)) return gameplay;

        return null;
    }

    public static void NextLevel()
    {
        int nextId = CurrentLevelId + 1;

        PlayerDataManager data = PlayerDataManager.Instance;
        if (data != null && !data.IsLevelUnlocked(nextId))
        {
            Debug.Log("[LevelFlow] Level " + nextId + " chưa mở khóa, về màn chọn level.");
            BackToSelect();
            return;
        }

        string scene = GetEntryScene(nextId);

        if (scene == null)
        {
            Debug.Log("[LevelFlow] Không có level " + nextId + " trong Build Settings, về màn chọn level.");
            BackToSelect();
            return;
        }

        if (!TryUseTurn())
            return;

        SelectedLevelData.Set(nextId, scene);
        LoadScene(scene);
    }

    public static void BackToSelect()
    {
        LoadScene(SelectSongScene);
    }

    // Hết lượt thì đưa về màn chọn level (ở đó có UI báo hết lượt / hồi lượt)
    private static bool TryUseTurn()
    {
        // Đang chuyển scene (bấm đúp) -> không trừ thêm lượt
        if (SceneFader.IsLoading)
            return false;

        PlayerDataManager data = PlayerDataManager.Instance;

        // Mở thẳng scene màn chơi trong Editor thì không có PlayerDataManager -> cho chơi
        if (data == null)
            return true;

        if (data.UseTurn())
            return true;

        BackToSelect();
        return false;
    }

    private static bool CanLoad(string scene)
    {
        return !string.IsNullOrEmpty(scene) && Application.CanStreamedLevelBeLoaded(scene);
    }

    private static void LoadScene(string scene)
    {
        // Đang pause / Game Over thì GIỮ NGUYÊN trạng thái dừng trong lúc tối dần
        // (tránh nhạc + nốt chạy lại thoáng qua). SceneFader tự trả timeScale = 1 khi scene mới load.
        SceneFader.Load(scene);   // tối dần rồi chuyển scene
    }
}
