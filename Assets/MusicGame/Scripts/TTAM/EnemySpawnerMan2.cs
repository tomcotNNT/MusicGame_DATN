// using System.Collections.Generic;
// using UnityEngine;

// /// Spawner enemy riêng, đọc nốt trực tiếp từ MidiReader (Mode Drums):
// ///   note.lane == 0 (kick / bùm)   -> spawnLeft
// ///   note.lane == 1 (snare / tách) -> spawnRight
// /// Chỉ mượn đồng hồ SongTime của PianoKeySpawner để khớp nhạc (countdown, pause, skip, tăng tốc vòng).
// /// Enemy chạm Player đúng lúc nốt vang lên.
// public class EnemySpawnerMan2 : MonoBehaviour
// {
//     [Header("References")]
//     [SerializeField] private MidiReader midiReader;
//     [Tooltip("Bỏ trống = tự dùng PianoKeySpawner.Instance")]
//     [SerializeField] private PianoKeySpawner pianoSpawner;
//     [SerializeField] private Transform player;
//     [SerializeField] private EnemyMan2 enemyPrefab;
//     [SerializeField] private Transform spawnLeft;
//     [SerializeField] private Transform spawnRight;

//     [Header("Settings")]
//     [Tooltip("Enemy bay bao nhiêu giây (thời gian thực) từ chỗ spawn tới Player")]
//     [SerializeField] private float travelTime = 1.5f;
//     [Tooltip("Khoảng cách tối thiểu (giây) giữa 2 enemy cùng bên")]
//     [SerializeField] private float minInterval = 0.15f;
//     [Tooltip("Đảo bên: lane 0 ra phải, lane 1 ra trái")]
//     [SerializeField] private bool swapSides = false;

//     [Header("Debug")]
//     [SerializeField] private bool debugLog = true;

//     private List<MidiReader.RhythmNote> notes;
//     private readonly List<EnemyMan2> alive = new List<EnemyMan2>();
//     private readonly float[] lastTimeOnSide = { -999f, -999f };
//     private int nextIndex;
//     private int lastRound = -1;
//     private float lastTime;
//     private float nextDebugTime;
//     private int spawnedCount;
//     private bool warnedSpawn;

//     private void Start()
//     {
//         if (midiReader == null)  Debug.LogError("EnemySpawnerMan2: chưa gán Midi Reader!", this);
//         if (player == null)      Debug.LogError("EnemySpawnerMan2: chưa gán Player!", this);
//         if (enemyPrefab == null) Debug.LogError("EnemySpawnerMan2: chưa gán Enemy Prefab!", this);
//         if (spawnLeft == null)   Debug.LogError("EnemySpawnerMan2: chưa gán Spawn Left!", this);
//         if (spawnRight == null)  Debug.LogError("EnemySpawnerMan2: chưa gán Spawn Right!", this);
//     }

//     private void Update()
//     {
//         PianoKeySpawner ps = pianoSpawner != null ? pianoSpawner : PianoKeySpawner.Instance;

//         if (debugLog && Time.unscaledTime >= nextDebugTime)
//         {
//             nextDebugTime = Time.unscaledTime + 2f;
//             Debug.Log($"[EnemySpawnerMan2] ps={(ps != null)} playing={(ps != null && ps.IsPlaying)} " +
//                       $"songTime={(ps != null ? ps.SongTime : -1f):F2} round={(ps != null ? ps.Round : -1)} " +
//                       $"notes={(notes != null ? notes.Count : -1)} nextIndex={nextIndex} spawned={spawnedCount}", this);
//         }

//         if (midiReader == null || ps == null || ps.IsTutorial || !ps.IsPlaying)
//             return;

//         float now = ps.SongTime;

//         // Lấy note lần đầu, khi đổi vòng, hoặc khi bài bắt đầu lại (SongTime nhảy lùi)
//         if (notes == null || notes.Count == 0 || ps.Round != lastRound || now < lastTime - 0.5f)
//         {
//             Rebuild(ps.RoundSpeed);
//             lastRound = ps.Round;
//         }
//         lastTime = now;

//         if (notes.Count == 0)
//             return;

//         alive.RemoveAll(e => e == null);

//         while (nextIndex < notes.Count && notes[nextIndex].time - travelTime <= now)
//         {
//             MidiReader.RhythmNote note = notes[nextIndex++];

//             if (note.time <= now)
//                 continue;   // nốt đã qua

//             bool left = (note.lane == 0) != swapSides;
//             int side = left ? 0 : 1;

//             if (note.time - lastTimeOnSide[side] < minInterval)
//                 continue;
//             lastTimeOnSide[side] = note.time;

//             Spawn(ps, note.time, now, left);
//         }
//     }

//     private void Rebuild(float roundSpeed)
//     {
//         for (int i = alive.Count - 1; i >= 0; i--)
//             if (alive[i] != null) Destroy(alive[i].gameObject);
//         alive.Clear();

//         // Cùng cách tính với PianoKeySpawner: thời gian chia cho tốc độ vòng
//         notes = new List<MidiReader.RhythmNote>(midiReader.GetNotesWithSpeed(roundSpeed));
//         notes.Sort((a, b) => a.time.CompareTo(b.time));

//         nextIndex = 0;
//         lastTimeOnSide[0] = lastTimeOnSide[1] = -999f;

//         if (debugLog)
//             Debug.Log($"[EnemySpawnerMan2] Rebuild: {notes.Count} note, x{roundSpeed:F2}", this);
//     }

//     private void Spawn(PianoKeySpawner ps, float hitTime, float now, bool left)
//     {
//         Transform point = left ? spawnLeft : spawnRight;
//         if (point == null || enemyPrefab == null || player == null)
//         {
//             if (!warnedSpawn)
//             {
//                 warnedSpawn = true;
//                 Debug.LogError("EnemySpawnerMan2: thiếu Spawn Left/Right, Enemy Prefab hoặc Player nên không spawn được!", this);
//             }
//             return;
//         }

//         EnemyMan2 enemy = Instantiate(enemyPrefab, point.position, Quaternion.identity);
//         enemy.Init(ps, player, now, hitTime);

//         // PianoKeySpawner sẽ dọn enemy khi hết vòng / EndSong
//         ps.RegisterActiveKey(enemy.gameObject);

//         alive.Add(enemy);
//         spawnedCount++;
//     }
// }