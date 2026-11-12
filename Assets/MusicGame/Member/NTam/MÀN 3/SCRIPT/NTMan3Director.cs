using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct NoteDataMan3
{
    public bool isLeftLane;
    public Man3NoteType noteType;
    public float hitTime;
    public float holdDuration;
    public float flightDuration;
}

public enum HitJudgment
{
    Perfect,
    Good,
    Miss
}

public class NTMan3Director : MonoBehaviour
{
    [Header("1. Căn Chỉnh Đa Màn Hình & Tầm Đánh")]
    [Range(0.25f, 0.44f)]
    [SerializeField] private float leftLaneViewportX = 0.38f;
    [SerializeField] private float maxLaneDistanceFromCenter = 1.15f;
    [SerializeField] private bool autoAlignOnStart = true;

    [Header("2. Các Điểm Tọa Độ Trong Scene")]
    [SerializeField] private Transform spawnPointL;
    [SerializeField] private Transform spawnPointR;
    [SerializeField] private Transform hitPointL;
    [SerializeField] private Transform hitPointR;
    [SerializeField] private SpriteRenderer laneTrackL;
    [SerializeField] private SpriteRenderer laneTrackR;

    [Header("3. Nhân Vật (Player2) & Bộ Animation Đòn Đánh")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private bool spriteDefaultFacesRight = true;
    [SerializeField] private float punchLungeDistance = 0.22f;
    [SerializeField] private float lungeForwardTime = 0.06f;
    [SerializeField] private float lungeRecoverTime = 0.10f;
    [Tooltip("Giai đoạn 3: Thêm tên các State đòn đánh mới vào danh sách này (VD: Uppercut, SpinKick)")]
    [SerializeField] private string[] normalAttackStates = { "Hit", "Hit2", "Kick" };
    [SerializeField] private string doubleAttackState = "SwingTHand";
    [SerializeField] private string hurtState = "Hurt";

    [Header("4. Prefabs Các Loại Nốt (Giai đoạn 4 thay ruột Pixel Art)")]
    [SerializeField] private GameObject noteLeftPrefab;
    [SerializeField] private GameObject noteRightPrefab;
    [SerializeField] private GameObject holdNotePrefab;
    [SerializeField] private GameObject doubleNotePrefab;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private GameObject buffPrefab;

    [Header("5. VFX Hiệu Ứng Va Chạm (Giai đoạn 3 kéo Prefab vào)")]
    [SerializeField] private GameObject perfectHitVfxPrefab;
    [SerializeField] private GameObject goodHitVfxPrefab;
    [SerializeField] private GameObject bombExplosionVfxPrefab;
    [SerializeField] private GameObject slashTrailVfxPrefab;
    [SerializeField] private GameObject buffAuraVfx;

    [Header("6. Giao Diện UI Thật & Máu (Giai đoạn 1 kéo từ Canvas)")]
    [SerializeField] private GameObject readyBannerUI;
    [SerializeField] private TextMeshProUGUI judgmentText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject speedUpWarningUI;

    [Header("7. Sự Kiện Rung Camera (Kéo Main Camera vào để gọi NT Hit Stop And Shake)")]
    public UnityEvent onDoubleHitCameraShake;
    public UnityEvent onBombHitCameraShake;

    [Header("8. Thông Số Chấm Nhịp & Máu")]
    [SerializeField] private float perfectWindow = 0.08f;
    [SerializeField] private float goodWindow = 0.18f;
    [SerializeField] private float earlyMissWindow = 0.30f;
    [SerializeField] private float chordBufferWindow = 0.07f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float missHealthPenalty = 10f;
    [SerializeField] private float bombHealthPenalty = 20f;

    private Camera mainCam;
    private float songStartTime;
    private int nextSpawnIndex = 0;
    private int attackComboStep = 0;
    private bool isGameOver = false;

    private AudioSource linkedAudioSource;
    private float chartAudioOffset = 0f;
    private bool hasExternalChartLoaded = false;

    private List<NoteDataMan3> chartNotes = new List<NoteDataMan3>();
    private List<NTVerticalNoteMan3> activeNotes = new List<NTVerticalNoteMan3>();

    private NTVerticalNoteMan3 activeHoldLeft;
    private NTVerticalNoteMan3 activeHoldRight;

    private float lastLeftPressTime = -10f;
    private float lastRightPressTime = -10f;
    private float buffRemainingTime = 0f;

    private Vector3 baseHitPointScaleL = Vector3.one;
    private Vector3 baseHitPointScaleR = Vector3.one;

    private Coroutine activeLungeCoroutine;
    private Coroutine leftTrackCoroutine;
    private Coroutine rightTrackCoroutine;

    private int currentScore = 0;
    private int currentCombo = 0;
    private float currentHealth;

    private void Awake()
    {
        mainCam = Camera.main;
        if (autoAlignOnStart) AlignLanesToScreen();

        if (hitPointL != null) baseHitPointScaleL = hitPointL.localScale;
        if (hitPointR != null) baseHitPointScaleR = hitPointR.localScale;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateAllUI();

        if (playerTransform != null)
            playerTransform.position = new Vector3(0f, playerTransform.position.y, playerTransform.position.z);

        if (readyBannerUI != null)
            StartCoroutine(HideReadyBannerRoutine());

        if (!hasExternalChartLoaded)
        {
            GenerateDummyPatternChart();
            songStartTime = Time.time;
        }
    }

    private IEnumerator HideReadyBannerRoutine()
    {
        readyBannerUI.SetActive(true);
        yield return new WaitForSeconds(1.2f);
        readyBannerUI.SetActive(false);
    }

    public void AlignLanesToScreen()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        float zDist = Mathf.Abs(mainCam.transform.position.z);
        float rawLeftX = mainCam.ViewportToWorldPoint(new Vector3(leftLaneViewportX, 0.5f, zDist)).x;

        float clampedDistance = Mathf.Clamp(Mathf.Abs(rawLeftX), 0.95f, maxLaneDistanceFromCenter);
        float worldLeftX = -clampedDistance;
        float worldRightX = clampedDistance;

        if (hitPointL != null) hitPointL.position = new Vector3(worldLeftX, hitPointL.position.y, 0f);
        if (hitPointR != null) hitPointR.position = new Vector3(worldRightX, hitPointR.position.y, 0f);

        if (spawnPointL != null) spawnPointL.position = new Vector3(worldLeftX, spawnPointL.position.y, 0f);
        if (spawnPointR != null) spawnPointR.position = new Vector3(worldRightX, spawnPointR.position.y, 0f);

        if (laneTrackL != null) laneTrackL.transform.position = new Vector3(worldLeftX, laneTrackL.transform.position.y, 0f);
        if (laneTrackR != null) laneTrackR.transform.position = new Vector3(worldRightX, laneTrackR.transform.position.y, 0f);
    }

