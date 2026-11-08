using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Hướng dẫn chơi: spawn nốt tới 2 hit point (dưới = trái, trên = phải),
/// khi người chơi ấn đúng hết các nốt của bước đó thì chuyển sang bước tiếp theo,
/// đồng thời đổi dòng chữ hướng dẫn.
///
/// Yêu cầu: PianoKeySpawner trong scene tutorial phải tick "Is Tutorial".
/// </summary>
public class TutorialFlow : MonoBehaviour
{
    // =====================================================
    // DATA
    // =====================================================

    [System.Serializable]
    public class TutorialNote
    {
        [Tooltip("Tick = nốt TRÁI (hit point DƯỚI). Bỏ tick = nốt PHẢI (hit point TRÊN).")]
        public bool left = true;

        [Tooltip("Tick = nốt dài (phải giữ).")]
        public bool isLong;

        [Tooltip("Độ dài nốt dài (giây). Chỉ dùng khi Is Long.")]
        public float duration = 1.5f;

        [Tooltip("Chờ bao nhiêu giây sau nốt trước rồi mới spawn nốt này.")]
        public float delay;
    }

    [System.Serializable]
    public class TutorialStep
    {
        [Tooltip("Dòng chữ hướng dẫn hiện suốt bước này.")]
        [TextArea(2, 4)] public string instruction;

        [Tooltip("Hiện khi người chơi ấn đúng hết các nốt của bước này.")]
        [TextArea(1, 2)] public string successText = "Tốt lắm!";

        [Tooltip("Các nốt sẽ spawn trong bước này. Người chơi phải ấn đúng TẤT CẢ.")]
        public List<TutorialNote> notes = new List<TutorialNote>();

        [Tooltip("Tick = spawn LẦN LƯỢT từng nốt: nốt sau chỉ xuất hiện khi người chơi đã ấn xong nốt trước " +
                 "(trượt thì nốt đó spawn lại). Bỏ tick = spawn theo Delay, không chờ.")]
        public bool sequential = true;

        [Tooltip("Dừng bao nhiêu giây (hiện successText) trước khi sang bước tiếp.")]
        public float pauseAfter = 1.2f;
    }

    public enum HitPointTarget
    {
        None,
        Bottom,   // hit point DƯỚI (nốt trái)
        Top,      // hit point TRÊN (nốt phải)
        Both
    }

    // Phần giới thiệu 2 hit point, chạy TRƯỚC khi spawn nốt
    [System.Serializable]
    public class IntroStep
    {
        [Tooltip("Dòng chữ giới thiệu.")]
        [TextArea(2, 4)] public string text;

        [Tooltip("Hit point nào được làm nổi bật (nhấp nháy phóng to).")]
        public HitPointTarget highlight = HitPointTarget.None;

        [Tooltip("Hiện bao nhiêu giây.")]
        public float duration = 3f;

        [Tooltip("Tick = sau khi hết thời gian, chờ người chơi chạm mới sang bước tiếp. " +
                 "Bottom: chạm NỬA TRÁI. Top: chạm NỬA PHẢI. Both/None: chạm bất kỳ đâu.")]
        public bool waitForTap = false;
    }

    // =====================================================
    // INSPECTOR
    // =====================================================

    [Header("References")]
    [SerializeField] private PianoKeySpawner spawner;
    [SerializeField] private TMP_Text instructionText;

    [Header("Intro: giới thiệu 2 hit point")]
    [Tooltip("Bật = giới thiệu 2 hit point trước, xong mới spawn nốt.")]
    [SerializeField] private bool playIntro = true;
    [Tooltip("Hit point DƯỚI (nốt trái). Kéo cùng object với Hit Point Bottom của PianoKeySpawner.")]
    [SerializeField] private Transform hitPointBottom;
    [Tooltip("Hit point TRÊN (nốt phải). Kéo cùng object với Hit Point Top của PianoKeySpawner.")]
    [SerializeField] private Transform hitPointTop;
    [Tooltip("Mũi tên / khung chỉ vào hit point dưới (tuỳ chọn, chỉ hiện lúc giới thiệu).")]
    [SerializeField] private GameObject bottomPointer;
    [Tooltip("Mũi tên / khung chỉ vào hit point trên (tuỳ chọn, chỉ hiện lúc giới thiệu).")]
    [SerializeField] private GameObject topPointer;
    [Tooltip("Dòng nhắc 'Chạm ... để tiếp tục' (tuỳ chọn). Chỉ hiện khi bước giới thiệu đang chờ chạm.")]
    [SerializeField] private TMP_Text tapHintText;
    [SerializeField] private float highlightScale = 1.3f;
    [SerializeField] private float pulseSpeed = 5f;
    [Tooltip("Để trống = dùng bộ giới thiệu mặc định: 2 hit point cùng nháy kèm một dòng hướng dẫn.")]
    [SerializeField] private List<IntroStep> introSteps = new List<IntroStep>();

