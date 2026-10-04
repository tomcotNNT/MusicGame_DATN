using UnityEngine;

public class LaneScroller : MonoBehaviour
{
    [Header("Lane")]
    [Tooltip("Các đoạn lane giống nhau, xếp liền kề theo trục X")]
    [SerializeField] private Transform[] segments;
    [SerializeField] private float speed = 5f;

    [Tooltip("Để 0 = tự lấy từ Renderer của đoạn đầu tiên")]
    [SerializeField] private float segmentWidth = 0f;

    private Camera cam;
private float centerOffset; // lệch giữa tâm thật của đoạn và transform.position (pivot)

private void Start()
{
    cam = Camera.main;

    if (segments.Length == 0) return;

    // Gộp bounds của TẤT CẢ renderer con trong đoạn đầu tiên
    Renderer[] rs = segments[0].GetComponentsInChildren<Renderer>();
    if (rs.Length == 0)
    {
        Debug.LogError("LaneScroller: đoạn lane không có Renderer nào!");
        return;
    }

    Bounds b = rs[0].bounds;
    for (int i = 1; i < rs.Length; i++)
        b.Encapsulate(rs[i].bounds);

    if (segmentWidth <= 0f)
        segmentWidth = b.size.x;

    centerOffset = b.center.x - segments[0].position.x;
}

private void Update()
{
    if (segments.Length == 0 || segmentWidth <= 0f)
        return;

    float move = speed * Time.deltaTime;
    float leftEdge = cam.ViewportToWorldPoint(Vector3.zero).x;

    foreach (Transform s in segments)
    {
        s.position += Vector3.left * move;

        // Mép phải thật của đoạn (đã tính lệch pivot)
        float rightEdge = s.position.x + centerOffset + segmentWidth * 0.5f;

        if (rightEdge < leftEdge)
            s.position += Vector3.right * segmentWidth * segments.Length;
    }
}
}