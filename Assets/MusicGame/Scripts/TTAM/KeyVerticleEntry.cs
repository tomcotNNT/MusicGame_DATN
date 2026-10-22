using UnityEngine;

// Gắn tự động vào key khi spawn. Chỉ chỉnh trục Y, không đụng tới X.
public class KeyVerticalEntry : MonoBehaviour
{
    private float playerX;
    private float baseY;
    private float yOffset;
    private float fullDist;
    private float settleDist;
    private AnimationCurve curve;

    public void Init(float playerX, float baseY, float yOffset,
                     float fullDist, float settleDist, AnimationCurve curve)
    {
        this.playerX = playerX;
        this.baseY = baseY;
        this.yOffset = yOffset;
        this.fullDist = fullDist;
        this.settleDist = settleDist;
        this.curve = curve;
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    private void Apply()
    {
        float dx = Mathf.Abs(transform.position.x - playerX);

        // t = 1 lúc mới spawn (lệch tối đa), t = 0 khi tới vùng settle (về đúng lane)
        float t = Mathf.Clamp01((dx - settleDist) / Mathf.Max(0.01f, fullDist - settleDist));
        float k = curve != null ? curve.Evaluate(t) : t;

        Vector3 p = transform.position;
        p.y = baseY + yOffset * k;
        transform.position = p;
    }
}