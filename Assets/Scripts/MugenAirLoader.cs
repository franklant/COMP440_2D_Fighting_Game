using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class MugenAirLoader : EditorWindow
{
    public string spriteFolderPath = "Assets/Resources/Chars/Gojo/Sprites";
    public TextAsset airFile; 

    // HARDCODED MAPPING (Overrides file comments)
    private Dictionary<int, string> actionNames = new Dictionary<int, string>()
    {
        // --- Ground Basics ---
        {0, "Idle"},
        {10, "Crouch"},
        {11, "Crouch_GetUp"},
        {12, "Crouch_Turn"},
        {20, "Walk_Fwd"},
        {21, "Walk_Back"},
        {40, "Jump_Start"},
        {41, "Jump_Up"},
        {42, "Jump_Fwd"},
        {43, "Jump_Back"},
        {180, "Intro_1"},
        {190, "Intro_Loop"},
        
        // --- Attacks (A-Chain) ---
        {200, "Jab_Light"}, 
        {210, "Kick_Light"},
        {220, "Attack_Medium"},
        {230, "Attack_Heavy"},
        {231, "Teleport_CrossUp"}, // From CNS 230->231
        {240, "Launcher_Air"},
        
        // --- Attacks (B-Chain) ---
        {300, "Forward_Strike_1"},
        {310, "Forward_Strike_2"},
        {320, "Forward_Strike_3"},
        {330, "Forward_Strike_4"},
        {340, "Forward_Strike_5"},
        
        {400, "Dash_Attack"}, 

        // --- Air Attacks ---
        {600, "Air_Jab"},
        {610, "Air_Kick"},
        {620, "Air_Heavy"},
        {630, "Air_Combo_End"},

        // --- Specials ---
        {1000, "Passive_Infinity"},
        {1100, "Counter_Start"},
        {1110, "Counter_Hit"},
        {1200, "Lift_Grab_Start"},
        {1210, "Lift_Grab_Air"},
        {1300, "Blue_Orb_Spin"}, 
        {1400, "Simple_Domain"},
        {1500, "Knockback_Burst"},
        {1600, "Red_Backwards"},
        {1700, "Blue_Cast"},
        {1800, "Red_Cast"},
        {1900, "Purple_Hollow"},
        
        // --- Supers ---
        {2000, "Black_Flash_Start"},
        {2001, "Black_Flash_Hit"},
        {3000, "Unlimited_Purple"},
        {3100, "Domain_Expansion_Void"},
        
        // --- Hit Reactions ---
        {5000, "Hit_Stand"},
        {5010, "Hit_Crouch"},
        {5030, "Hit_Air"},
        {5110, "Hit_LieDown"}
    };

    [MenuItem("Mugen/1. Smart Air Loader (Hardcoded)")]
    public static void ShowWindow() => GetWindow<MugenAirLoader>("Air Loader");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Step 1: Generate Named Clips", EditorStyles.boldLabel);
        airFile = (TextAsset)EditorGUILayout.ObjectField("AIR File", airFile, typeof(TextAsset), false);
        spriteFolderPath = EditorGUILayout.TextField("Sprite Path", spriteFolderPath);

        if (GUILayout.Button("Generate Clips"))
        {
            if (airFile == null) return;
            ParseAirAndCreateClips();
        }
    }

    void ParseAirAndCreateClips()
    {
        string text = airFile.text;
        string[] lines = text.Split('\n');
        
        int currentAction = -1;
        List<int[]> frames = new List<int[]>(); 
        bool isLooping = false;

        foreach (string line in lines)
        {
            string clean = line.Trim();
            if (string.IsNullOrEmpty(clean) || clean.StartsWith(";")) continue;

            if (clean.StartsWith("[Begin Action"))
            {
                if (currentAction != -1) BuildClip(currentAction, frames, isLooping);
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
                    // Group, Image, X, Y, Time
                    if (int.TryParse(parts[0], out int group) && 
                        int.TryParse(parts[1], out int image) && 
                        int.TryParse(parts[4], out int time))
                    {
                        frames.Add(new int[] { group, image, time });
                    }
                }
            }
        }
        if (currentAction != -1) BuildClip(currentAction, frames, isLooping);
        AssetDatabase.Refresh();
        Debug.Log("<b>[Air Loader]</b> Processed.");
    }

    void BuildClip(int actionID, List<int[]> animFrames, bool loop)
    {
        if (animFrames.Count == 0) return;

        // Determine Name
        string name = actionNames.ContainsKey(actionID) ? actionNames[actionID] : "Unknown";
        string folder = GetFolder(actionID);

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
            
            // Handle -1 time (infinite/large duration) for Unity
            float duration = (frame[2] == -1) ? 1f : frame[2] / 60f;
            currentTime += duration;
        }

        EditorCurveBinding binding = new EditorCurveBinding();
        binding.type = typeof(SpriteRenderer);
        binding.path = "";
        binding.propertyName = "m_Sprite";
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes.ToArray());

        string fileName = $"{actionID}_{name}.anim";
        string path = $"Assets/Resources/Chars/Gojo/Animations/{folder}/{fileName}";
        
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        AssetDatabase.CreateAsset(clip, path);
    }

    string GetFolder(int id)
    {
        if (id < 200) return "_Basics";
        if (id >= 200 && id < 600) return "_Attacks";
        if (id >= 1000 && id < 3000) return "_Specials";
        if (id >= 3000 && id < 5000) return "_Supers";
        if (id >= 5000) return "_HitReactions";
        return "_Misc";
    }

    Sprite FindSpriteInProject(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name, new[] { spriteFolderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string filename = Path.GetFileNameWithoutExtension(path);
            if (filename == name) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }
}