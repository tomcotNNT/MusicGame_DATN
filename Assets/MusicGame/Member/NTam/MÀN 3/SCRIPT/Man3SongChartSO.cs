using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMan3SongChart", menuName = "BeatRun/Man3 Song Chart")]
public class Man3SongChartSO : ScriptableObject
{
    [Header("1. Thông Tin Bài Hát Thực Tế")]
    public string songTitle;
    public AudioClip musicClip;
    public float bpm = 128f;
    [Tooltip("Chỉnh lệch nhịp (+/- giây) để khớp tiếng trống với vòng tròn")]
    public float audioOffset = 0f;

    [Header("2. Các Mốc Cao Trào (Báo hiệu SPEED UP trước 1.5s)")]
    public List<float> speedUpTimes = new List<float>();

    [Header("3. Danh Sách Nốt Quy Đổi Từ MIDI")]
    public List<NoteDataMan3> notes = new List<NoteDataMan3>();
}