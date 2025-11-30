using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class MugenCnsParser : EditorWindow
{
    public TextAsset[] cnsFiles;
    public MugenVfxDatabase database; 

    [MenuItem("Mugen/5. VFX Data Parser (Ultra)")]
    public static void ShowWindow() => GetWindow<MugenCnsParser>("VFX Parser Ultra");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Extracts: Scale, Pos, Postype, Bind, Priority", EditorStyles.boldLabel);
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        EditorGUILayout.PropertyField(so.FindProperty("cnsFiles"), true);
        so.ApplyModifiedProperties();
        database = (MugenVfxDatabase)EditorGUILayout.ObjectField("Database", database, typeof(MugenVfxDatabase), false);

        if (GUILayout.Button("Parse Full Data")) { if (database != null) ParseFiles(); }
    }

    void ParseFiles()
    {
        database.profiles.Clear();
        database.stateMappings.Clear();
        foreach (var file in cnsFiles) if (file != null) ParseText(file.text);
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        Debug.Log($"Parsed {database.profiles.Count} profiles!");
    }

    void ParseText(string text)
    {
        string[] lines = text.Split('\n');
        MugenVfxDatabase.VfxProfile currentProfile = null;
        string currentStateID = "";

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim().ToLower();
            if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;

            // --- STATE MAPPING ---
            if (line.StartsWith("[statedef"))
            {
                Match m = Regex.Match(line, @"\d+");
                if (m.Success) currentStateID = m.Value;
            }
            if (!string.IsNullOrEmpty(currentStateID) && line.StartsWith("anim") && line.Contains("="))
            {
                string val = line.Split('=')[1].Trim();
                if (int.TryParse(val, out int animID)) {
                    if (!database.stateMappings.Exists(x => x.stateID == currentStateID))
                        database.stateMappings.Add(new MugenVfxDatabase.StateMapping { stateID = currentStateID, defaultAnimID = val });
                }
                currentStateID = "";
            }

            // --- VFX PARSING ---
            if (line.StartsWith("type") && (line.Contains("explod") || line.Contains("helper")))
            {
                currentProfile = new MugenVfxDatabase.VfxProfile();
            }

            if (currentProfile != null)
            {
                // ID
                if ((line.StartsWith("anim") || line.StartsWith("stateno")) && line.Contains("=")) {
                    Match m = Regex.Match(line.Split('=')[1], @"\d+");
                    if (m.Success) currentProfile.id = m.Value;
                }
                // Scale
                if (line.StartsWith("scale")) {
                    string[] parts = line.Split('=')[1].Trim().Split(',');
                    if (parts.Length >= 1 && float.TryParse(parts[0], out float x)) {
                        float y = (parts.Length >= 2 && float.TryParse(parts[1], out float resY)) ? resY : x;
                        currentProfile.scale = new Vector3(x, y, 1);
                    }
                }
                // PosType
                if (line.StartsWith("postype")) {
                    if (line.Contains("back")) currentProfile.posType = MugenVfxDatabase.PosType.Back;
                    else if (line.Contains("front")) currentProfile.posType = MugenVfxDatabase.PosType.Front;
                    else if (line.Contains("left")) currentProfile.posType = MugenVfxDatabase.PosType.Left;
                    else if (line.Contains("right")) currentProfile.posType = MugenVfxDatabase.PosType.Right;
                }
                // Position
                if (line.StartsWith("pos") && !line.Contains("type") && !line.Contains("postype")) {
                    string[] parts = line.Split('=')[1].Trim().Split(',');
                    if (parts.Length >= 2 && float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y))
                        currentProfile.offset = new Vector3(x/100f, y/100f, 0);
                }
                // Sorting Order (Priority)
                if (line.StartsWith("sprpriority")) {
                    int.TryParse(Regex.Match(line, @"-?\d+").Value, out currentProfile.sprPriority);
                }
                // BindTime
                if (line.StartsWith("bindtime")) {
                    int.TryParse(Regex.Match(line, @"-?\d+").Value, out currentProfile.bindTime);
                }
                // Facing
                if (line.StartsWith("facing")) {
                    int.TryParse(Regex.Match(line, @"-?\d+").Value, out currentProfile.facing);
                }
                // Additive
                if (line.Contains("trans") && line.Contains("add")) currentProfile.isAdditive = true;

                // Save
                if (line.StartsWith("[state")) {
                    if (!string.IsNullOrEmpty(currentProfile.id)) database.profiles.Add(currentProfile);
                    currentProfile = null;
                }
            }
        }
    }
}