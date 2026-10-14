using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongItem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image albumImage;
    [SerializeField] private TMP_Text songName;
    [SerializeField] private TMP_Text singerName;

    // =========================================================
    // SONG DATA
    // =========================================================

    public void SetSong(Sprite album, string songTitle, string singer)
    {
        if (albumImage != null)
            albumImage.sprite = album;

        if (songName != null)
            songName.text = songTitle;

        if (singerName != null)
            singerName.text = singer;
    }

    // =========================================================
    // POSITION
    // =========================================================

    public void SetPosition(float x)
    {
        Vector3 pos = transform.localPosition;
        pos.x = x;
        transform.localPosition = pos;
    }

    public void SetPosition(Vector2 position)
    {
        Vector3 pos = transform.localPosition;

        pos.x = position.x;
        pos.y = position.y;

        transform.localPosition = pos;
    }

    public void SetPosition(Vector3 position)
    {
        transform.localPosition = position;
    }

    public Vector2 GetPosition()
    {
        return new Vector2(
            transform.localPosition.x,
            transform.localPosition.y
        );
    }

    // =========================================================
    // SCALE
    // =========================================================

    public void SetScale(float scale)
    {
        transform.localScale = Vector3.one * scale;
    }

    public float GetScale()
    {
        return transform.localScale.x;
    }

    // =========================================================
    // VISIBILITY
    // =========================================================

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    // =========================================================
    // SORTING
    // =========================================================

    public void BringToFront()
    {
        transform.SetAsLastSibling();
    }
}