using System;
using System.Collections.Generic;
using UnityEngine;

public enum Man3NoteType
{
    Normal,
    Hold,
    Double,
    Bomb,
    Buff,
    Coin // Chuỗi xu nhịp phụ trong khoảng lặng
}

[Serializable]
public struct NoteDataMan3
{
    [Tooltip("True = Làn Trái (-1.15) | False = Làn Phải (+1.15)")]
    public bool isLeftLane;
    public Man3NoteType noteType;
    [Tooltip("Thời điểm viên nốt chạm đúng tâm vòng tròn (giây)")]
    public float hitTime;
    [Tooltip("Thời gian giữ phím cho nốt Hold (giây), các nốt khác để 0")]
    public float holdDuration;
    [Tooltip("Thời gian nốt rơi từ SpawnPoint xuống HitPoint (giây)")]
    public float flightDuration;
}

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