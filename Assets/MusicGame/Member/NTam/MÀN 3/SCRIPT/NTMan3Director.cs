using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public enum HitJudgment
{
    Perfect,
    Good,
    Bad,
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
    [SerializeField] private string[] normalAttackStates = { "Hit", "Hit2", "Kick" };
    [SerializeField] private string doubleAttackState = "SwingTHand";
    [SerializeField] private string hurtState = "Hurt";

    [Header("4. Prefabs Các Loại Nốt")]
    [SerializeField] private GameObject noteLeftPrefab;
    [SerializeField] private GameObject noteRightPrefab;
    [SerializeField] private GameObject holdNotePrefab;
    [SerializeField] private GameObject doubleNotePrefab;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private GameObject buffPrefab;
    [SerializeField] private GameObject coinPrefab;

    [Header("5. VFX Hiệu Ứng Va Chạm")]
    [SerializeField] private GameObject perfectHitVfxPrefab;
    [SerializeField] private GameObject goodHitVfxPrefab;
    [SerializeField] private GameObject bombExplosionVfxPrefab;
    [SerializeField] private GameObject slashTrailVfxPrefab;
    [SerializeField] private GameObject buffAuraVfx;

    [Header("6. Giao Diện UI & Judgment Image Assets (Dùng Asset Hình Ảnh)")]
    [SerializeField] private Image judgmentImage; // Kéo đối tượng JudgmentPopup vào đây
    [SerializeField] private Sprite[] judgmentSprites; // [0]: Perfect, [1]: Great, [2]: Good, [3]: Bad, [4]: Miss
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private GameObject readyBannerUI;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject speedUpWarningUI;

    [Header("7. Sự Kiện Rung Camera")]
    public UnityEvent onDoubleHitCameraShake;
    public UnityEvent onBombHitCameraShake;

    [Header("8. Thông Số Chấm Nhịp & Máu")]
    [SerializeField] private float perfectWindow = 0.08f;
    [SerializeField] private float goodWindow = 0.15f;
    [SerializeField] private float badWindow = 0.22f; // Ngưỡng Bad mới
    [SerializeField] private float earlyMissWindow = 0.30f;
    [SerializeField] private float chordBufferWindow = 0.07f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float missHealthPenalty = 10f;
    [SerializeField] private float bombHealthPenalty = 20f;

    [Header("9. Thưởng & Âm Thanh Fever Coins")]
    [SerializeField] private int coinBaseScore = 25;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip coinPickupClip;

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

        if (judgmentImage != null) judgmentImage.gameObject.SetActive(false);

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
        }
    }

    private void Update()
    {
        if (isGameOver) return;

        float now = GetSongTime();

        while (nextSpawnIndex < chartNotes.Count &&
                now >= (chartNotes[nextSpawnIndex].hitTime - chartNotes[nextSpawnIndex].flightDuration))
        {
            SpawnNote(chartNotes[nextSpawnIndex]);
            nextSpawnIndex++;
        }

        CheckActiveHoldCompletion(ref activeHoldLeft, true, now);
        CheckActiveHoldCompletion(ref activeHoldRight, false, now);

        if (hitPointL != null)
            hitPointL.localScale = Vector3.Lerp(hitPointL.localScale, baseHitPointScaleL, 20f * Time.deltaTime);
        if (hitPointR != null)
            hitPointR.localScale = Vector3.Lerp(hitPointR.localScale, baseHitPointScaleR, 20f * Time.deltaTime);

        if (buffRemainingTime > 0f)
        {
            buffRemainingTime -= Time.deltaTime;
            if (buffAuraVfx != null) buffAuraVfx.SetActive(buffRemainingTime > 0f);
        }

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
        else if (data.noteType == Man3NoteType.Coin && coinPrefab != null) prefabToUse = coinPrefab;

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

        if (Time.time - lastRightPressTime <= chordBufferWindow) EvaluateDoubleHit();
        else EvaluateSingleHit(true);
    }

    public void OnReleaseLeftButton() { HandleHoldRelease(ref activeHoldLeft, true); }
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

        if (Time.time - lastLeftPressTime <= chordBufferWindow) EvaluateDoubleHit();
        else EvaluateSingleHit(false);
    }

    public void OnReleaseRightButton() { HandleHoldRelease(ref activeHoldRight, false); }

    private void HandleHoldRelease(ref NTVerticalNoteMan3 holdNote, bool isLeft)
    {
        if (holdNote == null) return;
        float now = GetSongTime();
        float endTime = holdNote.hitTime + holdNote.holdDuration;

        if (now < endTime - goodWindow)
        {
            holdNote.ConsumeNote();
            holdNote = null;
            RegisterMiss(HitJudgment.Miss, missHealthPenalty);
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
                RegisterMiss(HitJudgment.Miss, bombHealthPenalty);
            }
            return;
        }

        // Xử lý Xu Nhịp Phụ (Coin)
        if (closestNote.noteType == Man3NoteType.Coin)
        {
            if (minDiff <= goodWindow)
            {
                int multiplier = (buffRemainingTime > 0f) ? 2 : 1;
                int earned = coinBaseScore * multiplier;
                currentCombo++;
                currentScore += earned;

                if (sfxAudioSource != null && coinPickupClip != null)
                    sfxAudioSource.PlayOneShot(coinPickupClip, 0.9f);

                SpawnVfx(goodHitVfxPrefab, hitPos);
                ShowJudgmentAsset(HitJudgment.Good);
                UpdateAllUI();
                closestNote.ConsumeNote();
            }
            return;
        }

        // Bấm quá sớm ngoài vùng Bad
        if (minDiff > badWindow)
        {
            closestNote.ConsumeNote();
            RegisterMiss(HitJudgment.Bad, missHealthPenalty);
            return;
        }

        // Phân định chuẩn Timing Window dựa trên khoảng lệch minDiff
        HitJudgment judgment;
        int basePoints = 100;

        if (minDiff <= perfectWindow)
        {
            judgment = HitJudgment.Perfect;
            basePoints = 150;
            SpawnVfx(perfectHitVfxPrefab, hitPos);
        }
        else if (minDiff <= goodWindow)
        {
            judgment = HitJudgment.Good;
            basePoints = 100;
            SpawnVfx(goodHitVfxPrefab, hitPos);
        }
        else
        {
            judgment = HitJudgment.Bad;
            basePoints = 50;
            SpawnVfx(goodHitVfxPrefab, hitPos);
        }

        // Xử lý riêng cho nốt Hold: Bật trạng thái giữ và THOÁT HÀM ngay
        if (closestNote.noteType == Man3NoteType.Hold)
        {
            closestNote.StartHold();
            if (isLeft) activeHoldLeft = closestNote;
            else activeHoldRight = closestNote;

            RegisterHit(judgment, basePoints);
            return; // Ngăn chặn gọi ConsumeNote() bên dưới
        }
        else if (closestNote.noteType == Man3NoteType.Buff)
        {
            buffRemainingTime = 5.0f;
        }

        closestNote.ConsumeNote();
        RegisterHit(judgment, basePoints);
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

            HitJudgment judgment = (avgDiff <= perfectWindow) ? HitJudgment.Perfect : HitJudgment.Good;
            int points = (judgment == HitJudgment.Perfect) ? 300 : 200;

            GameObject vfxToSpawn = (judgment == HitJudgment.Perfect) ? perfectHitVfxPrefab : goodHitVfxPrefab;
            if (hitPointL != null) SpawnVfx(vfxToSpawn, hitPointL.position);
            if (hitPointR != null) SpawnVfx(vfxToSpawn, hitPointR.position);

            leftDouble.ConsumeNote();
            rightDouble.ConsumeNote();
            RegisterHit(judgment, points);
        }
        
    }

    private void HandleNoteMissed(NTVerticalNoteMan3 missedNote)
    {
        if (missedNote.noteType == Man3NoteType.Bomb || missedNote.noteType == Man3NoteType.Coin) return;

        if (missedNote == activeHoldLeft) activeHoldLeft = null;
        if (missedNote == activeHoldRight) activeHoldRight = null;

        RegisterMiss(HitJudgment.Miss, missHealthPenalty);
    }

    private void RegisterHit(HitJudgment judgment, int basePoints)
    {
        int multiplier = (buffRemainingTime > 0f) ? 2 : 1;
        currentCombo++;
        currentScore += basePoints * multiplier;

        ShowJudgmentAsset(judgment);
        UpdateAllUI();
    }

    private void RegisterMiss(HitJudgment judgment, float healthLoss)
    {
        currentCombo = 0;
        currentHealth = Mathf.Max(0f, currentHealth - healthLoss);

        ShowJudgmentAsset(judgment);
        UpdateAllUI();

        if (currentHealth <= 0f && !isGameOver)
        {
            isGameOver = true;
            if (linkedAudioSource != null) linkedAudioSource.Stop();
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }
    }

    // =========================================================================
    // HỆ THỐNG HIỂN THỊ HÌNH ẢNH JUDGMENT (THAY THẾ TEXT)
    // =========================================================================
    private void ShowJudgmentAsset(HitJudgment judgment)
    {
        if (judgmentImage == null || judgmentSprites == null || judgmentSprites.Length < 5) return;

        int index = 0;
        switch (judgment)
        {
            case HitJudgment.Perfect: index = 0; break;
            case HitJudgment.Good: index = 1; break; // Hoặc Good (index 2)
            case HitJudgment.Bad: index = 3; break;
            case HitJudgment.Miss: index = 4; break;
        }

        if (judgmentSprites[index] != null)
        {
            judgmentImage.sprite = judgmentSprites[index];
            judgmentImage.gameObject.SetActive(true);

            // Kích hoạt coroutine hiệu ứng thu nhỏ/phóng to mượt mà
            StopAllCoroutines();
            StartCoroutine(AnimateJudgmentPopup());
        }
    }

    private IEnumerator AnimateJudgmentPopup()
    {
        RectTransform rect = judgmentImage.rectTransform;
        CanvasGroup canvasGroup = judgmentImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = judgmentImage.gameObject.AddComponent<CanvasGroup>();

        // Thiết lập kích thước nhỏ gọn vừa phải (ví dụ: Width = 140, Height = 50 thay vì 800+)
        rect.sizeDelta = new Vector2(140f, 50f);

        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Hiệu ứng Pop Scale nảy nhẹ từ nhỏ ra kích thước chuẩn
            float scale = Mathf.Lerp(0.6f, 1.0f, Mathf.Sin(t * Mathf.PI * 0.5f));
            rect.localScale = new Vector3(scale, scale, 1f);

            // Mờ dần về cuối (Fade out)
            canvasGroup.alpha = Mathf.Lerp(1.0f, 0f, t);

            yield return null;
        }

        judgmentImage.gameObject.SetActive(false);
    }

    private void UpdateAllUI()
    {
        if (scoreText != null) scoreText.text = currentScore.ToString();
        if (comboText != null) comboText.text = currentCombo > 0 ? $"x{currentCombo}" : "";
        if (healthSlider != null) healthSlider.value = currentHealth / maxHealth;
        if (healthFillImage != null) healthFillImage.fillAmount = currentHealth / maxHealth;
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