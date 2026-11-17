using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementItemUI : MonoBehaviour
{
    [Range(0, 4)] public int achievementIndex;
    [Header("Assign children from this Archive item")]
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public TMP_Text progressText;
    public TMP_Text rewardText;
    public TMP_Text claimButtonText;
    public Image progressFill;
    public Button claimButton;

    private void OnEnable()
    {
        AchievementManager.OnAchievementsChanged += Refresh;
        if (claimButton != null) claimButton.onClick.AddListener(Claim);
        Refresh();
    }

    private void OnDisable()
    {
        AchievementManager.OnAchievementsChanged -= Refresh;
        if (claimButton != null) claimButton.onClick.RemoveListener(Claim);
    }

    public void Refresh()
    {
        var manager = AchievementManager.Instance;
        if (manager == null || manager.definitions == null || achievementIndex >= manager.definitions.Length) return;
        var a = manager.definitions[achievementIndex];
        int progress = manager.GetProgress(achievementIndex);
        bool completed = manager.IsCompleted(achievementIndex);
        bool claimed = manager.IsClaimed(achievementIndex);

        if (nameText != null) nameText.text = a.title;
        if (descriptionText != null)
            descriptionText.text = a.stat == AchievementStat.HighestCombo
                ? "Reach " + a.target + " Combo" : "Reach " + a.target.ToString("N0") + " Score";
        if (progressText != null) progressText.text = Mathf.Min(progress, a.target).ToString("N0") + "/" + a.target.ToString("N0");
        if (rewardText != null) rewardText.text = "+" + a.rewardAmount + " " + a.rewardType;
        if (progressFill != null) progressFill.fillAmount = a.target > 0 ? Mathf.Clamp01((float)progress / a.target) : 1f;
        if (claimButton != null) claimButton.interactable = completed && !claimed;
        if (claimButtonText != null) claimButtonText.text = claimed ? "CLAIMED" : completed ? "CLAIM" : "LOCKED";
    }

    private void Claim()
    {
        if (AchievementManager.Instance != null) AchievementManager.Instance.Claim(achievementIndex);
    }
}
