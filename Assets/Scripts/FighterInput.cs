using UnityEngine;

public class FighterInput : MonoBehaviour
{
    public Animator animator;
    //public Rigidbody2D myRigidBody;
    // Assign in Inspector or use default keys
    
    private KeyCode lowPunchKey = KeyCode.J;
    private KeyCode mediumPunchKey = KeyCode.K;
    private KeyCode highPunchKey = KeyCode.L;

    
    private KeyCode lowKickKey = KeyCode.I;
    private KeyCode mediumKickKey = KeyCode.O;
    private KeyCode highKickKey = KeyCode.P;


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
        AudioManager.Instance.PlayPunch();
        }
    void MediumPunch(){ 
        Debug.Log("MP");
        AudioManager.Instance.PlayKick();
    }
    void HighPunch()  { 
        Debug.Log("HP");
        AudioManager.Instance.PlayHit();
    }

    void LowKick()    { 
        Debug.Log("LK");
        AudioManager.Instance.PlaySpecial();
        AudioManager.Instance.VoiceLines();
    }
    void MediumKick() { 
        Debug.Log("MK");
        AudioManager.Instance.PlaySpecial();
        AudioManager.Instance.VoiceLines();
    }
    void HighKick() { 
        Debug.Log("HK");
        AudioManager.Instance.PlaySpecial();
        AudioManager.Instance.VoiceLines();
    }






    
}

