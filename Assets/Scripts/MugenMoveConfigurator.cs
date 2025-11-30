using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

public class MugenMoveConfigurator : EditorWindow
{
    public TextAsset specialsFile; // Drag Specials.txt here
    public TextAsset supersFile;   // Drag Supers.txt here
    
    [MenuItem("Mugen/6. Move Configurator (Auto-Fill)")]
    public static void ShowWindow() => GetWindow<MugenMoveConfigurator>("Move Config");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Auto-Fill Move IDs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select your Character in the Scene, drag in their text files, and click Auto-Fill.", MessageType.Info);

        specialsFile = (TextAsset)EditorGUILayout.ObjectField("Specials.txt", specialsFile, typeof(TextAsset), false);
        supersFile = (TextAsset)EditorGUILayout.ObjectField("Supers.txt", supersFile, typeof(TextAsset), false);

        if (GUILayout.Button("Auto-Fill Selected Character"))
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogError("No character selected in Hierarchy!");
                return;
            }

            CharacterScript charScript = selected.GetComponent<CharacterScript>();
            if (charScript == null)
            {
                Debug.LogError("Selected object does not have a CharacterScript!");
                return;
            }

            ConfigureMoves(charScript);
        }
    }

    void ConfigureMoves(CharacterScript charScript)
    {
        Undo.RecordObject(charScript, "Auto-Fill Moves");

        // 1. Parse Specials
        if (specialsFile != null)
        {
            List<int> specialIDs = ExtractStateIDs(specialsFile.text);
            // JUS characters usually use 1000, 1100, 1200, etc. for main moves.
            // We filter for "Round" numbers or just take the first few unique ones.
            
            // Strategy: Take the first 6 unique states found
            for (int i = 0; i < specialIDs.Count; i++)
            {
                string animName = $"{specialIDs[i]}_State";
                if (i == 0) charScript.special1 = animName;
                if (i == 1) charScript.special2 = animName;
                if (i == 2) charScript.special3 = animName;
                if (i == 3) charScript.special4 = animName;
                if (i == 4) charScript.special5 = animName;
                if (i == 5) charScript.special6 = animName;
            }
            Debug.Log($"Found {specialIDs.Count} Specials.");
        }

        // 2. Parse Supers
        if (supersFile != null)
        {
            List<int> superIDs = ExtractStateIDs(supersFile.text);
            
            for (int i = 0; i < superIDs.Count; i++)
            {
                string animName = $"{superIDs[i]}_State";
                if (i == 0) charScript.super1 = animName; // Usually 3000
                if (i == 1) charScript.super2 = animName; // Usually 3100 or 3200
            }
            Debug.Log($"Found {superIDs.Count} Supers.");
        }

        // 3. Set Defaults for Combo Chains (Standard JUS IDs)
        // These are almost always standard across JUS characters
        charScript.groundHit1 = "200_State";
        charScript.groundHit2 = "210_State";
        charScript.groundHit3 = "220_State";
        charScript.groundHit4 = "230_State";
        charScript.groundFinisher = "240_State";
        
        charScript.airHit1 = "600_State";
        charScript.airHit2 = "610_State";
        charScript.airHit3 = "620_State";

        EditorUtility.SetDirty(charScript);
        Debug.Log($"<color=green>SUCCESS:</color> Auto-filled moves for {charScript.gameObject.name}!");
    }

    List<int> ExtractStateIDs(string text)
    {
        List<int> ids = new List<int>();
        string[] lines = text.Split('\n');

        foreach (string line in lines)
        {
            string clean = line.Trim().ToLower();
            // Look for [Statedef 1000]
            if (clean.StartsWith("[statedef"))
            {
                Match match = Regex.Match(clean, @"\d+");
                if (match.Success)
                {
                    int id = int.Parse(match.Value);
                    
                    // Filter: In JUS, main moves are usually multiples of 100 (1000, 1100) 
                    // or multiples of 10 (1000, 1010). 
                    // We exclude helper states which are often random numbers like 1005 or 3050.
                    // HEURISTIC: Keep if divisible by 100 OR it's the very first one found.
                    if (ids.Count == 0 || id % 100 == 0 || id % 50 == 0) 
                    {
                        if (!ids.Contains(id)) ids.Add(id);
                    }
                }
            }
        }
        return ids;
    }
}