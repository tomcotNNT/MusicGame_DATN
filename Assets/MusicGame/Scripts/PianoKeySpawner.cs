using UnityEngine;

public class PianoKeySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MidiReader midiReader;
    [SerializeField] private GameObject pianoKeyPrefab;

    [Header("Fixed Spawn")]
    [SerializeField] private Transform spawnPoint;

    [Header("Key Distance")]
    [SerializeField] private float minKeyDistance = 2f;
    [SerializeField] private float maxKeyDistance = 8f;

    private AudioSource audioSource;
    private int nextNoteIndex = 0;

    private float nextKeyX;
    private bool hasSpawnedKey = false;

    private PianoKey.KeyType lastType;
    private int sameCount = 0;

    private void Start()
    {
        audioSource = FindFirstObjectByType<AudioSource>();

        if (spawnPoint == null)
            Debug.LogError("Chưa gán Spawn Point!");

        if (midiReader == null)
            Debug.LogError("Chưa gán MidiReader!");

        if (pianoKeyPrefab == null)
            Debug.LogError("Chưa gán PianoKey Prefab!");
    }

    private void Update()
    {
        if (midiReader == null ||
            audioSource == null ||
            spawnPoint == null)
            return;

        SpawnKeys();
    }

    private void SpawnKeys()
    {
        float currentTime = audioSource.time;

        // Spawn trước một khoảng thời gian
        float spawnAheadTime = 2f;

        while (
            nextNoteIndex < midiReader.Notes.Count &&
            midiReader.Notes[nextNoteIndex].time
            <= currentTime + spawnAheadTime)
        {
            SpawnKey(midiReader.Notes[nextNoteIndex]);

            nextNoteIndex++;
        }
    }

    private void SpawnKey(MidiReader.RhythmNote note)
    {
        float distance;

        // Key đầu tiên
        if (!hasSpawnedKey)
        {
            nextKeyX = spawnPoint.position.x;
            hasSpawnedKey = true;
        }
        else
        {
            // Khoảng cách giữa 2 key
            distance = Random.Range(
                minKeyDistance,
                maxKeyDistance
            );

            nextKeyX += distance;
        }

        Vector3 spawnPosition = new Vector3(
            nextKeyX,
            spawnPoint.position.y,
            spawnPoint.position.z
        );

        GameObject obj = Instantiate(
            pianoKeyPrefab,
            spawnPosition,
            Quaternion.identity
        );

        PianoKey key = obj.GetComponent<PianoKey>();

        if (key == null)
        {
            Debug.LogError(
                "PianoKey Prefab chưa có PianoKey.cs!"
            );
            return;
        }

        PianoKey.KeyType type = GetRandomKey();

        key.Setup(type, note.time);
    }

    private PianoKey.KeyType GetRandomKey()
    {
        PianoKey.KeyType type;

        if (sameCount >= 2)
        {
            type = lastType == PianoKey.KeyType.Left
                ? PianoKey.KeyType.Right
                : PianoKey.KeyType.Left;

            sameCount = 0;
        }
        else
        {
            type = Random.value < 0.5f
                ? PianoKey.KeyType.Left
                : PianoKey.KeyType.Right;
        }

        if (type == lastType)
            sameCount++;
        else
            sameCount = 1;

        lastType = type;

        return type;
    }
}