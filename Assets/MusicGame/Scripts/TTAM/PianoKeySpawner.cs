using UnityEngine;
using System.Collections.Generic;

public class PianoKeySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MidiReader midiReader;
    [SerializeField] private GameObject pianoKeyPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform player;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float longNoteYOffset = -1.2f;

    private bool paused;
    private double pauseStartDsp;
    private bool pausedBeforeAudioStart;

    public bool IsPlaying => songStarted && !songEnded;
    public bool IsPaused => paused;

    // Đóng băng SongTime khi pause
    public float SongTime => paused
        ? (float)(pauseStartDsp - songStartDsp)
        : (float)(AudioSettings.dspTime - songStartDsp);

    [Header("Rhythm Settings")]
    [Tooltip("Tốc độ key (unit/giây) - cố định cho cả bài")]
    [SerializeField] private float keySpeed = 8f;

    [Tooltip("Key xuất hiện trước thời điểm note bao nhiêu giây. " +
             "Nên >= khoảng cách spawn / keySpeed")]
    [SerializeField] private float spawnAheadTime = 2f;

    [Tooltip("Bật = tự tính spawnAheadTime = khoảng cách (SpawnPoint -> Player) / keySpeed")]
    [SerializeField] private bool autoSpawnAhead = true;

    [Header("Overlap")]
    [Tooltip("Khoảng cách tối thiểu giữa 2 key (unit). Nên >= chiều rộng prefab key. " +
             "Note nào sát hơn mức này sẽ bị bỏ.")]
    [SerializeField] private float minKeyGap = 1.5f;

    private float lastAcceptedNoteTime = -999f;

    [Header("Lane")]
    [Tooltip("Bật = random Left/Right, tắt = theo lane của MIDI (cao/thấp)")]
    [SerializeField] private bool randomLane = false;

    // ---- State ----
    private double songStartDsp;
    private bool songStarted;
    private bool songEnded;
    private int nextNoteIndex;
    private float spawnDirection = 1f;

    private readonly List<GameObject> activeKeys = new List<GameObject>();

    public static PianoKeySpawner Instance { get; private set; }

    [Header("Long Note")]
    [SerializeField] private GameObject longNotePrefab;
    [SerializeField] private ScoreSystem scoreSystem;
    [Tooltip("Note có duration >= giá trị này (giây) sẽ thành nốt dài")]
    [SerializeField] private float longNoteMinDuration = 0.5f;

    private float lastAcceptedNoteEnd = -999f;   // thay cho lastAcceptedNoteTime

    private void Awake()
    {
        Instance = this;
    }

    // =============================================
    // START SONG - gọi sau khi MidiReader.LoadMidi() xong
    // =============================================

    public event System.Action SongEnded;

    // Gọi từ LevelManager sau khi nạp level
    public void SetLevel(LevelData level)
    {
        if (level == null)
            return;

        keySpeed = level.noteSpeed;
        RecalculateSpawnAhead();
    }

    private void RecalculateSpawnAhead()
    {
        if (!autoSpawnAhead || spawnPoint == null || player == null || keySpeed <= 0f)
            return;

        spawnAheadTime = Mathf.Abs(spawnPoint.position.x - player.position.x) / keySpeed;
    }

    // leadIn: số giây từ bây giờ tới lúc nhạc thực sự phát (vd. 3 = hết countdown)
    public void StartSong(float leadIn = -1f)
    {
        RecalculateSpawnAhead();

        if (leadIn < 0f)
            leadIn = spawnAheadTime;
        if (midiReader == null || audioSource == null ||
            player == null || spawnPoint == null || pianoKeyPrefab == null)
        {
            Debug.LogError("PianoKeySpawner: thiếu reference!");
            return;
        }

        if (midiReader.Notes.Count == 0)
            Debug.LogError("PianoKeySpawner: Notes rỗng! Chưa gọi MidiReader.LoadMidi() hoặc MIDI không có note.");

        if (audioSource.clip == null)
            Debug.LogError("PianoKeySpawner: AudioSource chưa có clip nhạc!");

        Debug.Log($"StartSong: {midiReader.Notes.Count} notes, leadIn = {leadIn}");

        ClearAllKeys();

        nextNoteIndex = 0;
        lastAcceptedNoteEnd = -999f;
        songEnded = false;

        // Hướng key bay: từ spawnPoint về phía player
        spawnDirection = spawnPoint.position.x >= player.position.x ? 1f : -1f;

        // Nhạc phát sau leadIn giây; trong lúc đó SongTime < 0 và key vẫn spawn
        songStartDsp = AudioSettings.dspTime + leadIn;
        audioSource.Stop();
        audioSource.PlayScheduled(songStartDsp);

        songStarted = true;
    }

    private void Update()
    {
        if (!songStarted || songEnded)
            return;

        activeKeys.RemoveAll(k => k == null);

        SpawnKeys();

        // Hết bài: đã spawn hết note, không còn key và nhạc đã dừng
        if (nextNoteIndex >= midiReader.Notes.Count &&
            activeKeys.Count == 0 &&
            audioSource.clip != null &&
            SongTime >= audioSource.clip.length)
        {
            EndSong();
        }
    }

    // =============================================
    // SPAWN THEO THỜI GIAN NOTE
    // =============================================

    private void SpawnKeys()
    {
        float now = SongTime;
        List<MidiReader.RhythmNote> notes = midiReader.Notes;

        while (nextNoteIndex < notes.Count)
        {
            MidiReader.RhythmNote note = notes[nextNoteIndex];

            if (note.time > now + spawnAheadTime)
                break;

            nextNoteIndex++;

            bool isLong = longNotePrefab != null && note.duration >= longNoteMinDuration;

            // Note phải cách điểm kết thúc của note trước (kể cả đuôi nốt dài) đủ xa
            float minGapTime = minKeyGap / keySpeed;
            if (note.time - lastAcceptedNoteEnd < minGapTime)
                continue;

            float travelTime = note.time - now;
            if (travelTime <= 0f)
                continue;

            lastAcceptedNoteEnd = note.time + (isLong ? note.duration : 0f);

            if (isLong) SpawnLongKey(note, travelTime);
            else        SpawnKey(note, travelTime);
        }
    }
    private void SpawnLongKey(MidiReader.RhythmNote note, float travelTime)
    {
        float spawnX =
            player.position.x + spawnDirection * keySpeed * travelTime;

        Vector3 pos = new Vector3(
            spawnX,
            spawnPoint.position.y + longNoteYOffset,
            spawnPoint.position.z
        );

        GameObject obj = Instantiate(
            longNotePrefab,
            pos,
            Quaternion.identity
        );

        LongNote ln = obj.GetComponent<LongNote>();

        if (ln == null)
        {
            Debug.LogError("LongNote Prefab chưa có LongNote.cs!");
            Destroy(obj);
            return;
        }

        // Random lane độc lập cho từng nốt dài
        Lanee lane;

        if (randomLane)
        {
            lane = Random.value < 0.5f
                ? Lanee.Left
                : Lanee.Right;
        }
        else
        {
            lane = note.lane == 0
                ? Lanee.Left
                : Lanee.Right;
        }

        ln.Init(
            lane,
            note.time,
            note.duration,
            keySpeed,
            player.position.x,
            spawnDirection,
            this
        );

        activeKeys.Add(obj);

        Debug.Log(
            $"Long Note: {lane} | Start: {note.time:F2}s | " +
            $"Duration: {note.duration:F2}s"
        );
    }

    private void SpawnKey(MidiReader.RhythmNote note, float travelTime)
    {
        // Vị trí sao cho key tới player đúng lúc note.time
        float spawnX = player.position.x + spawnDirection * keySpeed * travelTime;

        Vector3 pos = new Vector3(
            spawnX,
            spawnPoint.position.y,
            spawnPoint.position.z
        );

        GameObject obj = Instantiate(pianoKeyPrefab, pos, Quaternion.identity);
        PianoKey key = obj.GetComponent<PianoKey>();

        if (key == null)
        {
            Debug.LogError("PianoKey Prefab chưa có PianoKey.cs!");
            Destroy(obj);
            return;
        }

        PianoKey.KeyType type = randomLane
            ? (Random.value < 0.5f ? PianoKey.KeyType.Left : PianoKey.KeyType.Right)
            : (note.lane == 0 ? PianoKey.KeyType.Left : PianoKey.KeyType.Right);

        // targetTime để PianoKey tự dùng nếu cần (hiện tại theo audio time)
        key.Setup(type, note.time, player, keySpeed);

        activeKeys.Add(obj);
    }

    // =============================================
    // END / CLEAR
    // =============================================

    public void EndSong()
    {
        if (songEnded) return;
        songEnded = true;
        ClearAllKeys();
        Debug.Log("PianoKeySpawner: Song Ended!");
        SongEnded?.Invoke();
    }

    public void ClearAllKeys()
    {
        for (int i = activeKeys.Count - 1; i >= 0; i--)
        {
            if (activeKeys[i] != null)
                Destroy(activeKeys[i]);
        }
        activeKeys.Clear();
    }

    public void PauseSong()
    {
        if (!songStarted || songEnded || paused)
            return;

        paused = true;
        pauseStartDsp = AudioSettings.dspTime;
        pausedBeforeAudioStart = pauseStartDsp < songStartDsp;

        if (pausedBeforeAudioStart)
            audioSource.Stop();      // nhạc chưa phát: huỷ lịch phát
        else
            audioSource.Pause();
    }

    public void ResumeSong()
    {
        if (!paused)
            return;

        // Dời mốc bắt đầu bài đúng bằng thời gian đã dừng
        songStartDsp += AudioSettings.dspTime - pauseStartDsp;
        paused = false;

        if (pausedBeforeAudioStart)
            audioSource.PlayScheduled(songStartDsp);
        else
            audioSource.UnPause();
    }
}