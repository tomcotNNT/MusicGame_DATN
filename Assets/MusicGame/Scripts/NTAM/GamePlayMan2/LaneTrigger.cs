using UnityEngine;

public class LaneTrigger : MonoBehaviour
{
    public bool isLeft = true;
    public PlayerDefenseController player;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        OFDP_Enemy enemy = collision.GetComponent<OFDP_Enemy>();
        if (enemy != null && player != null)
        {
            enemy.isInAttackRange = true;
            player.RegisterEnemy(enemy, isLeft);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        OFDP_Enemy enemy = collision.GetComponent<OFDP_Enemy>();
        if (enemy != null && player != null)
        {
            enemy.isInAttackRange = false;
            player.UnregisterEnemy(enemy, isLeft);
        }
    }
}