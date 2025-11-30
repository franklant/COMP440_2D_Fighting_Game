using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

public class MugenEventInjector : EditorWindow
{
    public TextAsset[] cnsFiles; 
    public string targetCharacterName = "Gojo"; // Helps filter if you have multiple chars

    private Dictionary<int, int> stateToAnimMap = new Dictionary<int, int>();

    [MenuItem("Mugen/2. Smart Event Injector (Robust)")]
    public static void ShowWindow() => GetWindow<MugenEventInjector>("Event Injector");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Robust Event Injector", EditorStyles.boldLabel);
        
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty stringsProperty = so.FindProperty("cnsFiles");
        EditorGUILayout.PropertyField(stringsProperty, true);
        so.ApplyModifiedProperties();

        targetCharacterName = EditorGUILayout.TextField("Char Name Filter", targetCharacterName);

        GUILayout.Space(10);
        if (GUILayout.Button("Inject All Events")) RunInjection();
    }

    void RunInjection()
    {
        Debug.Log("<b>[Injector]</b> Starting Scan...");
        stateToAnimMap.Clear();
        
        // 1. Scan CNS files to map States to Animations
        foreach (var file in cnsFiles) 
        {
            if(file != null) ScanForMappings(file.text);
        }
        
        // 2. Parse and Inject
        int totalInjections = 0;
        foreach (var file in cnsFiles) 
        {
            if(file != null) totalInjections += ParseAndInject(file.text);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"<b>[Injector]</b> Complete! Total Events Injected: {totalInjections}");
    }

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

    int ParseAndInject(string text)
    {
        string[] lines = text.Split('\n');
        int currentState = -1;
        int count = 0;
        
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
            // Look for Explods (VFX) or Helpers
            if (lower.Contains("type") && (lower.Contains("explod") || lower.Contains("helper")))
            {
                int targetAnimID = currentState; 
                if (stateToAnimMap.ContainsKey(currentState)) targetAnimID = stateToAnimMap[currentState];
                
                // Try to find the VFX ID inside this block
                if (ParseEffectBlock(lines, i, targetAnimID)) count++; 
            }
        }
        return count;
    }

    bool ParseEffectBlock(string[] allLines, int startIndex, int targetAnimID)
    {
        int fxID = -1;
        float triggerTime = 0f; 
        
        // Scan the next 30 lines for the parameters of this Explod/Helper
        for (int j = startIndex; j < startIndex + 30; j++) 
        {
            if (j >= allLines.Length) break;
            string subLine = allLines[j].Trim().ToLower();
            if (j > startIndex && subLine.StartsWith("[state")) break; // Stop if we hit next state

            // Find "anim = X" or "stateno = X"
            if ((subLine.StartsWith("anim") || subLine.StartsWith("stateno")) && subLine.Contains("="))
            {
                string val = subLine.Split('=')[1].Trim();
                if (!val.StartsWith("f")) // Ignore float variables
                {
                    Match match = Regex.Match(val, @"\d+");
                    if(match.Success && long.TryParse(match.Value, out long longID)) 
                    {
                        if (longID < int.MaxValue) fxID = (int)longID;
                    }
                }
            }

            // Find Timing (Animelem = X)
            if (subLine.Contains("animelem") && subLine.Contains("="))
            {
                Match match = Regex.Match(subLine.Split('=')[1], @"\d+"); // Fixed split index
                if(match.Success && int.TryParse(match.Value, out int frame))
                    triggerTime = (frame - 1) / 60f; 
            }
        }

        if (fxID != -1) 
        {
            return InjectEvent(targetAnimID.ToString(), fxID, triggerTime);
        }
        return false;
    }

    bool InjectEvent(string animID, int fxID, float time)
    {
        // SEARCH GLOBALLY for the animation clip (Ignores folders)
        // We look for "200" or "200_State"
        string[] guids = AssetDatabase.FindAssets($"{animID} t:AnimationClip", null);
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // FILTER: Make sure it belongs to the character we want (Gojo)
            if (!path.Contains(targetCharacterName)) continue;

            string filename = Path.GetFileName(path); 
            
            // STRICT MATCH: Ensure "200" doesn't match "2000"
            // We expect the file to start with the ID, followed by a non-digit (like '_')
            if (!Regex.IsMatch(filename, $"^{animID}[^0-9]")) continue;

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) continue;

            // Check for duplicates
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            bool alreadyExists = false;
            foreach (var e in events) 
            {
                if (e.functionName == "SpawnVFX" && e.stringParameter == fxID.ToString()) 
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                AnimationEvent evt = new AnimationEvent();
                evt.functionName = "SpawnVFX";
                evt.stringParameter = fxID.ToString();
                evt.time = Mathf.Max(0, time);

                ArrayUtility.Add(ref events, evt);
                AnimationUtility.SetAnimationEvents(clip, events);
                EditorUtility.SetDirty(clip);
                Debug.Log($"<color=green>LINKED:</color> {filename} -> FX {fxID} at {time}s");
                return true; 
            }
        }
        return false;
    }
}