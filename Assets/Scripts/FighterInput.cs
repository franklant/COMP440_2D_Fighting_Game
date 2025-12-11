using UnityEngine;

public class FighterInput : MonoBehaviour
{
    [Header("Settings")]
    public bool isPlayer1 = true;
    public bool isGojo = false;
    public bool isSukuna = false;
    public bool isNaruto = false;
    public bool isMadara = false;
    public bool isLuffy = false;
    public Animator animator;
    //public Rigidbody2D myRigidBody;
    // Assign in Inspector or use default keys
    


    void Update()
    {
        HandleBasicInputs();
        HandleSpecialInputs();
    }

    void HandleBasicInputs()
    {
        if(isPlayer1){
            if (Input.GetKeyDown(KeyCode.J))
                Punch();

            if (Input.GetKeyDown(KeyCode.K))
                Kick();
        }
        else{
            if (Input.GetKeyDown(KeyCode.G))
                Punch();

            if (Input.GetKeyDown(KeyCode.H))
                Kick();
        }
    }

    void HandleSpecialInputs()
    {
        if (isPlayer1)
        {
            if (Input.GetKeyDown(KeyCode.I))
                Special1();
                

            if (Input.GetKeyDown(KeyCode.O))
                Special2();
                

            if (Input.GetKeyDown(KeyCode.P))
                Special3();
                
        }
        else{
            if (Input.GetKeyDown(KeyCode.R))
                Special1();
                

            if (Input.GetKeyDown(KeyCode.T))
                Special2();
                

            if (Input.GetKeyDown(KeyCode.Y))
                Special3();
               
        }
    }

    void Punch()   { 
        Debug.Log("P");
        animator.SetTrigger("Attack");
        AudioManager.Instance.PlayPunch();
        }
    void Kick(){ 
        Debug.Log("K");
        animator.SetTrigger("Kick");
        AudioManager.Instance.PlayKick();
    }

    void Special1()    { 
        Debug.Log("Sp1");
        animator.SetTrigger("Meter1");
        AudioManager.Instance.PlaySpecial();
        VoiceLines();
    }
    void Special2() { 
        Debug.Log("Sp2");
        animator.SetTrigger("Meter2");
        AudioManager.Instance.PlaySpecial();
        VoiceLines();
    }
    void Special3() { 
        Debug.Log("Sp3");
        animator.SetTrigger("Meter3");
        AudioManager.Instance.PlaySpecial();
        VoiceLines();
    }

    void VoiceLines()
    {
        if (isGojo)
        {
            AudioManager.Instance.GojoVL();
        }
        else if (isNaruto)
        {
            AudioManager.Instance.NarutoVL();
        }
        else if (isSukuna)
        {
            AudioManager.Instance.SukunaVL();
        }
        else if (isMadara)
        {
            AudioManager.Instance.MadaraVL();
        }
        else if (isLuffy)
        {
            AudioManager.Instance.LuffyVL();
        }
    }
}

