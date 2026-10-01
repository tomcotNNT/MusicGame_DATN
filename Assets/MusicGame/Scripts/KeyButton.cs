using UnityEngine;
using UnityEngine.UI;

public class KeyButtonController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [Header("Button Images")]
    [SerializeField] private Image leftImage;
    [SerializeField] private Image rightImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color maxLightColor = Color.yellow;

    [Header("Distance")]
    [SerializeField] private float detectDistance = 4f;

    [Header("Timing")]
    [SerializeField] private float perfectWindow = 0.12f;
    [SerializeField] private float goodWindow = 0.25f;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private AudioSource audioSource;

    private PianoKey currentKey;

    private void Start()
    {
        leftButton.onClick.AddListener(OnLeftButton);
        rightButton.onClick.AddListener(OnRightButton);

        ResetButtons();
    }

    private void Update()
    {
        FindCurrentKey();
        UpdateButtonLight();

         HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        // Q = L
        if (Input.GetKeyDown(KeyCode.Q))
        {
            CheckInput(PianoKey.KeyType.Left);
        }

        // W = R
        if (Input.GetKeyDown(KeyCode.W))
        {
            CheckInput(PianoKey.KeyType.Right);
        }
    }

    private void FindCurrentKey()
    {
        currentKey = null;

        float closestDistance = Mathf.Infinity;

        foreach (PianoKey key in PianoKey.ActiveKeys)
        {
            if (key == null)
                continue;

            if (key.IsCompleted)
                continue;

            // Không lấy key đã nằm sau Player
            if (key.transform.position.x < player.position.x)
                continue;

            float distance = Vector3.Distance(
                player.position,
                key.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentKey = key;
            }
        }
    }

    private void UpdateButtonLight()
    {
        ResetButtons();

        if (currentKey == null)
            return;

        float distance = Vector3.Distance(
            player.position,
            currentKey.transform.position
        );

        if (distance > detectDistance)
            return;

        // Càng gần càng sáng
        float intensity =
            1f - Mathf.Clamp01(
                distance / detectDistance
            );

        if (currentKey.GetKeyType() == PianoKey.KeyType.Left)
        {
            leftImage.color = Color.Lerp(
                normalColor,
                maxLightColor,
                intensity
            );
        }
        else
        {
            rightImage.color = Color.Lerp(
                normalColor,
                maxLightColor,
                intensity
            );
        }
    }

    private void ResetButtons()
    {
        leftImage.color = normalColor;
        rightImage.color = normalColor;
    }

    private void OnLeftButton()
    {
        CheckInput(PianoKey.KeyType.Left);
    }

    private void OnRightButton()
    {
        CheckInput(PianoKey.KeyType.Right);
    }

//     private void CheckInput(PianoKey.KeyType inputType)
// {
//     if (currentKey == null)
//         return;

//     // Player phải đang va chạm với key
//     if (!currentKey.PlayerOnKey)
//     {
//         Debug.Log("Player chưa chạm vào key!");
//         return;
//     }

//     // Kiểm tra đúng nút L/R
//     if (currentKey.GetKeyType() != inputType)
//     {
//         Debug.Log("WRONG BUTTON");
//         return;
//     }

//     float currentTime = audioSource.time;

//     float difference = Mathf.Abs(
//         currentTime - currentKey.GetTargetTime()
//     );

//     if (difference <= perfectWindow)
//     {
//         Debug.Log("PERFECT +100");

//         // ScoreSystem.AddScore(100);

//         currentKey.Complete();
//     }
//     else if (difference <= goodWindow)
//     {
//         Debug.Log("GOOD +70");

//         // ScoreSystem.AddScore(70);

//         currentKey.Complete();
//     }
//     else
//     {
//         Debug.Log("MISS");
//     }
// }

private void CheckInput(PianoKey.KeyType inputType)
{
    if (currentKey == null)
        return;

    if (!currentKey.PlayerOnKey)
    {
        Debug.Log("Player chưa chạm Key");
        return;
    }

    if (currentKey.GetKeyType() != inputType)
    {
        Debug.Log("SAI NÚT");
        return;
    }

    Debug.Log("HIT +100");

    // ScoreSystem.AddScore(100);

    currentKey.Complete();
}
    private void OnDestroy()
    {
        if (leftButton != null)
            leftButton.onClick.RemoveListener(OnLeftButton);

        if (rightButton != null)
            rightButton.onClick.RemoveListener(OnRightButton);
    }
}