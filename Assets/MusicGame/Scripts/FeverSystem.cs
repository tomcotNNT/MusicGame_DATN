using UnityEngine;
using UnityEngine.UI;

public class FeverSystem : MonoBehaviour
{
    [Header("Fever UI")]
    [SerializeField] private Image feverBar;

    [Header("Fever Settings")]
    [SerializeField] private float maxFever = 100f;
    [SerializeField] private float feverIncreaseSpeed = 5f;
    [SerializeField] private float feverDecreaseSpeed = 10f;

    [Header("Game Speed")]
    [SerializeField] private float normalSpeed = 1f;
    [SerializeField] private float feverSpeed = 2f;

    private float currentFever;
    private bool isFever;

    public float SpeedMultiplier { get; private set; } = 1f;

    private void Start()
    {
        currentFever = 0f;
        isFever = false;

        if (feverBar != null)
        {
            feverBar.fillAmount = 0f;
        }
    }

    private void Update()
    {
        if (!isFever)
        {
            // Fever tăng từ từ
            currentFever += feverIncreaseSpeed * Time.deltaTime;

            if (currentFever >= maxFever)
            {
                currentFever = maxFever;
                StartFever();
            }
        }
        else
        {
            // Khi Fever đầy thì giảm từ từ
            currentFever -= feverDecreaseSpeed * Time.deltaTime;

            if (currentFever <= 0f)
            {
                currentFever = 0f;
                EndFever();
            }
        }

        UpdateFeverBar();
    }

    private void UpdateFeverBar()
    {
        if (feverBar != null)
        {
            feverBar.fillAmount = currentFever / maxFever;
        }
    }

    private void StartFever()
    {
        isFever = true;

        // Tăng nhịp độ game
        SpeedMultiplier = feverSpeed;

        Debug.Log("FEVER ACTIVE!");
    }

    private void EndFever()
    {
        isFever = false;

        // Trở về bình thường
        SpeedMultiplier = normalSpeed;

        Debug.Log("FEVER END!");
    }

    public float GetSpeedMultiplier()
    {
        return SpeedMultiplier;
    }
}