using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private int damage = 10;
    public GameObject Hitbox;

    [Header("Component")]
     private Animator animator;

    private float lastAttackTime;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Start()
    {
        Hitbox.SetActive(false);
    }

    private void Update()
    {
        // Test bằng phím Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Attack();
        }
    }

    public void Attack()
    {
        // Không cho đánh liên tục
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        // Chạy animation Attack
        animator.SetTrigger("Attack");
    }

    public void ActivateHitbox()
    {
        Hitbox.SetActive(true);
    }

    public void DeactivateHitbox()
    {
        Hitbox.SetActive(false);
    }
}