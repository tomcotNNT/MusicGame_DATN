using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RhythmNoteTestSimulator : MonoBehaviour
{
    [Header("1. Cấu hình nhịp nhạc mô phỏng")]
    [Tooltip("Tốc độ nhịp nhạc giả lập (BPM) để test độ dứt khoát của đòn đánh")]
    public float testBPM = 120f;

    [Header("2. Phím bấm Test")]
    public KeyCode leftNoteKey = KeyCode.L;   // Bấm phím L -> Đánh bên Trái
    public KeyCode rightNoteKey = KeyCode.R;  // Bấm phím R -> Đánh bên Phải

    [Header("3. Chế độ Auto-Play (Tự động đánh theo nhịp)")]
    public bool autoPlayTest = false;         // Tích chọn để nhân vật tự đánh ngẫu nhiên theo nhịp nhạc
    public bool includeAngelHoldInAuto = true;// Cho phép tự động test hóa thiên thần trong Auto-Play

    private Animator _anim;
    private Vector3 _baseScale;
    private bool _isHoldingAngel = false;

    // Khớp chính xác 4 Trigger đã nối trong Player Khoa.controller
    private readonly string[] _attackTriggers = { "Attack3", "Attack4", "Attack5", "Attack6" };

    // Biến đếm nhịp nội bộ cho chế độ Auto-Play
    private float _beatInterval;
    private float _beatTimer;
    private int _beatCounter = 0;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _baseScale = transform.localScale;
    }

    private void Start()
    {
        _beatInterval = 60f / testBPM;
    }

    private void Update()
    {
        // Cập nhật lại khoảng cách nhịp nếu bạn chỉnh BPM trực tiếp lúc đang Play
        if (testBPM > 0) _beatInterval = 60f / testBPM;

        if (autoPlayTest)
        {
            RunAutoBeatSimulation();
        }
        else
        {
            RunManualKeyboardTest();
        }
    }

    // --- CHẾ ĐỘ 1: TỰ BẤM PHÍM L / R ĐỂ TEST NHẠT NỐT ---
    private void RunManualKeyboardTest()
    {
        bool holdL = Input.GetKey(leftNoteKey);
        bool holdR = Input.GetKey(rightNoteKey);

        // Giữ đồng thời cả L + R -> Mô phỏng ăn nốt dài (Hóa thiên thần giữ nguyên)
        if (holdL && holdR)
        {
            SetAngelHold(true);
            return;
        }
        else if (_isHoldingAngel)
        {
            // Nhả 1 trong 2 phím -> Hết nốt dài, về Idle ngay lập tức
            SetAngelHold(false);
        }

        // Bấm nốt đơn Trái (L) -> Lật sang Trái (-1) và random đòn đánh
        if (Input.GetKeyDown(leftNoteKey))
        {
            TriggerRandomNoteHit(-1);
        }
        // Bấm nốt đơn Phải (R) -> Lật sang Phải (1) và random đòn đánh
        else if (Input.GetKeyDown(rightNoteKey))
        {
            TriggerRandomNoteHit(1);
        }
    }

    // --- CHẾ ĐỘ 2: TỰ ĐỘNG MÔ PHỎNG NHỊP RỚT NỐT (AUTO-TEST) ---
    private void RunAutoBeatSimulation()
    {
        _beatTimer += Time.deltaTime;
        if (_beatTimer >= _beatInterval)
        {
            _beatTimer -= _beatInterval;
            _beatCounter++;

            // Cứ mỗi 8 nhịp thì mô phỏng một đoạn nốt dài (giữ thiên thần trong 4 nhịp)
            if (includeAngelHoldInAuto && _beatCounter % 8 == 0)
            {
                SetAngelHold(true);
                return;
            }

            // Nếu đang giữ thiên thần, giữ đủ 4 nhịp rồi tự thả về đánh thường
            if (_isHoldingAngel)
            {
                if (_beatCounter % 8 == 4)
                {
                    SetAngelHold(false);
                }
                return;
            }

            // Các nhịp bình thường: Random hướng Trái (-1) hoặc Phải (1) và random đòn đánh
            int randomDir = (Random.value > 0.5f) ? 1 : -1;
            TriggerRandomNoteHit(randomDir);
        }
    }

    // Hàm gọi đòn đánh ngẫu nhiên (Dùng chung cho cả test và ghép code thật sau này)
    public void TriggerRandomNoteHit(int direction)
    {
        // Khóa trục tại Pivot = Bottom khi lật mặt Trái/Phải
        transform.localScale = new Vector3(Mathf.Abs(_baseScale.x) * direction, _baseScale.y, _baseScale.z);

        int randomIdx = Random.Range(0, _attackTriggers.Length);
        string chosenTrigger = _attackTriggers[randomIdx];
        _anim.SetTrigger(chosenTrigger);

        Debug.Log($"[Test Nhạc] Hướng: {(direction == -1 ? "TRÁI (L)" : "PHẢI (R)")} | Chiêu: {chosenTrigger}");
    }

    // Hàm bật/tắt trạng thái giữ nốt dài thiên thần
    public void SetAngelHold(bool active)
    {
        if (_isHoldingAngel == active) return;

        _isHoldingAngel = active;
        if (active)
        {
            // Đưa mặt về hướng chuẩn khi hóa thiên thần
            transform.localScale = new Vector3(Mathf.Abs(_baseScale.x), _baseScale.y, _baseScale.z);
            Debug.Log("[Test Nhạc] Bắt đầu giữ nốt dài -> HÓA THIÊN THẦN");
        }
        else
        {
            Debug.Log("[Test Nhạc] Thả nốt dài -> Trở về IDLE");
        }

        _anim.SetBool("IsAngelHold", active);
    }
}