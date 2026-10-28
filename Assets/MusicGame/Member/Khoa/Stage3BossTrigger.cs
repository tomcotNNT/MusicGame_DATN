
// using UnityEngine;

// public class Stage3BossTrigger : MonoBehaviour
// {
//     [Header("Boss Configuration")]
//     [SerializeField] private GameObject bossObject;
//     [SerializeField] private int requiredScore = 1000;

//     [Header("Boss State")]
//     [SerializeField] private bool bossActivated = false;

//     private int currentScore = 0;

//     private void Start()
//     {
//         currentScore = 0;
//         bossActivated = false;

//         if (bossObject == null)
//         {
//             Debug.LogWarning("Stage 3 Boss: Chua gan Boss Object!");
//             return;
//         }

//         bossObject.SetActive(false);

//         Debug.Log("Stage 3 Boss Trigger da san sang.");
//     }

//     public void UpdateScore(int score)
//     {
//         currentScore = score;

//         CheckBossActivation();
//     }

//     private void CheckBossActivation()
//     {
//         if (bossActivated)
//             return;

//         if (currentScore >= requiredScore)
//         {
//             ActivateBoss();
//         }
//     }

//     private void ActivateBoss()
//     {
//         if (bossObject == null)
//             return;

//         bossActivated = true;
//         bossObject.SetActive(true);

//         Debug.Log("Boss man 3 da duoc kich hoat!");

//         OnBossActivated();
//     }

//     private void OnBossActivated()
//     {
//         Debug.Log("Bat dau giai doan chien dau voi Boss.");
//     }

//     public bool IsBossActivated()
//     {
//         return bossActivated;
//     }

//     public int GetCurrentScore()
//     {
//         return currentScore;
//     }

//     public int GetRequiredScore()
//     {
//         return requiredScore;
//     }

//     public void ResetBossState()
//     {
//         currentScore = 0;
//         bossActivated = false;

//         if (bossObject != null)
//         {
//             bossObject.SetActive(false);
//         }

//         Debug.Log("Trang thai Boss man 3 da duoc reset.");
//     }
// }
