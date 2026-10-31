using UnityEngine;

public class RhythmItem : MonoBehaviour
{
    private float speed;
    private float moveDir;     // hướng bay = -spawnDirection
    private float destroyX;    // qua mốc này thì hủy

    public void Init(float speed, float spawnDirection, float hitX, float extraDistance = 8f)
    {
        this.speed = speed;
        moveDir = -spawnDirection;
        destroyX = hitX + moveDir * extraDistance;
    }

    private void Update()
    {
        transform.position += new Vector3(moveDir * speed * Time.deltaTime, 0f, 0f);

        bool passed = moveDir < 0f ? transform.position.x <= destroyX
                                   : transform.position.x >= destroyX;
        if (passed) Destroy(gameObject);
    }

    // Thêm OnTriggerEnter2D / OnTriggerEnter tuỳ ý để nhặt item, trừ máu...
}