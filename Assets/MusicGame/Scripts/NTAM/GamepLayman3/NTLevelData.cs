using UnityEngine;

[CreateAssetMenu(fileName = "NTLevelData", menuName = "MusicGame/NT Level Data")]
public class NTLevelData : ScriptableObject
{
    [Header("1. Thông Tin Màn Chơi")]
    [Tooltip("Tên hiển thị của màn chơi")]
    public string levelName;

    [Header("2. Âm Thanh (Audio)")]
    [Tooltip("File âm thanh bài hát (AudioClip) dùng cho màn này")]
    public AudioClip music;

    [Header("3. Cấu Hình Đọc Dữ Liệu MIDI")]
    [Tooltip("Tên file MIDI đặt trong thư mục StreamingAssets/MIDI/ (Ví dụ: song.mid)")]
    public string midiFileName;

    [Tooltip("Chọn thể loại nhạc cụ muốn đọc cho file dữ liệu này (Drums, Melody, Bass, AllNotes...)")]
    public NTMidiReader.ContentMode levelContentMode = NTMidiReader.ContentMode.Melody;

    [Tooltip("Chỉ định cụ thể track index trong file MIDI. Điền -1 = Tự động chọn track tối ưu dựa trên Content Mode. Điền số >= 0 (VD: 4) = Ép buộc tuyệt đối dùng track đó.")]
    public int forcedTrackIndex = -1;

    [Tooltip("Độ trễ thời gian (giây). Dương = Nốt xuất hiện muộn hơn, Âm = Nốt xuất hiện sớm hơn.")]
    public float timeOffset = 0f;

    [Header("4. Cấu Hình Tốc Độ & Độ Khó")]
    [Tooltip("Tốc độ di chuyển của nốt nhạc")]
    public float noteSpeed = 8f;

    [Tooltip("Mức độ khó từ 1 đến 4 (Ảnh hưởng trực tiếp đến khoảng cách tối thiểu giữa các nốt)")]
    [Range(1, 4)]
    public int difficulty = 2;

    [Tooltip("0 = Tự động tính theo độ khó. > 0 = Ép buộc khoảng cách tối thiểu cố định giữa 2 nốt (giây)")]
    public float minNoteDistanceOverride = 0f;

    /// <summary>
    /// Tính toán khoảng cách tối thiểu giữa các nốt dựa theo độ khó hoặc giá trị ghi đè.
    /// </summary>
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