
using UnityEngine;

public class PlayerRhythmInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawnerMan2 enemySpawner;

    private void Update()
    {
        // Keyboard
        if (Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Attack(true);
        }

        if (Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.RightArrow))
        {
            Attack(false);
        }

        // Mobile touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                bool isLeft =
                    touch.position.x < Screen.width * 0.5f;

                Attack(isLeft);
            }
        }
    }

    private void Attack(bool isLeft)
    {
        if (enemySpawner == null)
            return;

        bool success = enemySpawner.TryHit(isLeft);

        if (success)
        {
            Debug.Log(isLeft
                ? "LEFT HIT"
                : "RIGHT HIT");
        }
    }
}
