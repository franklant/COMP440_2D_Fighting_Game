using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class MugenAnimatorBuilder : EditorWindow
{
    public string animFolderPath = "Assets/Resources/Chars/Gojo/Animations";
    public string controllerSavePath = "Assets/Resources/Chars/Gojo/GojoController.controller";

    [MenuItem("Mugen/3. Animator Controller Builder")]
    public static void ShowWindow() => GetWindow<MugenAnimatorBuilder>("Animator Builder");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Step 2: Auto-Build Animator Controller", EditorStyles.boldLabel);
        animFolderPath = EditorGUILayout.TextField("Anim Clip Path", animFolderPath);
        controllerSavePath = EditorGUILayout.TextField("Output Path", controllerSavePath);

        if (GUILayout.Button("Build Animator Controller"))
        {
            BuildController();
        }
    }

    void BuildController()
    {
        // 1. Create or Load Controller
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerSavePath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerSavePath);
        }

        // 2. Clear existing states (optional, keeps it clean)
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        rootStateMachine.states = new ChildAnimatorState[0]; 

        // 3. Find all Anim Clips
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animFolderPath });
        
        AnimatorState idleState = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            
            // Name the state exactly the same as the clip (e.g., "200_Jab_Light")
            // This is crucial because CharacterScript.cs calls Play("200_Jab_Light")
            AnimatorState state = rootStateMachine.AddState(clip.name);
            state.motion = clip;

            // Try to auto-detect Idle to set as default
            if (clip.name.ToLower().Contains("idle") || clip.name.StartsWith("0_"))
            {
                idleState = state;
            }
        }

        // 4. Set Default State
        if (idleState != null)
        {
            rootStateMachine.defaultState = idleState;
            Debug.Log($"<b>[Animator Builder]</b> Set Default State to: {idleState.name}");
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"<b>[Animator Builder]</b> Controller Built! Added {guids.Length} states.");
    }
}