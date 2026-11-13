using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Lane của nốt: Left = hit point dưới, Right = hit point trên
public enum Lanee
{
    Left,
    Right
}

// Quản lý ấn / giữ ở 2 nửa màn hình:
//   nửa TRÁI  = Lanee.Left  (hit point dưới)
//   nửa PHẢI  = Lanee.Right (hit point trên)
// Gắn vào 1 GameObject trong scene (chỉ cần 1 cái).
//
// API dùng cho nốt:
//   HoldInput.IsHeld(lane)       -> đang giữ (nốt dài)
//   HoldInput.WasPressed(lane)   -> vừa chạm xuống ở frame này (nốt ngắn)
//   HoldInput.WasReleased(lane)  -> vừa nhả ở frame này
[DefaultExecutionOrder(-100)] // chạy trước PianoKey / LongNote để họ đọc được input của frame này
public class HoldInput : MonoBehaviour
{
    [Header("Screen Zones")]
    [Tooltip("Vị trí chia màn hình theo chiều ngang. 0.5 = chia đôi. Bên trái = Left, bên phải = Right")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float splitRatio = 0.5f;

    [Header("Ignore UI")]
    [Tooltip("Các vùng UI (nút Pause, Skip...) KHÔNG tính là chạm nốt")]
    [SerializeField] private RectTransform[] ignoreAreas;
    [Tooltip("Camera của Canvas. Để trống nếu Canvas là Screen Space - Overlay")]
    [SerializeField] private Camera uiCamera;

    [Header("Keyboard (để test trong Editor)")]
    [SerializeField] private bool enableKeyboard = true;

    // ---------------- Static API ----------------

    private static readonly bool[] held = new bool[2];
    private static readonly bool[] external = new bool[2];
    private static readonly int[] pressedFrame = { -1, -1 };
    private static readonly int[] releasedFrame = { -1, -1 };

    public static event Action<Lanee> Pressed;
    public static event Action<Lanee> Released;

    private static int Idx(Lanee lane) => lane == Lanee.Left ? 0 : 1;

    public static bool IsHeld(Lanee lane) => held[Idx(lane)];
    public static bool WasPressed(Lanee lane) => pressedFrame[Idx(lane)] == Time.frameCount;
    public static bool WasReleased(Lanee lane) => releasedFrame[Idx(lane)] == Time.frameCount;

    // Cho UI Button / script khác bấm giả (ví dụ OnPointerDown/Up của nút trên màn hình)
    public static void SetHeld(Lanee lane, bool down) => external[Idx(lane)] = down;

    // ---------------- Per-frame state ----------------

    private readonly bool[] heldNow = new bool[2];
    private readonly bool[] pressedNow = new bool[2];

    // Mỗi ngón tay / chuột khoá vào lane lúc chạm xuống => kéo ngón sang nửa kia vẫn giữ đúng lane
    private readonly Dictionary<int, int> lockedLane = new Dictionary<int, int>();
    private readonly HashSet<int> activeIds = new HashSet<int>();
    private readonly List<int> staleIds = new List<int>();

    private const int MouseId = -1;

    private void OnDisable()
    {
        for (int i = 0; i < 2; i++)
        {
            held[i] = false;
            external[i] = false;
        }

        lockedLane.Clear();
    }

    private void Update()
    {
        heldNow[0] = heldNow[1] = false;
        pressedNow[0] = pressedNow[1] = false;
        activeIds.Clear();

        // Pause thì không nhận input
        bool blocked = PianoKeySpawner.Instance != null && PianoKeySpawner.Instance.IsPaused;

        if (!blocked)
        {
            ReadPointers();

            if (enableKeyboard)
                ReadKeyboard();

            for (int i = 0; i < 2; i++)
                if (external[i])
                    heldNow[i] = true;
        }

        // Xoá khoá lane của ngón tay đã nhấc lên
        staleIds.Clear();
        foreach (var kv in lockedLane)
            if (!activeIds.Contains(kv.Key))
                staleIds.Add(kv.Key);
        foreach (int id in staleIds)
            lockedLane.Remove(id);

        Apply(0, Lanee.Left);
        Apply(1, Lanee.Right);
    }

    private void Apply(int i, Lanee lane)
    {
        bool now = heldNow[i];
        bool was = held[i];

        bool pressed = pressedNow[i] || (now && !was);
        bool released = was && !now;

        held[i] = now;

        if (pressed)
        {
            pressedFrame[i] = Time.frameCount;
            Pressed?.Invoke(lane);
        }

        if (released)
        {
            releasedFrame[i] = Time.frameCount;
            Released?.Invoke(lane);
        }
    }

    // =====================================================
    // POINTER (touch + mouse)
    // =====================================================

    private void ReadPointers()
    {
#if ENABLE_INPUT_SYSTEM
        Touchscreen ts = Touchscreen.current;
        if (ts != null)
        {
            foreach (var t in ts.touches)
            {
                bool down = t.press.isPressed;
                bool began = t.press.wasPressedThisFrame;
                if (!down && !began) continue;

                Register(t.touchId.ReadValue(), t.position.ReadValue(), down, began);
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            bool down = mouse.leftButton.isPressed;
            bool began = mouse.leftButton.wasPressedThisFrame;
            if (down || began)
                Register(MouseId, mouse.position.ReadValue(), down, began);
        }
#else
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);

            bool began = t.phase == TouchPhase.Began;
            bool down = t.phase == TouchPhase.Began ||
                        t.phase == TouchPhase.Moved ||
                        t.phase == TouchPhase.Stationary;
            if (!down) continue;

            Register(t.fingerId, t.position, down, began);
        }

        if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))
            Register(MouseId, Input.mousePosition, Input.GetMouseButton(0), Input.GetMouseButtonDown(0));
#endif
    }

    private void Register(int id, Vector2 pos, bool down, bool began)
    {
        int laneIdx;

        if (!lockedLane.TryGetValue(id, out laneIdx))
        {
            // Chạm bắt đầu ở ngoài game (UI) hoặc không phải frame bắt đầu => bỏ qua
            if (!began || IsOverIgnoredArea(pos))
                return;

            laneIdx = pos.x < Screen.width * splitRatio ? 0 : 1;
            lockedLane[id] = laneIdx;
        }

        activeIds.Add(id);

        if (down) heldNow[laneIdx] = true;
        if (began) pressedNow[laneIdx] = true;
    }

    private bool IsOverIgnoredArea(Vector2 screenPos)
    {
        if (ignoreAreas == null)
            return false;

        foreach (RectTransform r in ignoreAreas)
        {
            if (r != null && r.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(r, screenPos, uiCamera))
                return true;
        }

        return false;
    }

    // =====================================================
    // KEYBOARD (A / ← = trái, D / → = phải)
    // =====================================================

    private void ReadKeyboard()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) heldNow[0] = true;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) heldNow[1] = true;

        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) pressedNow[0] = true;
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) pressedNow[1] = true;
#else
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) heldNow[0] = true;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) heldNow[1] = true;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) pressedNow[0] = true;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) pressedNow[1] = true;
#endif
    }
}