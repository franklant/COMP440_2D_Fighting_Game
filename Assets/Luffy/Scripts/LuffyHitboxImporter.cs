using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

public class LuffyHitboxImporter : EditorWindow
{
    // UI Variables
    private TextAsset airFile;
    private string spriteFolderPath = "Assets/Luffy/Sprites";
    private string animationOutputPath = "Assets/Luffy/Animations";
    private string spriteNamingFormat = "{0}-{1}"; 
    private float pixelsPerUnit = 100f;

    // --- Data Structures ---
    private class MugenAction
    {
        public int ActionID;
        public List<MugenFrame> Frames = new List<MugenFrame>();
    }

    private class MugenFrame
    {
        public int Group;
        public int Index;
        public int Duration;
        public bool FlipH; 
        
        public Rect? Hurtbox; // Clsn2
        public Rect? Hitbox;  // Clsn1
    }

    [MenuItem("Tools/Luffy Hitbox Importer (Fixed)")]
    public static void ShowWindow()
    {
        GetWindow<LuffyHitboxImporter>("Hitbox Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Luffy Importer (Sprites + Hitboxes + Flips)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Requirement: Child objects 'Hurtbox' and 'Hitbox' with BoxCollider2D.", MessageType.Info);
        
        airFile = (TextAsset)EditorGUILayout.ObjectField("AIR File", airFile, typeof(TextAsset), false);

        spriteFolderPath = EditorGUILayout.TextField("Sprite Folder", spriteFolderPath);
        animationOutputPath = EditorGUILayout.TextField("Output Folder", animationOutputPath);
        spriteNamingFormat = EditorGUILayout.TextField("Sprite Format", spriteNamingFormat);
        pixelsPerUnit = EditorGUILayout.FloatField("Pixels Per Unit", pixelsPerUnit);

        if (GUILayout.Button("Import All"))
        {
            Import();
        }
    }

    private void Import()
    {
        if (airFile == null) return;
        Dictionary<int, MugenAction> actions = ParseAirFile(airFile.text);

        if (!Directory.Exists(animationOutputPath)) Directory.CreateDirectory(animationOutputPath);

        foreach (var kvp in actions)
        {
            CreateAnimationClip(kvp.Value, $"Action_{kvp.Key}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Imported {actions.Count} clips with Hitboxes & Flips.");
    }

    // --- Parsing ---

    private Dictionary<int, MugenAction> ParseAirFile(string content)
    {
        Dictionary<int, MugenAction> actions = new Dictionary<int, MugenAction>();
        string[] lines = content.Split('\n');
        MugenAction currentAction = null;

        Regex actionRegex = new Regex(@"^\s*\[Begin Action\s+(\d+)\]", RegexOptions.IgnoreCase);
        Regex frameRegex = new Regex(@"^\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)");
        Regex clsnBoxRegex = new Regex(@"=\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)");

        List<Rect> currentClsn2 = new List<Rect>(); 
        List<Rect> currentClsn1 = new List<Rect>(); 

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;

            Match actionMatch = actionRegex.Match(line);
            if (actionMatch.Success)
            {
                currentAction = new MugenAction();
                currentAction.ActionID = int.Parse(actionMatch.Groups[1].Value);
                if (!actions.ContainsKey(currentAction.ActionID)) actions.Add(currentAction.ActionID, currentAction);
                continue;
            }

            if (currentAction != null)
            {
                if (line.Contains("Clsn2Default") || line.Contains("Clsn1Default")) continue; 
                if (line.StartsWith("Clsn")) continue; 

                if (line.Contains("Clsn2[") || line.Contains("Clsn1["))
                {
                    Match boxMatch = clsnBoxRegex.Match(line);
                    if (boxMatch.Success)
                    {
                        float x1 = float.Parse(boxMatch.Groups[1].Value);
                        float y1 = float.Parse(boxMatch.Groups[2].Value);
                        float x2 = float.Parse(boxMatch.Groups[3].Value);
                        float y2 = float.Parse(boxMatch.Groups[4].Value);
                        Rect r = new Rect(x1, y1, x2 - x1, y2 - y1);
                        
                        if (line.Contains("Clsn2")) currentClsn2.Add(r);
                        else currentClsn1.Add(r);
                    }
                    continue;
                }

                Match frameMatch = frameRegex.Match(line);
                if (frameMatch.Success)
                {
                    MugenFrame frame = new MugenFrame();
                    frame.Group = int.Parse(frameMatch.Groups[1].Value);
                    frame.Index = int.Parse(frameMatch.Groups[2].Value);
                    frame.Duration = int.Parse(frameMatch.Groups[5].Value);
                    
                    // CHECK FOR FLIP FLAGS (H or V)
                    if (line.Contains("H") || line.Contains(",H")) frame.FlipH = true;

                    frame.Hurtbox = GetBoundingBox(currentClsn2);
                    frame.Hitbox = GetBoundingBox(currentClsn1);

                    currentAction.Frames.Add(frame);
                    currentClsn2.Clear();
                    currentClsn1.Clear();
                }
            }
        }
        return actions;
    }

