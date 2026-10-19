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
    public float KeySpeed => keySpeed;

    [Header("Coin")]
    [SerializeField] private CoinSpawner coinSpawner;


    [Header("Boss")]
    [SerializeField] private BossSpawner bossSpawner;

    public float SpawnDirection => spawnDirection;
    public float PlayerX => player.position.x;

    private bool paused;
    private double pauseStartDsp;
    private bool pausedBeforeAudioStart;

    public bool IsPlaying => songStarted && !songEnded;
    public bool IsPaused => paused;

    // Đóng băng SongTime khi pause
    public float SongTime => isTutorial
        ? tutorialClock
        : (paused
            ? (float)(pauseStartDsp - songStartDsp)
            : (float)(AudioSettings.dspTime - songStartDsp));

    [Header("Rhythm Settings")]
    [Tooltip("Tốc độ key (unit/giây) của vòng hiện tại (= tốc độ gốc x roundSpeed)")]
    [SerializeField] private float keySpeed = 8f;

    [Header("Loop tăng tốc")]
    [Tooltip("Hết bài thì phát lại với tốc độ cao hơn")]
    [SerializeField] private bool loopWithSpeedUp = true;
    [Tooltip("Mỗi vòng nhanh thêm bao nhiêu (0.15 = +15%)")]
    [SerializeField] private float speedStep = 0.15f;
    [Tooltip("Tốc độ tối đa. Đạt mức này thì vòng kế tiếp là vòng cuối, hết là EndSong")]
    [SerializeField] private float maxSpeed = 2.5f;
    [Tooltip("Số giây chờ giữa 2 vòng trước khi nhạc phát lại")]
    [SerializeField] private float nextRoundLeadIn = 1.5f;

    public int Round { get; private set; } = 1;
    public float RoundSpeed => roundSpeed;

    // (số vòng, hệ số tốc độ) - dùng cho UI "Vòng 2 - x1.15"
    public event System.Action<int, float> RoundStarted;

    private float roundSpeed = 1f;
    private float baseKeySpeed;
    private List<MidiReader.RhythmNote> playNotes = new List<MidiReader.RhythmNote>();

    [Header("Tutorial")]
    [Tooltip("Tick = scene tutorial: không dùng MIDI/nhạc, nốt do TutorialManager spawn thủ công.")]
    [SerializeField] private bool isTutorial;
    public bool IsTutorial => isTutorial;
    public float SpawnAheadTime => spawnAheadTime;

    private float tutorialClock;

    [Tooltip("Key xuất hiện trước thời điểm note bao nhiêu giây. " +
             "Nên >= khoảng cách spawn / keySpeed")]
    [SerializeField] private float spawnAheadTime = 2f;

    [Tooltip("Bật = tự tính spawnAheadTime = khoảng cách (SpawnPoint -> Player) / keySpeed")]
    [SerializeField] private bool autoSpawnAhead = true;

    [Header("Overlap")]
    [Tooltip("Khoảng cách tối thiểu giữa 2 key (unit). Nên >= chiều rộng prefab key. " +
             "Note nào sát hơn mức này sẽ bị bỏ.")]
    [SerializeField] private float minKeyGap = 1.5f;

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

    private float lastAcceptedNoteEnd = -999f;

    private void Awake()
    {
        Instance = this;
        baseKeySpeed = keySpeed;
    }

    // =============================================
    // START SONG - gọi sau khi MidiReader.LoadMidi() xong
    // =============================================

    public event System.Action SongEnded;

    // Gọi từ LevelManager sau khi nạp level (bắt đầu bài mới => reset vòng)
    public void SetLevel(LevelData level)
    {
        if (level == null)
            return;

        baseKeySpeed = level.noteSpeed;
        ResetRounds();
        RecalculateSpawnAhead();
    }

    // Đưa về vòng 1, tốc độ gốc
    public void ResetRounds()
    {
        roundSpeed = 1f;
        Round = 1;
        keySpeed = baseKeySpeed;

        if (audioSource != null)
            audioSource.pitch = 1f;
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
        if (isTutorial)
        {
            StartTutorial();
            return;
        }

        if (midiReader == null || audioSource == null ||
            player == null || spawnPoint == null || pianoKeyPrefab == null)
        {
            Debug.LogError("PianoKeySpawner: thiếu reference!");
            return;
        }

        // Key bay nhanh theo vòng => khoảng cách giữa các key trên màn hình giữ nguyên
        keySpeed = baseKeySpeed * roundSpeed;
        RecalculateSpawnAhead();

        if (leadIn < 0f)
            leadIn = spawnAheadTime;

        // Note của vòng này: thời gian chia cho roundSpeed (không sửa Notes gốc)
        playNotes = midiReader.GetNotesWithSpeed(roundSpeed);

        if (playNotes.Count == 0)
            Debug.LogError("PianoKeySpawner: Notes rỗng! Chưa gọi MidiReader.LoadMidi() hoặc MIDI không có note.");

        if (audioSource.clip == null)
            Debug.LogError("PianoKeySpawner: AudioSource chưa có clip nhạc!");

        Debug.Log($"StartSong: Round {Round} | x{roundSpeed:F2} | {playNotes.Count} notes, leadIn = {leadIn}");

        ClearAllKeys();

        nextNoteIndex = 0;
        lastAcceptedNoteEnd = -999f;
        songEnded = false;
        paused = false;

        // Hướng key bay: từ spawnPoint về phía player
        spawnDirection = spawnPoint.position.x >= player.position.x ? 1f : -1f;

        if (coinSpawner != null)
        {
            coinSpawner.Build(
                playNotes,
                minKeyGap,
                keySpeed,
                longNotePrefab != null ? longNoteMinDuration : float.MaxValue,
                spawnPoint.position.y,
                spawnPoint.position.y + longNoteYOffset);
        }

        // Nhạc phát sau leadIn giây; trong lúc đó SongTime < 0 và key vẫn spawn
        songStartDsp = AudioSettings.dspTime + leadIn;
        audioSource.Stop();
        audioSource.pitch = roundSpeed;
        audioSource.PlayScheduled(songStartDsp);

        songStarted = true;
    }

    private void Update()
    {
        if (isTutorial)
        {
            if (songStarted && !paused)
                tutorialClock += Time.deltaTime;
            return;
        }

        if (!songStarted || songEnded)
            return;

        activeKeys.RemoveAll(k => k == null);

        SpawnKeys();

        // Hết vòng: đã spawn hết note, không còn key và nhạc đã dừng
        // (nhạc phát nhanh x roundSpeed nên độ dài thực = clip.length / roundSpeed)
        if (nextNoteIndex >= playNotes.Count &&
            activeKeys.Count == 0 &&
            audioSource.clip != null &&
            SongTime >= audioSource.clip.length / roundSpeed)
        {
            if (loopWithSpeedUp && roundSpeed < maxSpeed)
                NextRound();
            else
                EndSong();
        }
    }

    // Sang vòng mới: nhanh hơn, giữ nguyên điểm số
    private void NextRound()
    {
        roundSpeed = Mathf.Min(roundSpeed + speedStep, maxSpeed);
        Round++;

        ClearAllKeys();
        if (coinSpawner != null)
            coinSpawner.ClearAll();

        RoundStarted?.Invoke(Round, roundSpeed);

        // Nhạc phát lại => boss xuất hiện
        if (bossSpawner != null)
            bossSpawner.StartBossFight(OnBossDefeated);

        StartSong(nextRoundLeadIn);
    }

    // Gắn vào Button "Skip" trên UI: bỏ qua phần nhạc còn lại của vòng này
    public void SkipSong()
    {
        if (isTutorial || !songStarted || songEnded)
            return;

        // Đang đếm ngược trước khi nhạc phát: bỏ qua để tránh bấm đúp nhảy 2 vòng
        if (SongTime < 0f)
            return;

        audioSource.Stop();

        if (loopWithSpeedUp && roundSpeed < maxSpeed)
            NextRound();
        else
            EndSong();
    }

    private void OnBossDefeated()
    {
        Debug.Log($"Boss vòng {Round} bị hạ!");
        // TODO: thưởng điểm/coin, hồi máu... tuỳ game của bạn
    }

    // =============================================
    // SPAWN THEO THỜI GIAN NOTE
    // =============================================

    private void SpawnKeys()
    {
        float now = SongTime;
        List<MidiReader.RhythmNote> notes = playNotes;

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
        if (coinSpawner != null)
            coinSpawner.ClearAll();

        if (audioSource != null)
            audioSource.pitch = 1f;

        Debug.Log($"PianoKeySpawner: Song Ended! (Round {Round}, x{roundSpeed:F2})");

        if (bossSpawner != null)
            bossSpawner.ClearBoss();
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

        if (isTutorial) { paused = true; return; }

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

        if (isTutorial) { paused = false; return; }

        // Dời mốc bắt đầu bài đúng bằng thời gian đã dừng
        songStartDsp += AudioSettings.dspTime - pauseStartDsp;
        paused = false;

        if (pausedBeforeAudioStart)
            audioSource.PlayScheduled(songStartDsp);
        else
            audioSource.UnPause();
    }

    // =============================================
    // TUTORIAL
    // =============================================

    private void StartTutorial()
    {
        if (player == null || spawnPoint == null || pianoKeyPrefab == null)
        {
            Debug.LogError("PianoKeySpawner (Tutorial): thiếu reference!");
            return;
        }

        RecalculateSpawnAhead();
        ClearAllKeys();

        spawnDirection = spawnPoint.position.x >= player.position.x ? 1f : -1f;
        tutorialClock = 0f;
        songEnded = false;
        songStarted = true;
    }

    // Spawn 1 nốt thường, tới Player sau SpawnAheadTime giây.
    public PianoKey SpawnTutorialKey(bool left)
    {
        if (!isTutorial || !songStarted)
            return null;

        float travelTime = spawnAheadTime;

        Vector3 pos = new Vector3(
            player.position.x + spawnDirection * keySpeed * travelTime,
            spawnPoint.position.y,
            spawnPoint.position.z);

        GameObject obj = Instantiate(pianoKeyPrefab, pos, Quaternion.identity);
        PianoKey key = obj.GetComponent<PianoKey>();

        if (key == null)
        {
            Debug.LogError("PianoKey Prefab chưa có PianoKey.cs!");
            Destroy(obj);
            return null;
        }

        key.Setup(
            left ? PianoKey.KeyType.Left : PianoKey.KeyType.Right,
            SongTime + travelTime,
            player,
            keySpeed);

        activeKeys.Add(obj);
        return key;
    }

    // Spawn 1 nốt dài. requireJump = false thì người chơi không cần nhảy vẫn giữ được.
    public LongNote SpawnTutorialLongKey(bool left, float duration, bool requireJump)
    {
        if (!isTutorial || !songStarted || longNotePrefab == null)
            return null;

        float travelTime = spawnAheadTime;

        Vector3 pos = new Vector3(
            player.position.x + spawnDirection * keySpeed * travelTime,
            spawnPoint.position.y + longNoteYOffset,
            spawnPoint.position.z);

        GameObject obj = Instantiate(longNotePrefab, pos, Quaternion.identity);
        LongNote ln = obj.GetComponent<LongNote>();

        if (ln == null)
        {
            Debug.LogError("LongNote Prefab chưa có LongNote.cs!");
            Destroy(obj);
            return null;
        }

        ln.SetTutorialAssist(!requireJump);
        ln.Init(
            left ? Lanee.Left : Lanee.Right,
            SongTime + travelTime,
            duration,
            keySpeed,
            player.position.x,
            spawnDirection,
            this);

        activeKeys.Add(obj);
        return ln;
    }
}