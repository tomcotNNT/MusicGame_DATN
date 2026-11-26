using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Tools > BeatRun > Animator nhân vật
///
/// 1) Tạo AnimationClip từ sprite sheet đã cắt (Sprite Mode = Multiple), nhập số frame dạng "841-847, 850".
/// 2) Chuẩn hoá Animator Controller để khớp với code gameplay:
///      Bool    IsGround  (PlayerController: đang đứng đất)
///      Bool    Sing      (giữ nốt dài - holdAttackTrigger)
///      Trigger Attack, Attack1, Attack2, Attack3 (đánh nốt)
///      Trigger Jump, Die
///    và nối các trạng thái có sẵn theo tên: Idle, Run, Attack..., Jump/Jumping, Die, Hold.
/// Chạy nhiều lần không bị nhân đôi transition.
/// </summary>
public class CharacterAnimatorTool : EditorWindow
{
    [System.Serializable]
    private class ClipSpec
    {
        public string name = "Jump";
        public string frames = "";
        public float fps = 12f;
        public bool loop = false;
    }

    private const string Root = "Assets/MusicGame/Animation/";

    private static readonly string[] KnownControllers =
    {
        Root + "P1/Player.controller",                 // Ramona
        Root + "P2/Chau.controller",                   // Knives Chau
        Root + "P3/Stephen Stills_50.controller"       // Stephen Stills
    };

    private AnimatorController controller;
    private Texture2D sheet;
    private List<ClipSpec> clips = new List<ClipSpec>
    {
        new ClipSpec { name = "Jump", fps = 12, loop = false },
        new ClipSpec { name = "Die",  fps = 10, loop = false },
        new ClipSpec { name = "Hold", fps = 12, loop = true }
    };
    private Vector2 scroll;

    [MenuItem("Tools/BeatRun/Animator nhân vật")]
    private static void Open() => GetWindow<CharacterAnimatorTool>("Animator nhân vật");

