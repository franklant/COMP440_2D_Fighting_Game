using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

public class GojoMugenImporter : EditorWindow
{
    // UI Variables
    private TextAsset airFile; // The anim.txt
    private List<TextAsset> cnsFiles = new List<TextAsset>(); // cns.txt, base.txt, etc.
    
    private string spriteFolderPath = "Assets/Gojo/Sprites";
    private string animationOutputPath = "Assets/Gojo/Animations";
    private string spriteNamingFormat = "{0}-{1}"; // Expects 200-0.png, 200-1.png
    private float pixelsPerUnit = 100f;
    private bool useCenterPivot = false; // If false, assumes Bottom Center usually

    // Data Holders
    private Dictionary<int, MugenAction> airActions = new Dictionary<int, MugenAction>();

    // Data Structures
    private class MugenAction
    {
        public int ActionID;
        public string Name; // Optional, derived from comments
        public List<MugenFrame> Frames = new List<MugenFrame>();
    }

    private class MugenFrame
    {
        public int Group;
        public int Index;
        public int XOffset;
        public int YOffset;
        public int Duration; // Ticks (1/60 sec)
        public bool FlipH;
        public bool FlipV;
    }

    [MenuItem("Tools/Gojo Importer")]
    public static void ShowWindow()
    {
        GetWindow<GojoMugenImporter>("Gojo Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Gojo Satoru Animation Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. Files
        GUILayout.Label("1. Input Files", EditorStyles.boldLabel);
        airFile = (TextAsset)EditorGUILayout.ObjectField("Anim File (anim.txt)", airFile, typeof(TextAsset), false);

        // Optional CNS files for naming context
        EditorGUILayout.LabelField("State Files (Optional - for naming):");
        for (int i = 0; i < cnsFiles.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            cnsFiles[i] = (TextAsset)EditorGUILayout.ObjectField(cnsFiles[i], typeof(TextAsset), false);
            if (GUILayout.Button("-", GUILayout.Width(20))) cnsFiles.RemoveAt(i);
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("Add CNS/State File")) cnsFiles.Add(null);

        EditorGUILayout.Space();

        // 2. Settings
        GUILayout.Label("2. Settings", EditorStyles.boldLabel);
        
        // Folder Selection
        EditorGUILayout.BeginHorizontal();
        spriteFolderPath = EditorGUILayout.TextField("Sprite Folder", spriteFolderPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Sprite Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path)) spriteFolderPath = FileUtil.GetProjectRelativePath(path);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        animationOutputPath = EditorGUILayout.TextField("Output Folder", animationOutputPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path)) animationOutputPath = FileUtil.GetProjectRelativePath(path);
        }
        EditorGUILayout.EndHorizontal();

        spriteNamingFormat = EditorGUILayout.TextField("Sprite Naming (e.g. {0}-{1})", spriteNamingFormat);
        
        EditorGUILayout.Space();
        GUILayout.Label("Actions", EditorStyles.boldLabel);

        // Button 1: The "Simple" approach but with correct timing
        if (GUILayout.Button("Import All Actions from AIR (Recommended)", GUILayout.Height(40)))
        {
            ImportAllFromAir();
        }

        // Button 2: The "Advanced" approach mapping states
        if (GUILayout.Button("Import Mapped States Only (Requires CNS)", GUILayout.Height(30)))
        {
            ImportMappedStates();
        }
    }

    private void ImportAllFromAir()
    {
        if (airFile == null) { Debug.LogError("Please assign the anim.txt file!"); return; }

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
        Debug.Log($"<color=cyan>Gojo Import:</color> Generated {count} animations.");
    }

    private void ImportMappedStates()
    {
        if (airFile == null) { Debug.LogError("Please assign the anim.txt file!"); return; }
        
        ParseAirFile(airFile.text);
        var stateMap = ParseStateFiles();

        if (!Directory.Exists(animationOutputPath)) Directory.CreateDirectory(animationOutputPath);

        int count = 0;
        foreach (var kvp in stateMap)
        {
            int stateID = kvp.Key;
            int animID = kvp.Value;

            if (airActions.ContainsKey(animID))
            {
                // Create clip named after the State ID (e.g., "200_LightPunch.anim")
                CreateAnimationClip(airActions[animID], $"{stateID}_State");
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=cyan>Gojo Import:</color> Generated {count} animations mapped from states.");
    }

    // --- Parsing Logic ---

    private void ParseAirFile(string content)
    {
        airActions.Clear();
        string[] lines = content.Split('\n');
        MugenAction currentAction = null;

        // Regex to find [Begin Action NNN]
        Regex actionRegex = new Regex(@"^\s*\[Begin Action\s+(\d+)\]", RegexOptions.IgnoreCase);
        
        // Regex for frame data: Group, Index, X, Y, Time
        // Handles optional Flip/Blend flags at the end
        // Example: 200,0, 0,0, 4
        // Example: 200,0, 0,0, 4, H
        Regex frameRegex = new Regex(@"^\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)");

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            
            // Skip comments and empty lines
            if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;

            // Check for Action Header
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

            // If we are inside an action, look for frames
            if (currentAction != null)
            {
                // Ignore Collision box definitions (Clsn2: 1, Clsn2[0] = ...)
                if (line.StartsWith("Clsn", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (line.Contains("LoopStart")) continue; // Unity loops automatically

                Match frameMatch = frameRegex.Match(line);
                if (frameMatch.Success)
                {
                    MugenFrame frame = new MugenFrame();
                    frame.Group = int.Parse(frameMatch.Groups[1].Value);
                    frame.Index = int.Parse(frameMatch.Groups[2].Value);
                    frame.XOffset = int.Parse(frameMatch.Groups[3].Value);
                    frame.YOffset = int.Parse(frameMatch.Groups[4].Value);
                    frame.Duration = int.Parse(frameMatch.Groups[5].Value);

                    // Check for horizontal flip flag (H)
                    if (line.Contains(", H") || line.Contains(",H")) frame.FlipH = true;
                    if (line.Contains(", V") || line.Contains(",V")) frame.FlipV = true;

                    currentAction.Frames.Add(frame);
                }
            }
        }
    }

    private Dictionary<int, int> ParseStateFiles()
    {
        Dictionary<int, int> map = new Dictionary<int, int>(); // StateID -> AnimID

        foreach (var file in cnsFiles)
        {
            if (file == null) continue;
            string[] lines = file.text.Split('\n');
            int currentState = -1;

            Regex stateDefRegex = new Regex(@"^\s*\[Statedef\s+(\d+)\]", RegexOptions.IgnoreCase);
            Regex animRegex = new Regex(@"^\s*anim\s*=\s*(\d+)", RegexOptions.IgnoreCase);

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;

                Match stateMatch = stateDefRegex.Match(line);
                if (stateMatch.Success)
                {
                    currentState = int.Parse(stateMatch.Groups[1].Value);
                    continue;
                }

                if (currentState != -1)
                {
                    Match animMatch = animRegex.Match(line);
                    if (animMatch.Success)
                    {
                        int animID = int.Parse(animMatch.Groups[1].Value);
                        if (!map.ContainsKey(currentState))
                        {
                            map.Add(currentState, animID);
                        }
                        currentState = -1; // Reset so we only grab the initial anim
                    }
                }
            }
        }
        return map;
    }

    // --- Creation Logic ---

    private void CreateAnimationClip(MugenAction action, string clipName)
    {
        if (action.Frames.Count == 0) return;

        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60; // MUGEN Standard

        // We only animate the Sprite property
        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        // Optional: Flip X property
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
            // Skip -1 group (invisible boxes in mugen)
            if (frame.Group == -1)
            {
                // Just add time, no sprite change (creates a gap/hold)
                currentTime += (frame.Duration / 60f);
                continue;
            }

            Sprite sprite = FindSprite(frame.Group, frame.Index);
            
            if (sprite != null)
            {
                // Add Sprite Key
                spriteKeys.Add(new ObjectReferenceKeyframe
                {
                    time = currentTime,
                    value = sprite
                });

                // Add Flip Key
                flipKeys.Add(new Keyframe(currentTime, frame.FlipH ? 1f : 0f)
                {
                    inTangent = float.PositiveInfinity, // Constant interpolation
                    outTangent = float.PositiveInfinity
                });
            }
            else
            {
                // If sprite missing, we still advance time to keep sync
                // Debug.LogWarning($"Missing Sprite: {frame.Group}-{frame.Index}");
            }

            // Advance time
            // MUGEN duration is ticks. If -1, it's infinite (we treat as 1 frame hold or loop end)
            float duration = (frame.Duration == -1) ? (1f / 60f) : (frame.Duration / 60f);
            currentTime += duration;
        }

        // Apply to clip
        if (spriteKeys.Count > 0)
        {
            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeys.ToArray());
            
            // Only add flip curve if flipping actually happens
            if (flipKeys.Any(k => k.value > 0))
            {
                AnimationUtility.SetEditorCurve(clip, flipBinding, new AnimationCurve(flipKeys.ToArray()));
            }

            // Loop Settings
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true; 
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Save
            string path = $"{animationOutputPath}/{clipName}.anim";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(clip, path);
        }
    }

    private Sprite FindSprite(int group, int index)
    {
        // Try Standard Format: 200-0.png
        string fileName = string.Format(spriteNamingFormat, group, index);
        string fullPath = $"{spriteFolderPath}/{fileName}";

        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath + ".png");
        
        // Fallback 1: Try .jpg
        if (s == null) s = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath + ".jpg");
        
        // Fallback 2: Try underscore 200_0.png
        if (s == null)
        {
            string altName = $"{group}_{index}";
            string altPath = $"{spriteFolderPath}/{altName}";
            s = AssetDatabase.LoadAssetAtPath<Sprite>(altPath + ".png");
        }

        return s;
    }
}