
using UnityEngine;

public class PlayerRhythmInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossSpawner2 enemySpawner;

    private void OnEnable()
    {
        // Mobile: 2 nút LaneButton (Left / Right) trên màn hình
        LaneButtonInput.Pressed += Attack;
    }

    private void OnDisable()
    {
        LaneButtonInput.Pressed -= Attack;
    }

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
