using Mono.Cecil;
using UnityEngine;

/// <summary>
/// CharacterSlotContainerScript manages each of the character slots within the 'Character Slot Container' object.
/// Slots are assigned an ID at the start of execution. In editor they appear from top to bottom, numbered from highest to lowest.
/// </summary>
public class CharacterSlotContainterScript : MonoBehaviour
{
    [Header("Container Attributes")]
    [Tooltip("The current number of cards within the container.")]
    public int currentNumberOfCards = 0;
    private GameObject[] characterSlots;     // used to get the game object of the character slot


    /// <summary>
    /// Updates each slot in the container with it's own unique ID.
    /// It also updates the current number of cards within the container.
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

            slotScript.ID = i;
            currentNumberOfCards += 1;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterSlots = GameObject.FindGameObjectsWithTag("CharacterSlot");
        if (characterSlots.Length == 0)
            Debug.LogWarning("There are no cards in the container!");

        UpdateSlotIDs();
        // Debug.Log(characterSlots.Length);

        if (currentNumberOfCards == 0)
            Debug.LogWarning("Number of slots is currently ZERO!");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
