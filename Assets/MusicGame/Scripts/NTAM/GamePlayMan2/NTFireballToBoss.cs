using UnityEngine;

public class NTFireballToBoss : MonoBehaviour
{
    [Header("Flight Settings")]
    [SerializeField] private float flightSpeed = 26f;
    [SerializeField] private GameObject explosionVfx;

    private NTBossFightDirector bossDirector;
    private Transform shadowTarget;

    public void InitFireball(NTBossFightDirector director)
    {
        bossDirector = director;
        if (bossDirector != null)
        {
            // Nhắm thẳng vào Cái Bóng trên cây
            shadowTarget = bossDirector.GetShadowTarget();

            Vector3 dir = shadowTarget.position - transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void Update()
    {
        if (bossDirector == null || shadowTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        // Bay lao vút lên ngực Cái Bóng trên cây
        transform.position = Vector3.MoveTowards(
            transform.position,
            shadowTarget.position,
            flightSpeed * Time.deltaTime
        );

        // Chạm trúng Cái Bóng
        if (Vector3.Distance(transform.position, shadowTarget.position) < 0.6f)
        {
            bossDirector.OnReflectedHitLanded();

            if (explosionVfx != null)
            {
                GameObject vfx = Instantiate(explosionVfx, transform.position, Quaternion.identity);
                Destroy(vfx, 0.5f);
            }

            Destroy(gameObject);
        }
    }
}