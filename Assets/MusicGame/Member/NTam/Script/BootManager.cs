using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BootManager : MonoBehaviour
{
    [Header("UI Chuyển Cảnh")]
    public Slider loadingSlider; 
    
    [Header("Cài đặt")]
    public int nextSceneIndex = 1; // Scene muốn load (Lobby)
    public float fakeDelay = 1f;   // Thời gian ngắm UI (giây)

    private void Start()
    {
        // Bắt đầu tiến trình chuyển cảnh ngay khi mở Boot Scene
        StartCoroutine(LoadSceneAsyncCoroutine());
    }

    private IEnumerator LoadSceneAsyncCoroutine()
    {
        Debug.Log("[BootManager] Bắt đầu gọi lệnh LoadScene ngầm...");
        
        // Load ngầm Scene kế tiếp
        AsyncOperation operation = SceneManager.LoadSceneAsync(nextSceneIndex);
        
        if (operation == null)
        {
            Debug.LogError("[BootManager] Lỗi: Không tìm thấy Scene số " + nextSceneIndex);
            yield break; 
        }

        // Chặn không cho nhảy Scene ngay khi load xong dữ liệu
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            // Tính toán % tiến độ (Unity load đến 0.9 là dừng lại chờ lệnh)
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            // Cập nhật UI Slider
            if (loadingSlider != null) 
            {
                loadingSlider.value = progress;
            }

            // Khi dữ liệu Scene mới đã nạp xong (progress = 1)
            if (operation.progress >= 0.9f)
            {
                Debug.Log($"[BootManager] Load xong 100%. Giữ màn hình thêm {fakeDelay} giây...");
                
                // Đợi 1 chút để người chơi kịp nghe điệu nhạc hoặc xem trọn vẹn animation logo
                yield return new WaitForSeconds(fakeDelay); 

                Debug.Log("[BootManager] CHUYỂN SCENE!");
                
                // Cấp phép nhảy Scene. Lúc này BootManager và Slider sẽ tự động bị Unity thiêu rụi.
                operation.allowSceneActivation = true;
            }
            
            yield return null;
        }
    }
}