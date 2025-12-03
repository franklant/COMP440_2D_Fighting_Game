using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

public class MadaraMugenImporter : EditorWindow
{
    // UI Variables
    private TextAsset airFile; // Drag MadaraAir.txt here
    
    private string spriteFolderPath = "Assets/Madara/Sprites";
    private string animationOutputPath = "Assets/Madara/Animations";
    private string spriteNamingFormat = "{0}-{1}"; // Change to {0}_{1} if files are 0_0.png

    private Dictionary<int, MugenAction> airActions = new Dictionary<int, MugenAction>();

    private class MugenAction
    {
        public int ActionID;
        public List<MugenFrame> Frames = new List<MugenFrame>();
    }

    private class MugenFrame
    {
        public int Group;
        public int Index;
        public int Duration; // Ticks (1/60 sec)
        public bool FlipH;
        public bool FlipV;
    }

    [MenuItem("Tools/Madara Importer")]
    public static void ShowWindow()
    {
        GetWindow<MadaraMugenImporter>("Madara Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Madara Uchiha Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. Files
        GUILayout.Label("1. Input Files", EditorStyles.boldLabel);
        airFile = (TextAsset)EditorGUILayout.ObjectField("Anim File (MadaraAir.txt)", airFile, typeof(TextAsset), false);

        // 2. Settings
        EditorGUILayout.Space();
        GUILayout.Label("2. Paths & Settings", EditorStyles.boldLabel);
        
        spriteFolderPath = EditorGUILayout.TextField("Sprite Folder", spriteFolderPath);
        animationOutputPath = EditorGUILayout.TextField("Output Folder", animationOutputPath);
        spriteNamingFormat = EditorGUILayout.TextField("Sprite Naming", spriteNamingFormat);
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Import Madara Animations", GUILayout.Height(40)))
        {
            ImportAllFromAir();
        }
    }

    private void ImportAllFromAir()
    {
        if (airFile == null) { Debug.LogError("Please assign the MadaraAir.txt file!"); return; }

        ParseAirFile(airFile.text);

        if (!Directory.Exists(animationOutputPath)) Directory.CreateDirectory(animationOutputPath);

        int count = 0;
        foreach (var kvp in airActions)
        {
            CreateAnimationClip(kvp.Value, $"Action_{kvp.Key}");
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=red>Madara Import:</color> Generated {count} animation clips.");
    }

    // --- Parsing Logic (Standard MUGEN AIR) ---
    private void ParseAirFile(string content)
    {
        airActions.Clear();
        string[] lines = content.Split('\n');
        MugenAction currentAction = null;

        // Regex for Action Header: [Begin Action 200]
        Regex actionRegex = new Regex(@"^\s*\[Begin Action\s+(\d+)\]", RegexOptions.IgnoreCase);
        
        // Regex for Frame: 200,0, 0,0, 4 (Group, Index, X, Y, Time)
        Regex frameRegex = new Regex(@"^\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)");

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;

            Match actionMatch = actionRegex.Match(line);
            if (actionMatch.Success)
            {
                currentAction = new MugenAction();
                currentAction.ActionID = int.Parse(actionMatch.Groups[1].Value);
                if (!airActions.ContainsKey(currentAction.ActionID))
                {
                    airActions.Add(currentAction.ActionID, currentAction);
                }
                continue;
            }

            if (currentAction != null)
            {
                if (line.StartsWith("Clsn", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (line.Contains("LoopStart")) continue; 

                Match frameMatch = frameRegex.Match(line);
                if (frameMatch.Success)
                {
                    MugenFrame frame = new MugenFrame();
                    frame.Group = int.Parse(frameMatch.Groups[1].Value);
                    frame.Index = int.Parse(frameMatch.Groups[2].Value);
                    frame.Duration = int.Parse(frameMatch.Groups[5].Value);

                    if (line.Contains(", H") || line.Contains(",H")) frame.FlipH = true;
                    if (line.Contains(", V") || line.Contains(",V")) frame.FlipV = true;

                    currentAction.Frames.Add(frame);
                }
            }
        }
    }

    // --- Creation Logic ---
    private void CreateAnimationClip(MugenAction action, string clipName)
    {
        if (action.Frames.Count == 0) return;

        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60; // MUGEN Standard

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        EditorCurveBinding flipBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_FlipX"
        };

        List<ObjectReferenceKeyframe> spriteKeys = new List<ObjectReferenceKeyframe>();
        List<Keyframe> flipKeys = new List<Keyframe>();

        float currentTime = 0f;

        foreach (var frame in action.Frames)
        {
            // Skip -1 group (invisible frames)
            if (frame.Group == -1)
            {
                currentTime += (frame.Duration / 60f);
                continue;
            }

            Sprite sprite = FindSprite(frame.Group, frame.Index);
            
            if (sprite != null)
            {
                spriteKeys.Add(new ObjectReferenceKeyframe
                {
                    time = currentTime,
                    value = sprite
                });

                flipKeys.Add(new Keyframe(currentTime, frame.FlipH ? 1f : 0f)
                {
                    inTangent = float.PositiveInfinity,
                    outTangent = float.PositiveInfinity
                });
            }

            float duration = (frame.Duration == -1) ? (1f / 60f) : (frame.Duration / 60f);
            currentTime += duration;
        }

        if (spriteKeys.Count > 0)
        {
            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeys.ToArray());
            
            if (flipKeys.Any(k => k.value > 0))
            {
                AnimationUtility.SetEditorCurve(clip, flipBinding, new AnimationCurve(flipKeys.ToArray()));
            }

            // Default to Loop Time TRUE (You will manually uncheck attacks later)
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true; 
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            string path = $"{animationOutputPath}/{clipName}.anim";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(clip, path);
        }
    }

    private Sprite FindSprite(int group, int index)
    {
        string fileName = string.Format(spriteNamingFormat, group, index);
        string fullPath = $"{spriteFolderPath}/{fileName}";

        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath + ".png");
        
        if (s == null) s = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath + ".jpg");
        if (s == null) s = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath + ".bmp");
        
        // Fallback for underscore format if user forgot to change setting
        if (s == null)
        {
            string altName = $"{group}_{index}";
            string altPath = $"{spriteFolderPath}/{altName}";
            s = AssetDatabase.LoadAssetAtPath<Sprite>(altPath + ".png");
        }

        return s;
    }
}