    [Header("Steps")]
    [Tooltip("Để trống = dùng bộ bước mặc định (nốt dưới, nốt trên, xen kẽ, rồi nốt dài), spawn lần lượt từng nốt.")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Texts")]
    [TextArea(1, 2)]
    [SerializeField] private string retryText = "Chưa đúng, thử lại nhé!";
    [TextArea(1, 2)]
    [SerializeField] private string completeText = "Hoàn thành hướng dẫn!";

    [Header("Timing")]
    [Tooltip("Chờ bao nhiêu giây sau khi bài bắt đầu rồi mới tới bước đầu tiên.")]
    [SerializeField] private float startDelay = 1f;
    [Tooltip("Bật = script này tự gọi spawner.StartSong(). Tắt = chờ script khác (vd. PlayerController đếm ngược) gọi.")]
    [SerializeField] private bool autoStartSong = false;

    [Header("When Completed")]
    [Tooltip("Sau khi xong, đợi bao nhiêu giây rồi mới làm tiếp.")]
    [SerializeField] private float finishDelay = 2f;
    [Tooltip("Scene sẽ vào sau khi xong hướng dẫn. Để trống = không chuyển scene.")]
    [SerializeField] private string nextSceneName = "";
    [SerializeField] private UnityEvent onCompleted;

    public const string DoneKey = "TutorialDone";
    public static bool IsDone => PlayerPrefs.GetInt(DoneKey, 0) == 1;

    // =====================================================
    // STATE
    // =====================================================

    private int hitsThisRound;
    private bool roundActive;

    private Vector3 bottomBaseScale = Vector3.one;
    private Vector3 topBaseScale = Vector3.one;
    private bool baseScalesCaptured;

    // Các nốt của vòng hiện tại. Nốt bị Destroy (trúng hoặc trượt) => coi như đã xử lý xong.
    private readonly List<Object> spawned = new List<Object>();

    // =====================================================
    // LIFECYCLE
    // =====================================================

    private void OnEnable()
    {
        PianoKey.NoteHit += OnNormalNoteHit;
        TutorialEvents.LongNoteCompleted += OnLongNoteCompleted;
    }

    private void OnDisable()
    {
        PianoKey.NoteHit -= OnNormalNoteHit;
        TutorialEvents.LongNoteCompleted -= OnLongNoteCompleted;

        ClearHighlight();
    }

    private void OnNormalNoteHit(PianoKey.KeyType type)
    {
        if (roundActive)
            hitsThisRound++;
    }

    private void OnLongNoteCompleted()
    {
        if (roundActive)
            hitsThisRound++;
    }

    private IEnumerator Start()
    {
        if (spawner == null)
        {
            Debug.LogError("TutorialFlow: Chưa gán PianoKeySpawner!", this);
            yield break;
        }

        if (!spawner.IsTutorial)
            Debug.LogWarning("TutorialFlow: PianoKeySpawner chưa tick 'Is Tutorial' -> nốt hướng dẫn sẽ không spawn.", this);

        if (steps == null || steps.Count == 0)
            steps = BuildDefaultSteps();

        SetText("");

        if (autoStartSong)
            spawner.StartSong();

        // Chờ bài (tutorial) bắt đầu
        while (!spawner.IsPlaying)
            yield return null;

        yield return new WaitForSeconds(startDelay);

        // Giới thiệu 2 hit point trước, xong mới spawn nốt
        if (playIntro)
        {
            if (introSteps == null || introSteps.Count == 0)
                introSteps = BuildDefaultIntro();

            if (hitPointBottom != null) bottomBaseScale = hitPointBottom.localScale;
            if (hitPointTop != null) topBaseScale = hitPointTop.localScale;
            baseScalesCaptured = true;

            for (int i = 0; i < introSteps.Count; i++)
                yield return StartCoroutine(RunIntro(introSteps[i]));

            SetText("");
            yield return new WaitForSeconds(0.5f);
        }

        for (int i = 0; i < steps.Count; i++)
            yield return StartCoroutine(RunStep(steps[i]));

        yield return StartCoroutine(Finish());
    }

    // =====================================================
    // STEP
    // =====================================================

    private IEnumerator RunStep(TutorialStep step)
    {
        if (step == null || step.notes == null || step.notes.Count == 0)
            yield break;

        SetText(step.instruction);

        if (step.sequential)
        {
            // Lần lượt từng nốt: nốt sau chỉ spawn khi nốt trước đã được ấn đúng
            foreach (TutorialNote note in step.notes)
                yield return StartCoroutine(RunSingleNote(step, note));
        }
        else
        {
            yield return StartCoroutine(RunAllAtOnce(step));
        }

        // Ấn đúng hết => chuyển bước, đổi chữ
        SetText(step.successText);
        yield return new WaitForSeconds(step.pauseAfter);
    }

    // Spawn 1 nốt, chờ người chơi ấn. Trượt thì thử lại đúng nốt đó.
    private IEnumerator RunSingleNote(TutorialStep step, TutorialNote note)
    {
        if (note.delay > 0f)
            yield return new WaitForSeconds(note.delay);

        while (true)
        {
            while (spawner.IsPaused)
                yield return null;

            hitsThisRound = 0;
            spawned.Clear();
            roundActive = true;

            Object obj = SpawnNote(note);

            if (obj == null)
            {
                roundActive = false;
                yield break;
            }

            spawned.Add(obj);

            // Chờ tới khi nốt xử lý xong (trúng hoặc trượt)
            while (!AllResolved())
                yield return null;

            // Cho event cuối cùng kịp ghi nhận
            yield return null;
            roundActive = false;

            if (hitsThisRound >= 1)
                yield break;

            // Trượt => nhắc rồi spawn lại nốt này
            SetText(retryText);
            yield return new WaitForSeconds(1.2f);
            SetText(step.instruction);
        }
    }

    // Spawn tất cả nốt của bước theo Delay (không chờ từng nốt), phải ấn đúng hết mới qua
    private IEnumerator RunAllAtOnce(TutorialStep step)
    {
        // Lặp lại vòng cho tới khi người chơi ấn đúng hết các nốt
        while (true)
        {
            hitsThisRound = 0;
            spawned.Clear();
            roundActive = true;

            int expectedHits = 0;

            foreach (TutorialNote note in step.notes)
            {
                if (note.delay > 0f)
                    yield return new WaitForSeconds(note.delay);

                // Pause thì chờ
                while (spawner.IsPaused)
                    yield return null;

                Object obj = SpawnNote(note);

                if (obj != null)
                {
                    spawned.Add(obj);
                    expectedHits++;
                }
            }

            if (expectedHits == 0)
            {
                roundActive = false;
                yield break;
            }

            // Chờ tới khi mọi nốt đã xử lý xong (trúng hoặc trượt)
            while (!AllResolved())
                yield return null;

            // Cho event cuối cùng kịp ghi nhận
            yield return null;
            roundActive = false;

            if (hitsThisRound >= expectedHits)
                break;

            // Chưa ấn đúng hết => thử lại
            SetText(retryText);
            yield return new WaitForSeconds(1.2f);
            SetText(step.instruction);
        }
    }

    private Object SpawnNote(TutorialNote note)
    {
        if (note.isLong)
            return spawner.SpawnTutorialLongKey(note.left, note.duration, false);

        return spawner.SpawnTutorialKey(note.left);
    }

    private bool AllResolved()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                return false;
        }

