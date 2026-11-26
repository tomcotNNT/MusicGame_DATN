using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boss màn 1: đi vào vị trí đứng rồi đánh nhau với người chơi.
/// Nếu cùng object có BossHealth thì máu, thanh máu, chết/lùi do BossHealth quản lý
/// (để đạn Fever và đánh nốt cùng trừ vào một thanh máu).
/// </summary>
public class Boss : MonoBehaviour
{
    [SerializeField] private int maxHealth = 20;
    [SerializeField] private float enterSpeed = 4f;
    [SerializeField] private Image healthFill;   // tùy chọn, Image Type = Filled

    public event Action Defeated;
    public bool IsReady { get; private set; }

    private int health;
    private Vector3 stopPosition;
    private bool dead;
    private BossHealth bossHealth;

    private void Awake()
    {
        bossHealth = GetComponent<BossHealth>();

        if (bossHealth != null)
            bossHealth.Died += OnBossHealthDied;
    }

    private void OnDestroy()
    {
        if (bossHealth != null)
            bossHealth.Died -= OnBossHealthDied;
    }

    /// <summary>Đặt máu tối đa (ví dụ = số nốt của bài). Gọi trước hoặc ngay sau Begin.</summary>
    public void SetMaxHealth(int value)
    {
        maxHealth = Mathf.Max(1, value);
        health = maxHealth;

        if (bossHealth != null)
            bossHealth.SetMaxHealth(maxHealth);

        RefreshBar();
    }

    public void Begin(Vector3 stopPos)
    {
        stopPosition = stopPos;
        health = maxHealth;
        RefreshBar();
    }

    private void Update()
    {
        if (IsReady || dead)
            return;

        // Đi vào vị trí đứng
        transform.position = Vector3.MoveTowards(
            transform.position, stopPosition, enterSpeed * Time.deltaTime);

        if ((transform.position - stopPosition).sqrMagnitude < 0.01f)
            IsReady = true;
    }

    // Trừ máu được ngay từ lúc boss xuất hiện (nốt đầu bài cũng tính)
    public void TakeDamage(int amount)
    {
        if (dead || amount <= 0)
            return;

        if (bossHealth != null)
        {
            bossHealth.TakeDamage(amount);   // chết -> OnBossHealthDied
            return;
        }

        health = Mathf.Max(0, health - amount);
        RefreshBar();

        if (health <= 0)
        {
            dead = true;
            Defeated?.Invoke();
            Destroy(gameObject);
        }
    }

    // BossHealth tự chạy animation chết + lùi + huỷ
    private void OnBossHealthDied()
    {
        if (dead) return;

        dead = true;
        IsReady = true;   // ngừng tự di chuyển, nhường cho BossHealth lùi
        Defeated?.Invoke();
    }

    // Boss tấn công. Tạm thời chỉ log, sau này thêm animation/hiệu ứng/trừ máu player ở đây
    public void Attack()
    {
        if (!IsReady || dead)
            return;

        Debug.Log("Boss Attack!");
    }

    private void RefreshBar()
    {
        if (healthFill != null)
            healthFill.fillAmount = (float)health / maxHealth;
    }
}
