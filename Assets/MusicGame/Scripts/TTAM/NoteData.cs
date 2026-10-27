public enum Lane
{
    Up,
    Down
}

[System.Serializable]
public class NoteData
{
    public float spawnTime;
    public Lane lane;

    public NoteData(float spawnTime, Lane lane)
    {
        this.spawnTime = spawnTime;
        this.lane = lane;
    }
}