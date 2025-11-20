using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// CharacterSlotScript manages the properties of it's specific instance of the 'Character Slot' object.
/// It's ID and position, relative to the screen, is assigned at the start of execution, with the help of the
/// 'CharacterSlotContainerScript'.
/// </summary>
public class CharacterSlotScript : MonoBehaviour
{
    [Header("Slot Attributes")]
    [Tooltip("The 'character slot container' assigns the ID to the slot at the start of execution.")]
    public int ID = -1;
    [Tooltip("The 'character slot container' assigns the position to the slot at the start of execution.")]
    public Vector3 position;
    private GameObject characterSlotContainer;
    private CharacterSlotContainterScript containerScript;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterSlotContainer = GameObject.FindGameObjectWithTag("CharacterSlotContainer");
        if (characterSlotContainer == null)
            Debug.LogError(ID.ToString() + " Cannot find the Character Container Slot!");
        
        containerScript = characterSlotContainer.GetComponent<CharacterSlotContainterScript>();
        if (containerScript == null)
            Debug.LogError(ID.ToString() + " Cannot find Character Container Script!");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
