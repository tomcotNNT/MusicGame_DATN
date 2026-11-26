using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Globalization;

public class ResultSystem : MonoBehaviour
{

    [Header("UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text scoreText;

    [Header("Sound (tùy chọn)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip starSound;

    private bool shown;

    private void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    // Gọi khi bài nhạc kết thúc
    public void ShowResult(int score)
    {
        if (shown)
            return;

        shown = true;

        // Scene dùng bảng kết quả mới (EndGameBase) -> không bật panel cũ chồng lên
        if (resultPanel != null && !EndGameBase.UsingNewPanel)
            resultPanel.SetActive(true);

        if (scoreText != null)
            scoreText.text = score.ToString("#,##0", CultureInfo.InvariantCulture);

        // Kết quả màn (coin, kỷ lục...) do EndGameBase lưu vào PlayerDataManager.
        // Không gọi SelectLevel.CompleteCurrentLevel() (hệ lưu PlayerPrefs cũ, không còn dùng).
    }

}