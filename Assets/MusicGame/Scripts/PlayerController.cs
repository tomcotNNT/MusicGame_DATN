using UnityEngine;

public class PlayerLaneController : MonoBehaviour
{
    [Header("Lane Position")]
    [SerializeField] private Transform upLane;
    [SerializeField] private Transform downLane;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 10f;

    private bool isUp = true;
    private Vector3 targetPosition;

    private void Start()
    {
        targetPosition = downLane.position;
        transform.position = targetPosition;
    }

    private void Update()
    {
        // Di chuyển mượt tới lane
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );
    }

    // Gọi hàm này khi bấm nút Switch
    public void SwitchLane()
    {
        isUp = !isUp;

        if (isUp)
        {
            targetPosition = upLane.position;
        }
        else
        {
            targetPosition = downLane.position;
        }
    }
}