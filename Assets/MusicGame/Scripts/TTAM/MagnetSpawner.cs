using UnityEngine;

public class MagnetSpawner : MonoBehaviour
{
    [SerializeField] private PianoKeySpawner songSpawner;
    [SerializeField] private Magnet magnetPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float yOffset = 0f;

    [Header("Random")]
    [SerializeField] private float minInterval = 10f;
    [SerializeField] private float maxInterval = 15f;

    [Range(0f, 1f)]
    [Tooltip("Tỉ lệ spawn mỗi lần tới mốc hẹn. 1 = luôn spawn, 0.5 = 50%")]
    [SerializeField] private float spawnChance = 0.5f;

    private float nextSpawnTime;
    private bool scheduled;

    private void Update()
    {
        if (songSpawner == null || magnetPrefab == null || player == null || spawnPoint == null)
            return;

        if (!songSpawner.IsPlaying || songSpawner.IsPaused || songSpawner.IsTutorial)
            return;

        float now = songSpawner.SongTime;

        // Hẹn lần spawn đầu khi bài bắt đầu.
        if (!scheduled)
        {
            nextSpawnTime = now + Random.Range(minInterval, maxInterval);
            scheduled = true;
            return;
        }

        if (now < nextSpawnTime)
            return;

        // Đang có hiệu lực magnet thì bỏ qua lượt này.
        bool active = ItemDurationSystem.Instance != null &&
                    ItemDurationSystem.Instance.IsActive("Magnet");

        // Random theo tỉ lệ
        bool roll = Random.value <= spawnChance;

        if (!active && roll)
            Spawn(now);

        nextSpawnTime = now + Random.Range(minInterval, maxInterval);
    }

    private void Spawn(float now)
    {
        float speed = songSpawner.KeySpeed;
        float travel = songSpawner.SpawnAheadTime;
        float dir = spawnPoint.position.x >= player.position.x ? 1f : -1f;

        Vector3 pos = new Vector3(
            player.position.x + dir * speed * travel,
            spawnPoint.position.y + yOffset,
            spawnPoint.position.z);

        Magnet m = Instantiate(magnetPrefab, pos, Quaternion.identity);
        m.Setup(now + travel, speed, player.position.x, dir);
    }
}