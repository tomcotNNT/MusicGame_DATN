using UnityEngine;

/// <summary>
/// Coin di chuyển theo SongTime giống PianoKey (luôn khớp nhạc, không bị trôi).
/// Prefab cần: Collider2D (Is Trigger = true). Player phải có Collider2D + Rigidbody2D, tag "Player".
/// </summary>
public class Coin : MonoBehaviour
{
    [Header("Value")]
    [SerializeField] private int value = 1;

    [Header("Despawn")]
    [Tooltip("Coin đi qua player quá khoảng này (unit) thì tự hủy")]
    [SerializeField] private float destroyBehindDistance = 2f;

    [Header("Collect VFX")]
    [SerializeField] private GameObject collectVfxPrefab;
    [SerializeField] private float vfxLifetime = 1f;
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;

    [Header("Collect SFX")]
    [SerializeField] private AudioClip collectClip;
    [Range(0f, 1f)]
    [SerializeField] private float collectVolume = 1f;

    private float targetTime;
    private float moveSpeed;
    private float hitX;
    private float direction;
    private bool collected;

    public void Setup(float time, float speed, float playerX, float dir)
    {
        targetTime = time;
        moveSpeed = speed;
        hitX = playerX;
        direction = dir;
        collected = false;
    }

    private void Update()
    {
        if (collected || PianoKeySpawner.Instance == null)
            return;

        float songTime = PianoKeySpawner.Instance.SongTime;

        Vector3 pos = transform.position;
        pos.x = hitX + direction * moveSpeed * (targetTime - songTime);
        transform.position = pos;

        // Đã đi qua player một đoạn -> hủy
        float passed = (hitX - pos.x) * direction;
        if (passed > destroyBehindDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collected || !collision.CompareTag("Player"))
            return;

        Collect();
    }

    public void Collect()
    {
        if (collected)
            return;

        collected = true;

        if (CoinSystem.Instance != null)
            CoinSystem.Instance.AddCoin(value);

        if (collectVfxPrefab != null)
        {
            GameObject vfx = Instantiate(
                collectVfxPrefab,
                transform.position + vfxOffset,
                Quaternion.identity);
            Destroy(vfx, vfxLifetime);
        }

        if (collectClip != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(collectClip, Camera.main.transform.position, collectVolume);

        Destroy(gameObject);
    }
}