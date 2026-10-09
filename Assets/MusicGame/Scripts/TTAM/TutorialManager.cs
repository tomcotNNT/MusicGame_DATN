// using System.Collections.Generic;
// using TMPro;
// using UnityEngine;

// /// <summary>
// /// Tutorial: những nốt thường / nốt dài đầu tiên sẽ hiện bàn tay chỉ dẫn,
// /// DỪNG nhạc + nốt (Player vẫn chạy/nhảy bình thường), chờ người chơi
// /// bấm đúng rồi mới chạy tiếp.
// /// </summary>
// public class TutorialManager : MonoBehaviour
// {
//     private enum Mode { None, Normal, LongSwipe, LongFly, Long, LongFollow }

//     [Header("References")]
//     [SerializeField] private PianoKeySpawner spawner;
//     [SerializeField] private RectTransform hand;          // Image bàn tay (UI), để tắt sẵn
//     [SerializeField] private RectTransform leftButton;    // nút Left trên UI
//     [SerializeField] private RectTransform rightButton;   // nút Right trên UI
//     [SerializeField] private TMP_Text hintText;           // tuỳ chọn
//     [SerializeField] private PlayerLaneController player; // để nhận sự kiện vuốt lên
//     [Tooltip("Điểm bàn tay bắt đầu vuốt (vd. giữa màn hình, gần Player).")]
//     [SerializeField] private RectTransform swipeStart;

//     [Header("Số lần hiện tutorial (MỖI lane đếm riêng)")]
//     [Tooltip("Số lần hướng dẫn nốt thường cho từng lane (Left và Right mỗi bên).")]
//     [SerializeField] private int normalTutorialCount = 3;
//     [Tooltip("Số lần hướng dẫn nốt dài cho từng lane (Left và Right mỗi bên).")]
//     [SerializeField] private int longTutorialCount = 3;

//     [Header("Nốt dài")]
//     [Tooltip("Dừng khi đầu nốt cách Player tối đa bao nhiêu giây.")]
//     [SerializeField] private float longPauseLead = 0.05f;
//     [Tooltip("Phải giữ nút liên tục bao lâu thì mới chạy tiếp.")]
//     [SerializeField] private float holdConfirmTime = 0.4f;
//     [Tooltip("Tutorial bỏ điều kiện phải nhảy lên đúng độ cao mới giữ được nốt.")]
//     [SerializeField] private bool waiveHeightInTutorial = true;

//     [Header("Vuốt nhảy (nốt dài)")]
//     [Tooltip("Mỗi lần hướng dẫn nốt dài sẽ có thêm bước vuốt lên để nhảy.")]
//     [SerializeField] private bool includeSwipeStep = true;
//     [Tooltip("Dừng để hướng dẫn vuốt khi đầu nốt còn cách Player bao nhiêu giây " +
//              "(đủ thời gian để Player nhảy lên tới nốt).")]
//     [SerializeField] private float jumpPauseLead = 0.6f;
//     [SerializeField] private float swipeDistance = 250f;  // pixel bàn tay lướt lên
//     [SerializeField] private float swipeSpeed = 1.2f;     // số lần vuốt / giây

//     [Header("Hand")]
//     [SerializeField] private Vector2 handOffset = new Vector2(0f, -30f);
//     [SerializeField] private float tapSpeed = 6f;
//     [SerializeField] private float tapScaleMin = 0.8f;
//     [SerializeField] private float bobAmount = 12f;

//     [Header("Hint Text")]
//     [SerializeField] private string normalHint = "Nhấn nút khi nốt chạm Player!";
//     [SerializeField] private string longHint = "Nhấn GIỮ nút cho đến hết nốt!";
//     [SerializeField] private string swipeHint = "Vuốt LÊN để nhảy tới nốt!";

//     private Mode mode = Mode.None;

//     private PianoKey currentKey;
//     private LongNote currentLong;
//     private RectTransform currentTarget;

//     // Bộ đếm riêng theo lane: [0] = Left, [1] = Right
//     private readonly int[] normalDone = new int[2];
//     private readonly int[] longDone = new int[2];
//     private int currentLane;
//     private float holdTimer;
//     private bool frozen;
//     private bool swipeDone;

//     private readonly HashSet<PianoKey> seenKeys = new HashSet<PianoKey>();
//     private readonly HashSet<LongNote> seenLongs = new HashSet<LongNote>();

//     private void Start()
//     {
//         HideHand();

//         if (player != null)
//             player.SwipeUp += OnSwipeUp;
//     }

