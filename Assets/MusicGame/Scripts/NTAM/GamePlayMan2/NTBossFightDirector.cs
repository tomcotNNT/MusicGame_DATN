using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NTBossFightDirector : MonoBehaviour
{
    public static event Action OnBossDefeated;

    public enum BossState 
    { 
        Inactive, 
        Phase1_Duel, 
        ShadowFalling, 
        RunningToPlayer, 
        Phase2_DuelLeft, 
        JumpingRight, 
        Phase2_DuelRight, 
        Defeated 
    }

    [Header("Current Status")]
    public BossState currentState = BossState.Inactive;

    [Header("Mode Configuration")]
    public bool autoTestWithoutMidi = true;
    [SerializeField] private float testFireInterval = 0.35f;
    [SerializeField] private float testFlightDuration = 1.2f;
    [SerializeField] private Transform testHitPointL;
    [SerializeField] private Transform testHitPointR;

    [Header("Attack Animation Sync")]
    [SerializeField] private float attackWindupDelay = 0.1f;

    [Header("1. Boss Thật Dưới Đất")]
    [SerializeField] private Transform groundStandPoint;
    [SerializeField] private Transform firePoint;

    [Header("2. Cái Bóng Trên Cây")]
    [SerializeField] private GameObject shadowObject;
    [SerializeField] private Animator shadowAnimator;
    [SerializeField] private SpriteRenderer shadowSpriteRenderer;
    [SerializeField] private Color shadowColor = new Color(0.6f, 0.2f, 1f, 0.55f);
    [SerializeField] private int phase1MaxHits = 4;
    private int phase1HitsTaken = 0;

    [Header("3. Checkpoints Cận Chiến Phase 2")]
    [SerializeField] private Transform meleeLeftPoint;  // Vị trí áp sát bên trái Player
    [SerializeField] private Transform meleeRightPoint; // Vị trí áp sát bên phải Player

    [Header("4. Nốt Nhạc & Quả Cầu Lửa Phase 1")]
    [SerializeField] private GameObject notePrefab;

    [Header("5. Phase 2: Combo Duel UI & Settings")]
    [Tooltip("Kéo Prefab chứa NTComboNoteItem vào đây")]
    [SerializeField] private GameObject comboNoteUIPrefab;
    [SerializeField] private Transform comboUIParent;
    [SerializeField] private Sprite noteSpriteL;
    [SerializeField] private Sprite noteSpriteR;
    [SerializeField] private GameObject meleeHitVfxPrefab; // Hiệu ứng chém trúng ngực Boss

    [Header("6. Dãy Phím Combo")]
    [SerializeField] private List<KeyCode> comboLeft = new List<KeyCode> { KeyCode.D, KeyCode.D, KeyCode.A, KeyCode.D, KeyCode.D, KeyCode.D, KeyCode.A };
    [SerializeField] private List<KeyCode> comboRight = new List<KeyCode> { KeyCode.A, KeyCode.A, KeyCode.D, KeyCode.A, KeyCode.A, KeyCode.A, KeyCode.D };
    private List<KeyCode> currentActiveCombo;
    private List<NTComboNoteItem> spawnedNoteItems = new List<NTComboNoteItem>();
    private int currentComboStep = 0;

    [Header("7. UI Health Bar & Screen")]
    [SerializeField] private Slider bossHealthSlider;
    [SerializeField] private GameObject winPanel;

    private Animator bossAnimator;
    private SpriteRenderer bossRenderer;
    private NTPlayerController2 playerRef;
    private Coroutine autoFireRoutine;
    private Coroutine bossMeleeRoutine;

   private void Awake()
    {
        bossAnimator = GetComponentInChildren<Animator>();
        bossRenderer = GetComponentInChildren<SpriteRenderer>();
        playerRef = FindFirstObjectByType<NTPlayerController2>();

        // Tự động tìm HitPointL và HitPointR nếu mang sang Scene mới mà chưa kéo tay
        if (testHitPointL == null)
        {
            GameObject hpL = GameObject.Find("HitPointL");
            if (hpL != null) testHitPointL = hpL.transform;
        }
        if (testHitPointR == null)
        {
            GameObject hpR = GameObject.Find("HitPointR");
            if (hpR != null) testHitPointR = hpR.transform;
        }

        // Tự động tìm khay Combo nếu chưa kéo
        if (comboUIParent == null)
        {
            GameObject comboBar = GameObject.Find("BossComboBar");
            if (comboBar != null) comboUIParent = comboBar.transform;
        }

        if (shadowObject != null)
        {
            if (shadowAnimator == null) shadowAnimator = shadowObject.GetComponentInChildren<Animator>();
            if (shadowSpriteRenderer == null) shadowSpriteRenderer = shadowObject.GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Start()
    {
        if (autoTestWithoutMidi)
        {
            StartBossBattle();
        }
        else
        {
            gameObject.SetActive(false);
            if (shadowObject != null) shadowObject.SetActive(false);
        }
    }

    public Transform GetShadowTarget()
    {
        return shadowObject != null ? shadowObject.transform : transform;
    }

    public void StartBossBattle()
    {
        gameObject.SetActive(true);
        currentState = BossState.Phase1_Duel;
        phase1HitsTaken = 0;

        if (groundStandPoint != null) transform.position = groundStandPoint.position;
        if (bossRenderer != null)
        {
            bossRenderer.color = Color.white;
            bossRenderer.flipX = false;
        }

        if (shadowObject != null)
        {
            shadowObject.SetActive(true);
            if (shadowSpriteRenderer != null) shadowSpriteRenderer.color = shadowColor;
            if (shadowAnimator != null) shadowAnimator.Play("Idle");
        }

        if (bossHealthSlider != null)
        {
            bossHealthSlider.gameObject.SetActive(true);
            bossHealthSlider.maxValue = phase1MaxHits + comboLeft.Count + comboRight.Count;
            bossHealthSlider.value = bossHealthSlider.maxValue;
        }

        if (autoTestWithoutMidi)
        {
            if (autoFireRoutine != null) StopCoroutine(autoFireRoutine);
            autoFireRoutine = StartCoroutine(AutoTestFiringLoop());
        }

        Debug.Log("<color=purple>[BOSS BATTLE]</color> Phase 1 bắt đầu!");
    }

    private IEnumerator AutoTestFiringLoop()
    {
        yield return new WaitForSeconds(1.0f);
        while (currentState == BossState.Phase1_Duel)
        {
            bool isLeft = UnityEngine.Random.value < 0.5f;
            Transform targetCp = isLeft ? testHitPointL : testHitPointR;

            if (targetCp != null)
            {
                FireShadowNote(isLeft, targetCp, Time.time + attackWindupDelay + testFlightDuration, () => Time.time);
            }
            yield return new WaitForSeconds(testFireInterval);
        }
    }

    public void FireShadowNote(bool isLeft, Transform targetCheckpoint, float hitTime, Func<float> songTimeProvider)
    {
        if (currentState != BossState.Phase1_Duel || notePrefab == null) return;
        StartCoroutine(PerformAttackAndSpawnNoteRoutine(isLeft, targetCheckpoint, hitTime, songTimeProvider));
    }

    private IEnumerator PerformAttackAndSpawnNoteRoutine(bool isLeft, Transform targetCheckpoint, float hitTime, Func<float> songTimeProvider)
    {
        string animState = isLeft ? "Attack1" : "Attack2";
        if (bossAnimator != null) bossAnimator.Play(animState, 0, 0f);

        if (attackWindupDelay > 0f) yield return new WaitForSeconds(attackWindupDelay);

        if (currentState != BossState.Phase1_Duel || notePrefab == null) yield break;

        Transform spawnPos = firePoint != null ? firePoint : transform;
        GameObject noteObj = Instantiate(notePrefab, spawnPos.position, Quaternion.identity);
        NTBossNoteMan2 bossNote = noteObj.GetComponent<NTBossNoteMan2>();

        if (bossNote != null)
        {
            bossNote.InitBossNote(this, isLeft, targetCheckpoint, songTimeProvider(), hitTime, songTimeProvider);
        }
    }

    public void OnReflectedHitLanded()
    {
        if (currentState != BossState.Phase1_Duel) return;

        phase1HitsTaken++;
        if (bossHealthSlider != null) bossHealthSlider.value--;

        if (shadowAnimator != null) shadowAnimator.SetTrigger("TakeHit");

        if (phase1HitsTaken >= phase1MaxHits)
        {
            if (autoFireRoutine != null) StopCoroutine(autoFireRoutine);
            StartCoroutine(ShadowMergeRoutine());
        }
    }

    private IEnumerator ShadowMergeRoutine()
    {
        currentState = BossState.ShadowFalling;

        if (shadowObject != null)
        {
            if (shadowAnimator != null) shadowAnimator.SetBool("IsFalling", true);

            Vector3 startDrop = shadowObject.transform.position;
            Vector3 endDrop = transform.position;
            float duration = 0.65f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                shadowObject.transform.position = Vector3.Lerp(startDrop, endDrop, elapsed / duration);
                yield return null;
            }

            shadowObject.SetActive(false);
        }

        yield return new WaitForSeconds(0.2f);

        // Chạy áp sát bên trái Player
        currentState = BossState.RunningToPlayer;
        if (bossAnimator != null) bossAnimator.SetBool("IsRunning", true);

        Vector3 endRun = meleeLeftPoint != null ? meleeLeftPoint.position : new Vector3(-1.6f, -1.6f, 0f);
        while (Vector3.Distance(transform.position, endRun) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, endRun, 8f * Time.deltaTime);
            yield return null;
        }

        transform.position = endRun;
        if (bossAnimator != null) bossAnimator.SetBool("IsRunning", false);

        // Bắt đầu đấu tay đôi Phase 2 (Trái)
        currentState = BossState.Phase2_DuelLeft;
        StartComboDuel(comboLeft);
    }

    // ==========================================
    // PHASE 2: ĐỐI KHÁNG CẬN CHIẾN & PHẢN ĐÒN
    // ==========================================

    private void StartComboDuel(List<KeyCode> comboList)
    {
        currentActiveCombo = comboList;
        currentComboStep = 0;

        RenderComboUI();

        // Kích hoạt vòng lặp Boss vung đòn dồn dập
        if (bossMeleeRoutine != null) StopCoroutine(bossMeleeRoutine);
        bossMeleeRoutine = StartCoroutine(BossContinuousMeleeLoop());
    }

    private IEnumerator BossContinuousMeleeLoop()
    {
        while (currentState == BossState.Phase2_DuelLeft || currentState == BossState.Phase2_DuelRight)
        {
            if (bossAnimator != null)
            {
                bossAnimator.SetTrigger(UnityEngine.Random.value < 0.5f ? "Attack1" : "Attack2");
            }
            yield return new WaitForSeconds(0.45f); // Nhịp vung đòn của Boss
        }
    }

    private void Update()
    {
        if (currentState != BossState.Phase2_DuelLeft && currentState != BossState.Phase2_DuelRight) return;

        // Bấm phím đối kháng
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ProcessPlayerInput(KeyCode.A);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            ProcessPlayerInput(KeyCode.D);
        }
    }

    private void ProcessPlayerInput(KeyCode pressedKey)
    {
        if (currentActiveCombo == null || currentComboStep >= currentActiveCombo.Count) return;

        // 1. KIỂM TRA BẤM ĐÚNG THỨ TỰ NỐT ĐANG SÁNG
        if (pressedKey == currentActiveCombo[currentComboStep])
        {
            // A. Player tung chiêu chém phản đòn về phía Boss
            bool bossIsOnLeft = (currentState == BossState.Phase2_DuelLeft);
            if (playerRef != null)
            {
                playerRef.PerformAttack(isLeft: bossIsOnLeft);
            }

            // B. Boss trúng đòn: Giật đòn TakeHit và trừ máu
            if (bossAnimator != null)
            {
                bossAnimator.ResetTrigger("Attack1");
                bossAnimator.ResetTrigger("Attack2");
                bossAnimator.SetTrigger("TakeHit");
            }

            if (bossHealthSlider != null) bossHealthSlider.value--;

            // C. Hiệu ứng chém tóe lửa ngực Boss & rung màn hình
            if (meleeHitVfxPrefab != null)
            {
                GameObject vfx = Instantiate(meleeHitVfxPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
                Destroy(vfx, 0.4f);
            }
            if (NTHitStopAndShake.Instance != null)
            {
                NTHitStopAndShake.Instance.TriggerHitImpact(0.06f, 0.1f, 0.03f);
            }

            // D. Tắt nốt vừa bấm và chuyển sáng sang nốt kế tiếp
            if (currentComboStep < spawnedNoteItems.Count)
            {
                spawnedNoteItems[currentComboStep].OnSuccessHit();
            }

            currentComboStep++;

            if (currentComboStep < spawnedNoteItems.Count)
            {
                spawnedNoteItems[currentComboStep].SetActive(); // BẬT SÁNG NỐT TIẾP THEO
            }
            else
            {
                // Hoàn thành trọn vẹn chuỗi Combo
                if (bossMeleeRoutine != null) StopCoroutine(bossMeleeRoutine);

                if (currentState == BossState.Phase2_DuelLeft)
                {
                    StartCoroutine(JumpToRightSideRoutine());
                }
                else if (currentState == BossState.Phase2_DuelRight)
                {
                    FinishBossBattle();
                }
            }
        }
        else
        {
            // Bấm sai: Đứt combo
            ComboSystem combo = FindFirstObjectByType<ComboSystem>();
            if (combo != null) combo.MissNote();
        }
    }

    private IEnumerator JumpToRightSideRoutine()
    {
        currentState = BossState.JumpingRight;
        ClearComboUI();

        if (bossAnimator != null) bossAnimator.SetTrigger("Jump");

        Vector3 startJump = transform.position;
        Vector3 endJump = meleeRightPoint != null ? meleeRightPoint.position : new Vector3(1.6f, -1.6f, 0f);
        float jumpDuration = 0.65f;
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            float arcHeight = Mathf.Sin(t * Mathf.PI) * 2.8f;
            Vector3 currentPos = Vector3.Lerp(startJump, endJump, t);
            currentPos.y += arcHeight;
            transform.position = currentPos;
            yield return null;
        }

        transform.position = endJump;
        if (bossRenderer != null) bossRenderer.flipX = true; // Quay mặt lại nhìn sang Player

        currentState = BossState.Phase2_DuelRight;
        StartComboDuel(comboRight);
    }

    private void FinishBossBattle()
    {
        currentState = BossState.Defeated;
        if (bossMeleeRoutine != null) StopCoroutine(bossMeleeRoutine);
        ClearComboUI();

        if (bossAnimator != null) bossAnimator.SetTrigger("Die");
        if (bossHealthSlider != null) bossHealthSlider.gameObject.SetActive(false);

        OnBossDefeated?.Invoke();
        if (winPanel != null) winPanel.SetActive(true);
        Debug.Log("<color=green>[VICTORY]</color> Kết liễu hoàn toàn Boss Màn 2!");
    }

    // ==========================================
    // KHỞI TẠO & ĐIỀU KHIỂN DÃY NỐT COMBO UI
    // ==========================================

    private void RenderComboUI()
    {
        ClearComboUI();
        if (comboNoteUIPrefab == null || comboUIParent == null) return;

        for (int i = 0; i < currentActiveCombo.Count; i++)
        {
            GameObject noteItemObj = Instantiate(comboNoteUIPrefab, comboUIParent);
            NTComboNoteItem item = noteItemObj.GetComponent<NTComboNoteItem>();

            if (item != null)
            {
                item.Setup(currentActiveCombo[i], noteSpriteL, noteSpriteR);
                spawnedNoteItems.Add(item);
            }
        }

        // Bật sáng nốt đầu tiên trong hàng
        if (spawnedNoteItems.Count > 0)
        {
            spawnedNoteItems[0].SetActive();
        }
    }

    private void ClearComboUI()
    {
        spawnedNoteItems.Clear();
        if (comboUIParent == null) return;
        foreach (Transform child in comboUIParent)
        {
            Destroy(child.gameObject);
        }
    }
}