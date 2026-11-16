using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerRhythmController : MonoBehaviour
{
    [Header("Cài đặt phím bấm")]
    public KeyCode leftKey = KeyCode.L;   // Phím đánh bên Trái
    public KeyCode rightKey = KeyCode.R;  // Phím đánh bên Phải

    private Animator _anim;
    private Vector3 _baseScale;
    private bool _isHoldingBoth = false;

    // Khớp chính xác tên 4 Trigger bạn đã tạo trong Animator
    private readonly string[] _attackTriggers = { "Attack3", "Attack4", "Attack5", "Attack6" };

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _baseScale = transform.localScale;
    }

    private void Update()
    {
        bool holdLeft = Input.GetKey(leftKey);
        bool holdRight = Input.GetKey(rightKey);

        // 1. Giữ đồng thời cả 2 phím R + L -> Hóa thiên thần giữ nguyên
        if (holdLeft && holdRight)
        {
            if (!_isHoldingBoth)
            {
                _isHoldingBoth = true;
                // Đưa mặt về hướng chuẩn (Phải) khi hóa thiên thần
                transform.localScale = new Vector3(Mathf.Abs(_baseScale.x), _baseScale.y, _baseScale.z);
                _anim.SetBool("IsAngelHold", true);
            }
            return;
        }
        else if (_isHoldingBoth)
        {
            // Nhả 1 trong 2 phím -> Thoát thiên thần về Idle ngay lập tức
            _isHoldingBoth = false;
            _anim.SetBool("IsAngelHold", false);
        }

        // 2. Bấm phím L -> Lật mặt sang Trái (Scale X âm) và tung chiêu
        if (Input.GetKeyDown(leftKey))
        {
            TriggerRandomAttack(-1);
        }
        // 3. Bấm phím R -> Lật mặt sang Phải (Scale X dương) và tung chiêu
        else if (Input.GetKeyDown(rightKey))
        {
            TriggerRandomAttack(1);
        }
    }

    private void TriggerRandomAttack(int direction)
    {
        // Khóa tâm tại chân (Pivot = Bottom) khi lật trái/phải
        transform.localScale = new Vector3(Mathf.Abs(_baseScale.x) * direction, _baseScale.y, _baseScale.z);

        // Chọn ngẫu nhiên 1 trong 4 đòn attack 3, 4, 5, 6
        int randomIndex = Random.Range(0, _attackTriggers.Length);
        _anim.SetTrigger(_attackTriggers[randomIndex]);
    }
}