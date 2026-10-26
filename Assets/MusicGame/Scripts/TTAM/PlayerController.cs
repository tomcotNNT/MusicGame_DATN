using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    private enum AirMode
    {
        None,
        Dash,
        Ride
    }

    [Header("Player")]
    [SerializeField] private bool isGround = true;

    [Header("Intro / Outro Movement")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform entryPoint;
    [SerializeField] private float introMoveSpeed = 5f;
    [SerializeField] private float outroMoveSpeed = 4f;

    private bool sequenceStarted;
    private bool gameEnded;

    [Header("Lane Trên")]
    [SerializeField] private float dashSpeed = 60f;
    [SerializeField] private float hangTime = 0.15f;
    [SerializeField] private float riseYOffset = 0f;

    [Header("Attack (random)")]
    [SerializeField] private string[] attackTriggers =
        { "Attack1", "Attack2", "Attack3" };

    [SerializeField] private bool avoidRepeat = true;

    private int lastAttackIndex = -1;

    public bool IsGround => isGround;
    public bool IsRiding => mode == AirMode.Ride;

    private AirMode mode = AirMode.None;
    private float savedGravity = 1f;

    private float dashTargetY;
    private float hangTimer;
    private bool reachedTarget;

    private Transform rideTarget;
    private float rideYOffset;

    [Header("Component")]
    private Animator animator;
    private Rigidbody2D rb;

    [SerializeField] private PianoKeySpawner pianoKeySpawner;

    [SerializeField] private GameObject WinPanel;

    [Header("Result")]
    [SerializeField] private ResultSystem resultSystem;
    [SerializeField] private ScoreSystem scoreSystem;

    [Header("Countdown UI")]
    [SerializeField] private TMP_Text countdownText;

    [Header("Countdown Timing")]
    [Tooltip("Tổng thời gian từ lúc bắt đầu gõ READY tới lúc hiện GO! (giây)")]
    [SerializeField] private float readyToGoTime = 3f;
    [Tooltip("Thời gian chữ GO! hiện trên màn hình (giây)")]
    [SerializeField] private float goDuration = 1f;

    [Header("Countdown Animation")]
    [Tooltip("Thời gian giữa mỗi chữ khi gõ READY (giây)")]
    [SerializeField] private float letterDelay = 0.1f;

    [Header("Countdown Sound")]
    [SerializeField] private AudioSource countdownAudio;
    [SerializeField] private AudioClip ReadySound;
    [SerializeField] private AudioClip goSound;

    [Header("Music")]
    [Tooltip("AudioSource nhạc, nên dùng cùng AudioSource với PianoKeySpawner.")]
    [SerializeField] private AudioSource audioSource;

    [Header("Hold Attack")]
    [SerializeField] private string holdAttackTrigger = "Sing";

    private readonly HashSet<string> animParams = new HashSet<string>();
    private readonly List<int> attackCandidates = new List<int>();

    private Coroutine introCoroutine;
    private Coroutine outroCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (rb == null)
        {
            Debug.LogError("PlayerController: Thiếu Rigidbody2D!");
            enabled = false;
            return;
        }

        if (animator != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
                animParams.Add(p.name);
        }

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        savedGravity = rb.gravityScale;
    }

    private void OnEnable()
    {
        SubscribeSongEnded();
    }

    private void OnDisable()
    {
        UnsubscribeSongEnded();
    }

    private void SubscribeSongEnded()
    {
        if (pianoKeySpawner == null)
            return;

        pianoKeySpawner.SongEnded -= EndGame;
        pianoKeySpawner.SongEnded += EndGame;
    }

    private void UnsubscribeSongEnded()
    {
        if (pianoKeySpawner != null)
            pianoKeySpawner.SongEnded -= EndGame;
    }

    private void Start()
    {
        // Phòng khi quay lại từ menu pause: WaitForSeconds sẽ đứng yên nếu timeScale = 0
        Time.timeScale = 1f;

        if (WinPanel != null)
            WinPanel.SetActive(false);

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "";
        }

        gameEnded = false;
        sequenceStarted = false;

        if (audioSource != null)
        {
            audioSource.Stop();

            if (audioSource.clip != null)
                audioSource.time = 0f;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (startPoint != null)
        {
            rb.position = startPoint.position;
        }
        else
        {
            Debug.LogWarning(
                "PlayerController: Chưa gán Start Point."
            );
        }

        // Đảm bảo đăng ký event nếu reference đã được gán.
        SubscribeSongEnded();

        introCoroutine = StartCoroutine(IntroSequence());
    }

    private void Update()
    {
        if (animator != null)
            animator.SetBool("IsGround", isGround);
    }

    // =====================================================
    // INTRO
    // =====================================================

    private IEnumerator IntroSequence()
    {
        if (sequenceStarted)
            yield break;

        sequenceStarted = true;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "";
        }

        if (entryPoint == null)
        {
            Debug.LogError(
                "PlayerController: Chưa gán Entry Point!"
            );
            yield break;
        }

        yield return StartCoroutine(
            MovePlayerTo(entryPoint.position, introMoveSpeed)
        );

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        yield return new WaitForSeconds(0.3f);

        // Chạy countdown sau khi Player tới nơi.
        yield return StartCoroutine(StartCountdown());

        introCoroutine = null;
    }

    // Chỉ di chuyển theo trục X, giữ nguyên Y để trọng lực tự đặt Player xuống đất.
    // Có timeout để không bao giờ bị kẹt vô hạn.
    private IEnumerator MovePlayerTo(Vector2 target, float moveSpeed)
    {
        if (moveSpeed <= 0f)
        {
            rb.position = new Vector2(target.x, rb.position.y);
            rb.linearVelocity = Vector2.zero;
            yield break;
        }

        const float timeout = 10f;
        float elapsed = 0f;

        while (Mathf.Abs(rb.position.x - target.x) > 0.03f && elapsed < timeout)
        {
            float x = Mathf.MoveTowards(
                rb.position.x,
                target.x,
                moveSpeed * Time.fixedDeltaTime
            );

            rb.MovePosition(new Vector2(x, rb.position.y));

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(new Vector2(target.x, rb.position.y));
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    // =====================================================
    // RANDOM ATTACK
    // =====================================================

    public void PlayAttack()
    {
        if (gameEnded ||
            animator == null ||
            attackTriggers == null ||
            attackTriggers.Length == 0)
            return;

        attackCandidates.Clear();

        for (int i = 0; i < attackTriggers.Length; i++)
        {
            if (!string.IsNullOrEmpty(attackTriggers[i]) &&
                animParams.Contains(attackTriggers[i]))
            {
                attackCandidates.Add(i);
            }
        }

        if (attackCandidates.Count == 0)
            return;

        int pick = attackCandidates[
            Random.Range(0, attackCandidates.Count)
        ];

        if (avoidRepeat && attackCandidates.Count > 1)
        {
            int safety = 0;

            while (pick == lastAttackIndex && safety++ < 20)
            {
                pick = attackCandidates[
                    Random.Range(0, attackCandidates.Count)
                ];
            }
        }

        lastAttackIndex = pick;

        foreach (int i in attackCandidates)
            animator.ResetTrigger(attackTriggers[i]);

        animator.SetTrigger(attackTriggers[pick]);
    }

    // =====================================================
    // HOLD ATTACK
    // =====================================================

    public void StartHoldAttack()
    {
        if (gameEnded || animator == null)
            return;

        if (animParams.Contains(holdAttackTrigger))
            animator.SetBool(holdAttackTrigger, true);
    }

    public void StopHoldAttack()
    {
        if (animator == null)
            return;

        if (animParams.Contains(holdAttackTrigger))
            animator.SetBool(holdAttackTrigger, false);
    }

    // =====================================================
    // LANE / DASH / RIDE
    // =====================================================

    public void DashUp(float noteY)
    {
        if (gameEnded || mode == AirMode.Ride)
            return;

        if (mode == AirMode.None)
            savedGravity = rb.gravityScale;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        mode = AirMode.Dash;
        dashTargetY = noteY + riseYOffset;
        reachedTarget = false;
        hangTimer = 0f;
    }

    public void Drop()
    {
        if (mode == AirMode.Dash)
            EndAir();
    }

    public void AttachToLongNote(Transform target, float yOffset)
    {
        if (gameEnded || target == null)
            return;

        if (mode == AirMode.None)
            savedGravity = rb.gravityScale;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        mode = AirMode.Ride;
        rideTarget = target;
        rideYOffset = yOffset;
    }

    public void DetachFromLongNote(Transform target)
    {
        if (mode != AirMode.Ride)
            return;

        if (target != null && rideTarget != target)
            return;

        EndAir();
    }

    private void EndAir()
    {
        mode = AirMode.None;
        rideTarget = null;

        rb.gravityScale = savedGravity;
        rb.linearVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        switch (mode)
        {
            case AirMode.Dash:
                UpdateDash();
                break;

            case AirMode.Ride:
                UpdateRide();
                break;
        }
    }

    private void UpdateDash()
    {
        rb.linearVelocity = Vector2.zero;

        float y = Mathf.MoveTowards(
            rb.position.y,
            dashTargetY,
            dashSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(new Vector2(rb.position.x, y));

        if (!reachedTarget)
        {
            if (Mathf.Abs(y - dashTargetY) < 0.001f)
                reachedTarget = true;

            return;
        }

        hangTimer += Time.fixedDeltaTime;

        if (hangTimer >= hangTime)
            EndAir();
    }

    private void UpdateRide()
    {
        if (rideTarget == null)
        {
            EndAir();
            return;
        }

        rb.linearVelocity = Vector2.zero;

        float targetY = rideTarget.position.y + rideYOffset;

        float y = Mathf.MoveTowards(
            rb.position.y,
            targetY,
            dashSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(new Vector2(rb.position.x, y));
    }

    // =====================================================
    // COUNTDOWN: READY (gõ từng chữ) -> GO!, tổng readyToGoTime giây
    // =====================================================

    private IEnumerator StartCountdown()
    {
        if (countdownText == null)
        {
            Debug.LogError(
                "PlayerController: Chưa gán Countdown Text trong Inspector!"
            );
            yield break;
        }

        float normalSize = countdownText.fontSize;

        // READY: hiện từng chữ
        countdownText.gameObject.SetActive(true);
        countdownText.fontSize = normalSize;
        countdownText.text = "";

        PlayCountdownSound(ReadySound);

        float startTime = Time.time;

        yield return StartCoroutine(TypeText("READY", letterDelay));

        // Chờ cho đủ readyToGoTime tính từ lúc bắt đầu gõ
        float remaining = readyToGoTime - (Time.time - startTime);
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        // GO
        countdownText.text = "GO!";
        PlayCountdownSound(goSound);

        // Bắt đầu gameplay sau khi hiện GO.
        if (pianoKeySpawner != null)
        {
            pianoKeySpawner.StartSong();
        }
        else
        {
            Debug.LogError(
                "PlayerController: Chưa gán PianoKeySpawner!"
            );
        }

        yield return new WaitForSeconds(goDuration);

        countdownText.text = "";
        countdownText.fontSize = normalSize;
    }

    private IEnumerator TypeText(string message, float delay)
    {
        countdownText.text = "";

        for (int i = 0; i < message.Length; i++)
        {
            countdownText.text = message.Substring(0, i + 1);
            yield return new WaitForSeconds(delay);
        }
    }

    private void PlayCountdownSound(AudioClip clip)
    {
        if (countdownAudio != null && clip != null)
            countdownAudio.PlayOneShot(clip);
    }

    // =====================================================
    // END GAME / OUTRO
    // =====================================================

    private void EndGame()
    {
        if (gameEnded)
            return;

        gameEnded = true;

        StopHoldAttack();

        if (mode != AirMode.None)
            EndAir();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (pianoKeySpawner != null)
            pianoKeySpawner.ClearAllKeys();

        // Hủy countdown nếu nhạc kết thúc bất thường trong lúc đếm.
        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        outroCoroutine = StartCoroutine(OutroSequence());
    }

    private IEnumerator OutroSequence()
    {
        yield return new WaitForSeconds(0.2f);

        // Lùi về StartPoint.
        if (startPoint != null)
        {
            yield return StartCoroutine(
                MovePlayerTo(startPoint.position, outroMoveSpeed)
            );
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        FinishGame();
        outroCoroutine = null;
    }

    private void FinishGame()
    {
        if (resultSystem != null && scoreSystem != null)
            resultSystem.ShowResult(scoreSystem.GetScore());

        if (WinPanel != null)
            WinPanel.SetActive(true);

        Debug.Log("WIN - Player đã lùi về vị trí ban đầu!");
    }
}