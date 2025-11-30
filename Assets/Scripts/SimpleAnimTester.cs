using UnityEngine;

public class SimpleAnimTester : MonoBehaviour
{
    public Animator anim;

    [Header("--- Animator Triggers ---")]
    public string jabState = "Attack";    
    public string kickState = "Kick";
    public string meter1State = "Meter1";
    public string meter2State = "Meter2";
    public string meter3State = "Meter3";
    public string idleState = "Idle"; 
    public string jumpState = "Jump";
    public string fallState = "Fall";

    void Update()
    {
        // --- INPUTS ---
        if (Input.GetKeyDown(KeyCode.J)) PlayAnim(jabState);
        if (Input.GetKeyDown(KeyCode.K)) PlayAnim(kickState);
        if (Input.GetKeyDown(KeyCode.I)) PlayAnim(meter1State);
        if (Input.GetKeyDown(KeyCode.O)) PlayAnim(meter2State);
        if (Input.GetKeyDown(KeyCode.P)) PlayAnim(meter3State);
        
        if (Input.GetKeyDown(KeyCode.Space)) PlayAnim(jumpState);
        if (Input.GetKeyDown(KeyCode.F)) PlayAnim(fallState);
        
        // Force Idle Manual Reset
        if (Input.GetKeyDown(KeyCode.L)) PlayAnim(idleState);

        // --- THE FIX: AUTOMATIC RETURN LOGIC ---
        CheckAutoReturn();
    }

    void PlayAnim(string stateName)
    {
        Debug.Log($"Testing Play('{stateName}')...");
        anim.Play(stateName, 0, 0f);
    }

    void CheckAutoReturn()
    {
        // 1. Get info about what is currently playing
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);

        // 2. Check if we are playing an Attack or Special
        // (We check IsName to see if we are stuck in a move)
        bool isAttacking = info.IsName(jabState) || 
                           info.IsName(kickState) || 
                           info.IsName(meter1State) || 
                           info.IsName(meter2State) || 
                           info.IsName(meter3State);

        // 3. If we are attacking AND the animation is done (normalizedTime >= 1.0)
        if (isAttacking && info.normalizedTime >= 1.0f)
        {
            Debug.Log("Animation Finished. Returning to Idle automatically.");
            anim.Play(idleState);
        }
    }
}