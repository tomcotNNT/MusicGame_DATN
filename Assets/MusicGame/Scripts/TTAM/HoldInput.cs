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

// Quản lý ấn / giữ bằng 2 nút LaneButton trên màn hình:
//   nút TRÁI  = Lanee.Left  (hit point dưới)
//   nút PHẢI  = Lanee.Right (hit point trên)
// Gắn vào 1 GameObject trong scene (chỉ cần 1 cái).
//
// API dùng cho nốt:
//   HoldInput.IsHeld(lane)       -> đang giữ (nốt dài)
//   HoldInput.WasPressed(lane)   -> vừa chạm xuống ở frame này (nốt ngắn)
//   HoldInput.WasReleased(lane)  -> vừa nhả ở frame này
[DefaultExecutionOrder(-100)] // chạy trước PianoKey / LongNote để họ đọc được input của frame này
public class HoldInput : MonoBehaviour
{
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
    // Pause / Game Over (timeScale = 0): nốt đứng yên ngay vạch -> không cho bấm trúng
    public static bool WasPressed(Lanee lane) => Time.timeScale > 0f && pressedFrame[Idx(lane)] == Time.frameCount;
    public static bool WasReleased(Lanee lane) => releasedFrame[Idx(lane)] == Time.frameCount;

    // Cho UI Button / script khác bấm giả (ví dụ OnPointerDown/Up của nút trên màn hình)
    public static void SetHeld(Lanee lane, bool down) => external[Idx(lane)] = down;

    // ---------------- Per-frame state ----------------

    private readonly bool[] heldNow = new bool[2];
    private readonly bool[] pressedNow = new bool[2];

    // Lần bấm nút đến từ EventSystem (có thể chạy trước/sau Update này) -> giữ lại tới Update kế tiếp.
    // Nhờ vậy chạm rất nhanh (bấm + nhả trong 1 frame) vẫn không bị mất nốt.
    private readonly bool[] buttonPressBuffer = new bool[2];

    private void OnEnable()
    {
        LaneButtonInput.Pressed += OnLaneButtonPressed;
    }

    private void OnDisable()
    {
        LaneButtonInput.Pressed -= OnLaneButtonPressed;

        for (int i = 0; i < 2; i++)
        {
            held[i] = false;
            external[i] = false;
            buttonPressBuffer[i] = false;
        }
    }

    private void OnLaneButtonPressed(bool isLeft)
    {
        buttonPressBuffer[isLeft ? 0 : 1] = true;
    }

    private void Update()
    {
        heldNow[0] = heldNow[1] = false;
        pressedNow[0] = pressedNow[1] = false;

        // Pause thì không nhận input
        bool blocked = PianoKeySpawner.Instance != null && PianoKeySpawner.Instance.IsPaused;

        if (!blocked)
        {
            ReadLaneButtons();

            if (enableKeyboard)
                ReadKeyboard();

            for (int i = 0; i < 2; i++)
                if (external[i])
                    heldNow[i] = true;
        }

        buttonPressBuffer[0] = buttonPressBuffer[1] = false;

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
    // 2 NÚT TRÊN MÀN HÌNH (LaneButton)
    // =====================================================

    private void ReadLaneButtons()
    {
        for (int i = 0; i < 2; i++)
        {
            bool isLeft = i == 0;

            if (LaneButtonInput.IsHeld(isLeft))
                heldNow[i] = true;

            if (buttonPressBuffer[i])
                pressedNow[i] = true;
        }
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