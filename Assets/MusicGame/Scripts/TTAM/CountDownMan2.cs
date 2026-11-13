
using System.Collections;
using TMPro;
using UnityEngine;

public class CountdownMan2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private EnemySpawnerMan2 enemySpawner;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip readySound;
    [SerializeField] private AudioClip goSound;

    [Header("Timing")]
    [SerializeField] private float readyToGoTime = 3f;
    [SerializeField] private float letterDelay = 0.15f;
    [SerializeField] private float goDuration = 0.5f;

    private bool isCounting;

    private void Start()
    {
        StartCountdown();
    }

    public void StartCountdown()
    {
        if (isCounting) return;

        if (countdownText == null || enemySpawner == null)
        {
            Debug.LogError("CountdownMan2: Thiếu Reference!");
            return;
        }

        if (sfxSource == null)
        {
            Debug.LogError("CountdownMan2: Chưa gán SFX AudioSource!");
            return;
        }

        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        isCounting = true;

        countdownText.gameObject.SetActive(true);

        float normalSize = countdownText.fontSize;
        countdownText.text = "";
        countdownText.fontSize = normalSize;

        // READY: phát âm thanh và chạy từng chữ
        if (readySound != null)
            sfxSource.PlayOneShot(readySound);

        float startTime = Time.time;

        yield return StartCoroutine(TypeText("READY", letterDelay));

        // Đảm bảo tổng thời gian READY là readyToGoTime
        float remaining = readyToGoTime - (Time.time - startTime);

        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);

        // GO!
        countdownText.text = "GO!";

        if (goSound != null)
            sfxSource.PlayOneShot(goSound);

        yield return new WaitForSecondsRealtime(goDuration);

        // Bắt đầu game
        enemySpawner.StartSpawner(0f);

        countdownText.text = "";
        countdownText.gameObject.SetActive(false);

        isCounting = false;
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
