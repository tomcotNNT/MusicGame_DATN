using UnityEngine;

public class PlayerAttackAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Trigger")]
    [Tooltip("Bật = dùng 2 trigger riêng cho Left/Right, tắt = dùng chung 1 trigger")]
    [SerializeField] private bool separateLanes = false;
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string attackLeftTrigger = "AttackLeft";
    [SerializeField] private string attackRightTrigger = "AttackRight";

    [Header("Đánh luân phiên (tuỳ chọn)")]
    [Tooltip("Bật = xen kẽ 2 anim đánh (Attack1/Attack2) cho đỡ lặp")]
    [SerializeField] private bool alternate = false;
    [SerializeField] private string attack2Trigger = "Attack2";

    private bool toggle;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()  { PianoKey.NoteHit += OnNoteHit; }
    private void OnDisable() { PianoKey.NoteHit -= OnNoteHit; }

    private void OnNoteHit(PianoKey.KeyType type)
    {
        if (animator == null)
            return;

        string trigger;

        if (separateLanes)
            trigger = type == PianoKey.KeyType.Left ? attackLeftTrigger : attackRightTrigger;
        else if (alternate)
        {
            toggle = !toggle;
            trigger = toggle ? attackTrigger : attack2Trigger;
        }
        else
            trigger = attackTrigger;

        animator.ResetTrigger(trigger);
        animator.SetTrigger(trigger);
    }
}