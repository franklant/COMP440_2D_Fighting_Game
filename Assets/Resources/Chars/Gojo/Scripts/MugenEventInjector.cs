using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

public class MugenEventInjector : EditorWindow
{
    public TextAsset[] cnsFiles; 
    // UPDATED PATH FOR GOJO
    public string animationPath = "Assets/Resources/Chars/Gojo/Animations";

    private Dictionary<int, int> stateToAnimMap = new Dictionary<int, int>();

    [MenuItem("Mugen/2. Smart Event Injector")]
    public static void ShowWindow() => GetWindow<MugenEventInjector>("Event Injector");

    void OnGUI()
    {
        EditorGUILayout.LabelField("MUGEN Smart Injector (Time + Frames)", EditorStyles.boldLabel);
        
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty stringsProperty = so.FindProperty("cnsFiles");
        EditorGUILayout.PropertyField(stringsProperty, true);
        so.ApplyModifiedProperties();

        animationPath = EditorGUILayout.TextField("Anim Clip Path", animationPath);

        GUILayout.Space(10);
        if (GUILayout.Button("Inject All Events")) RunInjection();
    }

    void RunInjection()
    {
        stateToAnimMap.Clear();
        foreach (var file in cnsFiles) if(file != null) ScanForMappings(file.text);
        foreach (var file in cnsFiles) if(file != null) ParseAndInject(file.text);
        AssetDatabase.SaveAssets();
        Debug.Log("<b>[Injector]</b> Complete! Logic linked for Gojo.");
    }

    // (The rest of the logic remains generic and works fine as is)
    void ScanForMappings(string text)
    {
        string[] lines = text.Split('\n');
        int currentState = -1;
        foreach (string line in lines)
        {
            string clean = line.Trim().ToLower();
            if (clean.StartsWith("[statedef"))
            {
                Match match = Regex.Match(clean, @"\d+");
                if (match.Success) currentState = int.Parse(match.Value);
            }
            if (currentState != -1 && clean.StartsWith("anim") && clean.Contains("="))
            {
                string[] parts = clean.Split('=');
                if (parts.Length > 1)
                {
                    Match match = Regex.Match(parts[1], @"\d+");
                    if (match.Success && !stateToAnimMap.ContainsKey(currentState))
                        stateToAnimMap.Add(currentState, int.Parse(match.Value));
                }
            }
        }
    }

    void ParseAndInject(string text)
    {
        string[] lines = text.Split('\n');
        int currentState = -1;
        
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim(); 
            if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;

            if (line.ToLower().StartsWith("[statedef"))
            {
                Match match = Regex.Match(line, @"\d+");
                if (match.Success) currentState = int.Parse(match.Value);
            }

            string lower = line.ToLower();
            if (lower.Contains("type") && (lower.Contains("explod") || lower.Contains("helper")))
            {
                int targetAnimID = currentState; 
                if (stateToAnimMap.ContainsKey(currentState)) targetAnimID = stateToAnimMap[currentState];
                ParseEffectBlock(lines, i, targetAnimID); 
            }
        }
    }

    void ParseEffectBlock(string[] allLines, int startIndex, int targetAnimID)
    {
        int fxID = -1;
        float triggerTime = 0f; 
        
        for (int j = startIndex; j < startIndex + 30; j++) 
        {
            if (j >= allLines.Length) break;
            string subLine = allLines[j].Trim().ToLower();
            if (j > startIndex && subLine.StartsWith("[state")) break;

            if ((subLine.StartsWith("anim") || subLine.StartsWith("stateno")) && subLine.Contains("="))
            {
                string val = subLine.Split('=')[1].Trim();
                if (!val.StartsWith("f")) 
                {
                    Match match = Regex.Match(val, @"\d+");
                    if(match.Success && long.TryParse(match.Value, out long longID)) 
                    {
                        if (longID > int.MaxValue) fxID = int.MaxValue;
                        else fxID = (int)longID;
                    }
                }
            }

            if (subLine.Contains("animelem") && subLine.Contains("="))
            {
                Match match = Regex.Match(subLine.Split('=')[2], @"\d+");
                if(match.Success && int.TryParse(match.Value, out int frame))
                    triggerTime = (frame - 1) / 60f; 
            }
        }

        if (fxID != -1) InjectEvent(targetAnimID.ToString(), fxID, triggerTime);
    }

    void InjectEvent(string animID, int fxID, float time)
    {
        string[] guids = AssetDatabase.FindAssets($"{animID} t:AnimationClip", new[] { animationPath });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string filename = Path.GetFileName(path); 
            
            // Strict Match
            if (!Regex.IsMatch(filename, $"^{animID}[^0-9]")) continue;

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) continue;

            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            foreach (var e in events) if (e.functionName == "SpawnVFX" && e.stringParameter == fxID.ToString()) return;

            AnimationEvent evt = new AnimationEvent();
            evt.functionName = "SpawnVFX";
            evt.stringParameter = fxID.ToString();
            evt.time = Mathf.Max(0, time);

            ArrayUtility.Add(ref events, evt);
            AnimationUtility.SetAnimationEvents(clip, events);
            EditorUtility.SetDirty(clip);
            Debug.Log($"<color=cyan>Linked!</color> {filename} -> FX {fxID} at {evt.time}s");
            return; 
        }
    }
}