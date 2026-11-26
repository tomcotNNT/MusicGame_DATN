using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Màn 4 - The Mysterious Cave: cơ chế NÉ.
/// Quái mẹ "Mother Maw" (nửa cái đầu ở mép phải) há miệng theo nhịp MIDI và nhả quái con (MedusaHead).
/// Quái con bay sang trái theo 1 trong 2 lane (trên / dưới) và PHÁT NỔ tại hit point đúng thời điểm nốt.
/// Người chơi đổi lane bằng nút Lên / Xuống:
///   - đứng ở lane khác lúc nổ  -> né thành công: + combo / điểm, quái mẹ mất 1 máu;
///   - đứng đúng lane bị nổ     -> Miss: mất combo, rung màn, trừ máu (nếu nhân vật có PlayerHealth).
/// Mọi vị trí tính theo khung camera (viewport) nên không phụ thuộc tỉ lệ màn hình.
/// </summary>
public class CaveMawSpawner : MonoBehaviour, IRhythmSongClock, ISongPausable
{
    public event Action SongEnded;
    public static event Action AnyBossDefeated;

    [Header("Nhạc / MIDI")]
    [SerializeField] private MidiReader midiReader;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private ComboSystem comboSystem;

    [Header("Prefab (ưu tiên dùng nếu có gán)")]
    [Tooltip("Prefab quái mẹ: SpriteRenderer + Animator (MotherMaw.controller), pivot mép phải-giữa")]
    [SerializeField] private GameObject bossPrefab;
    [Tooltip("Prefab quái con: SpriteRenderer + Animator (MawSpawn.controller)")]
    [SerializeField] private GameObject spawnPrefab;
    [Tooltip("Prefab biển cảnh báo lane (SpriteRenderer)")]
    [SerializeField] private GameObject warningPrefab;
    [Tooltip("Sorting Layer cho boss / quái. Phải nằm TRÊN lớp nền (Map), nếu không sẽ bị nền che mất")]
    [SerializeField] private string sortingLayerName = "Player";

    [Header("Asset quái mẹ (dùng khi không có prefab)")]
    [SerializeField] private Sprite bossSprite;
    [SerializeField] private RuntimeAnimatorController bossController;
    [Tooltip("Vị trí miệng so với pivot (mép phải-giữa) của sprite quái mẹ, đơn vị world khi chưa scale")]
    [SerializeField] private Vector2 mouthOffset = new Vector2(-3.0f, -2.2f);
    [Tooltip("Quái mẹ cao bao nhiêu phần chiều cao màn hình")]
    [SerializeField, Range(0.3f, 1f)] private float bossHeightFraction = 0.8f;

    [Header("Asset quái con (dùng khi không có prefab)")]
    [SerializeField] private Sprite spawnSprite;
    [SerializeField] private RuntimeAnimatorController spawnController;
    [SerializeField] private Sprite warningSprite;
    [SerializeField] private AudioClip explosionClip;
    [SerializeField] private int sortingOrder = 50;
    [Tooltip("Sprite MedusaHead gốc quay mặt sang PHẢI -> lật để quái con nhìn về phía người chơi (bên trái)")]
    [SerializeField] private bool flipSpawnX = true;

    [Header("Bố cục (toạ độ viewport 0..1)")]
    [SerializeField] private float laneTopY = 0.64f;
    [SerializeField] private float laneBottomY = 0.34f;
    [SerializeField] private float hitPointX = 0.21f;
    [SerializeField] private float playerX = 0.12f;

    [Header("Nhịp")]
    [Tooltip("Quái con bay từ miệng tới hit point mất bao lâu (giây)")]
    [SerializeField, Min(0.3f)] private float flyDuration = 1.6f;
    [Tooltip("Quái mẹ há miệng trước lúc nhả bao lâu")]
    [SerializeField, Min(0f)] private float openLead = 0.25f;
    [Tooltip("Quái con đổi mặt hoảng sợ + hiện cảnh báo lane trước nốt bao lâu")]
    [SerializeField, Min(0f)] private float alarmLead = 0.5f;
    [Tooltip("3 frame 'phồng' của clip Burst (3/15 s) -> bắt đầu Burst sớm để frame NỔ trùng đúng nốt")]
    [SerializeField, Min(0f)] private float burstLead = 0.2f;
    [Tooltip("Khoảng cách tối thiểu giữa 2 quái liên tiếp (giây). Nốt dày hơn sẽ bị bỏ bớt")]
    [SerializeField, Min(0.05f)] private float minNoteGap = 0.3f;
    [Tooltip("Hai nốt khác lane cách nhau ít hơn mức này thì ép về cùng lane (không bắt đổi lane quá gấp)")]
    [SerializeField, Min(0f)] private float minSwitchGap = 0.22f;
    [SerializeField] private float simultaneousThreshold = 0.03f;

