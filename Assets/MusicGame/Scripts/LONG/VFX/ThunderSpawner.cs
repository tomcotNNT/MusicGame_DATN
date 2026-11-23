
using System.Collections;
using UnityEngine;

public class ThunderSpawner : MonoBehaviour
{
    [Header("Thunder VFX")]
    [SerializeField] private GameObject[] thunderObjects;

    [Header("Spawn Settings")]
    [SerializeField] private float minDelay = 2f;
    [SerializeField] private float maxDelay = 5f;

    private int lastIndex = -1;

    private void Start()
    {
        // Tắt toàn bộ VFX ban đầu
        foreach (GameObject thunder in thunderObjects)
        {
            thunder.SetActive(false);
        }

        StartCoroutine(ShowThunder());
    }

    private IEnumerator ShowThunder()
    {
        while (true)
        {
            // Chờ ngẫu nhiên
            yield return new WaitForSeconds(
                Random.Range(minDelay, maxDelay)
            );

            if (thunderObjects.Length == 0)
                continue;

            // Chọn VFX khác lần trước
            int index;

            do
            {
                index = Random.Range(0, thunderObjects.Length);
            }
            while (thunderObjects.Length > 1 && index == lastIndex);

            lastIndex = index;

            GameObject thunder = thunderObjects[index];

            // Bật hiệu ứng
            thunder.SetActive(true);

            // Chờ animation kết thúc
            Animator animator = thunder.GetComponent<Animator>();

            if (animator != null)
            {
                yield return new WaitForSeconds(
                    animator.GetCurrentAnimatorStateInfo(0).length
                );
            }

            // Tắt hiệu ứng
            thunder.SetActive(false);
        }
    }
}