    private float GetSongTime()
    {
        if (linkedAudioSource != null && linkedAudioSource.clip != null)
            return linkedAudioSource.time + chartAudioOffset;

        return Time.time - songStartTime;
    }

    /// <summary>
    /// Cổng nhận dữ liệu bài hát từ NTMan3MidiBridge (Giai đoạn 2)
    /// </summary>
    public void LoadExternalChart(List<NoteDataMan3> externalNotes, List<float> speedUpMarks, AudioSource audioSource, float offset)
    {
        StopAllCoroutines();
        foreach (var n in activeNotes)
        {
            if (n != null) Destroy(n.gameObject);
        }
        activeNotes.Clear();
        activeHoldLeft = null;
        activeHoldRight = null;

        chartNotes = new List<NoteDataMan3>(externalNotes);
        nextSpawnIndex = 0;
        linkedAudioSource = audioSource;
        chartAudioOffset = offset;
        songStartTime = Time.time;
        hasExternalChartLoaded = true;
        isGameOver = false;

        if (speedUpMarks != null)
        {
            foreach (float markTime in speedUpMarks)
            {
                if (markTime > 1.5f)
                    StartCoroutine(TriggerSpeedUpWarningRoutine(markTime - 1.5f));
            }
        }
    }

    private void GenerateDummyPatternChart()
    {
        chartNotes.Clear();
        float beat = 0.50f;
        float t = 1.8f;
        float normalSpeed = 1.25f;

        for (int loop = 0; loop < 3; loop++)
        {
            chartNotes.Add(new NoteDataMan3 { isLeftLane = true, noteType = Man3NoteType.Normal, hitTime = t, flightDuration = normalSpeed }); t += beat;
            chartNotes.Add(new NoteDataMan3 { isLeftLane = false, noteType = Man3NoteType.Normal, hitTime = t, flightDuration = normalSpeed }); t += beat;
            chartNotes.Add(new NoteDataMan3 { isLeftLane = true, noteType = Man3NoteType.Hold, hitTime = t, holdDuration = 0.6f, flightDuration = normalSpeed }); t += beat * 2.0f;
            chartNotes.Add(new NoteDataMan3 { isLeftLane = false, noteType = Man3NoteType.Buff, hitTime = t, flightDuration = normalSpeed }); t += beat * 1.5f;

            chartNotes.Add(new NoteDataMan3 { isLeftLane = true, noteType = Man3NoteType.Normal, hitTime = t, flightDuration = normalSpeed }); t += beat * 0.75f;
            chartNotes.Add(new NoteDataMan3 { isLeftLane = true, noteType = Man3NoteType.Normal, hitTime = t, flightDuration = normalSpeed }); t += beat * 0.75f;
            chartNotes.Add(new NoteDataMan3 { isLeftLane = true, noteType = Man3NoteType.Bomb, hitTime = t, flightDuration = normalSpeed }); t += beat;
            chartNotes.Add(new NoteDataMan3 { isLeftLane = true, noteType = Man3NoteType.Normal, hitTime = t, flightDuration = normalSpeed }); t += beat * 1.5f;

            chartNotes.Add(new NoteDataMan3 { isLeftLane = true, noteType = Man3NoteType.Double, hitTime = t, flightDuration = normalSpeed });
            chartNotes.Add(new NoteDataMan3 { isLeftLane = false, noteType = Man3NoteType.Double, hitTime = t, flightDuration = normalSpeed });
            t += beat * 2.0f;

            StartCoroutine(TriggerSpeedUpWarningRoutine(t - 1.5f));
            float fastSpeed = 0.85f;
            for (int i = 0; i < 8; i++)
            {
                bool left = (i % 2 == 0);
                chartNotes.Add(new NoteDataMan3 { isLeftLane = left, noteType = Man3NoteType.Normal, hitTime = t, flightDuration = fastSpeed });
                t += beat * 0.7f;
            }
            t += beat * 1.5f;
        }
    }

