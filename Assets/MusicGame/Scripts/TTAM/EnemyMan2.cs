using System;
using UnityEngine;

/// Enemy bay từ chỗ spawn về Player. Vị trí tính theo PianoKeySpawner.SongTime
/// nên chạm Player đúng nốt nhạc, và đứng yên khi pause.
public class EnemyMan2 : MonoBehaviour
{
    public static event Action<EnemyMan2> AnyEnemyHit;   // lắng nghe để trừ máu, v.v.

    private PianoKeySpawner spawner;
    private Transform target;
    private Vector3 startPos;
    private float spawnTime;
    private float hitTime;

    public void Init(PianoKeySpawner spawner, Transform player, float spawnTime, float hitTime)
    {
        this.spawner = spawner;
        this.target = player;
        this.spawnTime = spawnTime;
        this.hitTime = hitTime;
        startPos = transform.position;
    }

    private void Update()
    {
        if (spawner == null || target == null)
        {
            Destroy(gameObject);
            return;
        }

        float t = Mathf.InverseLerp(spawnTime, hitTime, spawner.SongTime);

        // Bám vị trí hiện tại của player
        Vector3 end = target.position;
        end.z = startPos.z;
        transform.position = Vector3.Lerp(startPos, end, t);

        if (t >= 1f)
        {
            AnyEnemyHit?.Invoke(this);
            Destroy(gameObject);
        }
    }
}