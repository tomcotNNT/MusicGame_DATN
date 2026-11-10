
using System;
using UnityEngine;

public class EnemyMan2 : MonoBehaviour
{
    public static event Action<EnemyMan2> AnyEnemyHit;

    private Transform target;
    private Vector3 startPos;

    [Header("Sprite Flip")]             
[SerializeField] private SpriteRenderer spriteRenderer;
[SerializeField] private bool facePlayer = true;

    private Func<float> getSongTime;

    private float spawnTime;
    private float hitTime;

    public void Init(
        Transform player,
        float spawnTime,
        float hitTime,
        Func<float> songTimeProvider)
    {
        target = player;
        this.spawnTime = spawnTime;
        this.hitTime = hitTime;
        getSongTime = songTimeProvider;

        startPos = transform.position;
    }

    private void Update()
    {

        UpdateSpriteDirection();

        if (target == null || getSongTime == null)
        {
            Destroy(gameObject);
            return;
        }

        float songTime = getSongTime();

        float t = Mathf.InverseLerp(
            spawnTime,
            hitTime,
            songTime
        );



        Vector3 end = target.position;
        end.z = startPos.z;

        transform.position = Vector3.Lerp(startPos, end, t);

        if (songTime >= hitTime)
        {
            AnyEnemyHit?.Invoke(this);
            Destroy(gameObject);
        }
    }

    private void UpdateSpriteDirection()
{
    if (!facePlayer || target == null || spriteRenderer == null)
        return;

    // Player ở bên phải Enemy
    spriteRenderer.flipX = target.position.x < transform.position.x;
}
}
