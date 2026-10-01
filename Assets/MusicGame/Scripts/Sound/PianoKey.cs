using UnityEngine;
using System.Collections.Generic;

public class PianoKey : MonoBehaviour
{
    public enum KeyType
    {
        Left,
        Right
    }

    public static List<PianoKey> ActiveKeys = new List<PianoKey>();

    [Header("Visual")]
    [SerializeField] private GameObject keyLeft;
    [SerializeField] private GameObject keyRight;

    private KeyType keyType;
    private float targetTime;

    public bool PlayerOnKey { get; private set; }
    public bool IsCompleted { get; private set; }

    private void OnEnable()
    {
        if (!ActiveKeys.Contains(this))
            ActiveKeys.Add(this);
    }

    private void OnDisable()
    {
        ActiveKeys.Remove(this);
    }

    public void Setup(KeyType type, float time)
    {
        keyType = type;
        targetTime = time;

        PlayerOnKey = false;
        IsCompleted = false;

        keyLeft.SetActive(type == KeyType.Left);
        keyRight.SetActive(type == KeyType.Right);
    }

    public KeyType GetKeyType()
    {
        return keyType;
    }

    public float GetTargetTime()
    {
        return targetTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerOnKey = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerOnKey = false;
        }
    }

    public void Complete()
    {
        IsCompleted = true;
        Destroy(gameObject);
    }
}