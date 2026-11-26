using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chọn animation đòn đánh theo kết quả bắt beat (dùng chung cho Man2, Man3, Man4, Man5):
///   - Trượt / bấm hụt       -> đòn NHẸ  (Attack1, Attack4, Attack6)
///   - Good                  -> đòn NHẸ
///   - Great                 -> đòn VỪA  (Attack2, Attack5, Attack8)
///   - Perfect               -> đòn MẠNH (Attack3, Attack7) hoặc đòn vừa
///   - Mỗi mốc 10 combo      -> chiêu kết Special
/// Không lặp lại đúng đòn vừa đánh. Controller thiếu đòn nào thì tự bỏ qua đòn đó.
/// </summary>
public static class AttackPicker
{
    private static readonly string[] Light = { "Attack1", "Attack4", "Attack6" };
    private static readonly string[] Medium = { "Attack2", "Attack5", "Attack8" };
    private static readonly string[] Heavy = { "Attack3", "Attack7" };

    /// <summary>Mỗi bao nhiêu combo thì ra chiêu kết (0 = tắt).</summary>
    public const int FinisherEvery = 10;

    private static readonly Dictionary<Animator, string> last = new Dictionary<Animator, string>();
    private static readonly List<string> pool = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => last.Clear();

    /// <param name="hit">đòn này có trúng nốt không</param>
    /// <param name="combo">ComboSystem (để lấy độ chính xác và số combo). null = coi như Good</param>
    /// <returns>tên trigger, hoặc null nếu controller không có đòn nào</returns>
    public static string Pick(Animator animator, bool hit, ComboSystem combo)
    {
        if (animator == null) return null;

        if (hit && combo != null && FinisherEvery > 0)
        {
            int c = combo.GetCombo();
            if (c > 0 && c % FinisherEvery == 0 && AnimatorSafe.Has(animator, "Special"))
                return Remember(animator, "Special");
        }

        pool.Clear();
        if (!hit) Add(animator, Light);
        else
        {
            var grade = combo != null ? combo.LastGrade : ScoreSystem.ScoreGrade.Good;
            switch (grade)
            {
                case ScoreSystem.ScoreGrade.Perfect: Add(animator, Heavy); Add(animator, Medium); break;
                case ScoreSystem.ScoreGrade.Great: Add(animator, Medium); break;
                default: Add(animator, Light); break;
            }
        }
        // controller cũ chỉ có Attack1-3 -> vẫn có đòn để đánh
        if (pool.Count == 0) { Add(animator, Light); Add(animator, Medium); Add(animator, Heavy); }
        if (pool.Count == 0) return null;

        last.TryGetValue(animator, out string prev);
        if (pool.Count > 1) pool.Remove(prev);
        return Remember(animator, pool[Random.Range(0, pool.Count)]);
    }

    /// <summary>Chọn và bắn trigger luôn. Trả về false nếu không có đòn nào để chạy.</summary>
    public static bool Play(Animator animator, bool hit, ComboSystem combo)
    {
        string t = Pick(animator, hit, combo);
        if (t == null) return false;
        AnimatorSafe.SetTrigger(animator, t);
        return true;
    }

    private static void Add(Animator a, string[] names)
    {
        foreach (string n in names)
            if (AnimatorSafe.Has(a, n) && !pool.Contains(n)) pool.Add(n);
    }

    private static string Remember(Animator a, string t)
    {
        last[a] = t;
        return t;
    }
}