    public enum LaneMode
    {
        MidiLane,      // dùng lane MidiReader đã chia (của Man3 -> dễ lệch hẳn 1 lane)
        PitchContour,  // nốt cao hơn nốt trước -> lane TRÊN, thấp hơn -> lane DƯỚI (bám giai điệu)
        PitchMedian,   // cao hơn trung vị cao độ cả bài -> TRÊN
        Alternate,     // xen kẽ trên / dưới
        Random,
    }

    [Header("Chia lane")]
    [SerializeField] private LaneMode laneMode = LaneMode.PitchContour;
    [Tooltip("Tối đa bao nhiêu quái liên tiếp cùng 1 lane, quá thì đổi lane (0 = không giới hạn)")]
    [SerializeField, Min(0)] private int maxSameLane = 3;

    [Header("Luật chơi")]
    [SerializeField, Range(0.1f, 1f)] private float bossHpRatio = 0.8f;
    [SerializeField, Min(0)] private int damageOnHit = 10;
    [SerializeField, Min(0.01f)] private float laneSwitchTime = 0.08f;
    [Tooltip("Xoay 2 nút trái/phải thành mũi tên LÊN (nút trái) / XUỐNG (nút phải)")]
    [SerializeField] private bool rotateLaneButtons = true;

    [Header("Ẩn đồ của Man3 khi vào màn")]
    [SerializeField] private GameObject[] hideOnStart;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    [Tooltip("Tự né (để xem quái mẹ sinh nốt có mượt không mà không cần bấm)")]
    [SerializeField] private bool autoPlay;

    // ---------------------------------------------------------------
    private struct Note { public float time; public int lane; public int pitch; }
    private class Spawn
    {
        public Note note;
        public Transform tr;
        public Animator anim;
        public SpriteRenderer warning;
        public Vector3 from, to;
        public bool alarmed, burst, resolved;
    }

    private readonly List<Note> notes = new List<Note>();
    private readonly List<Spawn> active = new List<Spawn>();
    private readonly SongClock clock = new SongClock();
    private int nextOpen, nextSpawn;
    private bool isPlaying, songEnded;

    private Camera cam;
    private float unitScale = 1f;
    private Transform boss;
    private Animator bossAnim;
    private SpriteRenderer bossRenderer;
    private int bossHp, bossMaxHp;
    private bool bossDead;
    private RuntimeBossHpBar hpBar;

    private Transform player;
    private Animator playerAnim;
    private PlayerHealth playerHealth;
    private SpriteRenderer playerRenderer;
    private float playerYOffset;
    private int playerLane = 1;          // 0 = trên, 1 = dưới
    private float playerFlash;

    private int dodged, hitCount;

    public float SongTime => isPlaying ? clock.SongTime : 0f;
    public bool IsPlaying => isPlaying;
    public bool BossDefeated => bossDead;
    public int PlayerLane => playerLane;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => AnyBossDefeated = null;

    // ===============================================================
    private void Awake()
    {
        cam = Camera.main;
        if (midiReader == null) midiReader = FindFirstObjectByType<MidiReader>();
        if (musicSource == null && midiReader != null) musicSource = midiReader.GetComponent<AudioSource>();
        if (comboSystem == null) comboSystem = FindFirstObjectByType<ComboSystem>();

        if (hideOnStart != null)
            foreach (var go in hideOnStart) if (go != null) go.SetActive(false);
        foreach (var sub in FindObjectsByType<SubmarineRhythmSpawner>(FindObjectsSortMode.None)) sub.enabled = false;
        foreach (var sc in FindObjectsByType<SubmarineController>(FindObjectsSortMode.None)) sc.gameObject.SetActive(false);
    }

    private void OnEnable() => LaneButtonInput.Pressed += OnLaneButton;
    private void OnDisable() => LaneButtonInput.Pressed -= OnLaneButton;