//     private void OnDestroy()
//     {
//         if (player != null)
//             player.SwipeUp -= OnSwipeUp;
//     }

//     private void OnSwipeUp()
//     {
//         if (mode == Mode.LongSwipe)
//             swipeDone = true;
//     }

//     private void OnDisable()
//     {
//         if (frozen)
//             Unfreeze();
//     }

//     private void Update()
//     {
//         if (spawner == null || !spawner.IsPlaying)
//             return;

//         switch (mode)
//         {
//             case Mode.None:
//                 TryStartLong();      // ưu tiên nốt dài nếu cùng lúc
//                 if (mode == Mode.None)
//                     TryStartNormal();
//                 break;

//             case Mode.Normal:
//                 UpdateNormal();
//                 break;

//             case Mode.LongSwipe:
//                 UpdateLongSwipe();
//                 break;

//             case Mode.LongFly:
//                 UpdateLongFly();
//                 break;

//             case Mode.Long:
//                 UpdateLong();
//                 break;

//             case Mode.LongFollow:
//                 UpdateLongFollow();
//                 break;
//         }

//         if (mode != Mode.None)
//             AnimateHand();
//     }

//     // =====================================================
//     // NỐT THƯỜNG
//     // =====================================================

//     private static int LaneIndex(PianoKey.KeyType type)
//     {
//         return type == PianoKey.KeyType.Left ? 0 : 1;
//     }

//     private static int LaneIndex(Lanee lane)
//     {
//         return lane == Lanee.Left ? 0 : 1;
//     }

//     private void TryStartNormal()
//     {
//         foreach (PianoKey key in PianoKey.ActiveKeys)
//         {
//             if (key == null || key.IsCompleted || seenKeys.Contains(key))
//                 continue;

//             // Lane này đã đủ số lần hướng dẫn -> bỏ qua.
//             if (normalDone[LaneIndex(key.GetKeyType())] >= normalTutorialCount)
//                 continue;

//             // Player đã chạm nốt -> dừng lại hướng dẫn.
//             if (key.PlayerOnKey)
//             {
//                 BeginNormal(key);
//                 return;
//             }
//         }
//     }

//     private void BeginNormal(PianoKey key)
//     {
//         seenKeys.Add(key);
//         currentKey = key;
//         currentLane = LaneIndex(key.GetKeyType());
//         mode = Mode.Normal;

//         currentTarget = key.GetKeyType() == PianoKey.KeyType.Left
//             ? leftButton
//             : rightButton;

//         Freeze();
//         ShowHand(normalHint);
//     }

//     private void UpdateNormal()
//     {
//         // Người chơi bấm đúng -> PianoKey.Complete() -> tiếp tục.
//         if (currentKey == null || currentKey.IsCompleted)
//         {
//             normalDone[currentLane]++;
//             EndStep();
//         }
//     }

//     // =====================================================
//     // NỐT DÀI
//     // =====================================================

//     private void TryStartLong()
//     {
//         float now = spawner.SongTime;

//         foreach (LongNote n in LongNote.Active)
//         {
//             if (n == null || !n.IsWaiting() || seenLongs.Contains(n))
//                 continue;

//             // Lane này đã đủ số lần hướng dẫn -> bỏ qua.
//             if (longDone[LaneIndex(n.GetLane())] >= longTutorialCount)
//                 continue;

//             // Player đang đứng dưới đất -> cần bước vuốt nhảy trước, dừng sớm hơn.
//             bool needSwipe = includeSwipeStep && player != null && player.IsGround;
//             float lead = needSwipe ? jumpPauseLead : longPauseLead;

//             if (now >= n.GetNoteTime() - lead)
//             {
//                 BeginLong(n, needSwipe);
//                 return;
//             }
//         }
//     }

//     private void BeginLong(LongNote note, bool needSwipe)
//     {
//         seenLongs.Add(note);
//         currentLong = note;
//         currentLane = LaneIndex(note.GetLane());
//         mode = Mode.Long;
//         holdTimer = 0f;

//         if (waiveHeightInTutorial)
//             note.SetTutorialAssist(true);

//         currentTarget = note.GetLane() == Lanee.Left
//             ? leftButton
//             : rightButton;

//         Freeze();

//         if (needSwipe)
//         {
//             swipeDone = false;
//             mode = Mode.LongSwipe;
//             ShowHand(swipeHint);
//         }
//         else
//         {
//             ShowHand(longHint);
//         }
//     }

