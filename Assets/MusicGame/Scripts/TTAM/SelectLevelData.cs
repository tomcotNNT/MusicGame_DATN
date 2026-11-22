using UnityEngine;

/// <summary>
/// Lưu thông tin màn chơi đã chọn để truyền từ Level Select -> Select Char -> Level.
/// Biến static tồn tại xuyên suốt khi đổi scene.
/// Có lưu thêm PlayerPrefs để không bị mất khi test thẳng scene Select Char trong Editor.
/// </summary>
public static class SelectedLevelData
{
    private const string KEY_LEVEL_ID = "SelectedLevelId";
    private const string KEY_SCENE_NAME = "SelectedLevelScene";
    private const string KEY_CHAR_INDEX = "SelectedCharIndex";
    private const string KEY_CHAR_NAME = "SelectedCharName";

    private static int levelId = -1;
    private static string sceneName = null;
    private static int charIndex = -1;

    /// <summary>Index nhân vật đã chọn ở scene Select Char (mặc định 0).</summary>
    public static int CharacterIndex
    {
        get
        {
            if (charIndex < 0)
                charIndex = PlayerPrefs.GetInt(KEY_CHAR_INDEX, 0);
            return charIndex;
        }
    }

    private static string charName = null;

    /// <summary>Tên card nhân vật đã chọn ở scene Select Char.</summary>
    public static string CharacterName
    {
        get
        {
            if (charName == null)
                charName = PlayerPrefs.GetString(KEY_CHAR_NAME, "");
            return charName;
        }
    }

    public static void SetCharacter(int index, string name = "")
    {
        charIndex = index;
        charName = name ?? "";
        PlayerPrefs.SetInt(KEY_CHAR_INDEX, index);
        PlayerPrefs.SetString(KEY_CHAR_NAME, charName);
        PlayerPrefs.Save();
    }

    public static int LevelId
    {
        get
        {
            if (levelId < 0)
                levelId = PlayerPrefs.GetInt(KEY_LEVEL_ID, 1);
            return levelId;
        }
    }

    public static string SceneName
    {
        get
        {
            if (string.IsNullOrEmpty(sceneName))
                sceneName = PlayerPrefs.GetString(KEY_SCENE_NAME, "Level_1");
            return sceneName;
        }
    }

    public static void Set(int id, string scene)
    {
        levelId = id;
        sceneName = scene;

        PlayerPrefs.SetInt(KEY_LEVEL_ID, id);
        PlayerPrefs.SetString(KEY_SCENE_NAME, scene);
        PlayerPrefs.Save();
    }
}