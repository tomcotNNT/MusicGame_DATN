using UnityEngine;

public class InfiniteLane : MonoBehaviour
{
    [SerializeField] private GameObject lanePrefab;
    [SerializeField] private Transform spawnPoint;

    [SerializeField] private float speed = 5f;
    [SerializeField] private float tileWidth = 20f;

    private GameObject[] tiles;

    private void Start()
    {
        tiles = new GameObject[2];

        // Tile đầu tiên
        tiles[0] = Instantiate(
            lanePrefab,
            transform.position,
            Quaternion.identity
        );

        // Tile thứ hai nối tiếp tile đầu
        tiles[1] = Instantiate(
            lanePrefab,
            transform.position + Vector3.right * tileWidth,
            Quaternion.identity
        );
    }

    private void Update()
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].transform.position +=
                Vector3.left * speed * Time.deltaTime;

            // Tile chạy ra khỏi màn hình
            if (tiles[i].transform.position.x <= -tileWidth)
            {
                // Đưa tile ra phía sau tile còn lại
                GameObject otherTile = tiles[i == 0 ? 1 : 0];

                tiles[i].transform.position =
                    otherTile.transform.position +
                    Vector3.right * tileWidth;
            }
        }
    }
}