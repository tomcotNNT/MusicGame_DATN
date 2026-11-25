using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>Spawner có đồng hồ bài hát theo dspTime (BossSpawner2, SubmarineRhythmSpawner...).</summary>
public interface IRhythmSongClock
{
    /// <summary>Lên lịch phát nhạc sau <paramref name="countdown"/> giây (theo dspTime).</summary>
    void StartSpawner(float countdown);

    bool IsPlaying { get; }

    /// <summary>Giây tính từ lúc nhạc bắt đầu (âm khi còn đang đếm ngược).</summary>
    float SongTime { get; }
}

/// <summary>
/// Đếm ngược READY / GO dùng chung cho các màn chơi.
/// Nhạc được lên lịch NGAY khi bắt đầu đếm, chữ GO bám theo SongTime của spawner,
/// nên lúc chữ GO tắt đúng là lúc nhạc bắt đầu (SongTime = 0), không bị lệch.
/// Spawner phải tắt Spawn On Start, để countdown là nơi duy nhất bắt đầu bài.
/// </summary>
public abstract class RhythmCountdown : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected TMP_Text countdownText;

    [Header("Audio")]
    [SerializeField] protected AudioSource sfxSource;
    [SerializeField] protected AudioClip readySound;
    [SerializeField] protected AudioClip goSound;

    [Header("Timing")]
    [Tooltip("Thời gian hiện READY (giây), tính cả lúc gõ chữ")]
    [SerializeField] protected float readyToGoTime = 3f;
    [SerializeField] protected float letterDelay = 0.15f;
    [Tooltip("Thời gian hiện GO! (giây). Nhạc bắt đầu đúng lúc GO! tắt")]
    [SerializeField] protected float goDuration = 0.5f;

    private bool isCounting;

    /// <summary>Spawner mà countdown này điều khiển.</summary>
    protected abstract IRhythmSongClock Clock { get; }

    protected virtual void Start()
    {
        StartCountdown();
    }

    public void StartCountdown()
    {
        if (isCounting) return;

        if (countdownText == null || Clock == null)
        {
            Debug.LogError(GetType().Name + ": Thiếu Reference!");
            return;
        }

        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        isCounting = true;

        IRhythmSongClock clock = Clock;
        float leadIn = Mathf.Max(0f, readyToGoTime) + Mathf.Max(0f, goDuration);

        if (clock.IsPlaying)
            Debug.LogWarning(GetType().Name + ": Spawner đã tự chạy (Spawn On Start đang bật?) - đếm ngược sẽ không khớp nhạc.");
        else
            clock.StartSpawner(leadIn);

        if (!clock.IsPlaying)
        {
            // Spawner báo thiếu reference -> không có nhạc để đếm
            countdownText.gameObject.SetActive(false);
            isCounting = false;
            yield break;
        }

        countdownText.gameObject.SetActive(true);
        countdownText.text = "";

        // READY
        PlaySfx(readySound);
        yield return StartCoroutine(TypeText("READY", letterDelay));

        while (clock.IsPlaying && clock.SongTime < -goDuration)
            yield return null;

        // GO!
        countdownText.text = "GO!";
        PlaySfx(goSound);

        while (clock.IsPlaying && clock.SongTime < 0f)
            yield return null;

        countdownText.text = "";
        countdownText.gameObject.SetActive(false);

        isCounting = false;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null && sfxSource != null && AudioPrefs.SfxOn)
            sfxSource.PlayOneShot(clip);
    }

    private IEnumerator TypeText(string message, float delay)
    {
        countdownText.text = "";

        for (int i = 0; i < message.Length; i++)
        {
            countdownText.text = message.Substring(0, i + 1);
            yield return new WaitForSecondsRealtime(delay);
        }
    }
}
