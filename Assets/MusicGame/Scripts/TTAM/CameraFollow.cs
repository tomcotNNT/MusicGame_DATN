using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float followSpeed = 5f;

    private float fixedY;
    private float fixedZ;

    private void Start()
    {
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        Vector3 targetPosition = new Vector3(
            player.position.x,
            fixedY,
            fixedZ
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );
    }
}