    private IEnumerator Start()
    {
        CreateBoss();
        if (rotateLaneButtons) RotateLaneButtons();
        yield return null;           // đợi GameCharSelector bật đúng nhân vật
        BindPlayer();
    }

    // ===============================================================
    // VỊ TRÍ
    // ===============================================================
    private Vector3 VP(float x, float y)
    {
        if (cam == null) cam = Camera.main;
        Vector3 p = cam.ViewportToWorldPoint(new Vector3(x, y, Mathf.Abs(cam.transform.position.z)));
        p.z = 0f;
        return p;
    }

    private float LaneY(int lane) => VP(0, lane == 0 ? laneTopY : laneBottomY).y;
    private Vector3 HitPos(int lane) => new Vector3(VP(hitPointX, 0).x, LaneY(lane), 0f);
    private Vector3 MouthPos() => boss != null
        ? boss.position + (Vector3)(mouthOffset * boss.localScale.x)
        : VP(0.9f, 0.4f);

    private void CreateBoss()
    {
        if (cam == null) { Debug.LogError("[Man4] Không có Main Camera"); return; }
        float viewH = cam.orthographic ? cam.orthographicSize * 2f : 11.2f;

        GameObject go = MakeActor(bossPrefab, "MotherMaw", bossSprite, bossController, sortingOrder - 1,
                                  out bossRenderer, out bossAnim);
        boss = go.transform;
        float spriteH = bossRenderer != null && bossRenderer.sprite != null ? bossRenderer.sprite.bounds.size.y : 9f;
        float s = viewH * bossHeightFraction / spriteH;
        boss.localScale = Vector3.one * s;
        boss.position = VP(1f, 0.5f);       // pivot sprite ở mép phải-giữa
        unitScale = viewH / 11.2f;           // asset vẽ cho camera ortho 5.6
    }

