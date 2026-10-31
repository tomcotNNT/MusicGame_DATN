using System;
using UnityEngine;
using UnityEngine.UI;

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

        // Đi vào vị trí đứng, tới nơi mới cho phép đánh.
        transform.position = Vector3.MoveTowards(
            transform.position, stopPosition, enterSpeed * Time.deltaTime);

        if ((transform.position - stopPosition).sqrMagnitude < 0.01f)
            IsReady = true;
    }

    public void TakeDamage(int amount)
    {
        if (!IsReady || dead)
            return;

        health = Mathf.Max(0, health - amount);
        RefreshBar();

        if (health <= 0)
        {
            dead = true;
            Defeated?.Invoke();
            Destroy(gameObject);
        }
    }

    // Boss tấn công. Tạm thời chỉ log, sau này thêm animation/hiệu ứng/trừ máu player ở đây
    public void Attack()
    {
        if (!IsReady || dead)
            return;

        Debug.Log("Boss Attack!");
        // TODO: animator.SetTrigger("Attack"); ...
    }

    private void RefreshBar()
    {
        if (healthFill != null)
            healthFill.fillAmount = (float)health / maxHealth;
    }
}