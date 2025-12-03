using UnityEngine;

public class FighterInput : MonoBehaviour
{
    public Animator animator;
    //public Rigidbody2D myRigidBody;
    // Assign in Inspector or use default keys
    [Header("Punch Keys")]
    public KeyCode lowPunchKey = KeyCode.J;
    public KeyCode mediumPunchKey = KeyCode.K;
    public KeyCode highPunchKey = KeyCode.L;

    [Header("Kick Keys")]
    public KeyCode lowKickKey = KeyCode.U;
    public KeyCode mediumKickKey = KeyCode.I;
    public KeyCode highKickKey = KeyCode.O;

    [Header("Movement Keys")]
    public KeyCode walkForwardKey = KeyCode.D;
    public KeyCode walkBackwardKey = KeyCode.A;
    public KeyCode jumpKey = KeyCode.W;
    public KeyCode crouchKey = KeyCode.S;

    void Update()
    {
        HandlePunchInputs();
        HandleKickInputs();
    }

    void HandlePunchInputs()
    {
        if (Input.GetKeyDown(lowPunchKey))
            LowPunch();

        if (Input.GetKeyDown(mediumPunchKey))
            MediumPunch();

        if (Input.GetKeyDown(highPunchKey))
            HighPunch();
    }

    void HandleKickInputs()
    {
        if (Input.GetKeyDown(lowKickKey))
            LowKick();

        if (Input.GetKeyDown(mediumKickKey))
            MediumKick();

        if (Input.GetKeyDown(highKickKey))
            HighKick();
    }

    void LowPunch()   { 
        Debug.Log("LP");
        animator.SetTrigger("Light Punch");
        }
    void MediumPunch(){ 
        Debug.Log("MP");
        animator.SetTrigger("Medium Punch");
    }
    void HighPunch()  { 
        Debug.Log("HP");
        animator.SetTrigger("Heavy Punch");
    }

    void LowKick()    { 
        Debug.Log("LK");
        animator.SetTrigger("Light Kick");
    }
    void MediumKick() { 
        Debug.Log("MK");
        animator.SetTrigger("Medium Kick");
    }
    void HighKick() { 
        Debug.Log("HK");
        animator.SetTrigger("Heavy Kick");
    }

    
}

