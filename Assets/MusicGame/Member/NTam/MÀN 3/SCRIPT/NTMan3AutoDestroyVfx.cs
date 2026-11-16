using UnityEngine;

public class NTMan3AutoDestroyVfx : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.45f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}