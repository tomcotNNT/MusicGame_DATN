using System;
using UnityEngine;
using UnityEngine.UI;

public class Boss : MonoBehaviour
{
    [SerializeField] private int maxHealth = 20;
    [SerializeField] private float enterSpeed = 4f;
    [SerializeField] private Image healthFill;
    [SerializeField] private bool requireItemsToDefeat = true;

    public event Action Defeated;
    public event Action Appeared;
    public bool IsReady { get; private set; }
    public bool HasBegun { get; private set; }

    private int health;
    private Vector3 stopPosition;
    private bool dead;

    public void Begin(Vector3 stopPos)
    {
        IsReady = false;
        dead = false;
        stopPosition = stopPos;
        health = maxHealth;
        RefreshBar();
        HasBegun = true;
        Appeared?.Invoke(); // Boss bắt đầu xuất hiện => 3 loại item xuất hiện ngay
    }

    private void Update()
    {
        if (!HasBegun || IsReady || dead) return;
        transform.position = Vector3.MoveTowards(transform.position, stopPosition, enterSpeed * Time.deltaTime);
        if ((transform.position - stopPosition).sqrMagnitude < 0.01f)
            IsReady = true;
    }

    public void TakeDamage(int amount)
    {
        if (!IsReady || dead || requireItemsToDefeat) return;
        health = Mathf.Max(0, health - amount);
        RefreshBar();
        if (health <= 0) FinishByItems();
    }

    public void FinishByItems()
    {
        if (dead) return;
        dead = true;
        health = 0;
        RefreshBar();
        Defeated?.Invoke();
        Destroy(gameObject);
    }

    public void Attack()
    {
        if (!IsReady || dead) return;
        Debug.Log("Boss Attack!");
    }

    private void RefreshBar()
    {
        if (healthFill != null) healthFill.fillAmount = maxHealth > 0 ? (float)health / maxHealth : 0f;
    }
}