//     private void UpdateLong()
//     {
//         if (currentLong == null)
//         {
//             longDone[currentLane]++;
//             EndStep();
//             return;
//         }

//         // Phải giữ nút đúng lane đủ lâu mới chạy tiếp.
//         if (HoldInput.IsHeld(currentLong.GetLane()))
//             holdTimer += Time.unscaledDeltaTime;
//         else
//             holdTimer = 0f;

//         if (holdTimer >= holdConfirmTime)
//         {
//             longDone[currentLane]++;
//             Unfreeze();

//             // Chạy tiếp, nhưng vẫn giữ bàn tay đến khi nốt kết thúc.
//             mode = Mode.LongFollow;
//             SetHint("");
//         }
//     }

//     private void UpdateLongFollow()
//     {
//         if (currentLong == null ||
//             !(currentLong.IsWaiting() || currentLong.IsHolding()))
//         {
//             currentLong = null;
//             HideHand();
//             mode = Mode.None;
//         }
//     }

//     // Bước 1: chờ người chơi vuốt lên -> Player nhảy -> chạy tiếp.
//     private void UpdateLongSwipe()
//     {
//         if (currentLong == null)
//         {
//             EndStep();
//             return;
//         }

//         if (!swipeDone)
//             return;

//         Unfreeze();
//         HideHand();
//         mode = Mode.LongFly;
//     }

//     // Player đang bay lên, nốt đang chạy. Tới lúc đầu nốt chạm Player thì dừng lại bước giữ nút.
//     private void UpdateLongFly()
//     {
//         if (currentLong == null || !currentLong.IsWaiting())
//         {
//             currentLong = null;
//             mode = Mode.None;
//             return;
//         }

//         if (spawner.SongTime >= currentLong.GetNoteTime() - longPauseLead)
//         {
//             holdTimer = 0f;
//             mode = Mode.Long;
//             Freeze();
//             ShowHand(longHint);
//         }
//     }

//     // =====================================================
//     // FREEZE / HAND
//     // =====================================================

//     private void EndStep()
//     {
//         Unfreeze();
//         HideHand();
//         currentKey = null;
//         currentLong = null;
//         mode = Mode.None;
//     }

//     private void Freeze()
//     {
//         // Chỉ dừng nhạc + dòng thời gian bài (nốt, spawn).
//         // Player, vật lý và animation vẫn chạy bình thường.
//         frozen = true;
//         spawner.PauseSong();
//     }

//     private void Unfreeze()
//     {
//         frozen = false;
//         spawner.ResumeSong();
//     }

//     private void ShowHand(string hint)
//     {
//         if (hand != null)
//             hand.gameObject.SetActive(true);

//         SetHint(hint);
//         AnimateHand();
//     }

//     private void HideHand()
//     {
//         if (hand != null)
//             hand.gameObject.SetActive(false);

//         SetHint("");
//     }

//     private void SetHint(string text)
//     {
//         if (hintText != null)
//             hintText.text = text;
//     }

//     private void AnimateHand()
//     {
//         if (hand == null || mode == Mode.LongFly)
//             return;

//         float t = Time.unscaledTime;

//         // Bước vuốt: bàn tay lướt từ swipeStart lên trên.
//         if (mode == Mode.LongSwipe)
//         {
//             if (swipeStart == null)
//                 return;

//             float sp = Mathf.Repeat(t * swipeSpeed, 1f);
//             hand.position = swipeStart.position
//                           + (Vector3)handOffset
//                           + new Vector3(0f, sp * swipeDistance, 0f);
//             hand.localScale = Vector3.one;
//             return;
//         }

//         if (currentTarget == null)
//             return;
//         Vector3 basePos = currentTarget.position + (Vector3)handOffset;

//         bool holdStyle = mode == Mode.Long || mode == Mode.LongFollow;

//         if (holdStyle)
//         {
//             // Nốt dài: bàn tay ấn xuống và giữ (nhấp nhẹ).
//             hand.position = basePos;
//             float s = tapScaleMin + 0.04f * Mathf.Sin(t * 3f);
//             hand.localScale = Vector3.one * s;
//         }
//         else
//         {
//             // Nốt thường: bàn tay chạm - nhấc liên tục.
//             float p = (Mathf.Sin(t * tapSpeed) + 1f) * 0.5f; // 0..1
//             hand.position = basePos + new Vector3(0f, p * bobAmount, 0f);
//             hand.localScale = Vector3.one * Mathf.Lerp(tapScaleMin, 1f, p);
//         }
//     }
// }