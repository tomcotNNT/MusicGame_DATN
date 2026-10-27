using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Collections;

public class LevelDebugCheat : MonoBehaviour
{
    [Header("Phím Tắt Debug")]
    [SerializeField] private KeyCode toggleKey = KeyCode.G;

    [Header("Tham Chiếu")]
    [SerializeField] private SelectLevel selectLevel;
    [SerializeField] private Button nextButton;

    private bool isAllUnlocked = false;

    private void Start()
    {
        if (selectLevel == null)
            selectLevel = FindAnyObjectByType<SelectLevel>();

        if (nextButton == null)
        {
            GameObject btnObj = GameObject.Find("NEXT");
            if (btnObj != null) nextButton = btnObj.GetComponent<Button>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleCheat();
        }
    }

    // Chạy trong LateUpdate để ghi đè SAU KHI SelectLevel.Update() đã chạy xong
    private void LateUpdate()
    {
        if (isAllUnlocked && nextButton != null)
        {
            // Ép nút NEXT luôn sáng và bấm được
            nextButton.interactable = true;
        }
    }

    private void ToggleCheat()
    {
        isAllUnlocked = !isAllUnlocked;

        // Can thiệp thẳng vào mảng levels trong SelectLevel qua Reflection
        if (selectLevel != null)
        {
            FieldInfo levelsField = typeof(SelectLevel).GetField("levels", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (levelsField != null)
            {
                IList list = levelsField.GetValue(selectLevel) as IList;
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        FieldInfo unlockedField = item.GetType().GetField("unlockedByDefault", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (unlockedField != null)
                        {
                            // Mở toàn bộ nếu bật cheat, hoặc chỉ mở màn đầu nếu tắt
                            unlockedField.SetValue(item, isAllUnlocked);
                        }
                    }
                    // Màn 1 luôn luôn mở
                    if (list.Count > 0)
                    {
                        FieldInfo firstUnlocked = list[0].GetType().GetField("unlockedByDefault", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (firstUnlocked != null) firstUnlocked.SetValue(list[0], true);
                    }
                }
            }
        }

        if (isAllUnlocked)
        {
            Debug.Log("<color=green>>>> [CHEAT] ĐÃ BẬT: Ép mở khóa toàn bộ Màn 1, 2, 3! Nút NEXT đã sẵn sàng.</color>");
        }
        else
        {
            Debug.Log("<color=yellow>>>> [CHEAT] ĐÃ TẮT: Đưa về trạng thái ban đầu.</color>");
        }
    }
}