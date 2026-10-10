using UnityEngine;

public class DestroyBehindPlayer : MonoBehaviour
{
    private Transform player;
    [SerializeField] private float destroyDistance = 35f; // Khoảng cách sau lưng Player thì xóa

    void Start()
    {
        // Tự động tìm Player qua Tag (hãy đảm bảo Player được gán Tag "Player")
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player != null)
        {
            // Nếu vị trí của miếng đất nằm tụt lại phía sau Player quá xa
            if (transform.position.x < player.position.x - destroyDistance)
            {
                Destroy(gameObject);
            }
        }
    }
}