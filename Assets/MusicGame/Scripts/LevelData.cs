using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Rhythm/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level")]
    public string levelName;

    [Header("Music")]
    public AudioClip music;

    [Header("MIDI")]
    public string midiFileName;

    [Header("Gameplay")]
    public float noteSpeed = 5f;
    public int difficulty = 1;
}