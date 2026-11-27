using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đặt trong scene MÀN CHƠI.
/// Đọc nhân vật đã chọn ở scene Select Char, bật nhân vật đó + UI của nó,
/// ẩn tất cả nhân vật và UI còn lại.
/// </summary>
[DefaultExecutionOrder(-100)] // chạy trước các script khác
public class GameplayCharacterSelector : MonoBehaviour
{
    [System.Serializable]
    public class CharacterSetup
    {
        [Tooltip("Đặt GIỐNG HỆT tên GameObject card nhân vật ở scene Select Char để khớp theo tên.\n" +
                 "Không khớp tên thì sẽ dùng thứ tự trong danh sách.")]
        public string characterName = "Char";

        [Tooltip("Object nhân vật trong màn chơi")]
        public GameObject characterObject;

        [Tooltip("UI riêng của nhân vật (info, nút skill...). Có thể kéo nhiều object")]
        public List<GameObject> characterUIs = new List<GameObject>();
    }

    [Header("Thứ tự nên giống thứ tự nhân vật ở scene Select Char")]
    [SerializeField] private List<CharacterSetup> characters = new List<CharacterSetup>();

    [Header("Debug")]
    [Tooltip("-1 = dùng nhân vật đã chọn. Đặt 0,1,2... để test nhanh 1 nhân vật")]
    [SerializeField] private int debugCharacterIndex = -1;

    public int ActiveIndex { get; private set; } = -1;
    public GameObject ActiveCharacter { get; private set; }
    public EndGameBase d;

    private void Awake()
    {
        if (characters.Count == 0)
        {
            Debug.LogError("[GameplayChar] Chưa có nhân vật nào trong danh sách!");
            return;
        }

        Activate(ResolveIndex());
    }

    private void Start()
    {
        // Áp lại 1 lần nữa phòng khi script khác bật/tắt nhân vật trong Awake của nó
        if (ActiveIndex >= 0)
            Activate(ActiveIndex);
    }

    private int ResolveIndex()
    {
        int savedIndex = SelectedLevelData.CharacterIndex;
        string savedName = SelectedLevelData.CharacterName;

        Debug.Log("[GameplayChar] Dữ liệu từ Select Char: index " + savedIndex +
                  " - tên card: '" + savedName + "'");

        if (debugCharacterIndex >= 0)
        {
            Debug.LogWarning("[GameplayChar] Đang dùng Debug Character Index = " + debugCharacterIndex +
                             " (bỏ qua nhân vật đã chọn). Đặt lại -1 khi test thật!");
            return Mathf.Clamp(debugCharacterIndex, 0, characters.Count - 1);
        }

        // 1. Ưu tiên khớp theo tên
        if (!string.IsNullOrEmpty(savedName))
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] != null && characters[i].characterName == savedName)
                {
                    Debug.Log("[GameplayChar] Khớp theo tên '" + savedName + "' -> phần tử " + i);
                    return i;
                }
            }

            Debug.LogWarning("[GameplayChar] Không có Character Name nào = '" + savedName +
                             "', dùng theo thứ tự (index " + savedIndex + ").");
        }

        // 2. Không khớp tên -> theo thứ tự
        if (savedIndex < 0 || savedIndex >= characters.Count)
        {
            Debug.LogWarning("[GameplayChar] Index " + savedIndex + " không hợp lệ, dùng nhân vật 0.");
            return 0;
        }

        return savedIndex;
    }

    public void Activate(int index)
    {
        ActiveIndex = index;

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterSetup c = characters[i];
            if (c == null) continue;

            bool active = (i == index);

            if (c.characterObject != null)
                c.characterObject.SetActive(active);

            foreach (GameObject ui in c.characterUIs)
            {
                if (ui != null)
                    ui.SetActive(active);
            }

            if (active)
                ActiveCharacter = c.characterObject;
        }

        Debug.Log("[GameplayChar] Level " + SelectedLevelData.LevelId +
                  " - Bật nhân vật: " + characters[index].characterName + " (phần tử " + index + ")");
    }
}