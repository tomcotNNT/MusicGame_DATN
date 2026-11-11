
using System;
using UnityEngine;
using UnityEngine.Events;

public class EnemyRhythmController : MonoBehaviour
{
    public enum HitGrade
    {
        Perfect,
        Good,
        Miss
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyRhythmTimelineUI timelineUI;

    [Header("Combo")]
    [SerializeField] private ComboSystem comboSystem;

    [Header("Attack")]
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private GameObject hitVFX;
    [SerializeField] private Transform vfxPoint;

    [Header("Timing")]
    [SerializeField] private float perfectWindow = 0.10f;
    [SerializeField] private float goodWindow = 0.20f;

    [Header("Score")]
    [SerializeField] private int perfectScore = 100;
    [SerializeField] private int goodScore = 50;

    [Header("Events")]
    public UnityEvent<int> onNoteHit;
    public UnityEvent onMiss;
    public UnityEvent onCompleted;

    private float[] noteTimes;
    private Func<float> getSongTime;

    private int currentIndex;
    private bool isLeft;
    private bool initialized;
    private bool completed;

    public bool IsLeft => isLeft;
    public bool IsCompleted => completed;
    public int CurrentIndex => currentIndex;
    public int TotalNotes => noteTimes == null ? 0 : noteTimes.Length;

    public float CurrentTargetTime
    {
        get
        {
            if (noteTimes == null || currentIndex >= noteTimes.Length)
                return float.MaxValue;

            return noteTimes[currentIndex];
        }
    }

    public float TimingError =>
        getSongTime == null
            ? float.MaxValue
            : Mathf.Abs(getSongTime() - CurrentTargetTime);

    public void Initialize(
        float[] times,
        bool left,
        Func<float> songTimeProvider)
    {
        if (times == null || times.Length == 0)
        {
            Debug.LogError("Enemy has no MIDI notes!", this);
            return;
        }

        if (songTimeProvider == null)
        {
            Debug.LogError("SongTimeProvider is null!", this);
            return;
        }

        noteTimes = times;
        isLeft = left;
        getSongTime = songTimeProvider;

        currentIndex = 0;
        completed = false;
        initialized = true;

        if (timelineUI != null)
            timelineUI.Setup(noteTimes.Length, isLeft);
    }

    private void Awake()
    {
        if (comboSystem == null)
            comboSystem = FindFirstObjectByType<ComboSystem>();
    }

    private void Update()
    {
        if (!initialized || completed)
            return;

        if (currentIndex >= noteTimes.Length)
            return;

        float now = getSongTime();

        if (now > CurrentTargetTime + goodWindow)
            RegisterMiss();
    }

    public bool CanHitNow()
    {
        if (!initialized || completed)
            return false;

        if (currentIndex >= noteTimes.Length)
            return false;

        return TimingError <= goodWindow;
    }

    public bool TryHit(out HitGrade grade)
    {
        grade = HitGrade.Miss;

        if (!CanHitNow())
            return false;

        float error = TimingError;

        grade = error <= perfectWindow
            ? HitGrade.Perfect
            : HitGrade.Good;

        RegisterHit(grade);

        return true;
    }

    private void RegisterHit(HitGrade grade)
    {
        int score = grade == HitGrade.Perfect
            ? perfectScore
            : goodScore;

        if (animator != null)
            animator.SetTrigger(attackTrigger);

        // Hit VFX
        if (hitVFX != null)
        {
            Vector3 position = vfxPoint != null
                ? vfxPoint.position
                : transform.position;

            Instantiate(hitVFX, position, Quaternion.identity);
        }

        // Hit Sound
       

        onNoteHit?.Invoke(score);

        currentIndex++;

        if (timelineUI != null)
            timelineUI.UpdateProgress(currentIndex, noteTimes.Length);

        if (currentIndex >= noteTimes.Length)
            CompleteSequence();
    }

   

    private void RegisterMiss()
    {
        currentIndex++;

        // Reset combo khi hụt nốt
        if (comboSystem != null)
        {
            comboSystem.MissNote();
        }
        else
        {
            Debug.LogWarning("EnemyRhythmController: Không tìm thấy ComboSystem!");
        }

        onMiss?.Invoke();

        if (timelineUI != null)
            timelineUI.UpdateProgress(currentIndex, noteTimes.Length);

        if (currentIndex >= noteTimes.Length)
            FailSequence();
    }

    private void CompleteSequence()
    {
        completed = true;

        onCompleted?.Invoke();

        // Không còn Death VFX.
        Destroy(gameObject);
    }

    private void FailSequence()
    {
        completed = true;

        Debug.Log("Enemy sequence failed: " + gameObject.name);
    }
}
