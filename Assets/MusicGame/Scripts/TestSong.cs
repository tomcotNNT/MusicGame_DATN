using UnityEngine;
using System.Collections.Generic;

public class TestSong : MonoBehaviour
{
    [SerializeField] private NoteSpawner noteSpawner;

    [SerializeField] private float interval = 1f;

    private void Start()
    {
        List<NoteData> notes = new List<NoteData>();

        for (int i = 0; i < 1000; i++)
        {
            Lane lane = Random.Range(0, 2) == 0
                ? Lane.Up
                : Lane.Down;

            notes.Add(
                new NoteData(i * interval, lane)
            );
        }

        noteSpawner.SetNotes(notes);
    }
}