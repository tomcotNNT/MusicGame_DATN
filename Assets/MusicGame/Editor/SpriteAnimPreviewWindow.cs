using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Xem thử clip animation sprite ngay trong Editor (không cần Play):
/// phát đúng thời gian key của clip, bước từng frame, onion skin, đường pivot/đáy,
/// và bảng kiểm tra từng frame (kích thước, pivot, độ lệch đáy so với frame 0, thời lượng).
/// Chọn 1 AnimationClip, 1 Animator Controller hoặc 1 GameObject có Animator trong Project/Hierarchy.
/// </summary>
public class SpriteAnimPreviewWindow : EditorWindow
{
    [MenuItem("Tools/BeatRun/Xem thử Animation Sprite")]
    static void Open() => GetWindow<SpriteAnimPreviewWindow>("Sprite Anim Preview");

    struct Frame { public Sprite sprite; public float time; public float duration; }

    Object source;
    AnimationClip[] clips = new AnimationClip[0];
    int clipIndex;
    readonly List<Frame> frames = new List<Frame>();
    float length;
    bool loop;

    bool playing = true;
    float t;
    double lastTick;
    float speed = 1f;
    int zoom = 3;
    bool onion = true, ghostFirst, showPivot = true, showBounds;
    int warnPx = 2;
    Vector2 scroll;

    void OnEnable()
    {
        EditorApplication.update += Tick;
        lastTick = EditorApplication.timeSinceStartup;
        if (source == null) OnSelectionChange();
    }

    void OnDisable() => EditorApplication.update -= Tick;

    void OnSelectionChange()
    {
        var o = Selection.activeObject;
        if (o is AnimationClip || o is RuntimeAnimatorController ||
            (o is GameObject g && g.GetComponentInChildren<Animator>() != null))
        {
            SetSource(o);
            Repaint();
        }
    }

    void SetSource(Object o)
    {
        source = o;
        string prev = Current != null ? Current.name : null;
        clips = new AnimationClip[0];
        if (o is AnimationClip c) clips = new[] { c };
        else if (o is RuntimeAnimatorController rc) clips = rc.animationClips;
        else if (o is GameObject g)
        {
            var an = g.GetComponentInChildren<Animator>();
            if (an != null && an.runtimeAnimatorController != null) clips = an.runtimeAnimatorController.animationClips;
        }
        clips = clips.Where(x => x != null).Distinct().OrderBy(x => x.name).ToArray();
        clipIndex = Mathf.Max(0, System.Array.FindIndex(clips, x => x.name == prev));
        LoadClip();
    }

    AnimationClip Current => clips.Length > 0 && clipIndex < clips.Length ? clips[clipIndex] : null;

