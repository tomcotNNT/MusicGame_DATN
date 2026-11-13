
using UnityEngine;
using TMPro;

public class LobbyDiamondDisplay : MonoBehaviour
{
    [Header("Diamond Text")]
    public TMP_Text diamondText;

    private void Start()
    {
        UpdateDiamondUI();
    }

    public void UpdateDiamondUI()
    {
        int diamonds = PlayerPrefs.GetInt("Diamond", 0);

        if (diamondText != null)
        {
            diamondText.text = diamonds.ToString("N0");
        }
    }
}
