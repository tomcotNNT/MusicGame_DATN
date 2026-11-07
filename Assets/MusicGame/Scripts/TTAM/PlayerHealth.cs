using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private int damagePerMiss = 10;
    public Image HPbar;

    [Header("Game Over")]
    [Tooltip("Panel Game Over (để tắt sẵn trong Hierarchy)")]
    [SerializeField] private GameObject gameOverPanel;
    [Tooltip("Chờ bao lâu (giây thật) cho animation Die chạy rồi mới hiện panel")]
    [SerializeField, Min(0f)] private float gameOverDelay = 1.0f;
    [Tooltip("Dừng luôn nhạc/âm thanh khi chết")]
    [SerializeField] private bool pauseAudioOnDeath = true;
    [Tooltip("Tên scene menu chính (dùng cho nút Home). Để trống nếu không dùng")]
    [SerializeField] private string menuSceneName = "";

    private Animator anim;
    private int currentHealth;
    private bool isDead;

    public bool IsDead => isDead;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;

        // Đảm bảo vào game luôn chạy bình thường (phòng khi scene trước để timeScale = 0)
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Max(currentHealth - damage, 0);

        Debug.Log($"Player Health: {currentHealth}/{maxHealth}");

        UpdateBar();

        if (currentHealth <= 0)
            Die();
    }

    public void TakeMissDamage()
    {
        TakeDamage(damagePerMiss);
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;
        currentHealth = 0;
        UpdateBar();

        Debug.Log("Game Over!");

        // Cho animation Die vẫn chạy được khi timeScale = 0
        if (anim != null)
        {
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            anim.SetTrigger("Die");
        }

        // Dừng game: đạn, boss, nốt nhạc đều dùng Time/audio nên đứng lại
        Time.timeScale = 0f;

        if (pauseAudioOnDeath)
            AudioListener.pause = true;

        StartCoroutine(ShowGameOverRoutine());
    }

    private IEnumerator ShowGameOverRoutine()
    {
        // Realtime vì timeScale đang = 0
        yield return new WaitForSecondsRealtime(gameOverDelay);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    // ===== Gắn vào nút trên panel Game Over =====

    public void Retry()
    {
        ResumeGameState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMenu()
    {
        if (string.IsNullOrEmpty(menuSceneName))
            return;

        ResumeGameState();
        SceneManager.LoadScene(menuSceneName);
    }

    private void ResumeGameState()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void OnDestroy()
    {
        // Phòng trường hợp scene bị đổi mà chưa trả lại timeScale
        if (isDead)
            ResumeGameState();
    }

    private void UpdateBar()
    {
        if (HPbar != null)
            HPbar.fillAmount = (float)currentHealth / maxHealth;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }
}