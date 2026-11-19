#if false
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

public enum NTMan3NoteType
{
    Normal,
    Hold,
    Double,
    Bomb,
    Buff,
    Coin,
    Note1,
    NoteMan1,
    NoteMan2,
    NoteMan3_1,
    NoteMan3_2,
    NoteMan3_3
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

    [Header("4. Prefabs Các Loại Nốt Cơ Bản & Đặc Biệt")]
    [SerializeField] private GameObject noteLeftPrefab;
    [SerializeField] private GameObject noteRightPrefab;
    [SerializeField] private GameObject holdNotePrefab;
    [SerializeField] private GameObject doubleNotePrefab;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private GameObject buffPrefab;
    [SerializeField] private GameObject coinPrefab;
    
    [Header("--- 6 Prefabs Nốt Đại Dương Mới (Random Theo Nhịp MIDI) ---")]
    [SerializeField] private GameObject prefabNote1;
    [SerializeField] private GameObject prefabNoteMan1;
    [SerializeField] private GameObject prefabNoteMan2;
    [SerializeField] private GameObject prefabNoteMan3_1;
    [SerializeField] private GameObject prefabNoteMan3_2;
    [SerializeField] private GameObject prefabNoteMan3_3;

    [Header("5. VFX Hiệu Ứng Va Chạm")]
    [SerializeField] private GameObject perfectHitVfxPrefab;
    [SerializeField] private GameObject goodHitVfxPrefab;
    [SerializeField] private GameObject bombExplosionVfxPrefab;
    [SerializeField] private GameObject slashTrailVfxPrefab;
    [SerializeField] private GameObject buffAuraVfx;

    [Header("6. Giao Diện UI & Bảng Tổng Kết (Result Panel)")]
    [SerializeField] private Image judgmentImage; 
    [SerializeField] private Sprite[] judgmentSprites; 
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private GameObject readyBannerUI;
    [SerializeField] private GameObject resultPanel; 
    [SerializeField] private GameObject speedUpWarningUI;

    [Header("7. Sự Kiện Rung Camera")]
    public UnityEvent onDoubleHitCameraShake;
    public UnityEvent onBombHitCameraShake;

    [Header("8. Thông Số Chấm Nhịp (Đã tối ưu độ nhạy)")]
    [SerializeField] private float perfectWindow = 0.09f;   
    [SerializeField] private float goodWindow = 0.18f;      
    [SerializeField] private float badWindow = 0.25f;       
    [SerializeField] private float earlyMissWindow = 0.35f; 
    [SerializeField] private float chordBufferWindow = 0.07f;

    [Header("9. Thưởng Điểm Fever Coins")]
    [SerializeField] private int coinBaseScore = 25;

    [Header("10. Hệ Thống Âm Thanh Riêng Cho Từng Item & Note (Muse Dash Style)")]
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip normalHitClip;   
    [SerializeField] private AudioClip holdHitClip;     
    [SerializeField] private AudioClip doubleHitClip;   
    [SerializeField] private AudioClip buffHitClip;     
    [SerializeField] private AudioClip bombHitClip;     
    [SerializeField] private AudioClip coinPickupClip;  

    private Camera mainCam;
    private float songStartTime;
    private int nextSpawnIndex = 0;
    private int attackComboStep = 0;
    
    private bool isGameStarted = false;
    private bool isSongFinished = false;

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

    private void Awake()
    {
        mainCam = Camera.main;
        if (autoAlignOnStart) AlignLanesToScreen();

        if (hitPointL != null) baseHitPointScaleL = hitPointL.localScale;
        if (hitPointR != null) baseHitPointScaleR = hitPointR.localScale;
    }

