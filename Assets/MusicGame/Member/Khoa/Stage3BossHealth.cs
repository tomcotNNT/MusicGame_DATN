
// using UnityEngine;

// public class Stage3BossHealth : MonoBehaviour
// {
//     [Header("Boss Health Settings")]
//     [SerializeField] private int maxHealth = 3;
//     [SerializeField] private int currentHealth;

//     [Header("Boss State")]
//     [SerializeField] private bool isDefeated = false;

//     private void Start()
//     {
//         currentHealth = maxHealth;
//         isDefeated = false;

//         Debug.Log("Boss man 3 bat dau voi "
//             + currentHealth + " mang.");
//     }

//     public void TakeDamage(int damage)
//     {
//         if (isDefeated)
//             return;

//         if (damage <= 0)
//             return;

//         currentHealth -= damage;
//         currentHealth = Mathf.Max(0, currentHealth);

//         Debug.Log("Boss nhan sat thuong: " + damage);
//         Debug.Log("So mang con lai: " + currentHealth);

//         CheckBossHealth();
//     }

//     private void CheckBossHealth()
//     {
//         if (currentHealth <= 0)
//         {
//             DefeatBoss();
//         }
//         else
//         {
//             Debug.Log("Boss van dang chien dau.");
//         }
//     }

//     private void DefeatBoss()
//     {
//         if (isDefeated)
//             return;

//         isDefeated = true;

//         Debug.Log("Boss man 3 da bi danh bai!");

//         gameObject.SetActive(false);
//     }

//     public int GetCurrentHealth()
//     {
//         return currentHealth;
//     }

//     public int GetMaxHealth()
//     {
//         return maxHealth;
//     }

//     public bool IsDefeated()
//     {
//         return isDefeated;
//     }

//     public void ResetHealth()
//     {
//         currentHealth = maxHealth;
//         isDefeated = false;

//         gameObject.SetActive(true);

//         Debug.Log("Boss da duoc hoi phuc.");
//     }
// }
