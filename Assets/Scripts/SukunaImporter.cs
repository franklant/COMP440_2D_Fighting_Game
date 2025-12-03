using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

public class SukunaMugenImporter : EditorWindow
{
    // UI Variables
    private List<TextAsset> logicFiles = new List<TextAsset>();
    private TextAsset airFile;
    private string spriteFolderPath = "Assets/Sukuna/Sprites";
    private string animationOutputPath = "Assets/Sukuna/Animations";
    private string spriteNamingFormat = "{0}-{1}"; // {0} is Group, {1} is Index. E.g., 0-0.png
    private float pixelsPerUnit = 100f;
    private bool generateController = true;

    // Parsing Data Structures
    private class MugenAction
    {
        public int ActionID;
        public List<MugenFrame> Frames = new List<MugenFrame>();
    }

    private class MugenFrame
    {
        public int Group;
        public int Index;
        public int XOffset;
        public int YOffset;
        public int Duration; // in ticks
        public bool FlipH;
        public bool FlipV;
    }

    private class MugenState
    {
        public int StateID;
        public int AnimID = -1;
        public string ContextName = "Unknown";
    }

    [MenuItem("Tools/MUGEN to Unity Importer")]
    public static void ShowWindow()
    {
        GetWindow<SukunaMugenImporter>("MUGEN Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Sukuna Animation Importer", EditorStyles.boldLabel);

        // File Inputs
        EditorGUILayout.Space();
        GUILayout.Label("1. Logic Files (cns, st, specials.txt, etc.)", EditorStyles.label);
        
        // List handling for logic files
        for (int i = 0; i < logicFiles.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            logicFiles[i] = (TextAsset)EditorGUILayout.ObjectField(logicFiles[i], typeof(TextAsset), false);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                logicFiles.RemoveAt(i);
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("Add Logic File"))
        {
            logicFiles.Add(null);
        }

        EditorGUILayout.Space();
        GUILayout.Label("2. Animation Data (air.txt)", EditorStyles.label);
        airFile = (TextAsset)EditorGUILayout.ObjectField("AIR File", airFile, typeof(TextAsset), false);

        // Path Inputs
        EditorGUILayout.Space();
        GUILayout.Label("3. Paths & Settings", EditorStyles.label);
        
        if (GUILayout.Button("Select Sprite Folder"))
        {
            string path = EditorUtility.OpenFolderPanel("Select Sprite Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                spriteFolderPath = MakeRelative(path);
            }
        }
        GUILayout.Label($"Sprite Folder: {spriteFolderPath}");

        if (GUILayout.Button("Select Output Folder"))
        {
            string path = EditorUtility.OpenFolderPanel("Select Animation Output Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                animationOutputPath = MakeRelative(path);
            }
        }
        GUILayout.Label($"Output Folder: {animationOutputPath}");

        spriteNamingFormat = EditorGUILayout.TextField("Sprite Naming (e.g. {0}-{1})", spriteNamingFormat);
        pixelsPerUnit = EditorGUILayout.FloatField("Pixels Per Unit", pixelsPerUnit);

        EditorGUILayout.Space();
        if (GUILayout.Button("Import Animations", GUILayout.Height(40)))
        {
            Import();
        }
    }

    private string MakeRelative(string path)
    {
        if (path.StartsWith(Application.dataPath))
        {
            return "Assets" + path.Substring(Application.dataPath.Length);
        }
        return path;
    }

