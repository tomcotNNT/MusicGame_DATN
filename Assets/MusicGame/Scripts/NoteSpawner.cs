using UnityEngine;
using System.Collections.Generic;

public class NoteSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject notePrefab;

    [Header("Spawn Point")]
    [SerializeField] private Transform upSpawnPoint;
    [SerializeField] private Transform downSpawnPoint;

    [Header("Settings")]
    [SerializeField] private float spawnDistance = 10f;

    private List<NoteData> notes = new List<NoteData>();

    private int currentNoteIndex;

    private float currentTime;

    private void Update()
    {
        currentTime += Time.deltaTime;

        SpawnNotes();
    }

    private void SpawnNotes()
    {
        while (
            currentNoteIndex < notes.Count &&
            currentTime >= notes[currentNoteIndex].spawnTime
        )
        {
            SpawnNote(notes[currentNoteIndex]);

            currentNoteIndex++;
        }
    }

    private void SpawnNote(NoteData data)
    {
        Transform spawnPoint = GetSpawnPoint(data.lane);

        Instantiate(
            notePrefab,
            spawnPoint.position,
            Quaternion.identity
        );
    }

    private Transform GetSpawnPoint(Lane lane)
    {
        if (lane == Lane.Up)
            return upSpawnPoint;

        return downSpawnPoint;
    }

    public void SetNotes(List<NoteData> newNotes)
    {
        notes = newNotes;
        currentNoteIndex = 0;
        currentTime = 0;
    }
}