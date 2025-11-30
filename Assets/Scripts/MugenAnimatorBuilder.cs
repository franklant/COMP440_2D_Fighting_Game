using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class MugenAnimatorBuilder : EditorWindow
{
    public string characterName = "Gojo";

    [MenuItem("Mugen/3. Animator Controller Builder (Universal)")]
    public static void ShowWindow() => GetWindow<MugenAnimatorBuilder>("Animator Builder");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Universal Controller Builder", EditorStyles.boldLabel);
        characterName = EditorGUILayout.TextField("Character Name", characterName);

        if (GUILayout.Button($"Build {characterName}Controller"))
        {
            BuildController();
        }
    }

    void BuildController()
    {
        string animFolderPath = $"Assets/Resources/Chars/{characterName}/Animations";
        string controllerSavePath = $"Assets/Resources/Chars/{characterName}/{characterName}Controller.controller";

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerSavePath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerSavePath);
        }

        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        rootStateMachine.states = new ChildAnimatorState[0]; 

        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animFolderPath });
        AnimatorState idleState = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            
            AnimatorState state = rootStateMachine.AddState(clip.name);
            state.motion = clip;

            if (clip.name.ToLower().Contains("idle") || clip.name.StartsWith("0_"))
            {
                idleState = state;
            }
        }

        if (idleState != null) rootStateMachine.defaultState = idleState;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"<b>[Builder]</b> Created {controllerSavePath} with {guids.Length} states.");
    }
}