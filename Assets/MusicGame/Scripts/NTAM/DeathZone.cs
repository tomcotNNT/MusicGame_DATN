using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathZone : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float fallY = -6f; // Độ cao khu vực chết

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void LateUpdate()
    {
        // Luôn giữ khu vực chết chạy song song bên dưới Player
        if (player != null)
        {
            transform.position = new Vector3(player.position.x, fallY, 0);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Reset / Load lại Scene hiện tại khi Player rơi xuống
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

            // Mẹo: Nếu sau này có UI Game Over, bạn có thể gọi:
            // GameManager.Instance.ShowGameOver();
        }
    }
}