using Mono.Cecil;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

// TODO: Save the coordinates of the slots original position so that we can refer to it when adding hover effects.
// TODO: Fix padding so the slots succesfully align on the horizontal axis of the scree, ordered starting from the center of the 
//       screen to the ends of both sides. i.e. [ - - - - - | - - - - - ]
//       Where ' | ' is the center slot and ' - ' is the corresponding slots on either end.

/// <summary>
/// CharacterSlotContainerScript manages each of the character slots within the 'Character Slot Container' object.
/// Slots are assigned an ID at the start of execution. In editor they appear from top to bottom, numbered from highest to lowest.
/// </summary>
public class CharacterSlotContainterScript : MonoBehaviour
{
    [Header("Container Attributes")]
    [Tooltip("The current number of slots within the container.")]
    public int currentNumberOfSlots = 0;

    [Tooltip("The amount of padding in between each slot.")]
    public int padding = 1;            // pixel padding
    private GameObject[] characterSlots;     // used to get the game object of the character slot


    /// <summary>
    /// Updates each slot in the container with it's own unique ID.
    /// It also updates the current number of slots within the container.
    /// </summary>
    void UpdateSlotIDs()
    {
        for (int i = 0; i < characterSlots.Length; i++)
        {
            CharacterSlotScript slotScript = characterSlots[i].GetComponent<CharacterSlotScript>();
            if (slotScript == null)
            {
                Debug.LogError("Cannot update " + characterSlots[i].name + "'s ID");
                break;
            }

            slotScript.SetID(i);
            currentNumberOfSlots += 1;
        }
    }

    /// <summary>
    /// Updates the position of each slot relative to the number of slots in the container and the size of each slot.
    /// </summary>
    void UpdateSlotPositions()
    {
        int slotIndex = 0;          // keep track of the current slot index

        foreach (GameObject characterSlot in characterSlots)
        {
            Vector3 scale = Vector3.zero;
            CharacterSlotScript slotScript = characterSlot.GetComponent<CharacterSlotScript>();
            if (slotScript == null)
            {
                Debug.LogError("Cannot update " + characterSlot.name + "'s Position");
                break;
            }

            GameObject border = slotScript.GetBorder();

            // Debug.Log("Instance Name: <color=yellow>" + characterSlot.name + "</color>.");
            // Debug.Log("Border Name: <color=yellow>" + border.transform.parent.gameObject.name + "</color>.");

            if (characterSlot.name.Equals(border.transform.parent.gameObject.name))
            {
                // <color=red>Error: </color>
                Debug.Log("<color=green>Succesfully caught our border component!</color>");
            } else
            {
                Debug.Log("<color=red>Unable to verify border component!</color>");
            }

            scale = border.transform.localScale;
            
            float relativeX = (-currentNumberOfSlots * scale.x * padding) / 2;      // calculate the relative postition based on the amout of cards  

            float offsetX = slotIndex * scale.x + (slotIndex * padding);
            Vector3 finalPosition = new Vector3(offsetX + relativeX, 0, 0);
            slotScript.SetPosition(finalPosition);

            // update the slot index
            slotIndex += 1; 
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterSlots = GameObject.FindGameObjectsWithTag("CharacterSlot");
        if (characterSlots.Length == 0)
            Debug.LogWarning("There are no cards in the container!");

        UpdateSlotIDs();
        UpdateSlotPositions();

        if (currentNumberOfSlots == 0)
            Debug.LogWarning("Number of slots is currently ZERO!");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
