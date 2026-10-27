using UnityEngine;
using UnityEngine.UI;

public class AudioToggleControl : MonoBehaviour
{
    [Header("Music")]
    public Button musicButton;
    public Image musicImage;

    [Header("Sound")]
    public Button soundButton;
    public Image soundImage;

    [Header("Colors")]
    public Color onColor = Color.white;
    public Color offColor = Color.gray;

    private bool musicOn = true;
    private bool soundOn = true;

    private void Start()
    {
        // Gán sự kiện cho 2 Button
        musicButton.onClick.AddListener(ToggleMusic);
        soundButton.onClick.AddListener(ToggleSound);

        // Cập nhật màu ban đầu
        UpdateMusicColor();
        UpdateSoundColor();
    }

    private void ToggleMusic()
    {
        musicOn = !musicOn;

        UpdateMusicColor();

        Debug.Log("Music: " + (musicOn ? "ON" : "OFF"));
    }

    private void ToggleSound()
    {
        soundOn = !soundOn;

        UpdateSoundColor();

        Debug.Log("Sound: " + (soundOn ? "ON" : "OFF"));
    }

    private void UpdateMusicColor()
    {
        musicImage.color = musicOn ? onColor : offColor;
    }

    private void UpdateSoundColor()
    {
        soundImage.color = soundOn ? onColor : offColor;
    }
}