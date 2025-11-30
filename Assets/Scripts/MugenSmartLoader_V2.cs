using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class MugenSmartLoader_V4 : EditorWindow
{
    public string characterName = "Sukuna";
    public TextAsset airFile;
    public TextAsset[] cnsFiles; 

    private Dictionary<int, string> namingMap = new Dictionary<int, string>();

    [MenuItem("Mugen/4. Smart Loader V4 (Ultra - Sprite Validated)")]
    public static void ShowWindow() => GetWindow<MugenSmartLoader_V4>("Smart Loader V4");

    void OnGUI()
    {
        EditorGUILayout.LabelField("V4: Validated Animation Loader", EditorStyles.boldLabel);
        
        characterName = EditorGUILayout.TextField("Character Name", characterName);
        GUILayout.Space(5);
        airFile = (TextAsset)EditorGUILayout.ObjectField("AIR File", airFile, typeof(TextAsset), false);
        
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty stringsProperty = so.FindProperty("cnsFiles");
        EditorGUILayout.PropertyField(stringsProperty, true);
        so.ApplyModifiedProperties();

        if (GUILayout.Button($"Analyze & Generate for {characterName}"))
        {
            if (airFile == null) return;
            AnalyzeCNS();
            GenerateClips();
        }
    }

    void AnalyzeCNS()
    {
        namingMap.Clear();
        if (cnsFiles == null) return;

        foreach (var file in cnsFiles)
        {
            if (file == null) continue;
            string text = file.text;
            string[] lines = text.Split('\n');

            string currentBlockType = "";

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim().ToLower();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;

                if (line.StartsWith("[statedef"))
                {
                    Match match = Regex.Match(line, @"\d+");
                    if (match.Success) ForceCategory(int.Parse(match.Value), "State");
                    currentBlockType = ""; 
                }

                if (line.StartsWith("type") && line.Contains("=")) currentBlockType = line.Split('=')[1].Trim();

                int foundID = -1;
                if (currentBlockType.Contains("explod") && line.StartsWith("anim") && TryGetID(line, out foundID))
                    ForceCategory(foundID, "VFX");
                else if (currentBlockType.Contains("changeanim") && line.StartsWith("value") && TryGetID(line, out foundID))
                    ForceCategory(foundID, "State");
                else if (line.StartsWith("anim") && TryGetID(line, out foundID))
                    ForceCategory(foundID, "State");
            }
        }
    }

    bool TryGetID(string line, out int id)
    {
        id = -1;
        if (!line.Contains("=")) return false;
        string val = line.Split('=')[1].Trim();
        if (val.StartsWith("var") || val.StartsWith("anim") || val.StartsWith("const")) return false;
        Match match = Regex.Match(val, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int result)) { id = result; return true; }
        return false;
    }

    void ForceCategory(int id, string category)
    {
        if (!namingMap.ContainsKey(id)) namingMap.Add(id, category);
        else if (namingMap[id] == "VFX" && category == "State") namingMap[id] = "State";
    }

    void GenerateClips()
    {
        string text = airFile.text;
        string[] lines = text.Split('\n');
        
        int currentAction = -1;
        List<int[]> frames = new List<int[]>(); 
        bool isLooping = false;

        string basePath = $"Assets/Resources/Chars/{characterName}/Animations";
        if (Directory.Exists(basePath)) Directory.Delete(basePath, true);
        Directory.CreateDirectory(basePath);

        foreach (string line in lines)
        {
            string clean = line.Trim();
            if (string.IsNullOrEmpty(clean)) continue;

            if (clean.StartsWith("[Begin Action"))
            {
                if (currentAction != -1) BuildClip(currentAction, frames, isLooping, basePath);
                currentAction = int.Parse(Regex.Match(clean, @"\d+").Value);
                frames.Clear();
                isLooping = false;
            }
            else if (clean.ToLower().Contains("loopstart")) isLooping = true;
            else if (clean.StartsWith("Clsn")) continue;
            else if (currentAction != -1 && (char.IsDigit(clean[0]) || clean.StartsWith("-")))
            {
                string[] parts = clean.Split(',');
                if (parts.Length >= 5)
                {
                    if (int.TryParse(parts[0], out int group) && 
                        int.TryParse(parts[1], out int image) && 
                        int.TryParse(parts[4], out int time))
                    {
                        frames.Add(new int[] { group, image, time });
                    }
                }
            }
        }
        if (currentAction != -1) BuildClip(currentAction, frames, isLooping, basePath);
        AssetDatabase.Refresh();
    }

    void BuildClip(int actionID, List<int[]> animFrames, bool loop, string basePath)
    {
        if (animFrames.Count == 0) return;

        // --- ULTRA FEATURE: SPRITE VALIDATION ---
        int validSpritesFound = 0;
        foreach (var frame in animFrames)
        {
            if (FindSpriteInProject($"{frame[0]}-{frame[1]}") != null) validSpritesFound++;
        }

        // If this action has frames but NO valid sprites, it's an "Invisible Logic State"
        // We skip generating a .anim file to keep the project clean.
        if (validSpritesFound == 0) 
        {
            // Debug.Log($"Skipped Invisible Logic ID: {actionID}");
            return;
        }
        // ----------------------------------------

        string prefix = "Anim"; 
        string folder = "_Misc";

        if (namingMap.ContainsKey(actionID))
        {
            if (namingMap[actionID] == "State") { prefix = "State"; folder = "_States"; }
            if (namingMap[actionID] == "VFX") { prefix = "VFX"; folder = "_VFX"; }
        }
        else 
        {
            if (actionID < 200) { prefix = "Basic"; folder = "_Basics"; }
            else if (actionID >= 6000) { prefix = "VFX"; folder = "_VFX"; }
            else if (actionID >= 200 && actionID < 5000) { prefix = "State"; folder = "_States"; }
        }

        string clipName = $"{actionID}_{prefix}";

        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60; 
        if (loop) {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        List<ObjectReferenceKeyframe> keyframes = new List<ObjectReferenceKeyframe>();
        float currentTime = 0f;

        foreach (var frame in animFrames)
        {
            string spriteName = $"{frame[0]}-{frame[1]}";
            Sprite sprite = FindSpriteInProject(spriteName);
            
            if (sprite != null)
            {
                ObjectReferenceKeyframe keyframe = new ObjectReferenceKeyframe();
                keyframe.time = currentTime;
                keyframe.value = sprite;
                keyframes.Add(keyframe);
            }
            
            float duration = (frame[2] == -1) ? 1f : frame[2] / 60f;
            currentTime += duration;
        }

        EditorCurveBinding binding = new EditorCurveBinding();
        binding.type = typeof(SpriteRenderer);
        binding.path = "";
        binding.propertyName = "m_Sprite";
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes.ToArray());

        string path = $"{basePath}/{folder}/{clipName}.anim";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        AssetDatabase.CreateAsset(clip, path);
    }

    Sprite FindSpriteInProject(string name)
    {
        string searchPath = $"Assets/Resources/Chars/{characterName}/Sprites";
        string[] guids = AssetDatabase.FindAssets(name, new[] { searchPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string filename = Path.GetFileNameWithoutExtension(path);
            if (filename == name) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }
}