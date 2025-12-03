using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class GojoHitboxBaker : EditorWindow
{
    private TextAsset airFile;
    private string animationFolder = "Assets/Gojo/Animations";
    private string hitboxChildName = "MeleeHitbox"; 
    
    // Default PPU 32
    private float pixelsPerUnit = 32f; 

    [MenuItem("Tools/Gojo Hitbox Baker")]
    public static void ShowWindow()
    {
        GetWindow<GojoHitboxBaker>("Hitbox Baker");
    }

    void OnGUI()
    {
        GUILayout.Label("MUGEN Hitbox Baker", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        airFile = (TextAsset)EditorGUILayout.ObjectField("Anim File (anim.txt)", airFile, typeof(TextAsset), false);
        animationFolder = EditorGUILayout.TextField("Anim Clip Folder", animationFolder);
        hitboxChildName = EditorGUILayout.TextField("Hitbox Child Name", hitboxChildName);

        EditorGUILayout.Space();
        GUILayout.Label("Scaling Settings", EditorStyles.boldLabel);
        
        pixelsPerUnit = EditorGUILayout.FloatField("Pixels Per Unit", pixelsPerUnit);
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Bake Hitboxes", GUILayout.Height(40)))
        {
            BakeHitboxes();
        }
    }

    void BakeHitboxes()
    {
        if (airFile == null) { Debug.LogError("Assign anim.txt!"); return; }

        string text = airFile.text;
        var actions = ParseMugenFile(text);

        int count = 0;

        foreach (var kvp in actions)
        {
            int actionID = kvp.Key;
            List<FrameData> frames = kvp.Value;

            string clipPath = $"{animationFolder}/Action_{actionID}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

            if (clip == null) continue; 

            ApplyHitboxCurves(clip, frames);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=cyan>Hitbox Bake Complete:</color> Processed {count} clips.");
    }

    private void ApplyHitboxCurves(AnimationClip clip, List<FrameData> frames)
    {
        AnimationCurve cEnabled = new AnimationCurve();
        AnimationCurve cSizeX = new AnimationCurve();
        AnimationCurve cSizeY = new AnimationCurve();
        AnimationCurve cOffsetX = new AnimationCurve();
        AnimationCurve cOffsetY = new AnimationCurve();

        float time = 0f;

        foreach(var f in frames)
        {
            bool active = f.Hitbox.HasValue;
            
            AddKey(cEnabled, time, active ? 1f : 0f);

            if (active)
            {
                Rect r = f.Hitbox.Value;
                
                // --- COORDINATE FIX ---
                // MUGEN Rect: x, y is Top-Left. width, height is size.
                // But Y axis is INVERTED in MUGEN (Positive Y is Down).
                
                float w = r.width / pixelsPerUnit;
                float h = r.height / pixelsPerUnit;
                
                // Calculate Center in Unity Coords
                // Unity X = MUGEN X + (Width/2)
                float cx = (r.x + (r.width / 2f)) / pixelsPerUnit;
                
                // Unity Y = - (MUGEN Y + (Height/2))
                // We negate the whole thing because MUGEN Y is "down" from the axis, 
                // but Unity Y is "up".
                // Example: MUGEN Box at Y=-50 with Height=20. Center is -40. Unity needs +40 offset.
                // Wait, MUGEN Air file coords:
                // Clsn1[0] = x1, y1, x2, y2.
                // Usually y1 is "Top" (Smaller/More negative) and y2 is "Bottom" (Larger/Less negative).
                // Let's assume standard axes: Feet at 0. Head at -100.
                // If Box is -80 to -60. Center is -70.
                // Unity sprite pivot is usually Bottom Center.
                // So Unity Y Offset should be +70 / PPU.
                // So we need: NEGATIVE ( MUGEN_Center_Y )
                
                float mugenCenterY = r.y + (r.height / 2f);
                float cy = -mugenCenterY / pixelsPerUnit;

                AddKey(cSizeX, time, w);
                AddKey(cSizeY, time, h);
                AddKey(cOffsetX, time, cx);
                AddKey(cOffsetY, time, cy);
            }
            
            time += f.Duration;
            
            AddKey(cEnabled, time, active ? 1f : 0f);
            if(active)
            {
                Rect r = f.Hitbox.Value;
                float w = r.width / pixelsPerUnit;
                float h = r.height / pixelsPerUnit;
                float cx = (r.x + (r.width / 2f)) / pixelsPerUnit;
                float mugenCenterY = r.y + (r.height / 2f);
                float cy = -mugenCenterY / pixelsPerUnit;
                
                AddKey(cSizeX, time, w);
                AddKey(cSizeY, time, h);
                AddKey(cOffsetX, time, cx);
                AddKey(cOffsetY, time, cy);
            }
        }

        string path = hitboxChildName;
        
        clip.SetCurve(path, typeof(BoxCollider2D), "m_Enabled", cEnabled);
        clip.SetCurve(path, typeof(BoxCollider2D), "m_Size.x", cSizeX);
        clip.SetCurve(path, typeof(BoxCollider2D), "m_Size.y", cSizeY);
        clip.SetCurve(path, typeof(BoxCollider2D), "m_Offset.x", cOffsetX);
        clip.SetCurve(path, typeof(BoxCollider2D), "m_Offset.y", cOffsetY);
    }

    private void AddKey(AnimationCurve curve, float time, float val)
    {
        Keyframe k = new Keyframe(time, val);
        k.inTangent = float.PositiveInfinity;
        k.outTangent = float.PositiveInfinity;
        curve.AddKey(k);
    }

    class FrameData
    {
        public Rect? Hitbox; 
        public float Duration;
    }

    private Dictionary<int, List<FrameData>> ParseMugenFile(string content)
    {
        var result = new Dictionary<int, List<FrameData>>();
        string[] lines = content.Split('\n');
        
        int currentAction = -1;
        List<FrameData> currentFrames = new List<FrameData>();
        
        Regex actionRegex = new Regex(@"^\s*\[Begin Action\s+(\d+)\]", RegexOptions.IgnoreCase);
        Regex frameLineRegex = new Regex(@"^\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)");
        Regex clsnRegex = new Regex(@"Clsn1\[0\]\s*=\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+)");
        
        Rect? currentClsn = null;

        for(int i=0; i<lines.Length; i++)
        {
            string line = lines[i].Trim();
            if(string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;

            Match am = actionRegex.Match(line);
            if(am.Success)
            {
                if(currentAction != -1) result[currentAction] = new List<FrameData>(currentFrames);
                currentAction = int.Parse(am.Groups[1].Value);
                currentFrames.Clear();
                currentClsn = null;
                continue;
            }

            Match cm = clsnRegex.Match(line);
            if(cm.Success)
            {
                int x1 = int.Parse(cm.Groups[1].Value);
                int y1 = int.Parse(cm.Groups[2].Value);
                int x2 = int.Parse(cm.Groups[3].Value);
                int y2 = int.Parse(cm.Groups[4].Value);
                
                // MUGEN format: x1, y1 (Top Left) to x2, y2 (Bottom Right)
                // BUT y1 is usually smaller (more negative) than y2 in MUGEN air files.
                // We need strict Min/Max to get width/height positive.
                
                float x = Mathf.Min(x1, x2);
                float y = Mathf.Min(y1, y2);
                float width = Mathf.Abs(x2 - x1);
                float height = Mathf.Abs(y2 - y1);

                currentClsn = new Rect(x, y, width, height);
            }

            Match fm = frameLineRegex.Match(line);
            if(fm.Success)
            {
                int duration = int.Parse(fm.Groups[5].Value);
                
                FrameData frame = new FrameData();
                frame.Duration = (duration == -1 ? 1 : duration) / 60f; 
                frame.Hitbox = currentClsn; 
                
                currentFrames.Add(frame);
                currentClsn = null; 
            }
        }
        if(currentAction != -1) result[currentAction] = new List<FrameData>(currentFrames);
        
        return result;

        //comment
    }
}