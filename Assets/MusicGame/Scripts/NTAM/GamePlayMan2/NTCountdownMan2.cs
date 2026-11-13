using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class NTCountdownMan2 : MonoBehaviour
{
    public static event Action OnCountdownStarted;
    public static event Action OnCountdownFinished;

    [Header("References")]
    [Tooltip("Kéo TextMeshPro hiển thị chữ đếm ngược vào đây")]
    [SerializeField] private TMP_Text countdownText;
    [Tooltip("Tự động bắt NTEnemySpawnerMan2 nếu để trống")]
    [SerializeField] private NTEnemySpawnerMan2 enemySpawner;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip readySound;
    [SerializeField] private AudioClip goSound;

    [Header("Timing Settings")]
    [Tooltip("Tổng thời gian hiển thị chữ READY (giây)")]
    [SerializeField] private float readyDuration = 2.0f;
    [Tooltip("Tốc độ gõ từng chữ R-E-A-D-Y")]
    [SerializeField] private float letterDelay = 0.1f;
    [Tooltip("Thời gian hiển thị hiệu ứng nảy chữ GO!")]
    [SerializeField] private float goDuration = 0.6f;

    [Header("Visual Effects (Arcade Style)")]
    [SerializeField] private Color readyColor = new Color(1f, 0.85f, 0.2f, 1f); // Vàng sáng
    [SerializeField] private Color goColor = new Color(0.2f, 1f, 0.4f, 1f);     // Xanh Neon
    [SerializeField] private float goMaxScale = 1.4f;

    [Header("Unity Events (Tùy chọn kết nối UI khác)")]
    [SerializeField] private UnityEvent onGoTriggered;

    private bool isCounting = false;
    private Vector3 originalTextScale;

    private void Awake()
    {
        // Tự động tìm tham chiếu nếu quên kéo trên Inspector
        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<NTEnemySpawnerMan2>();

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (countdownText != null)
        {
            originalTextScale = countdownText.transform.localScale;
        }
    }

    private void Start()
    {
        StartCountdown();
    }

    public void StartCountdown()
    {
        if (isCounting) return;

        if (countdownText == null)
        {
            Debug.LogError("<color=red>[NTCountdown]</color> Chưa gán TMP_Text countdownText!");
            return;
        }

        if (enemySpawner == null)
        {
            Debug.LogError("<color=red>[NTCountdown]</color> Không tìm thấy NTEnemySpawnerMan2 trong Scene!");
            return;
        }

        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        isCounting = true;
        OnCountdownStarted?.Invoke();

        countdownText.gameObject.SetActive(true);
        countdownText.transform.localScale = originalTextScale;
        countdownText.color = readyColor;
        countdownText.text = "";

        // 1. PHÁT ÂM THANH & HIỆU ỨNG GÕ CHỮ "READY"
        if (readySound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(readySound);
        }

        // Dùng unscaledTime để không bị lỗi đơ nếu game pause hoặc timeScale = 0
        float readyStartTime = Time.unscaledTime;
        yield return StartCoroutine(TypeReadyText("READY", letterDelay));

        // Chờ đủ thời gian quy định của phase READY
        float readyRemaining = readyDuration - (Time.unscaledTime - readyStartTime);
        if (readyRemaining > 0f)
        {
            yield return new WaitForSecondsRealtime(readyRemaining);
        }

        // 2. HIỆU ỨNG NẨY LỰC "GO!" (PUNCH SCALE & FLASH)
        countdownText.text = "GO!";
        countdownText.color = goColor;

        if (goSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(goSound);
        }

        onGoTriggered?.Invoke();

        // 3. KHỞI CHẠY BÀI NHẠC & SPAWNER ĐỒNG TỐC
        enemySpawner.StartSpawner(leadIn: 0f);

        // Chạy animation chữ GO! nảy to rồi mờ dần
        yield return StartCoroutine(AnimateGoText(goDuration));

        // 4. KẾT THÚC & DỌN DẸP
        countdownText.text = "";
        countdownText.transform.localScale = originalTextScale;
        countdownText.gameObject.SetActive(false);

        isCounting = false;
        OnCountdownFinished?.Invoke();
    }

    /// <summary>
    /// Gõ từng ký tự kèm nhấp nháy nhẹ
    /// </summary>
    private IEnumerator TypeReadyText(string message, float delay)
    {
        for (int i = 0; i < message.Length; i++)
        {
            countdownText.text = message.Substring(0, i + 1);
            yield return new WaitForSecondsRealtime(delay);
        }
    }

    /// <summary>
    /// Hiệu ứng nảy Scale từ 1.4x co về 1.0x và mờ dần (Fade Out)
    /// </summary>
    private IEnumerator AnimateGoText(float duration)
    {
        float elapsed = 0f;
        Color initialColor = countdownText.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float percent = Mathf.Clamp01(elapsed / duration);

            // Nảy to lúc đầu rồi thu nhỏ dần về kích thước gốc
            float currentScale = Mathf.Lerp(goMaxScale, 1.0f, percent);
            countdownText.transform.localScale = originalTextScale * currentScale;

            // Nửa sau thời gian bắt đầu mờ dần (Fade Out)
            if (percent > 0.5f)
            {
                float alpha = Mathf.InverseLerp(1.0f, 0.5f, percent);
                countdownText.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
            }

            yield return null;
        }
    }
}