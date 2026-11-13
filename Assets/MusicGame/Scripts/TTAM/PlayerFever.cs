using System.Collections.Generic;
using UnityEngine;

public class PlayerFeverShooter : MonoBehaviour
{
    [Header("Đạn theo item")]
    [SerializeField] private List<FeverBulletEntry> bulletEntries;

    [Header("Bắn")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 12f;

    private Dictionary<string, GameObject> bulletMap;
    private GameObject currentBulletPrefab;
    private bool isFever;

    public bool IsFever => isFever;

    private void Awake()
    {
        bulletMap = new Dictionary<string, GameObject>();
        foreach (var e in bulletEntries)
        {
            if (!string.IsNullOrEmpty(e.itemId) && e.bulletPrefab != null)
                bulletMap[e.itemId] = e.bulletPrefab;
        }
    }

    private void Start()
    {
        if (ItemFeverBar.Instance != null)
            ItemFeverBar.Instance.OnFull += HandleFeverFull;
    }

    private void OnDestroy()
    {
        if (ItemFeverBar.Instance != null)
            ItemFeverBar.Instance.OnFull -= HandleFeverFull;
    }

    private void HandleFeverFull(string itemId)
    {
        if (!bulletMap.TryGetValue(itemId, out var prefab))
        {
            Debug.LogWarning($"Chưa cấu hình đạn cho item: {itemId}");
            return;
        }

        currentBulletPrefab = prefab;
        isFever = true;
    }

    /// Trả về true nếu đã bắn.
    public bool TryFireFever()
    {
        if (!isFever || currentBulletPrefab == null)
            return false;

        var bullet = Instantiate(
            currentBulletPrefab, firePoint.position, firePoint.rotation);

        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = firePoint.right * bulletSpeed;

        // Bắn xong mới reset thanh
        isFever = false;
        currentBulletPrefab = null;

        if (ItemFeverBar.Instance != null)
            ItemFeverBar.Instance.Consume();

        return true;
    }
}