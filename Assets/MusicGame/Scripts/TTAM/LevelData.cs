using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Rhythm/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level")]
    public string levelName;

    [Header("Music")]
    public AudioClip music;

    [Header("MIDI")]
    public string midiFileName;

    [Tooltip("-1 = tự chọn track giai điệu. Số >= 0 = ép dùng track đó (xem Console)")]
    public int forcedTrackIndex = -1;

    [Tooltip("Dịch toàn bộ nốt (giây). Dương = key đến muộn hơn, âm = sớm hơn")]
    public float timeOffset = 0f;

    [Header("Gameplay")]
    [Tooltip("Tốc độ key (unit/giây)")]
    public float noteSpeed = 8f;

    [Tooltip("1 = Dễ ... 4 = Khó. Quyết định độ dày của key")]
    [Range(1, 4)]
    public int difficulty = 2;

    [Tooltip("0 = tự theo độ khó. >0 = ép khoảng cách tối thiểu giữa 2 nốt (giây)")]
    public float minNoteDistanceOverride = 0f;

    // Khoảng cách tối thiểu giữa 2 nốt theo độ khó
    public float GetMinNoteDistance()
    {
        if (minNoteDistanceOverride > 0f)
            return minNoteDistanceOverride;

        switch (difficulty)
        {
            case 1:  return 0.30f;
            case 2:  return 0.22f;
            case 3:  return 0.15f;
            default: return 0.10f;
        }
    }
}