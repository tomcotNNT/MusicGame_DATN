using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundControl : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image background;

    [Header("Backgrounds")]
    [SerializeField] private List<Sprite> backgrounds = new List<Sprite>();

    private void Start()
    {
        if (background == null)
        {
            Debug.LogWarning("RandomLoadingBackground: Background Image chưa được gán.");
            return;
        }

        if (backgrounds == null || backgrounds.Count == 0)
        {
            Debug.LogWarning("RandomLoadingBackground: Chưa có background nào trong list.");
            return;
        }

        // Chọn ngẫu nhiên 1 background
        int randomIndex = Random.Range(0, backgrounds.Count);

        // Gán background
        background.sprite = backgrounds[randomIndex];
    }
}