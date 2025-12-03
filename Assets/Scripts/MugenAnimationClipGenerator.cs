using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MugenAnimationClipGenerator : EditorWindow
{
    string airFilePath = "";
    string spritesFolderPath = "";
    string outputFolder = "Assets/MugenAnimations";

    Dictionary<int, MugenAnimationParser.Animation> animData;

    [MenuItem("Mugen/Generate Animation Clips")]
    public static void OpenWindow()
    {
        GetWindow<MugenAnimationClipGenerator>("Mugen Animation Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("MUGEN AnimationClip Generator", EditorStyles.boldLabel);

        airFilePath = EditorGUILayout.TextField("AIR File (.txt)", airFilePath);
        spritesFolderPath = EditorGUILayout.TextField("Sprites Folder", spritesFolderPath);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        if (GUILayout.Button("Parse AIR"))
        {
            animData = MugenAnimationParser.ParseAirFile(airFilePath);
            Debug.Log($"Loaded {animData.Count} animations.");
        }

        if (animData != null && GUILayout.Button("Generate AnimationClips"))
        {
            GenerateAll();
        }
    }

    void GenerateAll()
    {
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        foreach (var kv in animData)
        {
            int actionId = kv.Key;
            var anim = kv.Value;

            AnimationClip clip = BuildClip(anim);
            string savePath = $"{outputFolder}/Action_{actionId}.anim";

            AssetDatabase.CreateAsset(clip, savePath);
            Debug.Log($"Generated: Action {actionId} → {savePath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All animation clips generated!");
    }

    AnimationClip BuildClip(MugenAnimationParser.Animation anim)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60;

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        List<ObjectReferenceKeyframe> keys = new List<ObjectReferenceKeyframe>();

        float currentTime = 0f;

        foreach (var frame in anim.Frames)
        {
            string spriteName = $"{frame.Group}_{frame.Index}";
            Sprite sprite = LoadSprite(spriteName);

            if (sprite == null)
            {
                Debug.LogWarning($"Missing sprite: {spriteName}");
                continue;
            }

            keys.Add(new ObjectReferenceKeyframe
            {
                time = currentTime,
                value = sprite
            });

            currentTime += frame.Duration / 60f; // convert mugen ticks → seconds
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keys.ToArray());

        return clip;
    }

    Sprite LoadSprite(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name, new[] { spritesFolderPath });

        if (guids.Length == 0) return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
