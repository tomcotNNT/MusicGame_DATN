using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào Canvas (hoặc object cha) -> TẤT CẢ Button con tự có tiếng khi bấm.
/// Nút nào đã có ButtonSound riêng thì giữ nguyên tiếng riêng của nút đó.
/// </summary>
public class ButtonSoundGroup : MonoBehaviour
{
    [Tooltip("Tiếng bấm mặc định cho mọi nút")]
    [SerializeField] private AudioClip clickClip;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Tooltip("Gắn cả nút đang bị ẩn (panel đang tắt) lúc vào scene")]
    [SerializeField] private bool includeInactive = true;

    private void Awake()
    {
        Button[] buttons = GetComponentsInChildren<Button>(includeInactive);

        foreach (Button b in buttons)
        {
            if (b.GetComponent<ButtonSound>() != null)
                continue;

            b.gameObject.AddComponent<ButtonSound>().Setup(clickClip, volume);
        }
    }
}
