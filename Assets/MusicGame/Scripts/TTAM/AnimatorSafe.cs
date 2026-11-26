using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Set tham số Animator mà không báo lỗi khi controller không có tham số đó.
/// Mỗi nhân vật (Ramona, Chau, Stephen) có bộ animation khác nhau: nhân vật thiếu
/// animation nào thì lệnh đó bị bỏ qua thay vì spam cảnh báo mỗi frame.
/// </summary>
public static class AnimatorSafe
{
    private static readonly Dictionary<RuntimeAnimatorController, HashSet<string>> cache =
        new Dictionary<RuntimeAnimatorController, HashSet<string>>();

    // Tắt Domain Reload: xoá cache mỗi lần Play (controller có thể vừa được sửa bằng tool)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => cache.Clear();

    public static bool Has(Animator animator, string param)
    {
        if (animator == null || string.IsNullOrEmpty(param) || animator.runtimeAnimatorController == null)
            return false;

        RuntimeAnimatorController ctrl = animator.runtimeAnimatorController;

        if (!cache.TryGetValue(ctrl, out HashSet<string> names))
        {
            names = new HashSet<string>();
            foreach (AnimatorControllerParameter p in animator.parameters)
                names.Add(p.name);
            cache[ctrl] = names;
        }

        return names.Contains(param);
    }

    public static void SetTrigger(Animator animator, string param)
    {
        if (Has(animator, param)) animator.SetTrigger(param);
    }

    public static void SetBool(Animator animator, string param, bool value)
    {
        if (Has(animator, param)) animator.SetBool(param, value);
    }
}