    private Rect? GetBoundingBox(List<Rect> rects)
    {
        if (rects.Count == 0) return null;
        float xMin = rects.Min(r => r.xMin);
        float xMax = rects.Max(r => r.xMax);
        float yMin = rects.Min(r => r.yMin);
        float yMax = rects.Max(r => r.yMax);
        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    // --- Animation Generation ---

    private void CreateAnimationClip(MugenAction action, string clipName)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60;

        // BINDINGS
        EditorCurveBinding spriteBinding = new EditorCurveBinding { type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
        EditorCurveBinding flipBinding = new EditorCurveBinding { type = typeof(SpriteRenderer), propertyName = "m_FlipX" };

        List<ObjectReferenceKeyframe> spriteKeys = new List<ObjectReferenceKeyframe>();
        List<Keyframe> flipKeys = new List<Keyframe>();

        string hurtPath = "Hurtbox";
        string hitPath = "Hitbox";

        // Curve containers
        AnimationCurve hurtOffX = new AnimationCurve();
        AnimationCurve hurtOffY = new AnimationCurve();
        AnimationCurve hurtSizeX = new AnimationCurve();
        AnimationCurve hurtSizeY = new AnimationCurve();
        AnimationCurve hurtEnabled = new AnimationCurve(); 

        AnimationCurve hitOffX = new AnimationCurve();
        AnimationCurve hitOffY = new AnimationCurve();
        AnimationCurve hitSizeX = new AnimationCurve();
        AnimationCurve hitSizeY = new AnimationCurve();
        AnimationCurve hitEnabled = new AnimationCurve();

        float time = 0f;

        foreach (var frame in action.Frames)
        {
            Sprite s = FindSprite(frame.Group, frame.Index);
            if (s != null)
            {
                spriteKeys.Add(new ObjectReferenceKeyframe { time = time, value = s });
                
                // FLIP KEY (Instant toggle)
                Keyframe kf = new Keyframe(time, frame.FlipH ? 1f : 0f);
                kf.inTangent = float.PositiveInfinity;
                kf.outTangent = float.PositiveInfinity;
                flipKeys.Add(kf);
            }

            // COLLIDER KEYS (Pass FlipH to calculate inverted Offset)
            AddColliderKeys(frame.Hurtbox, frame.FlipH, time, hurtOffX, hurtOffY, hurtSizeX, hurtSizeY, hurtEnabled);
            AddColliderKeys(frame.Hitbox, frame.FlipH, time, hitOffX, hitOffY, hitSizeX, hitSizeY, hitEnabled);

            float duration = (frame.Duration == -1 ? 1 : frame.Duration) / 60f;
            time += duration;
        }

        if (spriteKeys.Count > 0) AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeys.ToArray());
        
        // APPLY FLIP CURVE (Only if there is any flipping)
        if (flipKeys.Any(k => k.value > 0))
            AnimationUtility.SetEditorCurve(clip, flipBinding, new AnimationCurve(flipKeys.ToArray()));

        SetBoxCurves(clip, hurtPath, hurtOffX, hurtOffY, hurtSizeX, hurtSizeY, hurtEnabled);
        SetBoxCurves(clip, hitPath, hitOffX, hitOffY, hitSizeX, hitSizeY, hitEnabled);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = (action.ActionID == 0 || action.ActionID == 20 || action.ActionID == 2000); 
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, $"{animationOutputPath}/{clipName}.anim");
    }

    private void AddColliderKeys(Rect? rect, bool flipH, float time, AnimationCurve offX, AnimationCurve offY, AnimationCurve sizeX, AnimationCurve sizeY, AnimationCurve enabled)
    {
        if (rect.HasValue)
        {
            Rect r = rect.Value;
            
            // Standard calc
            float cX = (r.x + (r.width / 2f)) / pixelsPerUnit;
            float cY = -1f * (r.y + (r.height / 2f)) / pixelsPerUnit;
            float sX = r.width / pixelsPerUnit;
            float sY = r.height / pixelsPerUnit;

            // FIX: If Sprite is flipped, invert the X Offset relative to center
            if (flipH) cX = -cX;

            offX.AddKey(new Keyframe(time, cX, float.PositiveInfinity, float.PositiveInfinity));
            offY.AddKey(new Keyframe(time, cY, float.PositiveInfinity, float.PositiveInfinity));
            sizeX.AddKey(new Keyframe(time, sX, float.PositiveInfinity, float.PositiveInfinity));
            sizeY.AddKey(new Keyframe(time, sY, float.PositiveInfinity, float.PositiveInfinity));
            enabled.AddKey(new Keyframe(time, 1f, float.PositiveInfinity, float.PositiveInfinity)); 
        }
        else
        {
            enabled.AddKey(new Keyframe(time, 0f, float.PositiveInfinity, float.PositiveInfinity)); 
        }
    }

    private void SetBoxCurves(AnimationClip clip, string path, AnimationCurve ox, AnimationCurve oy, AnimationCurve sx, AnimationCurve sy, AnimationCurve en)
    {
        if (en.keys.Length == 0) return;
        clip.SetCurve(path, typeof(BoxCollider2D), "m_Offset.x", ox);
        clip.SetCurve(path, typeof(BoxCollider2D), "m_Offset.y", oy);
        clip.SetCurve(path, typeof(BoxCollider2D), "m_Size.x", sx);
        clip.SetCurve(path, typeof(BoxCollider2D), "m_Size.y", sy);
        clip.SetCurve(path, typeof(BoxCollider2D), "m_Enabled", en);
    }

    private Sprite FindSprite(int group, int index)
    {
        string name = string.Format(spriteNamingFormat, group, index);
        string path = $"{spriteFolderPath}/{name}";
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path + ".png");
        if (s == null) s = AssetDatabase.LoadAssetAtPath<Sprite>(path + ".jpg");
        return s;
    }
}