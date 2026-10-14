using UnityEngine;

public class SimpleFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float offsetX = 4f;

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 pos = transform.position;
            pos.x = target.position.x + offsetX; // Camera đi trước player một khoảng offsetX
            transform.position = pos;
        }
    }
}