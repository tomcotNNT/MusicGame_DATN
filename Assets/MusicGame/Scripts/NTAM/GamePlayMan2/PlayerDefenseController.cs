using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HitAccuracy { Perfect, Good, Miss }

public class PlayerDefenseController : MonoBehaviour
{
    [Header("Position Cố Định")]
    public Vector3 originPosition = new Vector3(0f, -2.6f, 0f);

    [Header("2 Checkpoints (HitZones X = +-2.5)")]
    public Transform hitZoneLeft;
    public Transform hitZoneRight;

    [Header("Phạm Vi Phân Loại Nhịp (Khoảng cách tới Checkpoint)")]
    public float perfectWindow = 0.4f; // Khoảng cách lý tưởng
    public float goodWindow = 1.0f;    // Khoảng cách chấp nhận được

    [Header("Hiệu Ứng VFX Đánh Trúng")]
    public GameObject vfxPerfectPrefab; // Kéo Prefab tia chém/nổ vàng rực
    public GameObject vfxGoodPrefab;    // Kéo Prefab nổ nhẹ màu xanh lá/trắng

    [Header("Hệ Thống Combo & Điểm")]
    public int currentCombo = 0;
    public int maxCombo = 0;
    public int currentScore = 0;
    public float missCooldown = 0.25f;
    private float cooldownTimer = 0f;

    [Header("Danh Sách Quái")]
    public List<OFDP_Enemy> leftEnemies = new List<OFDP_Enemy>();
    public List<OFDP_Enemy> rightEnemies = new List<OFDP_Enemy>();

    private Animator animator;
    private float originalScaleX;

    void Awake()
    {
        animator = GetComponent<Animator>();
        originPosition = transform.position;
        originalScaleX = Mathf.Abs(transform.localScale.x);
    }

    void Update()
    {
        transform.position = originPosition;

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        bool pressLeft = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        bool pressRight = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);

        if (Input.GetMouseButtonDown(0))
        {
            if (Input.mousePosition.x < Screen.width / 2f) pressLeft = true;
            else pressRight = true;
        }

        if (pressLeft)
        {
            HandleAttack(isLeft: true);
        }
        else if (pressRight)
        {
            HandleAttack(isLeft: false);
        }
    }

    void HandleAttack(bool isLeft)
    {
        // 1. Lật mặt đúng hướng bấm (ảnh gốc nhìn Phải)
        Vector3 currentScale = transform.localScale;
        currentScale.x = isLeft ? -originalScaleX : originalScaleX;
        transform.localScale = currentScale;

        // 2. Xác định tâm Checkpoint tương ứng
        Transform targetZone = isLeft ? hitZoneLeft : hitZoneRight;
        Vector3 checkCenter = targetZone != null ? targetZone.position : (originPosition + new Vector3(isLeft ? -2.5f : 2.5f, 0, 0));

        // 3. Tìm quái gần Checkpoint nhất và đánh giá độ chuẩn xác
        OFDP_Enemy target = FindBestTarget(isLeft, checkCenter, out HitAccuracy accuracy);

        if (accuracy != HitAccuracy.Miss && target != null)
        {
            // === ĐÁNH TRÚNG QUÁI (PERFECT HOẶC GOOD) ===
            if (isLeft) leftEnemies.Remove(target);
            else rightEnemies.Remove(target);

            currentCombo++;
            if (currentCombo > maxCombo) maxCombo = currentCombo;

            Vector3 hitPos = target.transform.position;

            if (accuracy == HitAccuracy.Perfect)
            {
                currentScore += 100;
                Debug.Log($"<color=#FFE600><b>[PERFECT!!!]</b></color> <color=#00FF66>COMBO x{currentCombo}</color> | Score: {currentScore}");
                SpawnVFX(vfxPerfectPrefab, hitPos);
            }
            else // Good
            {
                currentScore += 50;
                Debug.Log($"<color=#00E5FF><b>[GOOD!]</b></color> <color=#00FF66>COMBO x{currentCombo}</color> | Score: {currentScore}");
                SpawnVFX(vfxGoodPrefab, hitPos);
            }

            PlayRandomAttack();
            target.OnHit();
        }
        else
        {
            // === MISS: BẤM TRƯỢT HOẶC QUÁI CHƯA TỚI ===
            cooldownTimer = missCooldown;
            currentCombo = 0;
            Debug.Log("<color=red><b>[MISS!] ĐỨT CHUỖI COMBO! KHÓA PHÍM 0.25S</b></color>");

            // Kích hoạt Checkpoint nháy đỏ cảnh báo
            if (targetZone != null)
            {
                LaneTrigger lt = targetZone.GetComponent<LaneTrigger>();
                if (lt != null) lt.FlashMiss();
            }
        }
    }

    OFDP_Enemy FindBestTarget(bool isLeft, Vector3 zonePos, out HitAccuracy accuracy)
    {
        accuracy = HitAccuracy.Miss;
        List<OFDP_Enemy> list = isLeft ? leftEnemies : rightEnemies;
        list.RemoveAll(e => e == null);

        // Gom danh sách quái quanh vùng Checkpoint
        Collider2D[] cols = Physics2D.OverlapCircleAll(zonePos, goodWindow + 0.5f);
        OFDP_Enemy closestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (var c in cols)
        {
            OFDP_Enemy e = c.GetComponent<OFDP_Enemy>();
            if (e != null && e.isFromLeft == isLeft)
            {
                float dist = Mathf.Abs(e.transform.position.x - zonePos.x);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestEnemy = e;
                }
            }
        }

        if (closestEnemy != null)
        {
            if (minDistance <= perfectWindow)
            {
                accuracy = HitAccuracy.Perfect;
            }
            else if (minDistance <= goodWindow)
            {
                accuracy = HitAccuracy.Good;
            }
        }

        return closestEnemy;
    }

    void SpawnVFX(GameObject prefab, Vector3 pos)
    {
        if (prefab != null)
        {
            GameObject vfx = Instantiate(prefab, pos, Quaternion.identity);
            Destroy(vfx, 0.6f); // Tự hủy sau 0.6 giây
        }
    }

    void PlayRandomAttack()
    {
        if (animator == null) return;
        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
        animator.ResetTrigger("Attack3");
        animator.ResetTrigger("AttackLow");

        int rand = Random.Range(0, 4);
        switch (rand)
        {
            case 0: animator.SetTrigger("Attack1"); break;
            case 1: animator.SetTrigger("Attack2"); break;
            case 2: animator.SetTrigger("Attack3"); break;
            case 3: animator.SetTrigger("AttackLow"); break;
        }
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
        currentCombo = 0;
        if (animator != null) animator.SetTrigger("Hurt");
        Debug.Log("<color=red><b>PLAYER BỊ ĐÁNH TRÚNG! RESET COMBO VỀ 0!</b></color>");
    }

    private void OnDrawGizmosSelected()
    {
        if (hitZoneLeft != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hitZoneLeft.position, perfectWindow);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(hitZoneLeft.position, goodWindow);
        }
        if (hitZoneRight != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hitZoneRight.position, perfectWindow);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(hitZoneRight.position, goodWindow);
        }
    }
}