        return true;
    }

    // =====================================================
    // FINISH
    // =====================================================

    private IEnumerator Finish()
    {
        SetText(completeText);

        PlayerPrefs.SetInt(DoneKey, 1);
        PlayerPrefs.Save();

        onCompleted?.Invoke();

        yield return new WaitForSeconds(finishDelay);

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneLoader.Load(nextSceneName);
    }

    // =====================================================
    // INTRO: giới thiệu hit point
    // =====================================================

    private IEnumerator RunIntro(IntroStep step)
    {
        if (step == null)
            yield break;

        SetText(step.text);
        ShowPointers(step.highlight);

        Coroutine pulse = StartCoroutine(PulseRoutine(step.highlight));

        yield return new WaitForSeconds(step.duration);

        if (step.waitForTap)
        {
            ShowTapHint(step.highlight);

            // Bỏ qua lần chạm của frame hiện tại rồi mới nhận lần chạm mới
            yield return null;

            while (!TapMatches(step.highlight))
                yield return null;

            HideTapHint();
        }

        StopCoroutine(pulse);
        ClearHighlight();
    }

    // Hit point dưới => chạm NỬA TRÁI, hit point trên => chạm NỬA PHẢI, còn lại => chạm bất kỳ đâu
    private static bool TapMatches(HitPointTarget target)
    {
        switch (target)
        {
            case HitPointTarget.Bottom: return HoldInput.WasPressed(Lanee.Left);
            case HitPointTarget.Top:    return HoldInput.WasPressed(Lanee.Right);
            default:
                return HoldInput.WasPressed(Lanee.Left) || HoldInput.WasPressed(Lanee.Right);
        }
    }

    private void ShowTapHint(HitPointTarget target)
    {
        if (tapHintText == null)
            return;

        switch (target)
        {
            case HitPointTarget.Bottom: tapHintText.text = "Chạm NỬA TRÁI màn hình để tiếp tục"; break;
            case HitPointTarget.Top:    tapHintText.text = "Chạm NỬA PHẢI màn hình để tiếp tục"; break;
            default:                    tapHintText.text = "Chạm màn hình để tiếp tục"; break;
        }

        tapHintText.gameObject.SetActive(true);
    }

    private void HideTapHint()
    {
        if (tapHintText != null)
            tapHintText.gameObject.SetActive(false);
    }

    private void ShowPointers(HitPointTarget target)
    {
        bool bottom = target == HitPointTarget.Bottom || target == HitPointTarget.Both;
        bool top = target == HitPointTarget.Top || target == HitPointTarget.Both;

        if (bottomPointer != null) bottomPointer.SetActive(bottom);
        if (topPointer != null) topPointer.SetActive(top);
    }

    // Hit point được chọn sẽ nhấp nháy phóng to / thu nhỏ
    private IEnumerator PulseRoutine(HitPointTarget target)
    {
        bool bottom = target == HitPointTarget.Bottom || target == HitPointTarget.Both;
        bool top = target == HitPointTarget.Top || target == HitPointTarget.Both;

        if (!bottom && !top)
            yield break;

        while (true)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
            float scale = Mathf.Lerp(1f, highlightScale, wave);

            if (bottom && hitPointBottom != null)
                hitPointBottom.localScale = bottomBaseScale * scale;

            if (top && hitPointTop != null)
                hitPointTop.localScale = topBaseScale * scale;

            yield return null;
        }
    }

    // Trả hit point về kích thước ban đầu và ẩn mũi tên
    private void ClearHighlight()
    {
        // Chỉ trả scale khi đã lưu scale gốc (tránh ghi đè khi chưa chạy intro)
        if (baseScalesCaptured)
        {
            if (hitPointBottom != null) hitPointBottom.localScale = bottomBaseScale;
            if (hitPointTop != null) hitPointTop.localScale = topBaseScale;
        }

        if (bottomPointer != null) bottomPointer.SetActive(false);
        if (topPointer != null) topPointer.SetActive(false);

        HideTapHint();
    }

    // =====================================================
    // HELPERS
    // =====================================================

    private void SetText(string message)
    {
        if (instructionText != null)
            instructionText.text = message;
    }

    private static List<TutorialStep> BuildDefaultSteps()
    {
        return new List<TutorialStep>
        {
            // Lần lượt từng nốt: dưới -> trên -> xen kẽ -> nốt dài
            new TutorialStep
            {
                instruction = "Nốt tới vòng tròn DƯỚI:\nchạm NỬA TRÁI màn hình",
                successText = "Tốt lắm!",
                notes = new List<TutorialNote>
                {
                    new TutorialNote { left = true }
                }
            },
            new TutorialStep
            {
                instruction = "Nốt tới vòng tròn TRÊN:\nchạm NỬA PHẢI màn hình",
                successText = "Chính xác!",
                notes = new List<TutorialNote>
                {
                    new TutorialNote { left = false }
                }
            },
            new TutorialStep
            {
                instruction = "Thử xen kẽ nhé!\nDƯỚI: nửa trái, TRÊN: nửa phải",
                successText = "Tuyệt vời!",
                notes = new List<TutorialNote>
                {
                    new TutorialNote { left = true },
                    new TutorialNote { left = false, delay = 0.6f },
                    new TutorialNote { left = true,  delay = 0.6f },
                    new TutorialNote { left = false, delay = 0.6f }
                }
            },
            new TutorialStep
            {
                instruction = "Nốt dài: chạm và GIỮ ngón tay cho tới hết nốt",
                successText = "Hoàn hảo!",
                notes = new List<TutorialNote>
                {
                    new TutorialNote { left = true,  isLong = true, duration = 1.5f },
                    new TutorialNote { left = false, isLong = true, duration = 1.5f, delay = 0.8f }
                }
            }
        };
    }

    // Mở đầu: 2 hit point cùng nháy kèm một dòng hướng dẫn, sau đó mới spawn nốt
    private static List<IntroStep> BuildDefaultIntro()
    {
        return new List<IntroStep>
        {
            new IntroStep
            {
                text = "Đây là 2 vòng tròn.\nNốt sẽ chạy vào đây:\nDƯỚI chạm nửa TRÁI, TRÊN chạm nửa PHẢI",
                highlight = HitPointTarget.Both,
                duration = 4f,
                waitForTap = false
            }
        };
    }

    [ContextMenu("Fill Default Intro")]
    private void FillDefaultIntro()
    {
        introSteps = BuildDefaultIntro();
    }

    [ContextMenu("Fill Default Steps")]
    private void FillDefaultSteps()
    {
        steps = BuildDefaultSteps();
    }
}