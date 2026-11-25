using System;
using UnityEngine;

/// <summary>
/// Sự kiện nhịp điệu dùng chung giữa các màn (spawner phát, hiệu ứng / cơ chế màn lắng nghe).
/// </summary>
public static class RhythmEvents
{
    /// <summary>
    /// Một nốt / quái / item bị bỏ lỡ tại vị trí <c>position</c> (lane trái = true).
    /// Màn 4 dùng để cho quái vật một đầu phát nổ khi người chơi không đánh kịp.
    /// </summary>
    public static event Action<Vector3, bool> NoteMissed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => NoteMissed = null;

    public static void RaiseNoteMissed(Vector3 position, bool isLeft)
    {
        NoteMissed?.Invoke(position, isLeft);
    }
}
