using UnityEngine;

public class AnimDebugger : MonoBehaviour
{
    void Start()
    {
        Animator anim = GetComponent<Animator>();
        if (anim == null) 
        {
            Debug.LogError("NO ANIMATOR COMPONENT FOUND ON FX_PUNCH!");
            return;
        }

        // Force check if the state exists
        bool hasState = anim.HasState(0, Animator.StringToHash("Action_1552"));
        
        if (hasState)
        {
            Debug.Log("<color=green>SUCCESS: Animator confirms 'Action_1552' exists!</color>");
        }
        else
        {
            Debug.LogError("<color=red>FAILURE: Animator DOES NOT see 'Action_1552'. Check your spelling in the Animator Window!</color>");
        }
    }
}