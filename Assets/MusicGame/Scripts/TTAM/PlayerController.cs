using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerLaneController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private bool isGround = true;
    [SerializeField] private float jumpForce = 2f;

    [Header("Component")]
    private Animator animator;
    private Rigidbody2D rb;
    [SerializeField] private PianoKeySpawner pianoKeySpawner;

    [Header("Countdown UI")]
    [SerializeField] private TMP_Text countdownText;

    [Header("Countdown Animation")]
    [SerializeField] private float letterDelay = 0.1f;      // thời gian giữa mỗi chữ
    [SerializeField] private float readyHoldTime = 0.6f;    // giữ "Ready" sau khi gõ xong
    [SerializeField] private float shrinkDuration = 0.2f;   // thời gian thu nhỏ khi chuyển chữ
    [SerializeField] private float popDuration = 0.25f;     // thời gian "GO!" bật ra
    [SerializeField] private float popOvershoot = 1.3f;     // độ nảy quá đà của "GO!"

    [Header("Countdown Sound")]
    [SerializeField] private AudioSource countdownAudio;
    [SerializeField] private AudioClip ReadySound;
    [SerializeField] private AudioClip goSound;

    [Header("Music")]
    [SerializeField] private AudioSource audioSource; // PHẢI là cùng AudioSource với Spawner

    private bool canPlay = false;
    private bool gameEnded = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void OnEnable()
    {
        if (pianoKeySpawner != null)
            pianoKeySpawner.SongEnded += EndGame;
    }

    private void OnDisable()
    {
        if (pianoKeySpawner != null)
            pianoKeySpawner.SongEnded -= EndGame;
    }

    private void Start()
    {
        canPlay = false;
        gameEnded = false;

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.time = 0f;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        StartCoroutine(StartCountdown());
    }

    private void Update()
    {
        animator.SetBool("IsGround", isGround);
    }

    private IEnumerator StartCountdown()
    {
        float normalSize = countdownText.fontSize;   // lấy size đã set trong Inspector

        // ---- READY: hiện từng chữ ----
        countdownText.fontSize = normalSize;
        countdownText.text = "";
        PlayCountdownSound(ReadySound);

        yield return StartCoroutine(TypeText("Ready", letterDelay));
        yield return new WaitForSeconds(readyHoldTime);

        // ---- Ready thu nhỏ size về 0 ----
        yield return StartCoroutine(SizeOverTime(normalSize, 0f, shrinkDuration));

        // ---- GO!: size bật ra từ 0 ----
        countdownText.text = "GO!";
        PlayCountdownSound(goSound);
  

        canPlay = true;

        if (pianoKeySpawner != null)
            pianoKeySpawner.StartSong();
        else
            Debug.LogError("PlayerLaneController: chưa gán PianoKeySpawner -> không có nhạc/key!");

        yield return StartCoroutine(PopInSize(normalSize, popDuration, popOvershoot));

        yield return new WaitForSeconds(0.3f);

        // ---- GO! thu nhỏ rồi biến mất ----
        yield return StartCoroutine(SizeOverTime(normalSize, 0f, shrinkDuration));

        countdownText.text = "";
        countdownText.fontSize = normalSize;
    }

    private IEnumerator TypeText(string message, float delay)
    {
        countdownText.text = "";

        for (int i = 0; i < message.Length; i++)
        {
            countdownText.text += message[i];
            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator SizeOverTime(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            countdownText.fontSize = Mathf.Lerp(from, to, p * p); // ease-in
            yield return null;
        }

        countdownText.fontSize = to;
    }

    private IEnumerator PopInSize(float normalSize, float duration, float overshoot)
    {
        countdownText.fontSize = 0f;

        // Giai đoạn 1: 0 -> overshoot (70% thời gian)
        float up = duration * 0.7f;
        float elapsed = 0f;
        while (elapsed < up)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / up);
            countdownText.fontSize = Mathf.Lerp(0f, normalSize * overshoot, p);
            yield return null;
        }

        // Giai đoạn 2: overshoot -> bình thường (30% thời gian)
        float down = duration * 0.3f;
        elapsed = 0f;
        while (elapsed < down)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / down);
            countdownText.fontSize = Mathf.Lerp(normalSize * overshoot, normalSize, p);
            yield return null;
        }

        countdownText.fontSize = normalSize;
    }

    private void PlayCountdownSound(AudioClip clip)
    {
        if (countdownAudio != null && clip != null)
            countdownAudio.PlayOneShot(clip);
    }

    // =========================
    // JUMP
    // =========================

    public void Jump()
    {
        if (!canPlay || gameEnded)
            return;

        if (isGround)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            isGround = false;

            animator.SetBool("IsJump", true);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGround = true;

            animator.SetBool("IsJump", false);
            animator.SetBool("IsGround", true);
        }
    }

    // =========================
    // END GAME (được gọi bởi event SongEnded của spawner)
    // =========================

    private void EndGame()
    {
        if (gameEnded)
            return;

        gameEnded = true;
        canPlay = false;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (pianoKeySpawner != null)
            pianoKeySpawner.ClearAllKeys();

        Debug.Log("END GAME");
    }
}