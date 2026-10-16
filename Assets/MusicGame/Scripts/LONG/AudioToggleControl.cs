
using UnityEngine;
using UnityEngine.UI;

public class AudioToggleControl : MonoBehaviour
{
    [Header("Music")]
    public Button musicButton;
    public Image musicImage;
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;

    [Header("Sound")]
    public Button soundButton;
    public Image soundImage;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    private bool musicOn = true;
    private bool soundOn = true;

    private void Start()
    {
        musicButton.onClick.AddListener(ToggleMusic);
        soundButton.onClick.AddListener(ToggleSound);

        UpdateMusicSprite();
        UpdateSoundSprite();
    }

    private void ToggleMusic()
    {
        musicOn = !musicOn;
        UpdateMusicSprite();

        Debug.Log("Music: " + (musicOn ? "ON" : "OFF"));
    }

    private void ToggleSound()
    {
        soundOn = !soundOn;
        UpdateSoundSprite();

        Debug.Log("Sound: " + (soundOn ? "ON" : "OFF"));
    }

    private void UpdateMusicSprite()
    {
        musicImage.sprite = musicOn ? musicOnSprite : musicOffSprite;
    }

    private void UpdateSoundSprite()
    {
        soundImage.sprite = soundOn ? soundOnSprite : soundOffSprite;
    }
}
