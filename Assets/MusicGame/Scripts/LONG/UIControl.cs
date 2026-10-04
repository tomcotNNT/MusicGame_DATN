
using UnityEngine;
using TMPro;

public class UIControl : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tapToStartText;
    [SerializeField] private float blinkInterval = 0.8f;

    private float timer;

    private void Start()
    {
        tapToStartText.enabled = true;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= blinkInterval)
        {
            timer = 0f;
            tapToStartText.enabled = !tapToStartText.enabled;
        }
    }
}
