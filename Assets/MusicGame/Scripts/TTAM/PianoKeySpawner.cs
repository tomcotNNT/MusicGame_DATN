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
    [Tooltip("Offset Y thêm cho nốt dài so với hit point. Đặt 0 nếu muốn nốt dài ngang hit point.")]
    [SerializeField] private float longNoteYOffset = 0f;
    public float KeySpeed => keySpeed;
    public IReadOnlyList<MidiReader.RhythmNote> PlayNotes => playNotes;

    [Header("Hit Points")]
    [Tooltip("Hit point DƯỚI: nốt TRÁI (lane 0)")]
    [SerializeField] private Transform hitPointBottom;
    [Tooltip("Hit point TRÊN: nốt PHẢI (lane 1)")]
    [SerializeField] private Transform hitPointTop;

    [Header("Coin")]
    [SerializeField] private CoinSpawner coinSpawner;


    [Header("Boss")]
    [SerializeField] private BossSpawner bossSpawner;
    [Tooltip("Boss xuất hiện khi bài chạy tới tỉ lệ này (0.5 = giữa bài). Chỉ xuất hiện 1 lần")]
    [SerializeField, Range(0.05f, 0.95f)] private float bossAppearAt = 0.5f;
    private bool bossAppeared;

    [Tooltip("Bật = nốt được boss chọn sẽ THAY bằng đạn boss. Tắt = vẫn spawn nốt thường song song với đạn")]
    [SerializeField] private bool bossReplacesNote = true;

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
    [Tooltip("Tổng số vòng nhạc. 1 = chỉ phát bài một lần rồi kết thúc (không lặp lại)")]
    [SerializeField, Min(1)] private int maxRounds = 1;

    // Còn vòng kế tiếp không: chưa tới maxRounds và chưa chạm tốc độ tối đa
    private bool CanLoop => loopWithSpeedUp && Round < maxRounds && roundSpeed < maxSpeed;

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

    [Header("Item thay nốt")]
    [Tooltip("Danh sách item. Một số nốt thường sẽ được đổi thành item này")]
    [SerializeField] private List<GameObject> itemPrefabs = new List<GameObject>();
    [Tooltip("Cứ N nốt thường thì 1 nốt đổi thành item. 4 = nốt thứ 4, 8, 12...")]
    [SerializeField, Min(2)] private int itemEveryNNotes = 4;
    [Tooltip("Bật = chọn item ngẫu nhiên, tắt = lần lượt theo list")]
    [SerializeField] private bool itemRandomPick = false;
    [Tooltip("Bật = nốt dài cũng có thể đổi thành item. Tắt = chỉ đổi nốt thường")]
    [SerializeField] private bool itemCanReplaceLongNote = false;

    private int itemIndex;
    private int notesSinceItem;

    // ---- Hit point helpers ----

    // Trái = hit point dưới, Phải = hit point trên. Thiếu hit point thì dùng Player.
    private Transform GetHitPoint(bool left)
    {
        Transform hp = left ? hitPointBottom : hitPointTop;
        return hp != null ? hp : player;
    }

    private float GetLaneY(bool left, bool isLong)
    {
        float y = GetHitPoint(left).position.y;
        if (isLong) y += longNoteYOffset;
        return y;
    }

    // Vị trí X để nốt tới đúng hit point sau travelTime giây
    private float GetSpawnX(bool left, float travelTime)
    {
        return GetHitPoint(left).position.x + spawnDirection * keySpeed * travelTime;
    }

    // ---- Boss hooks (BossMusicAttack / BossProjectile dùng) ----

    // Điểm đích của đạn boss: cùng hit point với nốt cùng lane
    public Transform GetBossHitPoint(bool isLeft)
    {
        return GetHitPoint(isLeft);
    }

    // Đạn boss đăng ký để spawner quản lý vòng đời (xoá khi hết vòng, tính vào "còn key")
    public void RegisterActiveKey(GameObject obj)
    {
        if (obj != null && !activeKeys.Contains(obj))
            activeKeys.Add(obj);
    }

    // Tới giữa bài (bossAppearAt) thì boss xuất hiện, chỉ 1 lần
    private void TrySpawnBoss()
    {
        if (bossAppeared || bossSpawner == null || audioSource == null || audioSource.clip == null)
            return;

        float songLength = audioSource.clip.length / roundSpeed;

        if (SongTime >= songLength * bossAppearAt)
        {
            bossAppeared = true;
            bossSpawner.StartBossFight(OnBossDefeated);
        }
    }

    // Boss đang đánh và chọn nốt này => true
    private bool TryBossNote(MidiReader.RhythmNote note, bool left)
    {
        if (bossSpawner == null || !bossSpawner.IsFighting)
            return false;

        BossMusicAttack attack = bossSpawner.CurrentMusicAttack;
        if (attack == null)
            return false;

        return attack.TryScheduleBossNote(note.time, note.duration, left);
    }

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

        if (hitPointBottom == null || hitPointTop == null)
            Debug.LogWarning("PianoKeySpawner: chưa gán đủ HitPointBottom / HitPointTop, tạm dùng Player.");

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

        // Bắt đầu vòng mới: xoá hàng đợi tấn công cũ của boss
        if (bossSpawner != null && bossSpawner.CurrentMusicAttack != null)
            bossSpawner.CurrentMusicAttack.ResetAttackState();

        nextNoteIndex = 0;
        lastAcceptedNoteEnd = -999f;
        itemIndex = 0;
        notesSinceItem = 0;
        songEnded = false;
        paused = false;

        // Bài mới (vòng 1) thì boss được phép xuất hiện lại; các vòng sau thì không
        if (Round == 1)
            bossAppeared = false;

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
        TrySpawnBoss();

        // Hết vòng: đã spawn hết note, không còn key và nhạc đã dừng
        // (nhạc phát nhanh x roundSpeed nên độ dài thực = clip.length / roundSpeed)
        if (nextNoteIndex >= playNotes.Count &&
            activeKeys.Count == 0 &&
            audioSource.clip != null &&
            SongTime >= audioSource.clip.length / roundSpeed)
        {
            if (CanLoop)
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

        if (CanLoop)
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

            // Lane của nốt này (dùng chung cho nốt thường, item và đạn boss)
            bool left = randomLane ? Random.value < 0.5f : note.lane == 0;

            // Boss đang đánh: nốt được chọn sẽ thành đạn boss
            bool bossTook = TryBossNote(note, left);
            if (bossTook && bossReplacesNote)
            {
                lastAcceptedNoteEnd = note.time;
                continue;
            }

            // Đến lượt thì nốt này đổi thành item
            bool canBeItem = itemPrefabs != null && itemPrefabs.Count > 0
                             && (!isLong || itemCanReplaceLongNote);

            if (canBeItem)
            {
                notesSinceItem++;
                if (notesSinceItem >= itemEveryNNotes)
                {
                    notesSinceItem = 0;
                    lastAcceptedNoteEnd = note.time;   // item không có đuôi dài
                    SpawnItem(note.time, travelTime, left);
                    continue;
                }
            }

            lastAcceptedNoteEnd = note.time + (isLong ? note.duration : 0f);

            if (isLong) SpawnLongKey(note, travelTime, left);
            else        SpawnKey(note, travelTime, left);
        }
    }

    private void SpawnItem(float noteTime, float travelTime, bool left)
    {
        GameObject prefab = itemRandomPick
            ? itemPrefabs[Random.Range(0, itemPrefabs.Count)]
            : itemPrefabs[itemIndex++ % itemPrefabs.Count];

        if (prefab == null) return;

        Vector3 pos = new Vector3(
            GetSpawnX(left, travelTime),
            GetLaneY(left, false),
            spawnPoint.position.z);

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);

        RhythmItemm item = obj.GetComponent<RhythmItemm>();
        if (item != null)
            item.Setup(left, noteTime, GetHitPoint(left), keySpeed);
        else
            Debug.LogWarning("PianoKeySpawner: item prefab chưa có RhythmItemm.cs!");

        activeKeys.Add(obj);
    }

    private void SpawnLongKey(MidiReader.RhythmNote note, float travelTime, bool left)
    {
        Lanee lane = left ? Lanee.Left : Lanee.Right;
        Transform hitPoint = GetHitPoint(left);

        Vector3 pos = new Vector3(
            GetSpawnX(left, travelTime),
            GetLaneY(left, true),
            spawnPoint.position.z
        );

        GameObject obj = Instantiate(longNotePrefab, pos, Quaternion.identity);
        LongNote ln = obj.GetComponent<LongNote>();

        if (ln == null)
        {
            Debug.LogError("LongNote Prefab chưa có LongNote.cs!");
            Destroy(obj);
            return;
        }

        ln.Init(lane, note.time, note.duration, keySpeed,
                hitPoint.position.x, spawnDirection, this);

        activeKeys.Add(obj);
    }

    private void SpawnKey(MidiReader.RhythmNote note, float travelTime, bool left)
    {
        PianoKey.KeyType type = left ? PianoKey.KeyType.Left : PianoKey.KeyType.Right;

        Vector3 pos = new Vector3(
            GetSpawnX(left, travelTime),
            GetLaneY(left, false),
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

        // Đích của nốt là hit point tương ứng (trái = dưới, phải = trên)
        key.Setup(type, note.time, GetHitPoint(left), keySpeed);
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

        // Hết nhạc boss: boss đi lùi về spawn point rồi biến mất
        if (bossSpawner != null)
            bossSpawner.RetreatBoss();
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

    // Spawn 1 nốt thường, tới hit point sau SpawnAheadTime giây.
    public PianoKey SpawnTutorialKey(bool left)
    {
        if (!isTutorial || !songStarted)
            return null;

        float travelTime = spawnAheadTime;

        Vector3 pos = new Vector3(
            GetSpawnX(left, travelTime),
            GetLaneY(left, false),
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
            GetHitPoint(left),
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
            GetSpawnX(left, travelTime),
            GetLaneY(left, true),
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
            GetHitPoint(left).position.x,
            spawnDirection,
            this);

        activeKeys.Add(obj);
        return ln;
    }
}