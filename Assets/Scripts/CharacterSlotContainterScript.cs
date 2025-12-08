using UnityEngine;
using UnityEngine.Serialization;
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
    private Color[] colors = {Color.green, Color.greenYellow, Color.blue, Color.red, Color.orange};


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
    /// global float row to store the current row of slot containers
    int row = 0;
    int slotsPerRow = 3;
    int z = 1;
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


            // start a new row
            if (slotIndex >= 3)
            {
                slotIndex = 0;
                row += 1;
                z++;
                int cardRemainder = currentNumberOfSlots - slotsPerRow;

                if (cardRemainder < slotsPerRow)
                {
                    slotsPerRow = cardRemainder;
                }
            }

            // calculate the relative postition based on the amout of slots in the container
            float relativeX = ((slotsPerRow - 1) * scale.x) / 2;  

            float offsetX = slotIndex * scale.x;
            float offsetY = row * scale.y + 0.2f;

            Vector3 finalPosition = new Vector3(relativeX - offsetX, transform.position.y - offsetY, z);

            slotScript.SetPosition(finalPosition);

            if (row >= 1)
                slotScript.UpdateOrderInLayer(row);

            // set the slot color as well
            // int colorIndex = Random.Range(0, colors.Length);
            // slotScript.SetBackgroundColor(colors[colorIndex]);

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

    /// <summary>
    /// Returns an array of character slots.
    /// </summary>
    /// <returns>Returns the character slots within the container as an array of game objects.</returns>
    public GameObject[] GetCharacterSlots()
    {
        return characterSlots;
    }

    // Update is called once per frame
    void Update()
    {
        // foreach (GameObject characterSlot in characterSlots)
        // {
        //     CharacterSlotScript slotScript = characterSlot.GetComponent<CharacterSlotScript>();
        //     if (slotScript == null) { Debug.LogError("Could not fetch character slot's script"); }

        //     if (slotScript.isHovering && Input.GetMouseButton(0))
        //     {
        //         slotScript.isSelected = true;
        //         break;
        //     }

        //     if (slotScript.isSelected && !slotScript.isHovering && Input.GetMouseButton(0))
        //     {
        //         slotScript.isSelected = false;
        //         break;
        //     }
        // }
    }
}
