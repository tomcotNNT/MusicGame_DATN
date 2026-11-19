// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class NTMan3MidiBridge : MonoBehaviour
// {
//     [Header("1. Chế Độ Nguồn Dữ Liệu")]
//     [Tooltip("Tích = Chạy chuỗi nốt mẫu | Bỏ tích = Lấy dữ liệu từ NTMidiReader")]
//     [SerializeField] private bool useDummyTestChart = false;

//     [Header("2. Tham Chiếu Tới Hệ Thống Mới")]
//     [SerializeField] private NTMidiReader ntMidiReader; 
//     [SerializeField] private AudioSource midiAudioSource;
//     [SerializeField] private NTMan3Director man3Director;

//     [Header("3. Thông Số Quy Đổi Sang Gameplay Màn 3")]
//     [SerializeField] private float defaultFlightDuration = 1.25f;
//     [SerializeField] private float startMusicDelay = 1.2f;
//     [SerializeField] private float audioOffset = 0f;

//     [Header("4. Tự Động Sinh Buff, Bom & Chuỗi Xu (Fever Coins)")]
//     [SerializeField] private bool autoInjectBuffAndBomb = true;
//     [SerializeField] private int buffEveryNNotes = 16;
//     [SerializeField] private int bombEveryNNotes = 12;

//     [Header("5. Cấu Hình Random Đa Dạng Hóa 6 Nốt Đại Dương & Item")]
//     [SerializeField] private bool enableRandomOceanNotes = true;
//     [Range(0f, 1f)][SerializeField] private float holdChance = 0.10f;   // 10% Nốt giữ (Hold)
//     [Range(0f, 1f)][SerializeField] private float doubleChance = 0.08f;  // 8% Nốt đôi
//     [Range(0f, 1f)][SerializeField] private float bombChance = 0.07f;    // 7% Bom
//     [Range(0f, 1f)][SerializeField] private float coinChance = 0.15f;    // 15% Xu
//     [Range(0f, 1f)][SerializeField] private float buffChance = 0.05f;    // 5% Buff

//     [Space(5)]
//     [SerializeField] private bool autoInjectFeverCoins = true;
//     [SerializeField] private float minGapForCoins = 2.0f;
//     [SerializeField] private float coinSafeBuffer = 0.65f;
//     [SerializeField] private int minCoinsPerChain = 3;
//     [SerializeField] private int maxCoinsPerChain = 5;

//     private void Start()
//     {
//         if (man3Director == null) man3Director = GetComponent<NTMan3Director>();
        
//         if (ntMidiReader == null) ntMidiReader = FindFirstObjectByType<NTMidiReader>();

//         if (!useDummyTestChart)
//         {
//             StartCoroutine(LoadFromMidiSystemRoutine());
//         }
//     }

//     private IEnumerator LoadFromMidiSystemRoutine()
//     {
//         yield return null;
//         yield return null;

//         if (ntMidiReader == null || man3Director == null)
//         {
//             Debug.LogError("[NTMan3MidiBridge] Thiếu tham chiếu NTMidiReader hoặc NTMan3Director!");
//             yield break;
//         }

//         List<NoteDataMan3> convertedNotes = ConvertMidiReaderToMan3Chart();

//         if (convertedNotes.Count == 0)
//         {
//             Debug.LogWarning("[NTMan3MidiBridge] NTMidiReader chưa có dữ liệu nốt. Kiểm tra lại NTLevelData!");
//             yield break;
//         }

//         if (startMusicDelay > 0f)
//         {
//             yield return new WaitForSeconds(startMusicDelay);
//         }

//         if (midiAudioSource != null && midiAudioSource.clip != null)
//         {
//             midiAudioSource.Play();
//         }

//         man3Director.LoadExternalChart(convertedNotes, null, midiAudioSource, audioOffset);
//     }

//     private List<NoteDataMan3> ConvertMidiReaderToMan3Chart()
//     {
//         List<NoteDataMan3> result = new List<NoteDataMan3>();

//         if (ntMidiReader == null || ntMidiReader.Notes == null || ntMidiReader.Notes.Count == 0)
//         {
//             Debug.LogWarning("[NTMan3MidiBridge] NTMidiReader chưa có dữ liệu nốt!");
//             return result;
//         }

//         foreach (var n in ntMidiReader.Notes)
//         {
//             bool randomLeftLane = Random.value > 0.5f;
//             Man3NoteType type = Man3NoteType.Normal;
//             float holdDur = 0f;

//             if (n.duration > 0.25f)
//             {
//                 type = Man3NoteType.Hold;
//                 holdDur = n.duration;
//             }
//             else if (enableRandomOceanNotes)
//             {
//                 float rand = Random.value;

//                 if (rand < holdChance)
//                 {
//                     type = Man3NoteType.Hold;
//                     holdDur = 0.5f;
//                 }
//                 else if (rand < holdChance + doubleChance)
//                 {
//                     type = Man3NoteType.Double;
//                 }
//                 else if (rand < holdChance + doubleChance + bombChance)
//                 {
//                     type = Man3NoteType.Bomb;
//                 }
//                 else if (rand < holdChance + doubleChance + bombChance + coinChance)
//                 {
//                     type = Man3NoteType.Coin;
//                 }
//                 else if (rand < holdChance + doubleChance + bombChance + coinChance + buffChance)
//                 {
//                     type = Man3NoteType.Buff;
//                 }
//                 else
//                 {
//                     int oceanIndex = Random.Range(0, 6);
//                     switch (oceanIndex)
//                     {
//                         case 0: type = Man3NoteType.Note1; break;
//                         case 1: type = Man3NoteType.NoteMan1; break;
//                         case 2: type = Man3NoteType.NoteMan2; break;
//                         case 3: type = Man3NoteType.NoteMan3_1; break;
//                         case 4: type = Man3NoteType.NoteMan3_2; break;
//                         case 5: type = Man3NoteType.NoteMan3_3; break;
//                     }
//                 }
//             }

//             result.Add(new NoteDataMan3
//             {
//                 isLeftLane = randomLeftLane,
//                 noteType = type,
//                 hitTime = n.time,
//                 holdDuration = holdDur,
//                 flightDuration = defaultFlightDuration
//             });
//         }

//         result.Sort((a, b) => a.hitTime.CompareTo(b.hitTime));
//         return result;
//     }
// }