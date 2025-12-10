 using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SimpleSpriteAnimationGenerator : EditorWindow
{
    string spritesFolder = "Assets/Mugen/Sprites";
    string outputFolder = "Assets/MugenAnimations";
    float frameDuration = 0.1f; // seconds per frame

    [MenuItem("Mugen/Simple Animation Generator")]
    public static void Open()
    {
        GetWindow<SimpleSpriteAnimationGenerator>("Sprite Animation Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Generate Animations From Sprite Prefixes", EditorStyles.boldLabel);

        spritesFolder = EditorGUILayout.TextField("Sprites Folder", spritesFolder);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        frameDuration = EditorGUILayout.FloatField("Frame Duration (sec)", frameDuration);

        if (GUILayout.Button("Generate Animations"))
        {
            Generate();
        }
    }

    void Generate()
    {
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { spritesFolder });
        var sprites = new List<Sprite>();

        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null)
                sprites.Add(s);
        }

        // Group by prefix before "-"
        var grouped = sprites
            .GroupBy(s =>
            {
                string name = s.name;
                return name.Split('-')[0];   // "0", "1", "7012", etc.
            });

        foreach (var group in grouped)
        {
            string animName = group.Key; // group number string

            // Sort by frame number after "-"
            var orderedFrames = group.OrderBy(s =>
            {
                string[] parts = s.name.Split('-');
                if (parts.Length < 2) return 0;
                int.TryParse(parts[1], out int n);
                return n;
            }).ToList();

            CreateAnimationClip(animName, orderedFrames);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Done generating grouped animations!");
    }

    void CreateAnimationClip(string animName, List<Sprite> frames)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 1f / frameDuration;

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        var keyframes = new ObjectReferenceKeyframe[frames.Count];

        for (int i = 0; i < frames.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i * frameDuration,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        // Enable looping
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string savePath = $"{outputFolder}/{animName}.anim";
        AssetDatabase.CreateAsset(clip, savePath);

        Debug.Log($"Created animation: {savePath}");
    }
}
