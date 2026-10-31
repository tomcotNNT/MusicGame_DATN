using UnityEngine;

[CreateAssetMenu(fileName = "NewCharData", menuName = "BeatRun/Character Data")]
public class CharacterDataSO : ScriptableObject
{
    [Header("1. Thông tin chung")]
    public string characterID = "Scott";
    public string displayName = "SCOTT";
    public bool isDefaultUnlocked = false; // Ramona tích chọn, các nhân vật khác bỏ tích
    public GameObject characterModelPrefab; // Model đứng trên cầu

    [Header("2. Điều kiện mở khóa = Tiền tệ")]
    public int unlockPriceDiamond = 999;
    public int unlockPriceCoin = 0;

    [Header("3. Điều kiện mở khóa = Ghép Chữ Cái")]
    public string requiredWord = "SCOTT"; // Từ khóa cần ghép đủ
}