using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtonFloat : MonoBehaviour
{
    [Header("Button Movement")]
    public float distance = 15f;
    public float speed = 2f;
    public float phase = 0f;

    private RectTransform rect;
    private Vector2 startPos;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        startPos = rect.anchoredPosition;
    }

    void Update()
    {
        // Di chuyển trái <-> phải
        float offset = Mathf.Sin(
            Time.unscaledTime * speed + phase
        ) * distance;

        rect.anchoredPosition =
            startPos + new Vector2(offset, 0);
    }

    // Dành cho nút PLAY
    public void PlayGame()
    {
        SceneManager.LoadScene("Game");
    }
}