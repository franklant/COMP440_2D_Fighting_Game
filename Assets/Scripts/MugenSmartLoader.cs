using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class MugenSmartLoader : EditorWindow
{
    // UPDATED PATH FOR GOJO
    public string spriteFolderPath = "Assets/Resources/Chars/Gojo/Sprites";
    public TextAsset airFile;
    public TextAsset[] cnsFiles; 

    private Dictionary<int, string> categoryMap = new Dictionary<int, string>();

    [MenuItem("Mugen/1. Smart Context Loader")]
    public static void ShowWindow() => GetWindow<MugenSmartLoader>("Context Loader");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Step 1: Group Animations by Logic", EditorStyles.boldLabel);
        airFile = (TextAsset)EditorGUILayout.ObjectField("AIR File", airFile, typeof(TextAsset), false);
        
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty stringsProperty = so.FindProperty("cnsFiles");
        EditorGUILayout.PropertyField(stringsProperty, true);
        so.ApplyModifiedProperties();

        spriteFolderPath = EditorGUILayout.TextField("Sprite Path", spriteFolderPath);

        if (GUILayout.Button("Generate Context-Aware Clips"))
        {
            if (airFile == null) return;
            BuildCategoryMap();
            ParseAirAndCreateClips();
        }
    }

    void BuildCategoryMap()
    {
        // (Logic remains same as before)
        categoryMap.Clear();
        if (cnsFiles == null) return;
        foreach (var file in cnsFiles)
        {
            if (file == null) continue;
            string text = file.text;
            string[] lines = text.Split('\n');
            int currentState = -1;
            string currentCategory = "_Common";

            foreach (string line in lines)
            {
                string clean = line.Trim().ToLower();
                if (clean.StartsWith("[statedef"))
                {
                    Match match = Regex.Match(clean, @"\d+");
                    if (match.Success)
                    {
                        currentState = int.Parse(match.Value);
                        if (currentState < 200) currentCategory = "_Basics";
                        else if (currentState >= 200 && currentState < 1000) currentCategory = "_Attacks";
                        else if (currentState >= 1000 && currentState < 3000) currentCategory = "_Specials";
                        else if (currentState >= 3000 && currentState < 5000) currentCategory = "_Supers";
                        else if (currentState >= 5000) currentCategory = "_System";
                    }
                }
                if (currentState != -1 && (clean.StartsWith("anim") || clean.StartsWith("stateno")) && clean.Contains("="))
                {
                    Match match = Regex.Match(clean.Split('=')[1], @"\d+");
                    if (match.Success && int.TryParse(match.Value, out int id))
                    {
                        if (!categoryMap.ContainsKey(id)) categoryMap.Add(id, currentCategory);
                    }
                }
            }
        }
    }

    void ParseAirAndCreateClips()
    {
        string text = airFile.text;
        string[] lines = text.Split('\n');
        int currentAction = -1;
        string lastComment = "Unknown";
        List<int[]> frames = new List<int[]>(); 
        bool isLooping = false;

        foreach (string line in lines)
        {
            string clean = line.Trim();
            if (string.IsNullOrEmpty(clean)) continue;
            
            if (clean.StartsWith("[Begin Action"))
            {
                if (currentAction != -1) BuildClip(currentAction, lastComment, frames, isLooping);
                currentAction = int.Parse(Regex.Match(clean, @"\d+").Value);
                frames.Clear();
                isLooping = false;
            }
            else if (clean.ToLower().Contains("loopstart")) isLooping = true;
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
        if (currentAction != -1) BuildClip(currentAction, lastComment, frames, isLooping);
        AssetDatabase.Refresh();
    }

    void BuildClip(int actionID, string name, List<int[]> animFrames, bool loop)
    {
        if (animFrames.Count == 0) return;

        string folder = "_Unused";
        if (categoryMap.ContainsKey(actionID)) folder = categoryMap[actionID];
        else if (actionID < 200) folder = "_Basics";
        else if (actionID >= 6000) folder = "_VFX_Generic"; 
        
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
            
            ObjectReferenceKeyframe keyframe = new ObjectReferenceKeyframe();
            keyframe.time = currentTime;
            keyframe.value = sprite;
            keyframes.Add(keyframe);
            float duration = (frame[2] == -1) ? 1f : frame[2] / 60f;
            currentTime += duration;
        }

        EditorCurveBinding binding = new EditorCurveBinding();
        binding.type = typeof(SpriteRenderer);
        binding.path = "";
        binding.propertyName = "m_Sprite";
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes.ToArray());

        // UPDATED OUTPUT PATH
        string fileName = $"{actionID}_{name}.anim";
        string path = $"Assets/Resources/Chars/Gojo/Animations/{folder}/{fileName}";
        
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        AssetDatabase.CreateAsset(clip, path);
    }

    Sprite FindSpriteInProject(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name, new[] { spriteFolderPath });
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return null;
    }
}