    private void Start()
    {
        currentScore = 0;
        currentCombo = 0;
        UpdateAllUI();

        if (resultPanel != null) resultPanel.SetActive(false);
        if (judgmentImage != null) judgmentImage.gameObject.SetActive(false);

        if (playerTransform != null)
            playerTransform.position = new Vector3(0f, playerTransform.position.y, playerTransform.position.z);

        if (readyBannerUI != null)
            StartCoroutine(HideReadyBannerRoutine());

        StartCoroutine(StartGameDelayRoutine());

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

    private IEnumerator StartGameDelayRoutine()
    {
        yield return new WaitForSeconds(1.3f);
        isGameStarted = true;
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
        if (linkedAudioSource != null && linkedAudioSource.clip != null && linkedAudioSource.isPlaying)
        {
            return linkedAudioSource.time + chartAudioOffset;
        }

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
        isSongFinished = false;
        isGameStarted = false;
        StartCoroutine(StartGameDelayRoutine());
    }

    private void GenerateDummyPatternChart()
    {
        chartNotes.Clear();
        float beat = 0.50f;
        float t = 1.8f;
        float normalSpeed = 1.25f;

        for (int loop = 0; loop < 3; loop++)
        {
            chartNotes.Add(new NoteDataMan3 { isLeftLane = true, noteType = NTMan3NoteType.Normal, hitTime = t, flightDuration = normalSpeed }); t += beat;
            chartNotes.Add(new NoteDataMan3 { isLeftLane = false, noteType = NTMan3NoteType.Normal, hitTime = t, flightDuration = normalSpeed }); t += beat;
        }
    }

    private void Update()
    {
        if (isSongFinished) return;

        float now = GetSongTime();

        while (nextSpawnIndex < chartNotes.Count &&
                now >= (chartNotes[nextSpawnIndex].hitTime - chartNotes[nextSpawnIndex].flightDuration))
        {
            SpawnNote(chartNotes[nextSpawnIndex]);
            nextSpawnIndex++;
        }

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var note = activeNotes[i];
            if (note == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }

            if (!note.isBeingHeld && note.noteType != NTMan3NoteType.Bomb && note.noteType != NTMan3NoteType.Coin)
            {
                if (now > note.hitTime + 0.35f) 
                {
                    note.ConsumeNote();
                    activeNotes.RemoveAt(i);
                    RegisterMiss(HitJudgment.Miss);
                }
            }
        }

        CheckActiveHoldCompletion(ref activeHoldLeft, true, now);
        CheckActiveHoldCompletion(ref activeHoldRight, false, now);

        if (hasExternalChartLoaded && nextSpawnIndex >= chartNotes.Count && activeNotes.Count == 0)
        {
            if (linkedAudioSource != null && !linkedAudioSource.isPlaying && now > 1.0f)
            {
                TriggerSongComplete();
            }
            else if (linkedAudioSource == null && now > 5.0f)
            {
                TriggerSongComplete();
            }
        }

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

    private void TriggerSongComplete()
    {
        if (isSongFinished) return;
        isSongFinished = true;

        if (linkedAudioSource != null) linkedAudioSource.Stop();

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }
    }