    [MenuItem("Tools/BeatRun/Chuẩn hoá Animator cả 3 nhân vật")]
    private static void StandardizeAllMenu()
    {
        int done = 0;
        foreach (string path in KnownControllers)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null) { Debug.LogWarning("[AnimatorTool] Không thấy " + path); continue; }
            // Ramona (P1) đã có đủ animation và transition riêng -> chỉ bổ sung tham số còn thiếu
            bool paramsOnly = path.Contains("/P1/");
            Debug.Log("[AnimatorTool] " + path + ": " + Standardize(ctrl, paramsOnly));
            done++;
        }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Animator nhân vật", "Đã chuẩn hoá " + done + " controller. Xem chi tiết trong Console.", "OK");
    }

    // ================= TRẠNG THÁI MỚI (bắt beat) =================

    /// <summary>Mọi controller nhân vật đang được scene/prefab dùng (cả bản chạy Man1).</summary>
    private static readonly string[] AllPlayerControllers =
    {
        Root + "P1/Player.controller",
        Root + "P1/Player 1.controller",
        Root + "P2/Chau.controller",
        Root + "P2/Chau_Run.controller",
        Root + "P3/Stephen Stills_50.controller",
        Root + "P3/Stephen_Run.controller",
    };

    /// <summary>Trigger mới. Clip cùng tên phải nằm cạnh controller (P1/P2/P3).</summary>
    private static readonly string[] ExtraStates = { "Hurt", "Dodge", "Special", "AirAttack", "Victory" };

    [MenuItem("Tools/BeatRun/Thêm trạng thái Hurt - Dodge - Special - AirAttack - Victory")]
    private static void AddExtraStatesMenu()
    {
        var report = new List<string>();
        foreach (string path in AllPlayerControllers)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null) { report.Add(path + ": không thấy"); continue; }
            string r = AddExtraStates(ctrl);
            report.Add(Path.GetFileNameWithoutExtension(path) + ": " + r);
            Debug.Log("[AnimatorTool] " + path + ": " + r);
        }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Animator nhân vật", string.Join("\n", report), "OK");
    }

    private static string AddExtraStates(AnimatorController ctrl)
    {
        var log = new List<string>();
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(ctrl)).Replace('\\', '/');
        AnimatorState home = sm.defaultState ?? Find(sm, "Idle");
        if (home == null) return "không có state mặc định - bỏ qua";

        for (int i = 0; i < ExtraStates.Length; i++)
        {
            string name = ExtraStates[i];
            AnimatorState s = Find(sm, name) ?? AddStateFromClip(sm, folder, name, log);
            if (s == null) { log.Add("thiếu clip " + name); continue; }

            // xếp gọn trong cửa sổ Animator
            var cs = sm.states;
            for (int k = 0; k < cs.Length; k++)
                if (cs[k].state == s && cs[k].position == Vector3.zero)
                    cs[k].position = new Vector3(650, 40 + i * 70, 0);
            sm.states = cs;

            EnsureParam(ctrl, name, AnimatorControllerParameterType.Trigger, log);
            bool isVictory = name == "Victory";
            EnsureAnyStateTrigger(sm, s, name, log, canTransitionToSelf: !isVictory);
            if (!isVictory) EnsureExitBack(s, home, log);   // Victory lặp mãi tới khi rời màn
        }

        EditorUtility.SetDirty(ctrl);
        return log.Count == 0 ? "đã đủ, không đổi" : string.Join("; ", log);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "B1: chọn Controller + Sprite Sheet (đã cắt Multiple).\n" +
            "B2: nhập số frame cho từng clip (số ở cuối tên sprite, ví dụ 'Knives Chau_841' -> 841). Dạng: 841-847, 850\n" +
            "B3: bấm 'Tạo clip', rồi 'Chuẩn hoá tham số & trạng thái'.", MessageType.Info);

        controller = (AnimatorController)EditorGUILayout.ObjectField("Controller", controller, typeof(AnimatorController), false);
        sheet = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", sheet, typeof(Texture2D), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Clip cần tạo", EditorStyles.boldLabel);

        for (int i = 0; i < clips.Count; i++)
        {
            ClipSpec c = clips[i];
            EditorGUILayout.BeginHorizontal();
            c.name = EditorGUILayout.TextField(c.name, GUILayout.Width(80));
            c.frames = EditorGUILayout.TextField(c.frames);
            c.fps = EditorGUILayout.FloatField(c.fps, GUILayout.Width(40));
            c.loop = EditorGUILayout.ToggleLeft("Loop", c.loop, GUILayout.Width(50));
            if (GUILayout.Button("-", GUILayout.Width(22))) { clips.RemoveAt(i); i--; }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Thêm clip")) clips.Add(new ClipSpec { name = "Run", fps = 12, loop = true });

        EditorGUILayout.Space();
        GUI.enabled = controller != null && sheet != null;
        if (GUILayout.Button("Tạo clip", GUILayout.Height(26))) CreateClips();
        GUI.enabled = controller != null;
        if (GUILayout.Button("Chuẩn hoá tham số & trạng thái", GUILayout.Height(26)))
        {
            Debug.Log("[AnimatorTool] " + Standardize(controller));
            AssetDatabase.SaveAssets();
        }
        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    // ================= TẠO CLIP =================

    private void CreateClips()
    {
        string sheetPath = AssetDatabase.GetAssetPath(sheet);
        Dictionary<int, Sprite> byIndex = new Dictionary<int, Sprite>();

        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
        {
            if (o is Sprite s)
            {
                int us = s.name.LastIndexOf('_');
                if (us >= 0 && int.TryParse(s.name.Substring(us + 1), out int idx))
                    byIndex[idx] = s;
            }
        }

        string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(controller)).Replace('\\', '/');
        int created = 0;

        foreach (ClipSpec spec in clips)
        {
            List<int> frames = ParseFrames(spec.frames);
            if (string.IsNullOrWhiteSpace(spec.name) || frames.Count == 0) continue;

            List<Sprite> sprites = new List<Sprite>();
            foreach (int f in frames)
            {
                if (byIndex.TryGetValue(f, out Sprite sp)) sprites.Add(sp);
                else Debug.LogWarning("[AnimatorTool] Không có sprite số " + f + " trong " + sheet.name);
            }
            if (sprites.Count == 0) continue;

            AnimationClip clip = BuildClip(sprites, Mathf.Max(1f, spec.fps), spec.loop);
            string path = folder + "/" + spec.name + ".anim";

            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(clip, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(clip, path);
            }

            created++;
            Debug.Log($"[AnimatorTool] Clip {path}: {sprites.Count} frame, {spec.fps} FPS, loop={spec.loop}");
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Animator nhân vật", "Đã tạo/cập nhật " + created + " clip. Bấm 'Chuẩn hoá' để nối vào Controller.", "OK");
    }

    private static AnimationClip BuildClip(List<Sprite> sprites, float fps, bool loop)
    {
        AnimationClip clip = new AnimationClip { frameRate = fps };
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Count + 1];
        for (int i = 0; i < sprites.Count; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        // Giữ frame cuối hết thời lượng của nó
        keys[sprites.Count] = new ObjectReferenceKeyframe { time = sprites.Count / fps, value = sprites[sprites.Count - 1] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    private static List<int> ParseFrames(string text)
    {
        List<int> result = new List<int>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        foreach (string part in text.Split(','))
        {
            string p = part.Trim();
            if (p.Length == 0) continue;

            int dash = p.IndexOf('-');
            if (dash > 0 &&
                int.TryParse(p.Substring(0, dash), out int a) &&
                int.TryParse(p.Substring(dash + 1), out int b))
            {
                int step = a <= b ? 1 : -1;
                for (int i = a; i != b + step; i += step) result.Add(i);
            }
            else if (int.TryParse(p, out int single))
            {
                result.Add(single);
            }
        }
        return result;
    }

    // ================= CHUẨN HOÁ CONTROLLER =================

    private static string Standardize(AnimatorController ctrl, bool paramsOnly = false)
    {
        List<string> log = new List<string>();

        EnsureParam(ctrl, "IsGround", AnimatorControllerParameterType.Bool, log);
        EnsureParam(ctrl, "Sing", AnimatorControllerParameterType.Bool, log);
        foreach (string t in new[] { "Attack", "Attack1", "Attack2", "Attack3", "Jump", "Die" })
            EnsureParam(ctrl, t, AnimatorControllerParameterType.Trigger, log);

        // Mặc định đang đứng đất -> không rơi vào trạng thái nhảy khi vừa vào màn
        AnimatorControllerParameter[] ps = ctrl.parameters;   // getter trả bản sao -> sửa rồi gán lại
        foreach (var p in ps)
            if (p.name == "IsGround") p.defaultBool = true;
        ctrl.parameters = ps;

        if (paramsOnly)
        {
            EditorUtility.SetDirty(ctrl);
            return log.Count == 0 ? "đủ tham số" : string.Join("; ", log);
        }

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(ctrl)).Replace('\\', '/');

        AnimatorState idle = sm.defaultState ?? Find(sm, "Idle");
        if (idle == null) return "Không có trạng thái Idle/mặc định - bỏ qua.";

        // Tạo state cho clip mới (Jump/Die/Hold) nếu clip có trong thư mục controller mà chưa có state
        AnimatorState jump = Find(sm, "Jump", "Jumping") ?? AddStateFromClip(sm, folder, "Jump", log);
        AnimatorState die = Find(sm, "Die") ?? AddStateFromClip(sm, folder, "Die", log);
        AnimatorState hold = Find(sm, "Hold") ?? AddStateFromClip(sm, folder, "Hold", log);

        List<AnimatorState> attacks = sm.states
            .Select(s => s.state)
            .Where(s => s.name.StartsWith("Attack"))
            .OrderBy(s => s.name)
            .ToList();

        // Attack / Attack1..3 -> các state đánh có sẵn (thiếu thì dùng lại state đánh đầu tiên)
        if (attacks.Count > 0)
        {
            string[] triggers = { "Attack", "Attack1", "Attack2", "Attack3" };
            for (int i = 0; i < triggers.Length; i++)
            {
                AnimatorState target = attacks[Mathf.Clamp(i == 0 ? 0 : i - 1, 0, attacks.Count - 1)];
                EnsureAnyStateTrigger(sm, target, triggers[i], log);
            }

            foreach (AnimatorState a in attacks)
                EnsureExitBack(a, idle, log);
        }
        else log.Add("chưa có state Attack");

        if (die != null)
            EnsureAnyStateTrigger(sm, die, "Die", log, canTransitionToSelf: false);

        if (jump != null)
        {
            EnsureBoolTransition(idle, jump, "IsGround", false, log);
            EnsureBoolTransition(jump, idle, "IsGround", true, log);
        }

        if (hold != null)
        {
            EnsureBoolTransition(idle, hold, "Sing", true, log);
            EnsureBoolTransition(hold, idle, "Sing", false, log);
        }

        EditorUtility.SetDirty(ctrl);
        return log.Count == 0 ? "đã chuẩn, không cần đổi" : string.Join("; ", log);
    }

    private static void EnsureParam(AnimatorController c, string name, AnimatorControllerParameterType type, List<string> log)
    {
        if (c.parameters.Any(p => p.name == name)) return;
        c.AddParameter(name, type);
        log.Add("+param " + name);
    }

    private static AnimatorState Find(AnimatorStateMachine sm, params string[] names)
    {
        foreach (var cs in sm.states)
            foreach (string n in names)
                if (string.Equals(cs.state.name, n, System.StringComparison.OrdinalIgnoreCase))
                    return cs.state;
        return null;
    }

    private static AnimatorState AddStateFromClip(AnimatorStateMachine sm, string folder, string clipName, List<string> log)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + "/" + clipName + ".anim");
        if (clip == null) return null;

        AnimatorState s = sm.AddState(clipName);
        s.motion = clip;
        log.Add("+state " + clipName);
        return s;
    }

    private static void EnsureAnyStateTrigger(AnimatorStateMachine sm, AnimatorState target, string trigger, List<string> log, bool canTransitionToSelf = true)
    {
        foreach (var t in sm.anyStateTransitions)
            if (t.conditions.Any(c => c.parameter == trigger))
                return;

        var tr = sm.AddAnyStateTransition(target);
        tr.AddCondition(AnimatorConditionMode.If, 0, trigger);
        tr.hasExitTime = false;
        tr.duration = 0f;
        tr.canTransitionToSelf = canTransitionToSelf;
        log.Add("Any->" + target.name + " (" + trigger + ")");
    }

    private static void EnsureExitBack(AnimatorState from, AnimatorState to, List<string> log)
    {
        if (from == to || from.transitions.Any(t => t.destinationState == to)) return;

        var tr = from.AddTransition(to);
        tr.hasExitTime = true;
        tr.exitTime = 1f;
        tr.duration = 0f;
        log.Add(from.name + "->" + to.name);
    }

    private static void EnsureBoolTransition(AnimatorState from, AnimatorState to, string param, bool value, List<string> log)
    {
        if (from == to) return;
        if (from.transitions.Any(t => t.destinationState == to && t.conditions.Any(c => c.parameter == param)))
            return;

        var tr = from.AddTransition(to);
        tr.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
        tr.hasExitTime = false;
        tr.duration = 0f;
        log.Add(from.name + "->" + to.name + " (" + param + "=" + value + ")");
    }
}
