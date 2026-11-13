using UnityEngine;

public class EnemyTestRunner : MonoBehaviour
{
    public float moveSpeed = 6f;
    public bool moveRight = true; // true: từ trái sang phải, false: từ phải sang trái

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Init(bool isLeftLane)
    {
        moveRight = isLeftLane;
        if (sr != null)
        {
            sr.flipX = !isLeftLane; // Lật sprite nếu đi từ bên phải qua
        }
    }

    void Update()
    {
        float dir = moveRight ? 1f : -1f;
        transform.position += Vector3.right * dir * moveSpeed * Time.deltaTime;

        // Tự hủy nếu đi quá xa qua bên kia màn hình
        if (Mathf.Abs(transform.position.x) > 15f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Khi chạm vào CheckPoint
        if (collision.CompareTag("CheckPoint") || collision.name.StartsWith("CP_"))
        {
            Debug.Log($"Enemy đã vào vùng đánh: {collision.name}");
        }
    }
}