using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle")]
    [SerializeField] private GameObject obstaclePrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform upSpawnPoint;
    [SerializeField] private Transform downSpawnPoint;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 2f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnObstacle();
            timer = 0f;
        }
    }

    private void SpawnObstacle()
    {
        int randomLane = Random.Range(0, 2);

        Transform spawnPoint;

        if (randomLane == 0)
        {
            spawnPoint = upSpawnPoint;
        }
        else
        {
            spawnPoint = downSpawnPoint;
        }

        Instantiate(
            obstaclePrefab,
            spawnPoint.position,
            Quaternion.identity
        );
    }
}