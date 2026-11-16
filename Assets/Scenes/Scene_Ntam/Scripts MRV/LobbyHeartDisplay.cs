
using UnityEngine;
using TMPro;

public class LobbyHeartDisplay : MonoBehaviour
{
    [Header("Heart Display")]
    public TMP_Text heartText;

    private void Start()
    {
        UpdateHeartUI();
    }

    public void UpdateHeartUI()
    {
        int currentHeart = PlayerPrefs.GetInt("Heart", 0);

        if (heartText != null)
        {
            heartText.text = currentHeart.ToString("N0");
        }
    }
}