    void LoadClip()
    {
        frames.Clear();
        t = 0;
        var clip = Current;
        if (clip == null) return;
        var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).FirstOrDefault(b => b.propertyName == "m_Sprite");
        var keys = binding.propertyName == null ? new ObjectReferenceKeyframe[0] : AnimationUtility.GetObjectReferenceCurve(clip, binding);
        length = clip.length;
        loop = AnimationUtility.GetAnimationClipSettings(clip).loopTime;
        for (int i = 0; i < keys.Length; i++)
        {
            float end = i + 1 < keys.Length ? keys[i + 1].time : Mathf.Max(length, keys[i].time + 1f / Mathf.Max(1f, clip.frameRate));
            frames.Add(new Frame { sprite = keys[i].value as Sprite, time = keys[i].time, duration = end - keys[i].time });
        }
        if (frames.Count > 0) length = Mathf.Max(length, frames[frames.Count - 1].time + frames[frames.Count - 1].duration);
    }

    void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        float dt = (float)(now - lastTick);
        lastTick = now;
        if (!playing || frames.Count == 0 || length <= 0) return;
        t += dt * speed;
        if (t >= length) t = loop ? t % length : 0f;   // clip không lặp: phát lại từ đầu để dễ xem
        Repaint();
    }

    int FrameAt(float time)
    {
        int idx = 0;
        for (int i = 0; i < frames.Count; i++) if (frames[i].time <= time + 1e-5f) idx = i;
        return idx;
    }

    void Step(int d)
    {
        if (frames.Count == 0) return;
        playing = false;
        int i = (FrameAt(t) + d + frames.Count) % frames.Count;
        t = frames[i].time;
        Repaint();
    }

    void OnGUI()
    {
        HandleKeys();

        EditorGUI.BeginChangeCheck();
        var o = EditorGUILayout.ObjectField("Clip / Controller / Object", source, typeof(Object), true);
        if (EditorGUI.EndChangeCheck()) SetSource(o);

        if (clips.Length == 0)
        {
            EditorGUILayout.HelpBox("Chọn 1 AnimationClip, 1 Animator Controller hoặc 1 GameObject có Animator.", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUI.BeginChangeCheck();
            clipIndex = EditorGUILayout.Popup(clipIndex, clips.Select(c => c.name).ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(180));
            if (EditorGUI.EndChangeCheck()) LoadClip();
            if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(26))) Step(-1);
            if (GUILayout.Button(playing ? "❚❚" : "▶", EditorStyles.toolbarButton, GUILayout.Width(30))) playing = !playing;
            if (GUILayout.Button("▶|", EditorStyles.toolbarButton, GUILayout.Width(28))) Step(1);
            GUILayout.Label("Tốc độ", GUILayout.Width(42));
            speed = GUILayout.HorizontalSlider(speed, 0.1f, 2f, GUILayout.Width(80));
            GUILayout.Label(speed.ToString("0.0") + "x", GUILayout.Width(32));
            GUILayout.Label("Zoom", GUILayout.Width(36));
            zoom = Mathf.RoundToInt(GUILayout.HorizontalSlider(zoom, 1, 8, GUILayout.Width(70)));
            GUILayout.FlexibleSpace();
        }
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            onion = GUILayout.Toggle(onion, "Onion (frame trước)", EditorStyles.toolbarButton);
            ghostFirst = GUILayout.Toggle(ghostFirst, "Bóng frame 0", EditorStyles.toolbarButton);
            showPivot = GUILayout.Toggle(showPivot, "Pivot / đáy", EditorStyles.toolbarButton);
            showBounds = GUILayout.Toggle(showBounds, "Khung sprite", EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Cảnh báo lệch >", GUILayout.Width(95));
            warnPx = EditorGUILayout.IntField(warnPx, GUILayout.Width(28));
            GUILayout.Label("px", GUILayout.Width(18));
        }

        if (frames.Count == 0)
        {
            EditorGUILayout.HelpBox("Clip '" + Current.name + "' không có key sprite (m_Sprite).", MessageType.Warning);
            return;
        }

        int cur = FrameAt(t);
        var clip = Current;
        EditorGUILayout.LabelField(string.Format("{0}  |  frame {1}/{2}  |  {3:0.000}s / {4:0.000}s  |  {5} fps  |  {6}",
            clip.name, cur, frames.Count - 1, t, length, clip.frameRate, loop ? "Loop" : "Không lặp"), EditorStyles.miniBoldLabel);

        Rect area = GUILayoutUtility.GetRect(10, 10000, 220, 10000, GUILayout.ExpandHeight(true));
        DrawPreview(area, cur);
        DrawTable(cur);
    }

    void HandleKeys()
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown) return;
        if (e.keyCode == KeyCode.Comma || e.keyCode == KeyCode.LeftArrow) { Step(-1); e.Use(); }
        else if (e.keyCode == KeyCode.Period || e.keyCode == KeyCode.RightArrow) { Step(1); e.Use(); }
        else if (e.keyCode == KeyCode.Space) { playing = !playing; e.Use(); }
    }

    void DrawPreview(Rect area, int cur)
    {
        EditorGUI.DrawRect(area, new Color(0.13f, 0.13f, 0.16f));
        // điểm pivot: giữa ngang, 70% chiều cao (đa số sprite nhân vật có pivot ở chân)
        Vector2 origin = new Vector2(Mathf.Round(area.center.x), Mathf.Round(area.y + area.height * 0.7f));

        if (ghostFirst && cur != 0) DrawSprite(frames[0].sprite, origin, new Color(0.4f, 0.8f, 1f, 0.35f));
        if (onion && frames.Count > 1)
        {
            int prev = cur - 1 < 0 ? (loop ? frames.Count - 1 : -1) : cur - 1;
            if (prev >= 0) DrawSprite(frames[prev].sprite, origin, new Color(1f, 0.45f, 0.45f, 0.35f));
        }
        DrawSprite(frames[cur].sprite, origin, Color.white);

        if (showPivot)
        {
            EditorGUI.DrawRect(new Rect(area.x, origin.y, area.width, 1), new Color(0.3f, 1f, 0.5f, 0.8f));
            EditorGUI.DrawRect(new Rect(origin.x, area.y, 1, area.height), new Color(0.3f, 1f, 0.5f, 0.35f));
            var s = frames[cur].sprite;
            if (s != null)
            {
                float bottom = origin.y - s.bounds.min.y * s.pixelsPerUnit * zoom;
                EditorGUI.DrawRect(new Rect(area.x, bottom, area.width, 1), new Color(1f, 0.85f, 0.2f, 0.8f));
            }
            GUI.Label(new Rect(area.x + 4, area.y + 2, 400, 18), "xanh = pivot   vàng = đáy hình (mesh Tight)   đỏ mờ = frame trước   lam mờ = frame 0", EditorStyles.miniLabel);
        }
    }

    void DrawSprite(Sprite s, Vector2 origin, Color tint)
    {
        if (s == null || s.texture == null) return;
        Rect r = s.rect;
        Vector2 p = s.pivot;   // pixel, tính từ góc trái-dưới của rect
        var dst = new Rect(origin.x - p.x * zoom, origin.y - (r.height - p.y) * zoom, r.width * zoom, r.height * zoom);
        var tex = s.texture;
        var uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
        var old = GUI.color;
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(dst, tex, uv);
        GUI.color = old;
        if (showBounds && tint == Color.white)
        {
            var c = new Color(1f, 1f, 1f, 0.3f);
            EditorGUI.DrawRect(new Rect(dst.x, dst.y, dst.width, 1), c);
            EditorGUI.DrawRect(new Rect(dst.x, dst.yMax, dst.width, 1), c);
            EditorGUI.DrawRect(new Rect(dst.x, dst.y, 1, dst.height), c);
            EditorGUI.DrawRect(new Rect(dst.xMax, dst.y, 1, dst.height), c);
        }
    }

    void DrawTable(int cur)
    {
        var f0 = frames[0].sprite;
        float b0 = f0 != null ? f0.bounds.min.y * f0.pixelsPerUnit : 0;
        float c0 = f0 != null ? f0.bounds.center.x * f0.pixelsPerUnit : 0;
        float d0 = frames[0].duration;
        var warnings = new List<string>();

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(Mathf.Min(24 + frames.Count * 18, 220)));
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (var (h, w) in new[] { ("#", 24), ("Sprite", 170), ("Lúc (s)", 55), ("Dài (ms)", 60), ("Cỡ (px)", 70), ("Pivot (px)", 80), ("Đáy Δ", 50), ("Tâm X Δ", 55) })
                GUILayout.Label(h, EditorStyles.miniBoldLabel, GUILayout.Width(w));
        }
        for (int i = 0; i < frames.Count; i++)
        {
            var f = frames[i];
            var s = f.sprite;
            float db = s != null ? s.bounds.min.y * s.pixelsPerUnit - b0 : 0;
            float dc = s != null ? s.bounds.center.x * s.pixelsPerUnit - c0 : 0;
            bool bad = s == null || Mathf.Abs(db) > warnPx;
            if (s == null) warnings.Add("Frame " + i + " thiếu sprite.");
            else if (Mathf.Abs(db) > warnPx) warnings.Add(string.Format("Frame {0} ({1}): đáy lệch {2:+0;-0} px so với frame 0 → nhân vật sẽ nhún/giật (bỏ qua nếu là clip Jump).", i, s.name, db));
            if (i > 0 && s != null && s == frames[i - 1].sprite) warnings.Add("Frame " + i + " trùng sprite với frame trước → khựng 1 nhịp.");
            if (Mathf.Abs(f.duration - d0) > 0.002f && i < frames.Count - 1) warnings.Add(string.Format("Frame {0} dài {1:0} ms, khác frame 0 ({2:0} ms).", i, f.duration * 1000, d0 * 1000));

            var style = new GUIStyle(EditorStyles.miniLabel);
            if (bad) style.normal.textColor = new Color(1f, 0.5f, 0.4f);
            if (i == cur) style.fontStyle = FontStyle.Bold;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(i.ToString(), EditorStyles.miniButton, GUILayout.Width(24))) { playing = false; t = f.time; }
                GUILayout.Label(s != null ? s.name : "(null)", style, GUILayout.Width(170));
                GUILayout.Label(f.time.ToString("0.000"), style, GUILayout.Width(55));
                GUILayout.Label((f.duration * 1000).ToString("0"), style, GUILayout.Width(60));
                GUILayout.Label(s != null ? s.rect.width + "×" + s.rect.height : "-", style, GUILayout.Width(70));
                GUILayout.Label(s != null ? string.Format("{0:0.#}, {1:0.#}", s.pivot.x, s.pivot.y) : "-", style, GUILayout.Width(80));
                GUILayout.Label(db.ToString("+0;-0;0"), style, GUILayout.Width(50));
                GUILayout.Label(dc.ToString("+0;-0;0"), style, GUILayout.Width(55));
            }
        }
        EditorGUILayout.EndScrollView();

        if (warnings.Count == 0)
            EditorGUILayout.HelpBox("Không thấy lệch đáy, frame trùng hay nhịp không đều. Vẫn nên xem bằng mắt ở tốc độ 0.25x.", MessageType.Info);
        else
            EditorGUILayout.HelpBox(string.Join("\n", warnings.Take(8)) + (warnings.Count > 8 ? "\n… và " + (warnings.Count - 8) + " cảnh báo khác" : ""), MessageType.Warning);
        EditorGUILayout.LabelField("Phím tắt: Space = phát/dừng,  ← / , = frame trước,  → / . = frame sau (bấm vào cửa sổ trước).", EditorStyles.miniLabel);
    }
}
