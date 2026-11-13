using UnityEngine;

// Lưu nhân vật đã chọn (qua các scene, và giữ lại cả khi tắt game)
// và scene gameplay cần vào sau khi chọn xong.
public static class CharacterSelection
{
    private const string Key = "SelectedCharacterId";

    // ID nhân vật đã chọn. Rỗng = chưa chọn (dùng nhân vật đầu tiên).
    public static string SelectedId
    {
        get { return PlayerPrefs.GetString(Key, ""); }
        set
        {
            PlayerPrefs.SetString(Key, value ?? "");
            PlayerPrefs.Save();
        }
    }

    // Scene gameplay sẽ vào sau khi chọn nhân vật (SelectLevel ghi vào khi bấm Play)
    public static string PendingScene { get; set; }
}