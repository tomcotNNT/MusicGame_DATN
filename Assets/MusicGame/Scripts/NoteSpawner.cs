using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MidiReader midiReader;
    [SerializeField] private GameObject notePrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Settings")]
    [SerializeField] private float spawnAheadTime = 2f;

    private int nextNoteIndex = 0;

    private AudioSource audioSource;
    [SerializeField] private LevelData currentLevel;

    private void Start()
    {
        audioSource = FindFirstObjectByType<AudioSource>();

    }

    private void Update()
    {
        if (midiReader == null)
            return;

        if (midiReader.Notes == null)
            return;

        if (audioSource == null)
            return;

        SpawnNotes();
    }

    private void SpawnNotes()
    {
        float currentTime = audioSource.time;

        while (nextNoteIndex < midiReader.Notes.Count)
        {
            MidiReader.RhythmNote note =
                midiReader.Notes[nextNoteIndex];

            float timeUntilNote = note.time - currentTime;

            if (timeUntilNote > spawnAheadTime)
                break;

            SpawnNote(note);

            nextNoteIndex++;
        }
    }

    private void SpawnNote(MidiReader.RhythmNote note)
    {
        if (note.lane < 0 || note.lane >= spawnPoints.Length)
            return;

        GameObject newNote = Instantiate(
            notePrefab,
            spawnPoints[note.lane].position,
            Quaternion.identity
        );

        Note movement = newNote.GetComponent<Note>();

        if (movement != null)
        {
            movement.SetSpeed(currentLevel.noteSpeed);
        }

        Debug.Log(
            $"Spawn Note | Time: {note.time:F2} | Lane: {note.lane}"
        );
    }
}