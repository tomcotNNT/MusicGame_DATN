using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Current Level")]
    [SerializeField] private LevelData currentLevel;

    [Header("References")]
    [SerializeField] private MidiReader midiReader;
    [SerializeField] private AudioSource audioSource;

    private void Start()
    {
        LoadLevel();
    }

    private void LoadLevel()
    {
        if (currentLevel == null)
        {
            Debug.LogError("Chưa chọn LevelData!");
            return;
        }

        if (midiReader == null)
        {
            Debug.LogError("Chưa gán MidiReader!");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("Chưa gán AudioSource!");
            return;
        }

        // Load MIDI
        midiReader.LoadMidi(currentLevel);

        // Load nhạc
        if (currentLevel.music != null)
        {
            audioSource.clip = currentLevel.music;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("Level chưa có AudioClip!");
        }

        Debug.Log("Loaded Level: " + currentLevel.levelName);
    }
}