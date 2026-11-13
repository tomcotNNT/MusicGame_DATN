using System.Collections;
using UnityEngine;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [Header("=== THÔNG TIN NGƯỜI CHƠI ===")]
    public TextMeshProUGUI playerNameText; 
    public TextMeshProUGUI coinNumberText; 

    [Header("=== QUẢN LÝ POPUP ===")]
    public GameObject settingsPopup; 
    public GameObject storePopup;    // Khung chứa UI Cửa hàng

    [Header("=== HỆ THỐNG ÂM THANH ===")]
    public AudioSource bgmSource;
    public AudioSource sfxClickSource;

    private int currentCoins = 0;

    void Start()
    {
        // 1. Phát nhạc nền Lobby
        if (bgmSource != null) bgmSource.Play();

        // 2. Tải dữ liệu người chơi
        LoadPlayerData();
    }

    private void LoadPlayerData()
    {
        // Tải Tên (Mặc định là "Tín Đồ Neon" nếu chưa có)
        string savedName = PlayerPrefs.GetString("PlayerName", "Tín Đồ Neon");
        if (playerNameText != null)
        {
            playerNameText.text = savedName;
        }

        // Tải Tiền (Khởi điểm 1000 xu làm vốn)
        currentCoins = PlayerPrefs.GetInt("PlayerCoins", 1000);
        UpdateCoinUI();
    }

    public void UpdateCoinUI()
    {
        if (coinNumberText != null)
        {
            // Định dạng số có dấu phẩy (VD: 1,000)
            coinNumberText.text = currentCoins.ToString("N0"); 
        }
    }

    // ==========================================
    // CÁC HÀM XỬ LÝ NÚT BẤM (BUTTON EVENTS)
    // ==========================================

    public void PlayClickSound()
    {
        if (sfxClickSource != null) sfxClickSource.Play();
    }

    // --- POPUP CÀI ĐẶT ---
    public void OnSettingsOpenClicked()
    {
        PlayClickSound();
        if (settingsPopup != null) settingsPopup.SetActive(true);
    }

    public void OnSettingsCloseClicked()
    {
        PlayClickSound();
        if (settingsPopup != null) settingsPopup.SetActive(false);
    }

    // --- POPUP CỬA HÀNG (STORE) ---
    public void OnStoreOpenClicked()
    {
        PlayClickSound();
        if (storePopup != null) storePopup.SetActive(true);
    }

    public void OnStoreCloseClicked()
    {
        PlayClickSound();
        if (storePopup != null) storePopup.SetActive(false);
    }

    // Nút [+] cạnh đồng tiền cũng sẽ gọi mở Store
    public void OnAddCoinClicked()
    {
        OnStoreOpenClicked(); 
    }

    // --- HÀM ẢO TEST CHỨC NĂNG (Dành cho Dev) ---
    public void TestAdd100Coins()
    {
        currentCoins += 100;
        PlayerPrefs.SetInt("PlayerCoins", currentCoins); 
        PlayerPrefs.Save();
        UpdateCoinUI(); 
        PlayClickSound();
    }
}