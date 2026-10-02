using System.Collections.Generic;
using UnityEngine;

public class InfiniteLane : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject lanePrefab;

    [Header("Lane Settings")]
    [SerializeField] private float laneWidth = 20f;
    [SerializeField] private int initialLaneCount = 5;

    private readonly Queue<GameObject> lanePool = new Queue<GameObject>();

    private void Start()
    {
        CreateInitialLanes();
    }

    private void Update()
    {
        RecycleLane();
    }

    private void CreateInitialLanes()
    {
        float startX = transform.position.x;

        for (int i = 0; i < initialLaneCount; i++)
        {
            Vector3 position = new Vector3(
                startX + i * laneWidth,
                transform.position.y,
                transform.position.z
            );

            GameObject lane = Instantiate(
                lanePrefab,
                position,
                Quaternion.identity,
                transform
            );

            lanePool.Enqueue(lane);
        }
    }

    private void RecycleLane()
    {
        if (lanePool.Count == 0)
            return;

        GameObject firstLane = lanePool.Peek();

        // Khi Player đã đi qua đoạn lane đầu tiên
        if (player.position.x >
            firstLane.transform.position.x + laneWidth)
        {
            firstLane = lanePool.Dequeue();

            GameObject lastLane = GetLastLane();

            Vector3 newPosition = new Vector3(
                lastLane.transform.position.x + laneWidth,
                lastLane.transform.position.y,
                lastLane.transform.position.z
            );

            firstLane.transform.position = newPosition;

            lanePool.Enqueue(firstLane);
        }
    }

    private GameObject GetLastLane()
    {
        GameObject last = null;

        foreach (GameObject lane in lanePool)
        {
            last = lane;
        }

        return last;
    }
}