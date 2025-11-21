using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Animator animator; // Drag your Animator component here in Inspector

    void Update()
    {
        // Check if the 'A' key is being held down
        bool holdingBlock = Input.GetKey(KeyCode.A);

        // Tell the Animator the result
        animator.SetBool("IsBlocking", holdingBlock);


        // Jab1
        bool hitJab = Input.GetKey(KeyCode.J); 

        animator.SetBool("IsJab", hitJab);


        // Jab2
        bool hitJab_2 = Input.GetKey(KeyCode.K); 

        animator.SetBool("IsJab2", hitJab_2);

        bool walking = Input.GetKey(KeyCode.D);

        animator.SetBool("IsWalking", walking);

        bool crouching = Input.GetKey(KeyCode.S);

        animator.SetBool("IsCrouch", crouching);
        


    }
}