    private IEnumerator TriggerSpeedUpWarningRoutine(float triggerTime)
    {
        yield return new WaitUntil(() => GetSongTime() >= triggerTime);
        if (speedUpWarningUI != null)
        {
            speedUpWarningUI.SetActive(true);
            yield return new WaitForSeconds(1.2f);
            speedUpWarningUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (isGameOver) return;

        float now = GetSongTime();

        // 1. Sinh nốt rơi
        while (nextSpawnIndex < chartNotes.Count &&
               now >= (chartNotes[nextSpawnIndex].hitTime - chartNotes[nextSpawnIndex].flightDuration))
        {
            SpawnNote(chartNotes[nextSpawnIndex]);
            nextSpawnIndex++;
        }

        // 2. Kiểm tra hoàn thành nốt kéo dài (Hold) đang giữ
        CheckActiveHoldCompletion(ref activeHoldLeft, true, now);
        CheckActiveHoldCompletion(ref activeHoldRight, false, now);

        // 3. Thu hồi kích thước vòng tròn mục tiêu
        if (hitPointL != null)
            hitPointL.localScale = Vector3.Lerp(hitPointL.localScale, baseHitPointScaleL, 20f * Time.deltaTime);
        if (hitPointR != null)
            hitPointR.localScale = Vector3.Lerp(hitPointR.localScale, baseHitPointScaleR, 20f * Time.deltaTime);

        // 4. Đếm ngược Buff x2
        if (buffRemainingTime > 0f)
        {
            buffRemainingTime -= Time.deltaTime;
            if (buffAuraVfx != null) buffAuraVfx.SetActive(buffRemainingTime > 0f);
        }

        // 5. Giả lập phím A và D (cả nhấn xuống và nhả phím cho Hold)
        if (Input.GetKeyDown(KeyCode.A)) OnPressLeftButton();
        if (Input.GetKeyUp(KeyCode.A)) OnReleaseLeftButton();

        if (Input.GetKeyDown(KeyCode.D)) OnPressRightButton();
        if (Input.GetKeyUp(KeyCode.D)) OnReleaseRightButton();
    }

    private void CheckActiveHoldCompletion(ref NTVerticalNoteMan3 holdNote, bool isLeft, float now)
    {
        if (holdNote == null) return;

        if (now >= holdNote.hitTime + holdNote.holdDuration)
        {
            Vector3 pos = isLeft ? hitPointL.position : hitPointR.position;
            SpawnVfx(perfectHitVfxPrefab, pos);
            RegisterHit(HitJudgment.Perfect, 200);
            holdNote.ConsumeNote();
            holdNote = null;
        }
    }

    private void SpawnNote(NoteDataMan3 data)
    {
        if (spawnPointL == null || spawnPointR == null || hitPointL == null || hitPointR == null) return;

        Vector3 spawnPos = data.isLeftLane ? spawnPointL.position : spawnPointR.position;
        Vector3 targetPos = data.isLeftLane ? hitPointL.position : hitPointR.position;

        GameObject prefabToUse = data.isLeftLane ? noteLeftPrefab : noteRightPrefab;
        if (data.noteType == Man3NoteType.Hold && holdNotePrefab != null) prefabToUse = holdNotePrefab;
        else if (data.noteType == Man3NoteType.Double && doubleNotePrefab != null) prefabToUse = doubleNotePrefab;
        else if (data.noteType == Man3NoteType.Bomb && bombPrefab != null) prefabToUse = bombPrefab;
        else if (data.noteType == Man3NoteType.Buff && buffPrefab != null) prefabToUse = buffPrefab;

        if (prefabToUse == null) return;

        GameObject obj = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
        NTVerticalNoteMan3 note = obj.GetComponent<NTVerticalNoteMan3>();
        if (note == null) note = obj.AddComponent<NTVerticalNoteMan3>();

        note.Init(
            data.isLeftLane,
            data.noteType,
            spawnPos,
            targetPos,
            data.hitTime - data.flightDuration,
            data.hitTime,
            data.holdDuration,
            GetSongTime,
            HandleNoteMissed
        );

        activeNotes.Add(note);
    }

    // =========================================================================
    // SỰ KIỆN CHẠM & NHẤC TAY (GẮN VÀO POINTER DOWN & POINTER UP CỦA BTNLEFT/RIGHT)
    // =========================================================================

    public void OnPressLeftButton()
    {
        if (isGameOver) return;

        lastLeftPressTime = Time.time;
        FaceDirection(true);
        TriggerMicroLunge(-punchLungeDistance);
        if (hitPointL != null)
        {
            hitPointL.localScale = baseHitPointScaleL * 1.28f;
            SpawnVfx(slashTrailVfxPrefab, hitPointL.position, true);
        }
        HighlightLane(true);

        if (Time.time - lastRightPressTime <= chordBufferWindow)
            EvaluateDoubleHit();
        else
            EvaluateSingleHit(true);
    }

    public void OnReleaseLeftButton()
    {
        HandleHoldRelease(ref activeHoldLeft, true);
    }

    public void OnPressRightButton()
    {
        if (isGameOver) return;

        lastRightPressTime = Time.time;
        FaceDirection(false);
        TriggerMicroLunge(punchLungeDistance);
        if (hitPointR != null)
        {
            hitPointR.localScale = baseHitPointScaleR * 1.28f;
            SpawnVfx(slashTrailVfxPrefab, hitPointR.position, false);
        }
        HighlightLane(false);

        if (Time.time - lastLeftPressTime <= chordBufferWindow)
            EvaluateDoubleHit();
        else
            EvaluateSingleHit(false);
    }

    public void OnReleaseRightButton()
    {
        HandleHoldRelease(ref activeHoldRight, false);
    }

    private void HandleHoldRelease(ref NTVerticalNoteMan3 holdNote, bool isLeft)
    {
        if (holdNote == null) return;

        float now = GetSongTime();
        float endTime = holdNote.hitTime + holdNote.holdDuration;

        // Nếu người chơi nhấc tay sớm hơn 0.18s trước khi kết thúc nốt Hold -> Tính MISS
        if (now < endTime - goodWindow)
        {
            holdNote.ConsumeNote();
            holdNote = null;
            RegisterMiss($"NHẢ SỚM NỐT GIỮ {(isLeft ? "TRÁI" : "PHẢI")}", missHealthPenalty);
        }
        else
        {
            Vector3 pos = isLeft ? hitPointL.position : hitPointR.position;
            SpawnVfx(goodHitVfxPrefab, pos);
            RegisterHit(HitJudgment.Good, 150);
            holdNote.ConsumeNote();
            holdNote = null;
        }
    }

    private void EvaluateSingleHit(bool isLeft)
    {
        PlayNextNormalAttackImmediate();

        float now = GetSongTime();
        activeNotes.RemoveAll(n => n == null);

        NTVerticalNoteMan3 closestNote = null;
        float minDiff = float.MaxValue;

        foreach (var note in activeNotes)
        {
            // Bỏ qua nốt Double để không ăn nhầm 1 bên trước khi ngón thứ 2 chạm xuống
            if (note.isLeftLane != isLeft || note.noteType == Man3NoteType.Double || note.isBeingHeld) continue;

            float diff = Mathf.Abs(now - note.hitTime);
            if (diff <= earlyMissWindow && diff < minDiff)
            {
                minDiff = diff;
                closestNote = note;
            }
        }

        if (closestNote == null) return;

        Vector3 hitPos = isLeft ? hitPointL.position : hitPointR.position;

        // Xử lý Bẫy Bom
        if (closestNote.noteType == Man3NoteType.Bomb)
        {
            if (minDiff <= goodWindow)
            {
                closestNote.ConsumeNote();
                SpawnVfx(bombExplosionVfxPrefab, hitPos);
                PlayStateSafe(hurtState);
                onBombHitCameraShake?.Invoke();
                RegisterMiss("DÍNH BOM!", bombHealthPenalty);
            }
            return;
        }

        // Bấm quá sớm ngoài vùng Good
        if (minDiff > goodWindow)
        {
            closestNote.ConsumeNote();
            RegisterMiss("BẤM SỚM!", missHealthPenalty);
            return;
        }

        // Xử lý Nốt Kéo Dài (Hold)
        if (closestNote.noteType == Man3NoteType.Hold)
        {
            closestNote.StartHold();
            if (isLeft) activeHoldLeft = closestNote;
            else activeHoldRight = closestNote;

            SpawnVfx(goodHitVfxPrefab, hitPos);
            RegisterHit(minDiff <= perfectWindow ? HitJudgment.Perfect : HitJudgment.Good, 100);
            return;
        }

        // Kích hoạt Buff x2
        if (closestNote.noteType == Man3NoteType.Buff)
        {
            buffRemainingTime = 5.0f;
        }

        if (minDiff <= perfectWindow)
        {
            SpawnVfx(perfectHitVfxPrefab, hitPos);
            RegisterHit(HitJudgment.Perfect, 150);
        }
        else
        {
            SpawnVfx(goodHitVfxPrefab, hitPos);
            RegisterHit(HitJudgment.Good, 100);
        }

        closestNote.ConsumeNote();
    }

    private void EvaluateDoubleHit()
    {
        TriggerMicroLunge(0f);
        PlayStateSafe(doubleAttackState);

        float now = GetSongTime();
        activeNotes.RemoveAll(n => n == null);

        NTVerticalNoteMan3 leftDouble = null;
        NTVerticalNoteMan3 rightDouble = null;
        float avgDiff = 0f;

        foreach (var note in activeNotes)
        {
            if (note.noteType != Man3NoteType.Double) continue;
            float diff = Mathf.Abs(now - note.hitTime);
            if (diff <= goodWindow)
            {
                if (note.isLeftLane) { leftDouble = note; avgDiff += diff; }
                else { rightDouble = note; avgDiff += diff; }
            }
        }

        if (leftDouble != null && rightDouble != null)
        {
            avgDiff *= 0.5f;
            onDoubleHitCameraShake?.Invoke();

            if (avgDiff <= perfectWindow)
            {
                if (hitPointL != null) SpawnVfx(perfectHitVfxPrefab, hitPointL.position);
                if (hitPointR != null) SpawnVfx(perfectHitVfxPrefab, hitPointR.position);
                RegisterHit(HitJudgment.Perfect, 300);
            }
            else
            {
                if (hitPointL != null) SpawnVfx(goodHitVfxPrefab, hitPointL.position);
                if (hitPointR != null) SpawnVfx(goodHitVfxPrefab, hitPointR.position);
                RegisterHit(HitJudgment.Good, 200);
            }

            leftDouble.ConsumeNote();
            rightDouble.ConsumeNote();
        }
    }

    private void HandleNoteMissed(NTVerticalNoteMan3 missedNote)
    {
        if (missedNote == activeHoldLeft) activeHoldLeft = null;
        if (missedNote == activeHoldRight) activeHoldRight = null;

        RegisterMiss($"TRƯỢT NHỊP {(missedNote.isLeftLane ? "TRÁI" : "PHẢI")}", missHealthPenalty);
    }

    private void RegisterHit(HitJudgment judgment, int basePoints)
    {
        int multiplier = (buffRemainingTime > 0f) ? 2 : 1;
        currentCombo++;
        currentScore += basePoints * multiplier;

        string label = (judgment == HitJudgment.Perfect) ? "PERFECT!" : "GOOD!";
        ShowJudgmentFeedback(label, judgment == HitJudgment.Perfect ? Color.green : Color.yellow);
        UpdateAllUI();
    }

    private void RegisterMiss(string reason, float healthLoss)
    {
        currentCombo = 0;
        currentHealth = Mathf.Max(0f, currentHealth - healthLoss);

        ShowJudgmentFeedback("MISS", Color.red);
        UpdateAllUI();

        if (currentHealth <= 0f && !isGameOver)
        {
            isGameOver = true;
            if (linkedAudioSource != null) linkedAudioSource.Stop();
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }
    }

    private void UpdateAllUI()
    {
        if (scoreText != null) scoreText.text = currentScore.ToString();
        if (comboText != null) comboText.text = currentCombo > 0 ? $"x{currentCombo}" : "";
        if (healthSlider != null) healthSlider.value = currentHealth / maxHealth;
        if (healthFillImage != null) healthFillImage.fillAmount = currentHealth / maxHealth;
    }

    private void ShowJudgmentFeedback(string text, Color color)
    {
        if (judgmentText == null) return;
        judgmentText.text = text;
        judgmentText.color = color;
    }

    private void SpawnVfx(GameObject vfxPrefab, Vector3 position, bool flipX = false)
    {
        if (vfxPrefab == null) return;
        GameObject vfx = Instantiate(vfxPrefab, position, Quaternion.identity);
        if (flipX)
        {
            Vector3 s = vfx.transform.localScale;
            s.x = -Mathf.Abs(s.x);
            vfx.transform.localScale = s;
        }
    }

    private void PlayNextNormalAttackImmediate()
    {
        if (normalAttackStates == null || normalAttackStates.Length == 0) return;
        string stateName = normalAttackStates[attackComboStep % normalAttackStates.Length];
        attackComboStep++;
        PlayStateSafe(stateName);
    }

    private void PlayStateSafe(string stateName)
    {
        if (playerAnimator == null || string.IsNullOrEmpty(stateName)) return;
        int stateHash = Animator.StringToHash(stateName);
        if (playerAnimator.HasState(0, stateHash))
        {
            playerAnimator.Play(stateHash, 0, 0f);
        }
    }

    private void FaceDirection(bool faceLeft)
    {
        if (playerTransform == null) return;
        Vector3 s = playerTransform.localScale;
        float absX = Mathf.Abs(s.x);

        if (spriteDefaultFacesRight)
            s.x = faceLeft ? -absX : absX;
        else
            s.x = faceLeft ? absX : -absX;

        playerTransform.localScale = s;
    }

    private void HighlightLane(bool isLeft)
    {
        if (isLeft)
        {
            if (laneTrackL == null) return;
            if (leftTrackCoroutine != null) StopCoroutine(leftTrackCoroutine);
            leftTrackCoroutine = StartCoroutine(FadeTrackRoutine(laneTrackL));
        }
        else
        {
            if (laneTrackR == null) return;
            if (rightTrackCoroutine != null) StopCoroutine(rightTrackCoroutine);
            rightTrackCoroutine = StartCoroutine(FadeTrackRoutine(laneTrackR));
        }
    }

    private IEnumerator FadeTrackRoutine(SpriteRenderer track)
    {
        Color c = track.color;
        c.a = 0.55f;
        track.color = c;
        while (track.color.a > 0.16f)
        {
            c.a -= 2.5f * Time.deltaTime;
            track.color = c;
            yield return null;
        }
    }

    private void TriggerMicroLunge(float targetOffsetX)
    {
        if (playerTransform == null) return;
        if (activeLungeCoroutine != null) StopCoroutine(activeLungeCoroutine);
        activeLungeCoroutine = StartCoroutine(MicroLungeRoutine(targetOffsetX));
    }

    private IEnumerator MicroLungeRoutine(float peakOffsetX)
    {
        float startX = playerTransform.position.x;
        float elapsed = 0f;

        while (elapsed < lungeForwardTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lungeForwardTime);
            float easeOut = 1f - Mathf.Pow(1f - t, 2f);
            playerTransform.position = new Vector3(
                Mathf.Lerp(startX, peakOffsetX, easeOut),
                playerTransform.position.y,
                playerTransform.position.z
            );
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < lungeRecoverTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lungeRecoverTime);
            playerTransform.position = new Vector3(
                Mathf.Lerp(peakOffsetX, 0f, t),
                playerTransform.position.y,
                playerTransform.position.z
            );
            yield return null;
        }

        playerTransform.position = new Vector3(0f, playerTransform.position.y, playerTransform.position.z);
        activeLungeCoroutine = null;
    }
}