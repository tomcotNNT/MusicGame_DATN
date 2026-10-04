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

    [Header("Countdown Sound")]
    [SerializeField] private AudioSource countdownAudio;
    [SerializeField] private AudioClip threeSound;
    [SerializeField] private AudioClip twoSound;
    [SerializeField] private AudioClip oneSound;
    [SerializeField] private AudioClip goSound;

    [Header("Music")]
    [SerializeField] private AudioSource audioSource; // PHẢI là cùng AudioSource với Spawner

    private const float CountdownSeconds = 3f;

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
        // Bắt đầu spawn key ngay bây giờ; nhạc sẽ vào đúng lúc hết countdown.
        // LƯU Ý: MidiReader.LoadMidi() phải chạy xong TRƯỚC dòng này.
        if (pianoKeySpawner != null)
            pianoKeySpawner.StartSong(CountdownSeconds);
        else
            Debug.LogError("PlayerLaneController: chưa gán PianoKeySpawner -> không có nhạc/key!");

        countdownText.text = "3";
        PlayCountdownSound(threeSound);
        yield return new WaitForSeconds(1f);

        countdownText.text = "2";
        PlayCountdownSound(twoSound);
        yield return new WaitForSeconds(1f);

        countdownText.text = "1";
        PlayCountdownSound(oneSound);
        yield return new WaitForSeconds(1f);

        countdownText.text = "GO!";
        PlayCountdownSound(goSound);

        canPlay = true; // nhạc đã được lên lịch phát bởi spawner

        yield return new WaitForSeconds(0.5f);

        countdownText.text = "";
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