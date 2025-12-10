using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

public class LuffySimpleImporter : EditorWindow
{
    // UI Variables
    private TextAsset airFile;
    private string spriteFolderPath = "Assets/Luffy/Sprites";
    private string animationOutputPath = "Assets/Luffy/Animations";
    private string spriteNamingFormat = "{0}-{1}"; // {0}=Group, {1}=Index. Check if your files are 0-0.png or 0_0.png
    
    // Data Storage
    private Dictionary<int, MugenAction> actions = new Dictionary<int, MugenAction>();

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
        public bool FlipV;
    }

    [MenuItem("Tools/Luffy Simple Importer")]
    public static void ShowWindow()
    {
        GetWindow<LuffySimpleImporter>("Luffy Simple Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Luffy Importer (Air Only)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This version ignores CNS names and only outputs 'Action_ID'.", MessageType.Info);
        
        EditorGUILayout.Space();
        airFile = (TextAsset)EditorGUILayout.ObjectField("AIR File", airFile, typeof(TextAsset), false);

        EditorGUILayout.Space();
        GUILayout.Label("Paths", EditorStyles.label);
        spriteFolderPath = EditorGUILayout.TextField("Sprite Folder", spriteFolderPath);
        animationOutputPath = EditorGUILayout.TextField("Output Folder", animationOutputPath);
        spriteNamingFormat = EditorGUILayout.TextField("Sprite Format", spriteNamingFormat);

        EditorGUILayout.Space();
        if (GUILayout.Button("Import Animations"))
        {
            Import();
        }
    }

    private void Import()
    {
        if (airFile == null) return;

        ParseAirFile(airFile.text);

        if (!Directory.Exists(animationOutputPath)) Directory.CreateDirectory(animationOutputPath);

        int count = 0;
        foreach (var kvp in actions)
        {
            // PURE MADARA STYLE: naming is just "Action_" + ID
            string clipName = $"Action_{kvp.Key}"; 
            CreateAnimationClip(kvp.Value, clipName);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Imported {count} clips as Action_X.");
    }

    private void ParseAirFile(string content)
    {
        actions.Clear();
        string[] lines = content.Split('\n');
        MugenAction currentAction = null;

        Regex actionRegex = new Regex(@"^\s*\[Begin Action\s+(\d+)\]", RegexOptions.IgnoreCase);
        // Regex: Group, Index, X, Y, Time
        Regex frameRegex = new Regex(@"^\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)");

        foreach (string line in lines)
        {
            if (line.Trim().StartsWith(";")) continue;

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
                if (line.ToLower().Contains("clsn") || line.ToLower().Contains("loopstart")) continue;
                
                Match frameMatch = frameRegex.Match(line);
                if (frameMatch.Success)
                {
                    MugenFrame frame = new MugenFrame();
                    frame.Group = int.Parse(frameMatch.Groups[1].Value);
                    frame.Index = int.Parse(frameMatch.Groups[2].Value);
                    frame.Duration = int.Parse(frameMatch.Groups[5].Value);
                    if (line.Contains("H")) frame.FlipH = true;
                    if (line.Contains("V")) frame.FlipV = true;
                    currentAction.Frames.Add(frame);
                }
            }
        }
    }

    private void CreateAnimationClip(MugenAction action, string clipName)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60;
        
        EditorCurveBinding spriteBinding = new EditorCurveBinding { type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
        EditorCurveBinding flipBinding = new EditorCurveBinding { type = typeof(SpriteRenderer), propertyName = "m_FlipX" };

        List<ObjectReferenceKeyframe> spriteKeys = new List<ObjectReferenceKeyframe>();
        List<Keyframe> flipKeys = new List<Keyframe>();

        float time = 0f;
        foreach (var frame in action.Frames)
        {
            Sprite s = FindSprite(frame.Group, frame.Index);
            if (s != null)
            {
                spriteKeys.Add(new ObjectReferenceKeyframe { time = time, value = s });
                flipKeys.Add(new Keyframe(time, frame.FlipH ? 1f : 0f) { inTangent = float.PositiveInfinity, outTangent = float.PositiveInfinity });
            }
            // Mugen duration -1 is "infinite", we treat it as 1 frame hold here
            time += (frame.Duration == -1 ? 1 : frame.Duration) / 60f;
        }

        if (spriteKeys.Count > 0)
        {
            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeys.ToArray());
            // Only add flip curve if needed
            if(flipKeys.Any(k => k.value > 0)) 
                AnimationUtility.SetEditorCurve(clip, flipBinding, new AnimationCurve(flipKeys.ToArray()));
            
            // Loop settings: Loop Idle (0) and Walk/Run (20/100), clamp attacks
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = (action.ActionID == 0 || action.ActionID == 20 || action.ActionID == 100); 
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            string path = $"{animationOutputPath}/{clipName}.anim";
            AssetDatabase.CreateAsset(clip, AssetDatabase.GenerateUniqueAssetPath(path));
        }
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