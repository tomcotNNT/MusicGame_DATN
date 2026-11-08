using System.Collections.Generic;
using UnityEngine;

public static class LetterInventoryManager
{
    private const string PREFIX = "Letter_Inv_";

    // Lấy số lượng của một chữ cái trong túi
    public static int GetLetterCount(char letter)
    {
        char upper = char.ToUpper(letter);
        return PlayerPrefs.GetInt(PREFIX + upper, 0);
    }

    // Cộng thêm chữ cái khi nhặt trong màn chơi hoặc khi test
    public static void AddLetter(char letter, int amount = 1)
    {
        char upper = char.ToUpper(letter);
        int current = GetLetterCount(upper);
        PlayerPrefs.SetInt(PREFIX + upper, current + amount);
        PlayerPrefs.Save();
    }

    // Kiểm tra xem túi đồ có đủ toàn bộ chữ cái để ghép thành từ yêu cầu không
    public static bool HasEnoughLettersForWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return false;

        // Đếm nhu cầu chữ của từ khóa
        Dictionary<char, int> needed = GetWordLetterCounts(word);

        foreach (var pair in needed)
        {
            if (GetLetterCount(pair.Key) < pair.Value)
            {
                return false; // Thiếu ít nhất 1 chữ
            }
        }
        return true;
    }

    // Trừ chữ cái trong kho khi người chơi bấm CLAIM mở khóa nhân vật
    public static void ConsumeLettersForWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return;

        Dictionary<char, int> needed = GetWordLetterCounts(word);

        foreach (var pair in needed)
        {
            int current = GetLetterCount(pair.Key);
            int remaining = Mathf.Max(0, current - pair.Value);
            PlayerPrefs.SetInt(PREFIX + pair.Key, remaining);
        }
        PlayerPrefs.Save();
        Debug.Log($"<color=green>[INVENTORY] Đã trừ đủ các chữ cái cho từ {word}!</color>");
    }

    // Hàm phụ đếm tần suất xuất hiện của từng ký tự trong từ (Ví dụ: SCOTT -> S:1, C:1, O:1, T:2)
    public static Dictionary<char, int> GetWordLetterCounts(string word)
    {
        Dictionary<char, int> counts = new Dictionary<char, int>();
        foreach (char c in word.ToUpper())
        {
            if (!char.IsLetter(c)) continue;
            if (counts.ContainsKey(c)) counts[c]++;
            else counts[c] = 1;
        }
        return counts;
    }
}