    private void CheckActiveHoldCompletion(ref NTVerticalNoteMan3 holdNote, bool isLeft, float now)
    {
        if (holdNote == null) return;

        if (now >= holdNote.hitTime + holdNote.holdDuration)
        {
            Vector3 pos = isLeft ? hitPointL.position : hitPointR.position;
            SpawnVfx(perfectHitVfxPrefab, pos);
            RegisterHit(HitJudgment.Perfect, 200);
            PlaySfx(holdHitClip);

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

        switch (data.noteType)
        {
            case NTMan3NoteType.Hold: if (holdNotePrefab != null) prefabToUse = holdNotePrefab; break;
            case NTMan3NoteType.Double: if (doubleNotePrefab != null) prefabToUse = doubleNotePrefab; break;
            case NTMan3NoteType.Bomb: if (bombPrefab != null) prefabToUse = bombPrefab; break;
            case NTMan3NoteType.Buff: if (buffPrefab != null) prefabToUse = buffPrefab; break;
            case NTMan3NoteType.Coin: if (coinPrefab != null) prefabToUse = coinPrefab; break;
            case NTMan3NoteType.Note1: if (prefabNote1 != null) prefabToUse = prefabNote1; break;
            case NTMan3NoteType.NoteMan1: if (prefabNoteMan1 != null) prefabToUse = prefabNoteMan1; break;
            case NTMan3NoteType.NoteMan2: if (prefabNoteMan2 != null) prefabToUse = prefabNoteMan2; break;
            case NTMan3NoteType.NoteMan3_1: if (prefabNoteMan3_1 != null) prefabToUse = prefabNoteMan3_1; break;
            case NTMan3NoteType.NoteMan3_2: if (prefabNoteMan3_2 != null) prefabToUse = prefabNoteMan3_2; break;
            case NTMan3NoteType.NoteMan3_3: if (prefabNoteMan3_3 != null) prefabToUse = prefabNoteMan3_3; break;
        }

        if (prefabToUse == null) 
        {
            prefabToUse = data.isLeftLane ? noteLeftPrefab : noteRightPrefab;
            if (prefabToUse == null) return;
        }

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
        if (isSongFinished) return;
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
        if (isSongFinished) return;
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
            RegisterMiss(HitJudgment.Miss);
        }
        else
        {
            Vector3 pos = isLeft ? hitPointL.position : hitPointR.position;
            SpawnVfx(goodHitVfxPrefab, pos);
            RegisterHit(HitJudgment.Good, 150);
            PlaySfx(holdHitClip);
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
            if (note.isLeftLane != isLeft || note.noteType == NTMan3NoteType.Double || note.isBeingHeld) continue;

            float diff = Mathf.Abs(now - note.hitTime);
            if (diff <= earlyMissWindow && diff < minDiff)
            {
                minDiff = diff;
                closestNote = note;
            }
        }

        if (closestNote == null) return;
        Vector3 hitPos = isLeft ? hitPointL.position : hitPointR.position;

        if (closestNote.noteType == NTMan3NoteType.Bomb)
        {
            if (minDiff <= goodWindow)
            {
                closestNote.ConsumeNote();
                SpawnVfx(bombExplosionVfxPrefab, hitPos);
                PlayStateSafe(hurtState);
                onBombHitCameraShake?.Invoke();
                PlaySfx(bombHitClip);
                RegisterMiss(HitJudgment.Miss);
            }
            return;
        }

        if (closestNote.noteType == NTMan3NoteType.Coin)
        {
            if (minDiff <= goodWindow)
            {
                int multiplier = (buffRemainingTime > 0f) ? 2 : 1;
                int earned = coinBaseScore * multiplier;
                currentCombo++;
                currentScore += earned;

                PlaySfx(coinPickupClip);
                SpawnVfx(goodHitVfxPrefab, hitPos);
                ShowJudgmentAsset(HitJudgment.Good);
                UpdateAllUI();
                closestNote.ConsumeNote();
            }
            return;
        }

        if (minDiff > badWindow)
        {
            closestNote.ConsumeNote();
            RegisterMiss(HitJudgment.Bad);
            return;
        }

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

        if (closestNote.noteType == NTMan3NoteType.Hold)
        {
            closestNote.StartHold();
            if (isLeft) activeHoldLeft = closestNote;
            else activeHoldRight = closestNote;

            PlaySfx(holdHitClip);
            RegisterHit(judgment, basePoints);
            return;
        }
        else if (closestNote.noteType == NTMan3NoteType.Buff)
        {
            buffRemainingTime = 5.0f;
            PlaySfx(buffHitClip);
        }
        else
        {
            PlaySfx(normalHitClip);
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
            if (note.noteType != NTMan3NoteType.Double) continue;
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

            PlaySfx(doubleHitClip);

            leftDouble.ConsumeNote();
            rightDouble.ConsumeNote();
            RegisterHit(judgment, points);
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip, 1.0f);
        }
    }

    private void HandleNoteMissed(NTVerticalNoteMan3 missedNote)
    {
        if (missedNote == null) return;

        if (missedNote.noteType == NTMan3NoteType.Bomb || missedNote.noteType == NTMan3NoteType.Coin) 
            return;

        if (missedNote == activeHoldLeft) activeHoldLeft = null;
        if (missedNote == activeHoldRight) activeHoldRight = null;

        RegisterMiss(HitJudgment.Miss);
    }

    private void RegisterHit(HitJudgment judgment, int basePoints)
    {
        int multiplier = (buffRemainingTime > 0f) ? 2 : 1;
        currentCombo++;
        currentScore += basePoints * multiplier;

        ShowJudgmentAsset(judgment);
        UpdateAllUI();
    }

    private void RegisterMiss(HitJudgment judgment)
    {
        if (!isGameStarted || isSongFinished) return;

        currentCombo = 0;
        ShowJudgmentAsset(judgment);
        UpdateAllUI();
    }

    private void ShowJudgmentAsset(HitJudgment judgment)
    {
        if (judgmentImage == null || judgmentSprites == null || judgmentSprites.Length < 5) return;

        int index = 0;
        switch (judgment)
        {
            case HitJudgment.Perfect: index = 0; break;
            case HitJudgment.Good: index = 1; break;
            case HitJudgment.Bad: index = 3; break;
            case HitJudgment.Miss: index = 4; break;
        }

        if (judgmentSprites[index] != null)
        {
            judgmentImage.sprite = judgmentSprites[index];
            judgmentImage.gameObject.SetActive(true);

            StopAllCoroutines();
            StartCoroutine(AnimateJudgmentPopup());
        }
    }

    private IEnumerator AnimateJudgmentPopup()
    {
        RectTransform rect = judgmentImage.rectTransform;
        CanvasGroup canvasGroup = judgmentImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = judgmentImage.gameObject.AddComponent<CanvasGroup>();

        rect.sizeDelta = new Vector2(140f, 50f);

        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scale = Mathf.Lerp(0.6f, 1.0f, Mathf.Sin(t * Mathf.PI * 0.5f));
            rect.localScale = new Vector3(scale, scale, 1f);

            canvasGroup.alpha = Mathf.Lerp(1.0f, 0f, t);

            yield return null;
        }

        judgmentImage.gameObject.SetActive(false);
    }

    private void UpdateAllUI()
    {
        if (scoreText != null) scoreText.text = currentScore.ToString("D7");
        if (comboText != null) comboText.text = currentCombo > 1 ? $"x{currentCombo}" : "";
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
#endif