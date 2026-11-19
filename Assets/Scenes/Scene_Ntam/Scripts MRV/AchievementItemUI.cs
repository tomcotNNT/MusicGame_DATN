using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementItemUI : MonoBehaviour
{
    [Range(0, 4)] public int achievementIndex;

    [Header("Text UI trong Archive")]
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public TMP_Text progressText;
    public TMP_Text rewardText;

    [Header("Keo Reward > RewardImage vao day (Image)")]
    public Image rewardImage;

    [Header("Tuy chon - co the de trong")]
    public Image progressFill;

    [Header("Hien thi anh khi chua du dieu kien / da nhan")]
    [Range(0f, 1f)] public float lockedAlpha = 0.55f;
    [Range(0f, 1f)] public float claimedAlpha = 0.35f;

    private Button rewardButton;

    private void Awake()
    {
        SetupRewardButton();
    }

    private void OnEnable()
    {
        AchievementManager.OnAchievementsChanged += Refresh;
        SetupRewardButton();
        Refresh();
    }

    private void OnDisable()
    {
        AchievementManager.OnAchievementsChanged -= Refresh;
        if (rewardButton != null)
            rewardButton.onClick.RemoveListener(ClaimReward);
    }

    private void SetupRewardButton()
{
    if (rewardImage == null) return;

    rewardImage.raycastTarget = true;

    rewardButton = rewardImage.GetComponent<Button>();

    if (rewardButton == null)
        rewardButton = rewardImage.gameObject.AddComponent<Button>();

    rewardButton.transition = Selectable.Transition.None;

    rewardButton.onClick.RemoveListener(ClaimReward);
    rewardButton.onClick.AddListener(ClaimReward);
}

    public void Refresh()
    {
        AchievementManager manager = AchievementManager.Instance;
        if (manager == null || manager.definitions == null ||
            achievementIndex < 0 || achievementIndex >= manager.definitions.Length)
            return;

        AchievementDefinition a = manager.definitions[achievementIndex];
        int progress = manager.GetProgress(achievementIndex);
        bool completed = manager.IsCompleted(achievementIndex);
        bool claimed = manager.IsClaimed(achievementIndex);

        if (nameText != null) nameText.text = a.title;
        if (descriptionText != null)
            descriptionText.text = a.stat == AchievementStat.HighestCombo
                ? "Reach " + a.target + " Combo"
                : "Reach " + a.target.ToString("N0") + " Score";
        if (progressText != null)
            progressText.text = Mathf.Min(progress, a.target).ToString("N0") + "/" + a.target.ToString("N0");
        if (rewardText != null)
            rewardText.text = "+" + a.rewardAmount;
        if (progressFill != null)
            progressFill.fillAmount = a.target > 0
                ? Mathf.Clamp01((float)progress / a.target) : 1f;

        if (rewardImage != null)
        {
            Color c = rewardImage.color;
            c.a = claimed ? claimedAlpha : completed ? 1f : lockedAlpha;
            rewardImage.color = c;
        }
        if (rewardButton != null)
            rewardButton.interactable = completed && !claimed;
    }

    public void ClaimReward()
    {
        AchievementManager manager = AchievementManager.Instance;
        if (manager == null)
        {
            Debug.LogError("[Achievement] Khong tim thay AchievementManager trong scene.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[Achievement] Khong tim thay GameManager. Hay them GameManager vao scene khoi dau (LobbyNT).");
            return;
        }

        if (manager.definitions == null || achievementIndex < 0 ||
            achievementIndex >= manager.definitions.Length)
        {
            Debug.LogError("[Achievement] Achievement Index khong hop le: " + achievementIndex);
            return;
        }

        if (manager.IsClaimed(achievementIndex))
        {
            Debug.LogWarning("[Achievement] Phan thuong da duoc nhan: " + achievementIndex);
            Refresh();
            return;
        }

        if (!manager.IsCompleted(achievementIndex))
        {
            Debug.LogWarning("[Achievement] Chua hoan thanh nhiem vu: " + achievementIndex);
            Refresh();
            return;
        }

        bool success = manager.Claim(achievementIndex);
        if (success)
        {
            Debug.Log("[Achievement] Nhan thuong thanh cong! Index=" + achievementIndex +
                ", Coin=" + GameManager.Instance.playerData.totalCoins +
                ", Diamond=" + GameManager.Instance.playerData.totalDiamonds);
        }
        else
        {
            Debug.LogError("[Achievement] Claim tra ve false. Kiem tra AchievementManager.Claim(). Index=" + achievementIndex);
        }

        Refresh();
    }
}
