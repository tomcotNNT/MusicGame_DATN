using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private PianoKeySpawner spawner;
    [SerializeField] private TMP_Text tutorialText;

    [Header("Flow")]
    [SerializeField] private float startDelay = 3f;
    [SerializeField] private int normalNotesRequired = 3;
    [SerializeField] private float longNoteDuration = 2f;
    [SerializeField] private int longNotesRequired = 3;
    [Tooltip("Bật = bắt buộc nhảy lên đúng độ cao mới giữ được nốt dài.")]
    [SerializeField] private bool requireJump = false;
    [SerializeField] private string nextScene = "Man1";

    private bool normalHit, longStarted, longCompleted;

    private void OnEnable()
    {
        TutorialEvents.NormalNoteHit += OnNormalHit;
        TutorialEvents.LongNoteStarted += OnLongStarted;
        TutorialEvents.LongNoteCompleted += OnLongCompleted;
    }

    private void OnDisable()
    {
        TutorialEvents.NormalNoteHit -= OnNormalHit;
        TutorialEvents.LongNoteStarted -= OnLongStarted;
        TutorialEvents.LongNoteCompleted -= OnLongCompleted;
    }

    private void OnNormalHit() => normalHit = true;
    private void OnLongStarted() => longStarted = true;
    private void OnLongCompleted() => longCompleted = true;

    private void Start()
    {
        spawner.StartSong();   // ở chế độ tutorial chỉ khởi động đồng hồ
        StartCoroutine(Flow());
    }

    private IEnumerator Flow()
    {
        SetText("Chuẩn bị bắt đầu...");
        yield return new WaitForSeconds(startDelay);

        // ---------- Phần 1: nốt thường ----------
        int hits = 0;
        bool left = true;

        while (hits < normalNotesRequired)
        {
            SetText((left ? "Nhấn nút TRÁI" : "Nhấn nút PHẢI") +
                    $" khi nốt chạm Player ({hits}/{normalNotesRequired})");

            normalHit = false;
            PianoKey key = spawner.SpawnTutorialKey(left);
            float deadline = spawner.SongTime + spawner.SpawnAheadTime + 1.5f;

            while (!normalHit && spawner.SongTime < deadline)
                yield return null;

            if (normalHit)
            {
                hits++;
                left = !left;
                SetText("Tốt lắm!");
            }
            else
            {
                SetText("Trượt rồi, thử lại nhé!");
                if (key != null) Destroy(key.gameObject);
            }

            yield return new WaitForSeconds(0.8f);
        }

        // ---------- Phần 2: nốt dài TRÁI / PHẢI ----------
        SetText("Giỏi lắm! Giờ tới nốt dài.");
        yield return new WaitForSeconds(1.5f);

        int longDone = 0;
        bool longLeft = true;

        while (longDone < longNotesRequired)
        {
            longStarted = false;
            longCompleted = false;

            string side = longLeft ? "TRÁI" : "PHẢI";
            string progress = $"({longDone}/{longNotesRequired})";

            SetText(requireJump
                ? $"NHẢY lên rồi GIỮ nút {side} khi nốt dài tới Player {progress}"
                : $"GIỮ nút {side} khi nốt dài tới Player, giữ đến hết nốt {progress}");

            LongNote note = spawner.SpawnTutorialLongKey(longLeft, longNoteDuration, requireJump);
            bool shown = false;

            while (note != null)   // tự Destroy khi xong / miss
            {
                if (longStarted && !shown)
                {
                    shown = true;
                    SetText("Giữ nút... đừng thả!");
                }

                // Thả sớm: hủy luôn để thử lại, khỏi chờ nốt bay hết.
                if (note.IsBroken())
                {
                    Destroy(note.gameObject);
                    break;
                }

                yield return null;
            }

            if (longCompleted)
            {
                longDone++;
                longLeft = !longLeft;
                SetText("Tuyệt vời!");
                yield return new WaitForSeconds(0.8f);
            }
            else
            {
                SetText("Chưa được, thử lại nhé!");
                yield return new WaitForSeconds(1f);
            }
        }

        SetText("Hoàn thành tutorial!");
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(nextScene);
    }

    private void SetText(string msg)
    {
        if (tutorialText != null)
            tutorialText.text = msg;
    }
}