    private void Import()
    {
        if (airFile == null)
        {
            Debug.LogError("AIR file missing!");
            return;
        }

        // 1. Parse AIR File
        Dictionary<int, MugenAction> actions = ParseAirFile(airFile.text);
        Debug.Log($"Parsed {actions.Count} actions from AIR file.");

        // 2. Parse Logic Files for Context
        List<MugenState> states = new List<MugenState>();
        foreach (var file in logicFiles)
        {
            if (file != null)
            {
                states.AddRange(ParseStateFile(file.text));
            }
        }
        Debug.Log($"Parsed {states.Count} states from logic files.");

        // 3. Map States to Actions and Generate Clips
        if (!Directory.Exists(animationOutputPath))
        {
            Directory.CreateDirectory(animationOutputPath);
        }

        int clipCount = 0;

        // We iterate through identified states to prioritize named animations
        foreach (var state in states)
        {
            if (state.AnimID != -1 && actions.ContainsKey(state.AnimID))
            {
                string clipName = $"{state.StateID}_{SanitizeFilename(state.ContextName)}";
                CreateAnimationClip(actions[state.AnimID], clipName);
                clipCount++;
                
                // Remove processed action so we don't duplicate if we iterate actions later (optional strategy)
                // actions.Remove(state.AnimID); 
            }
        }

        // Optional: Generate clips for actions that weren't linked to a specific state (Generic anims like dust, sparks)
        foreach(var kvp in actions)
        {
            // Check if we already created a clip for this action via state mapping
            bool alreadyCreated = states.Any(s => s.AnimID == kvp.Key);
            
            if(!alreadyCreated)
            {
                CreateAnimationClip(kvp.Value, $"Action_{kvp.Key}");
                clipCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Import Complete. Created {clipCount} AnimationClips.");
    }

    // --- Parsing Logic ---

    private Dictionary<int, MugenAction> ParseAirFile(string content)
    {
        Dictionary<int, MugenAction> actions = new Dictionary<int, MugenAction>();
        string[] lines = content.Split('\n');
        MugenAction currentAction = null;

        // Regex for [Begin Action NNN]
        Regex actionRegex = new Regex(@"^\s*\[Begin Action\s+(\d+)\]", RegexOptions.IgnoreCase);
        
        // Regex for frames: Group, Index, X, Y, Time, Flip?, Blend?
        // e.g. 5000,0, 0,0, 4
        Regex frameRegex = new Regex(@"^\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)(?:,\s*([A-Za-z]*))?");

        foreach (string line in lines)
        {
            string cleanLine = line.Trim();
            if (string.IsNullOrEmpty(cleanLine) || cleanLine.StartsWith(";")) continue;

            Match actionMatch = actionRegex.Match(cleanLine);
            if (actionMatch.Success)
            {
                currentAction = new MugenAction();
                currentAction.ActionID = int.Parse(actionMatch.Groups[1].Value);
                if (!actions.ContainsKey(currentAction.ActionID))
                {
                    actions.Add(currentAction.ActionID, currentAction);
                }
                continue;
            }

            if (currentAction != null)
            {
                // Check for Clsn lines (skip them for now, though we could import colliders)
                if (cleanLine.StartsWith("Clsn", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (cleanLine.Equals("LoopStart", System.StringComparison.OrdinalIgnoreCase)) continue; // Unity loops automatically based on settings

                Match frameMatch = frameRegex.Match(cleanLine);
                if (frameMatch.Success)
                {
                    MugenFrame frame = new MugenFrame();
                    frame.Group = int.Parse(frameMatch.Groups[1].Value);
                    frame.Index = int.Parse(frameMatch.Groups[2].Value);
                    frame.XOffset = int.Parse(frameMatch.Groups[3].Value);
                    frame.YOffset = int.Parse(frameMatch.Groups[4].Value);
                    frame.Duration = int.Parse(frameMatch.Groups[5].Value);
                    
                    string flags = frameMatch.Groups[6].Value;
                    if (!string.IsNullOrEmpty(flags))
                    {
                        if (flags.Contains("H")) frame.FlipH = true;
                        if (flags.Contains("V")) frame.FlipV = true;
                    }

                    currentAction.Frames.Add(frame);
                }
            }
        }
        return actions;
    }

    private List<MugenState> ParseStateFile(string content)
    {
        List<MugenState> states = new List<MugenState>();
        string[] lines = content.Split('\n');
        
        string lastComment = "Unknown";
        MugenState currentState = null;

        Regex stateDefRegex = new Regex(@"^\s*\[Statedef\s+(\d+)\]", RegexOptions.IgnoreCase);
        // Regex to find "anim = NNN"
        Regex animRegex = new Regex(@"^\s*anim\s*=\s*(\d+)", RegexOptions.IgnoreCase);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith(";"))
            {
                // Clean up separators like ;----------
                string comment = line.TrimStart(';', '-', '=', ' ').Trim();
                if (!string.IsNullOrEmpty(comment))
                {
                    lastComment = comment;
                }
                continue;
            }

            Match stateMatch = stateDefRegex.Match(line);
            if (stateMatch.Success)
            {
                currentState = new MugenState();
                currentState.StateID = int.Parse(stateMatch.Groups[1].Value);
                currentState.ContextName = lastComment;
                states.Add(currentState);
                // Reset comment after assignment so we don't reuse old comments for generic states
                lastComment = "State_" + currentState.StateID; 
                continue;
            }

            if (currentState != null)
            {
                Match animMatch = animRegex.Match(line);
                if (animMatch.Success)
                {
                    // Only capture the first anim declaration (usually the base anim)
                    if (currentState.AnimID == -1)
                    {
                        currentState.AnimID = int.Parse(animMatch.Groups[1].Value);
                    }
                }
                
                // Stop checking specific state if we hit another state block (ChangeState) 
                // but usually Statedef handles the boundary.
            }
        }

        return states;
    }

    // --- Generation Logic ---

    private void CreateAnimationClip(MugenAction action, string clipName)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60; // MUGEN logic runs at 60 ticks per second

        // Setup ObjectReferenceCurveBinding for Sprites
        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = "";
        spriteBinding.propertyName = "m_Sprite";

        // Setup Curve for FlipX (if needed)
        EditorCurveBinding flipBinding = new EditorCurveBinding();
        flipBinding.type = typeof(SpriteRenderer);
        flipBinding.path = "";
        flipBinding.propertyName = "m_FlipX";

        List<ObjectReferenceKeyframe> spriteKeyframes = new List<ObjectReferenceKeyframe>();
        List<Keyframe> flipKeyframes = new List<Keyframe>();

        float currentTime = 0f;

        // Iterate frames
        foreach (var frame in action.Frames)
        {
            // 1. Find Sprite
            Sprite sprite = FindSprite(frame.Group, frame.Index);
            
            if (sprite != null)
            {
                ObjectReferenceKeyframe keyframe = new ObjectReferenceKeyframe();
                keyframe.time = currentTime;
                keyframe.value = sprite;
                spriteKeyframes.Add(keyframe);

                // Handle FlipH
                Keyframe flipKey = new Keyframe(currentTime, frame.FlipH ? 1f : 0f);
                // Set tangents to Constant to avoid interpolation on booleans
                flipKey.inTangent = float.PositiveInfinity; 
                flipKey.outTangent = float.PositiveInfinity;
                flipKeyframes.Add(flipKey);
            }
            else
            {
                if(frame.Group != -1) // -1 is invisible/dummy frame
                    Debug.LogWarning($"Sprite missing for Action {action.ActionID}: {frame.Group}-{frame.Index}");
            }

            // MUGEN Duration is in ticks (1/60th of a second)
            // If duration is -1, it's infinite, usually indicates end of animation or loop.
            // For Unity, we just extend it a bit or stop.
            int duration = frame.Duration == -1 ? 1 : frame.Duration; 
            
            currentTime += (duration / 60f);
        }

        // Apply bindings
        if (spriteKeyframes.Count > 0)
        {
            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyframes.ToArray());
        }
        
        // Only add flip curve if there is actual flipping happening to keep it clean
        if (flipKeyframes.Any(k => k.value > 0))
        {
            AnimationCurve flipCurve = new AnimationCurve(flipKeyframes.ToArray());
            AnimationUtility.SetEditorCurve(clip, flipBinding, flipCurve);
        }

        // Save Asset
        string path = $"{animationOutputPath}/{clipName}.anim";
        // Ensure unique name
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        
        AssetDatabase.CreateAsset(clip, path);
    }

    private Sprite FindSprite(int group, int index)
    {
        // Format sprite name based on user setting
        string fileName = string.Format(spriteNamingFormat, group, index);
        string fullPath = $"{spriteFolderPath}/{fileName}";
        
        // Try exact match first
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath + ".png");
        if (s == null) s = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath + ".jpg");
        if (s == null) s = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath + ".bmp"); // MUGEN uses pcx usually converted to bmp/png

        return s;
    }

    private string SanitizeFilename(string name)
    {
        string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        foreach (char c in invalidChars)
        {
            name = name.Replace(c.ToString(), "");
        }
        return name.Replace(" ", "_").Trim();
    }
}