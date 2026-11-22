using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý danh sách thành tựu: đọc chỉ số từ dữ liệu JSON (PlayerDataManager),
/// so sánh với mục tiêu, cho nhận thưởng và hiện panel Notice.
/// </summary>
public class AchievementPanel : MonoBehaviour
{
    [Header("Danh sách thành tựu")]
    [SerializeField] private List<AchievementDefinition> achievements = new List<AchievementDefinition>();

    [Header("List UI")]
    [SerializeField] private Transform contentRoot;          // Content của ScrollView
    [SerializeField] private AchievementItemUI itemPrefab;

    [Header("Notice Panel (hiện khi nhận thưởng)")]
    [SerializeField] private GameObject noticePanel;
    [SerializeField] private TMP_Text noticeTitleText;       // "Notice" (có thể bỏ trống)
    [SerializeField] private TMP_Text noticeMessageText;     // "Bạn nhận được"
    [SerializeField] private Image noticeRewardIcon;
    [SerializeField] private TMP_Text noticeAmountText;      // "x100"
    [SerializeField] private Button noticeOkButton;

    [Header("Icon phần thưởng")]
    [SerializeField] private Sprite coinIcon;
    [SerializeField] private Sprite diamondIcon;

    private readonly List<AchievementItemUI> items = new List<AchievementItemUI>();
    private bool built = false;

    private void Start()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("AchievementPanel: PlayerDataManager chưa tồn tại!");
            return;
        }

        BuildList();

        if (noticePanel != null)
            noticePanel.SetActive(false);

        if (noticeOkButton != null)
            noticeOkButton.onClick.AddListener(CloseNotice);

        // Mọi thay đổi dữ liệu (tiền, level, điểm...) đều cập nhật lại glow/tiến độ ngay
        PlayerDataManager.Instance.OnDataChanged += RefreshAll;

        RefreshAll();
    }

    private void OnEnable()
    {
        // Mở lại panel thì làm mới (Start chỉ chạy lần đầu)
        if (built)
            RefreshAll();
    }

    private void OnDestroy()
    {
        if (PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.OnDataChanged -= RefreshAll;

        if (noticeOkButton != null)
            noticeOkButton.onClick.RemoveListener(CloseNotice);
    }

    // ================= BUILD =================

    private void BuildList()
    {
        if (contentRoot == null || itemPrefab == null)
        {
            Debug.LogError("AchievementPanel: chưa gán Content Root hoặc Item Prefab!");
            return;
        }

        HashSet<string> usedIds = new HashSet<string>();

        foreach (AchievementDefinition def in achievements)
        {
            if (string.IsNullOrEmpty(def.id))
            {
                Debug.LogWarning("AchievementPanel: bỏ qua thành tựu '" + def.title + "' vì thiếu ID.");
                continue;
            }

            if (!usedIds.Add(def.id))
            {
                Debug.LogWarning("AchievementPanel: ID bị trùng '" + def.id + "', bỏ qua.");
                continue;
            }

            AchievementItemUI item = Instantiate(itemPrefab, contentRoot);
            item.Setup(def, GetRewardIcon(def.rewardType), OnItemClicked);
            items.Add(item);
            definitions.Add(def);
        }

        built = true;
    }

    // Danh sách def tương ứng với items (cùng thứ tự), đã loại ID lỗi/trùng
    private readonly List<AchievementDefinition> definitions = new List<AchievementDefinition>();

    // ================= REFRESH =================

    private void RefreshAll()
    {
        if (!built || PlayerDataManager.Instance == null || PlayerDataManager.Instance.Data == null)
            return;

        for (int i = 0; i < items.Count; i++)
        {
            AchievementDefinition def = definitions[i];
            long value = GetStatValue(def.stat);

            items[i].Refresh(value, GetState(def, value));
        }
    }

    private AchievementState GetState(AchievementDefinition def, long currentValue)
    {
        if (PlayerDataManager.Instance.IsAchievementClaimed(def.id))
            return AchievementState.Claimed;

        if (currentValue >= def.targetValue)
            return AchievementState.Claimable;

        return AchievementState.InProgress;
    }

    // Lấy giá trị từ dữ liệu JSON
    private long GetStatValue(AchievementStat stat)
    {
        PlayerData data = PlayerDataManager.Instance.Data;

        switch (stat)
        {
            case AchievementStat.Coin:
                return data.currency.coin;

            case AchievementStat.Diamond:
                return data.currency.diamond;

            case AchievementStat.TotalCoinEarned:
                return data.currency.totalCoinEarned;

            case AchievementStat.TotalDiamondEarned:
                return data.currency.totalDiamondEarned;

            case AchievementStat.LevelsUnlocked:
                return data.progression.unlockedLevels.Count;

            case AchievementStat.BestScoreAnyLevel:
            {
                long best = 0;
                foreach (LevelRecordData r in data.levelRecords)
                    best = System.Math.Max(best, r.highScore);
                return best;
            }

            case AchievementStat.BestComboAnyLevel:
            {
                long best = 0;
                foreach (LevelRecordData r in data.levelRecords)
                    best = System.Math.Max(best, r.maxCombo);
                return best;
            }

            case AchievementStat.TotalGood:
                return data.comboStats.Get(ComboType.Good);

            case AchievementStat.TotalGreat:
                return data.comboStats.Get(ComboType.Great);

            case AchievementStat.TotalPerfect:
                return data.comboStats.Get(ComboType.Perfect);

            case AchievementStat.TotalHighScore:
            {
                long total = 0;
                foreach (LevelRecordData r in data.levelRecords)
                    total += r.highScore;
                return total;
            }

            default:
                return 0;
        }
    }

    // ================= CLAIM =================

    private void OnItemClicked(AchievementDefinition def)
    {
        PlayerDataManager pm = PlayerDataManager.Instance;

        if (pm == null || pm.IsAchievementClaimed(def.id))
            return;

        // Kiểm tra lại điều kiện phòng trường hợp dữ liệu vừa đổi
        if (GetStatValue(def.stat) < def.targetValue)
            return;

        if (!pm.ClaimAchievementReward(def.id, def.rewardType, def.rewardAmount))
            return;

        // Dữ liệu đã lưu -> OnDataChanged tự refresh icon sang trạng thái Claimed
        ShowNotice(def);
    }

    private Sprite GetRewardIcon(CurrencyManager.CurrencyType type)
    {
        return type == CurrencyManager.CurrencyType.Coin ? coinIcon : diamondIcon;
    }

    // ================= NOTICE =================

    private void ShowNotice(AchievementDefinition def)
    {
        if (noticePanel == null)
        {
            Debug.LogWarning("AchievementPanel: chưa gán Notice Panel!");
            return;
        }

        if (noticeTitleText != null)
            noticeTitleText.text = "Notice";

        if (noticeMessageText != null)
            noticeMessageText.text = "Bạn nhận được";

        if (noticeRewardIcon != null)
            noticeRewardIcon.sprite = GetRewardIcon(def.rewardType);

        if (noticeAmountText != null)
            noticeAmountText.text = "x" + def.rewardAmount.ToString("N0");

        noticePanel.SetActive(true);
    }

    private void CloseNotice()
    {
        if (noticePanel != null)
            noticePanel.SetActive(false);
    }
}