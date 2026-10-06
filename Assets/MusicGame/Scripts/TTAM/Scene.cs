using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class Scenes : MonoBehaviour
{
  

   public void GetScene(string sceneName)
    {
       // Chuyển sang scene được chỉ định
        SceneManager.LoadScene(sceneName);
    }
}