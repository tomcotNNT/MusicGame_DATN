using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDefenseController : MonoBehaviour
{
    [Header("Original Center Position")]
    public Vector3 originPosition = new Vector3(0f, -1.8f, 0f);

    [Header("Dash Speed (Lướt tới mục tiêu)")]
    public float dashSpeed = 35f;
    public float returnSpeed = 30f;
    private bool isDashing = false;

    [Header("Checkpoints (HitPoints)")]
    public Transform cpLeftTop;
    public Transform cpLeftBottom;
    public Transform cpRightTop;
    public Transform cpRightBottom;

    [Header("Miss Cooldown (Phạt bấm hụt)")]
    public float missCooldown = 0.35f;
    private float cooldownTimer = 0f;

    [Header("Danh sách quái theo 2 cánh")]
    public List<OFDP_Enemy> leftEnemies = new List<OFDP_Enemy>();
    public List<OFDP_Enemy> rightEnemies = new List<OFDP_Enemy>();

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        originPosition = transform.position;
    }

    void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        // CHỈ CẦN 2 NÚT DUY NHẤT: BÊN TRÁI VÀ BÊN PHẢI
        // Bấm Bên Trái: Nút A, Mũi tên Trái, hoặc Chuột Trái
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetMouseButtonDown(0))
        {
            HandleAttackInput(isLeft: true);
        }
        // Bấm Bên Phải: Nút D, Mũi tên Phải, hoặc Chuột Phải
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetMouseButtonDown(1))
        {
            HandleAttackInput(isLeft: false);
        }
    }

    void HandleAttackInput(bool isLeft)
    {
        // 1. Quay mặt theo hướng đánh
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !isLeft; // Tùy sprite gốc quay hướng nào, điều chỉnh lật mặt sang bên đánh
        }

        List<OFDP_Enemy> targetList = isLeft ? leftEnemies : rightEnemies;
        targetList.RemoveAll(e => e == null);

        // Tìm con quái gần nhất đang trong vùng tầm đánh Checkpoint
        OFDP_Enemy target = null;
        float minDistance = float.MaxValue;

        foreach (var enemy in targetList)
        {
            if (enemy.isInAttackRange)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    target = enemy;
                }
            }
        }

        if (target != null)
        {
            // ĐÁNH TRÚNG QUÁI!
            targetList.Remove(target);

            // Xác định điểm Checkpoint cần bay tới dựa vào làn của con quái đó
            Transform targetCheckpoint = null;
            if (isLeft)
            {
                targetCheckpoint = target.isHighLane ? cpLeftTop : cpLeftBottom;
            }
            else
            {
                targetCheckpoint = target.isHighLane ? cpRightTop : cpRightBottom;
            }

            // Kích hoạt animation tương ứng
            if (animator != null)
            {
                animator.SetTrigger(target.isHighLane ? "AttackHigh" : "AttackLow");
            }

            // Bay tới vị trí Checkpoint chém quái rồi giật về giữa
            StopAllCoroutines();
            StartCoroutine(DashAttackRoutine(targetCheckpoint.position, target));
        }
        else
        {
            // BẤM HỤT (Không có quái nào trong vùng Checkpoint)
            cooldownTimer = missCooldown;
            Debug.Log("<color=red>BẤM HỤT! Bị khóa đòn đánh trong thời gian ngắn!</color>");
        }
    }

    IEnumerator DashAttackRoutine(Vector3 targetPos, OFDP_Enemy enemy)
    {
        isDashing = true;

        // 1. Lướt siêu tốc tới Checkpoint
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, dashSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;

        // 2. Chém tiêu diệt quái tại Checkpoint
        if (enemy != null)
        {
            enemy.OnHit();
        }

        // Chờ 0.05s để tạo cảm giác chém trúng lực (Hitstop)
        yield return new WaitForSeconds(0.05f);

        // 3. Giật lùi về tâm (0, -1.8, 0)
        while (Vector3.Distance(transform.position, originPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originPosition, returnSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = originPosition;

        isDashing = false;
    }

    public void RegisterEnemy(OFDP_Enemy enemy, bool isLeft)
    {
        List<OFDP_Enemy> list = isLeft ? leftEnemies : rightEnemies;
        if (!list.Contains(enemy)) list.Add(enemy);
    }

    public void UnregisterEnemy(OFDP_Enemy enemy, bool isLeft)
    {
        List<OFDP_Enemy> list = isLeft ? leftEnemies : rightEnemies;
        if (list.Contains(enemy)) list.Remove(enemy);
    }

    public void TakeDamage()
    {
        if (animator != null) animator.SetTrigger("Hurt");
        Debug.Log("<color=red>PLAYER DÍNH ĐÒN!</color>");
    }
}