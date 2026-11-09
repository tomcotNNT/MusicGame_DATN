using UnityEngine;

public class LaneTrigger : MonoBehaviour
{
    [Header("Cấu hình làn")]
    public bool isLeft = true;
    public PlayerDefenseController player;

    [Header("Hiệu ứng ánh sáng Checkpoint")]
    public Color normalColor = new Color(0.2f, 1f, 0.4f, 0.5f);  // Xanh nhạt bình thường
    public Color activeGlowColor = new Color(1f, 0.95f, 0.2f, 1f); // Vàng rực khi có quái trong nhịp
    public Color missColor = new Color(1f, 0.2f, 0.2f, 1f);        // Đỏ cảnh báo Miss
    public float pulseScale = 1.3f;

    private SpriteRenderer sr;
    private Vector3 initialScale;
    private int enemyCount = 0;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
        if (sr != null) sr.color = normalColor;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        OFDP_Enemy enemy = collision.GetComponent<OFDP_Enemy>();
        if (enemy != null)
        {
            enemyCount++;
            enemy.isInAttackRange = true;

            if (player != null)
            {
                player.RegisterEnemy(enemy, isLeft);
            }

            // Bật hiệu ứng sáng đèn báo nhịp
            SetVisualState(activeGlowColor, pulseScale);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        OFDP_Enemy enemy = collision.GetComponent<OFDP_Enemy>();
        if (enemy != null)
        {
            enemyCount = Mathf.Max(0, enemyCount - 1);

            // NẾU QUÁI RA KHỎI VÙNG MÀ VẪN CÒN SỐNG -> NGƯỜI CHƠI BỎ LỠ (MISS)
            if (enemy.isInAttackRange)
            {
                enemy.isInAttackRange = false;
                if (player != null)
                {
                    player.UnregisterEnemy(enemy, isLeft);
                }

                // Ra lệnh cho quái lao thẳng vào người Player gây sát thương
                enemy.TriggerMissRush();

                // Nháy đèn đỏ báo Miss
                SetVisualState(missColor, 1.0f);
            }

            if (enemyCount == 0)
            {
                Invoke(nameof(ResetToNormal), 0.15f);
            }
        }
    }

    void ResetToNormal()
    {
        if (enemyCount == 0)
        {
            SetVisualState(normalColor, 1.0f);
        }
    }

    void SetVisualState(Color col, float scaleMult)
    {
        if (sr != null) sr.color = col;
        transform.localScale = initialScale * scaleMult;
    }
    public void FlashMiss()
    {
        CancelInvoke(nameof(ResetToNormal));
        if (sr != null) sr.color = missColor;
        Invoke(nameof(ResetToNormal), 0.2f);
    }
}