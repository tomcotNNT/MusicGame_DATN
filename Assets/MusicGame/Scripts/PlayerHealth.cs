using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private int damagePerMiss = 10;
    public Image HPbar;

    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"Player Health: {currentHealth}/{maxHealth}");

        // Update the health bar
        if (HPbar != null)
        {
            HPbar.fillAmount = (float)currentHealth / maxHealth;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeMissDamage()
    {
        TakeDamage(damagePerMiss);
    }

    private void Die()
    {
        Debug.Log("Game Over!");

        // Xử lý Game Over ở đây
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }
}