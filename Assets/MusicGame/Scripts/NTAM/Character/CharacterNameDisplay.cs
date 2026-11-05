using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class LetterSpritePair
{
    public char letter;       // Ký tự (A, B, C...)
    public Sprite neonSprite; // Sprite Neon xanh hồng sáng
    public Sprite graySprite; // Sprite Xám tắt đèn
}

public class CharacterNameDisplay : MonoBehaviour
{
    [Header("1. Thư viện ký tự Neon & Xám")]
    [SerializeField] private List<LetterSpritePair> alphabetDatabase = new List<LetterSpritePair>();

    [Header("2. Danh sách 7 Slot chữ cái cố định")]
    [SerializeField] private List<Image> letterSlots = new List<Image>();

    /// <summary>
    /// Hiển thị tên nhân vật và tự động bật sáng chữ cái nếu kho đồ có đủ
    /// </summary>
    public void DisplayWord(string word, bool isCharacterUnlocked)
    {
        if (string.IsNullOrEmpty(word) || letterSlots == null || letterSlots.Count == 0)
        {
            HideAllSlots();
            return;
        }

        string upperWord = word.ToUpper();
        Dictionary<char, int> usedLetterCounts = new Dictionary<char, int>();

        for (int i = 0; i < letterSlots.Count; i++)
        {
            // Chỉ bật số lượng slot bằng đúng độ dài của từ (ví dụ SCOTT bật 5 slot, STEPHEN bật 7 slot)
            if (i < upperWord.Length)
            {
                char c = upperWord[i];
                letterSlots[i].gameObject.SetActive(true);

                LetterSpritePair pair = GetSpritePair(c);
                if (pair == null) continue;

                // 1. Nếu nhân vật ĐÃ MỞ KHÓA -> Sáng Neon toàn bộ
                if (isCharacterUnlocked)
                {
                    letterSlots[i].sprite = pair.neonSprite;
                    continue;
                }

                // 2. Nếu nhân vật CHƯA MỞ KHÓA -> So khớp với túi đồ
                int ownedCount = LetterInventoryManager.GetLetterCount(c);
                int usedCount = usedLetterCounts.ContainsKey(c) ? usedLetterCounts[c] : 0;

                if (usedCount < ownedCount)
                {
                    letterSlots[i].sprite = pair.neonSprite; // Trong túi có chữ -> Sáng Neon
                    usedLetterCounts[c] = usedCount + 1;
                }
                else
                {
                    letterSlots[i].sprite = pair.graySprite; // Chưa có chữ trong túi -> Tối màu Xám
                }
            }
            else
            {
                // Tắt các slot thừa ở phía sau
                letterSlots[i].gameObject.SetActive(false);
            }
        }
    }

    public void HideAllSlots()
    {
        for (int i = 0; i < letterSlots.Count; i++)
        {
            if (letterSlots[i] != null)
                letterSlots[i].gameObject.SetActive(false);
        }
    }

    private LetterSpritePair GetSpritePair(char letter)
    {
        return alphabetDatabase.Find(x => char.ToUpper(x.letter) == char.ToUpper(letter));
    }
}