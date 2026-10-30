using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [Tooltip("Chọn loại âm thanh phát khi bấm nút này")]
    [SerializeField] private UISfxType soundType = UISfxType.Click;

    private void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(PlaySound);
        }
    }

    private void PlaySound()
    {
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.PlaySFX(soundType);
        }
    }
}