    /// <summary>Tạo 1 đối tượng từ prefab (nếu có) hoặc dựng từ sprite + controller, rồi đặt sorting layer.</summary>
    private GameObject MakeActor(GameObject prefab, string name, Sprite sprite, RuntimeAnimatorController ctrl,
                                 int order, out SpriteRenderer sr, out Animator anim)
    {
        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab);
            go.name = name;
            sr = go.GetComponentInChildren<SpriteRenderer>();
            anim = go.GetComponentInChildren<Animator>();
        }
        else
        {
            go = new GameObject(name);
            sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            anim = null;
            if (ctrl != null)
            {
                anim = go.AddComponent<Animator>();
                anim.runtimeAnimatorController = ctrl;
            }
        }
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerID = SortingLayerId();
        sr.sortingOrder = order;
        return go;
    }

    private int SortingLayerId()
    {
        // Lớp "Default" là lớp THẤP NHẤT của project -> nền hang (lớp Map) sẽ che mất boss
        int id = SortingLayer.NameToID(sortingLayerName);
        if (SortingLayer.IsValid(id) && !string.IsNullOrEmpty(sortingLayerName)) return id;
        var layers = SortingLayer.layers;
        foreach (var l in layers) if (l.name == "Player") return l.id;
        return layers.Length > 0 ? layers[layers.Length - 1].id : 0;
    }

    private void RotateLaneButtons()
    {
        // ◀ xoay -90° -> ▲ (LÊN);  ▶ xoay -90° -> ▼ (XUỐNG)
        foreach (var b in FindObjectsByType<LaneButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            b.transform.localRotation *= Quaternion.Euler(0f, 0f, -90f);
    }

    private void BindPlayer()
    {
        PlayerControllerSub pc = null;
        foreach (var c in FindObjectsByType<PlayerControllerSub>(FindObjectsSortMode.None))
            if (c.isActiveAndEnabled) { pc = c; break; }

        if (pc == null)
        {
            Debug.LogWarning("[Man4] Không tìm thấy nhân vật (PlayerControllerSub) đang bật - vẫn chạy, chỉ không có nhân vật di chuyển.");
            return;
        }

        player = pc.transform;
        pc.enabled = false;                  // tắt đánh trái/phải của Man3
        playerAnim = pc.GetComponentInChildren<Animator>();
        playerRenderer = pc.GetComponentInChildren<SpriteRenderer>();
        playerHealth = pc.GetComponentInParent<PlayerHealth>();
        if (playerRenderer != null)
        {
            playerRenderer.flipX = false;
            playerYOffset = player.position.y - playerRenderer.bounds.center.y;
        }
        player.position = new Vector3(VP(playerX, 0).x, LaneY(playerLane) + playerYOffset, player.position.z);
    }

    // ===============================================================
    // BẮT ĐẦU BÀI (CountdownCave gọi)
    // ===============================================================
    public void StartSpawner(float countdown)
    {
        if (isPlaying) return;
        if (midiReader == null || musicSource == null || musicSource.clip == null || spawnController == null)
        {
            Debug.LogError("[Man4] CaveMawSpawner thiếu reference (MidiReader / AudioSource có clip / MawSpawn controller).");
            return;
        }

        BuildNotes();
        if (notes.Count == 0) { Debug.LogError("[Man4] MIDI không có nốt lane 0/1."); return; }

        bossMaxHp = bossHp = Mathf.Max(1, Mathf.CeilToInt(notes.Count * bossHpRatio));
        hpBar = RuntimeBossHpBar.Create("MOTHER MAW");
        if (hpBar != null) hpBar.SetFill(1f, bossHp, bossMaxHp);

        nextOpen = nextSpawn = 0;
        dodged = hitCount = 0;
        songEnded = bossDead = false;
        clock.Start(musicSource, countdown);
        isPlaying = true;
        Debug.Log($"[Man4] Bắt đầu: {notes.Count} quái, máu boss {bossMaxHp}, nốt đầu {notes[0].time:F2}s, bay {flyDuration:F2}s");
    }

    private void BuildNotes()
    {
        notes.Clear();
        var raw = new List<MidiReader.RhythmNote>(midiReader.GetNotesWithSpeed(1));
        raw.Sort((a, b) => a.time.CompareTo(b.time));

        // 1) lọc nhịp: bỏ hợp âm / nốt quá sát (không thể né liên tục)
        int dropped = 0;
        foreach (var n in raw)
        {
            if (laneMode == LaneMode.MidiLane && n.lane != 0 && n.lane != 1) continue;
            if (notes.Count > 0)
            {
                Note last = notes[notes.Count - 1];
                float gap = n.time - last.time;
                if (gap <= simultaneousThreshold || gap < minNoteGap)
                {
                    // hợp âm: giữ nốt cao nhất (giai điệu thường nằm ở nốt cao)
                    if (gap <= simultaneousThreshold && n.pitch > last.pitch)
                        notes[notes.Count - 1] = new Note { time = last.time, lane = n.lane, pitch = n.pitch };
                    dropped++;
                    continue;
                }
            }
            notes.Add(new Note { time = n.time, lane = n.lane, pitch = n.pitch });
        }

        // 2) chia lane (0 = TRÊN, 1 = DƯỚI)
        var sorted = new List<int>();
        foreach (var n in notes) sorted.Add(n.pitch);
        sorted.Sort();
        int median = sorted.Count > 0 ? sorted[sorted.Count / 2] : 60;
        var rng = new System.Random(notes.Count);

        int lastLane = 1, same = 0, lastPitch = -1, flips = 0;
        for (int i = 0; i < notes.Count; i++)
        {
            Note n = notes[i];
            int lane;
            switch (laneMode)
            {
                case LaneMode.PitchContour:
                    lane = lastPitch < 0 ? (n.pitch >= median ? 0 : 1)
                         : n.pitch > lastPitch ? 0 : n.pitch < lastPitch ? 1 : lastLane;
                    break;
                case LaneMode.PitchMedian: lane = n.pitch >= median ? 0 : 1; break;
                case LaneMode.Alternate: lane = i % 2; break;
                case LaneMode.Random: lane = rng.Next(2); break;
                default: lane = n.lane; break;
            }

            // không để quá nhiều quái liên tiếp cùng lane (nếu còn đủ thời gian đổi lane)
            float gap = i > 0 ? n.time - notes[i - 1].time : 99f;
            if (maxSameLane > 0 && lane == lastLane && same >= maxSameLane && gap >= minSwitchGap)
            {
                lane = 1 - lane;
                flips++;
            }
            // đổi lane quá gấp -> giữ lane cũ
            else if (i > 0 && lane != lastLane && gap < minSwitchGap)
                lane = lastLane;

            same = lane == lastLane ? same + 1 : 1;
            lastLane = lane;
            lastPitch = n.pitch;
            n.lane = lane;
            notes[i] = n;
        }

        int top = 0;
        foreach (var n in notes) if (n.lane == 0) top++;
        if (showDebug)
            Debug.Log($"[Man4] Lọc MIDI ({laneMode}): giữ {notes.Count}, bỏ {dropped} nốt quá dày/hợp âm | lane TRÊN {top}, DƯỚI {notes.Count - top}, ép đổi lane {flips}");
    }

    // ===============================================================
    // VÒNG LẶP
    // ===============================================================
    private void Update()
    {
        ReadKeyboard();
        MovePlayer();
        if (!isPlaying) return;

        float now = SongTime;

        // 1) quái mẹ há miệng trước lúc nhả
        while (nextOpen < notes.Count && now >= notes[nextOpen].time - flyDuration - openLead)
        {
            if (!bossDead) AnimatorSafe.SetTrigger(bossAnim, "Attack");
            nextOpen++;
        }

        // 2) nhả quái con
        while (nextSpawn < notes.Count && now >= notes[nextSpawn].time - flyDuration)
        {
            Note n = notes[nextSpawn++];
            if (!bossDead && n.time - now > 0.05f) SpawnChild(n);
        }

        // 3) cập nhật quái đang bay
        for (int i = active.Count - 1; i >= 0; i--)
            if (UpdateChild(active[i], now)) active.RemoveAt(i);

        if (autoPlay) AutoDodge(now);
        CheckSongEnd(now);
    }

    private void SpawnChild(Note n)
    {
        GameObject go = MakeActor(spawnPrefab, "MawSpawn " + n.time.ToString("F2"), spawnSprite, spawnController,
                                  sortingOrder, out SpriteRenderer body, out Animator an);
        if (body != null) body.flipX = flipSpawnX;
        go.transform.localScale *= unitScale;

        MakeActor(warningPrefab, "LaneWarning", warningSprite, null, sortingOrder + 1, out SpriteRenderer warn, out _);
        warn.transform.localScale *= unitScale * 1.5f;
        warn.transform.position = HitPos(n.lane) + Vector3.up * 1.4f * unitScale;
        warn.enabled = false;

        var sp = new Spawn
        {
            note = n, tr = go.transform, anim = an, warning = warn,
            from = MouthPos(), to = HitPos(n.lane),
        };
        sp.tr.position = sp.from;
        active.Add(sp);
    }

    /// <returns>true khi đã xong (xoá khỏi danh sách)</returns>
    private bool UpdateChild(Spawn s, float now)
    {
        if (s.tr == null) return true;
        float hit = s.note.time;
        float arrive = hit - burstLead;                       // tới hit point thì bắt đầu phồng
        float start = hit - flyDuration;
        float k = Mathf.Clamp01((now - start) / Mathf.Max(0.01f, arrive - start));

        if (!s.burst)
        {
            // bay theo đường cong nhẹ: rời miệng rồi lượn về đúng lane
            Vector3 p = Vector3.Lerp(s.from, s.to, k);
            p.y = Mathf.Lerp(s.from.y, s.to.y, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, k * 1.6f)));
            p.y += Mathf.Sin(k * Mathf.PI * 3f) * 0.12f * unitScale * (1f - k);
            s.tr.position = p;
        }

        if (!s.alarmed && now >= hit - alarmLead)
        {
            s.alarmed = true;
            AnimatorSafe.SetTrigger(s.anim, "Alarm");
        }
        if (s.warning != null)
        {
            bool show = s.alarmed && !s.resolved;
            s.warning.enabled = show && Mathf.Repeat(now * 8f, 1f) < 0.6f;
        }

        if (!s.burst && now >= arrive)
        {
            s.burst = true;
            s.tr.position = s.to;
            AnimatorSafe.SetTrigger(s.anim, "Explode");
        }

        if (!s.resolved && now >= hit)
        {
            s.resolved = true;
            Resolve(s);
        }

        if (now >= hit + 0.5f)
        {
            Destroy(s.tr.gameObject);
            if (s.warning != null) Destroy(s.warning.gameObject);
            return true;
        }
        return false;
    }

    private void Resolve(Spawn s)
    {
        GameplaySfx.Play(explosionClip, 0.8f);
        bool caught = playerLane == s.note.lane;

        if (caught)
        {
            hitCount++;
            if (comboSystem != null) comboSystem.MissNote();
            var shake = FindFirstObjectByType<CameraShake>();
            if (shake != null) shake.Shake(0.2f, 0.25f);
            if (playerHealth != null && damageOnHit > 0) playerHealth.TakeDamage(damageOnHit);
            playerFlash = 0.35f;
            if (showDebug) Debug.Log($"[Man4] TRÚNG NỔ lane {(s.note.lane == 0 ? "TRÊN" : "DƯỚI")} @ {s.note.time:F2}s");
        }
        else
        {
            dodged++;
            if (comboSystem != null) comboSystem.HitNote();
            DamageBoss();
        }
    }

    private void DamageBoss()
    {
        if (bossDead) return;
        bossHp--;
        if (hpBar != null) hpBar.SetFill((float)bossHp / bossMaxHp, bossHp, bossMaxHp);

        if (bossHp <= 0)
        {
            bossDead = true;
            AnimatorSafe.SetTrigger(bossAnim, "Die");
            if (hpBar != null) hpBar.FadeOutAndDestroy();
            AnyBossDefeated?.Invoke();
            Debug.Log("[Man4] Hạ Mother Maw!");
            return;
        }

        // chỉ giật Hit khi đang nghỉ, để không cắt ngang động tác há miệng
        if (bossAnim != null && bossAnim.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
            AnimatorSafe.SetTrigger(bossAnim, "Hit");
    }

    private void CheckSongEnd(float now)
    {
        if (songEnded) return;
        if (nextSpawn < notes.Count || active.Count > 0 || now < musicSource.clip.length) return;

        songEnded = true;
        isPlaying = false;
        Debug.Log($"[Man4] Hết bài: né {dodged}, trúng nổ {hitCount}, máu boss còn {bossHp}/{bossMaxHp}");
        SongEnded?.Invoke();
    }

    // ===============================================================
    // NGƯỜI CHƠI
    // ===============================================================
    private void ReadKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.A)) SetLane(0);
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.D)) SetLane(1);
    }

    // nút trái = LÊN, nút phải = XUỐNG
    private void OnLaneButton(bool isLeft) => SetLane(isLeft ? 0 : 1);

    private void SetLane(int lane)
    {
        if (Time.timeScale == 0f || clock.IsPaused) return;
        if (playerHealth != null && playerHealth.IsDead) return;
        if (lane == playerLane) return;
        playerLane = lane;
        AnimatorSafe.SetTrigger(playerAnim, "Jump");
    }

    private void MovePlayer()
    {
        if (player == null) return;
        Vector3 p = player.position;
        float targetY = LaneY(playerLane) + playerYOffset;
        float speed = Mathf.Abs(LaneY(0) - LaneY(1)) / laneSwitchTime;
        p.y = Mathf.MoveTowards(p.y, targetY, speed * Time.deltaTime);
        player.position = p;

        if (playerRenderer != null)
        {
            playerFlash = Mathf.Max(0f, playerFlash - Time.deltaTime);
            playerRenderer.color = playerFlash > 0f && Mathf.Repeat(playerFlash * 20f, 1f) < 0.5f
                ? new Color(1f, 0.4f, 0.4f) : Color.white;
        }
    }

    private void AutoDodge(float now)
    {
        // né quái gần nhất chưa nổ
        foreach (var s in active)
        {
            if (s.resolved) continue;
            if (s.note.time - now < 0.35f && s.note.lane == playerLane) SetLane(1 - playerLane);
            break;
        }
    }

    // ===============================================================
    // PAUSE
    // ===============================================================
    public void PauseSong() { if (isPlaying) clock.Pause(musicSource); }
    public void ResumeSong() { if (isPlaying) clock.Resume(musicSource); }

    private void OnDrawGizmos()
    {
        var c = Camera.main;
        if (c == null) return;
        cam = c;
        Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.8f);
        for (int lane = 0; lane < 2; lane++)
        {
            Vector3 h = HitPos(lane);
            Gizmos.DrawWireSphere(h, 0.7f);
            Gizmos.DrawLine(new Vector3(VP(0, 0).x, h.y), new Vector3(VP(1, 0).x, h.y));
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(new Vector3(VP(playerX, 0).x, LaneY(1)), 0.4f);
    }
}
