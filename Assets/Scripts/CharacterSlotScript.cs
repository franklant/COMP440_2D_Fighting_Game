using System.Diagnostics.CodeAnalysis;
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


    /// <summary>
    /// Used by the 'CharacterSlotContainerScript' to get the border of this instance.
    /// The border is used to calculate the position and internal padding of each slot.
    /// </summary>
    /// <returns>A game object for the border of this instance.</returns>
    public GameObject GetBorder()
    {
        GameObject border = transform.GetChild(0).gameObject;          // the border component should be the first component
        //Debug.Log("Border Name: <color=yellow>" + border.name + "</color>");

        if (border == null)
            Debug.LogError("Could not find the border!");
        
        return border;
    }

    /// <summary>
    /// Used to get the background of this instance.
    /// </summary>
    /// <returns>A game object for the background of this instance.</returns>
    public GameObject GetBackground()
    {
        GameObject background = transform.GetChild(1).gameObject;          // the border component should be the first component
        //Debug.Log("Border Name: <color=yellow>" + border.name + "</color>");

        if (background == null)
            Debug.LogError("Could not find the background!");
        
        return background;
    }

    /// <summary>
    /// Used by the 'CharacterSlotContainerScript' to set the ID of this instance. 
    /// </summary>
    /// <param name="sID">The ID value used to set the ID of this instance.</param>
    public void SetID(int sID)
    {
        ID = sID;
    }

    /// <summary>
    /// Used by the 'CharacterSlotContainerScript' to set the position of this instance.
    /// </summary>
    /// <param name="sPosition">The position value used to set the position this instance.</param>
    public void SetPosition(Vector3 sPosition)
    {
        transform.position = sPosition;
        position = transform.position;      // debug
    }

    /// <summary>
    /// - PROTOTYPE - Sets the background color for this instance.
    /// </summary>
    /// <param name="color">The 'Color' object used to set the background color of this instance.</param>
    public void SetBackgroundColor(Color color)
    {
        GameObject background = GetBackground();
        SpriteRenderer backgroundRenderer = background.GetComponent<SpriteRenderer>();

        if (backgroundRenderer == null)
            Debug.LogError("Could not find the Background object Sprite Renderer!");

        // Set the color
        backgroundRenderer.color = color;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterSlotContainer = GameObject.FindGameObjectWithTag("CharacterSlotContainer");
        if (characterSlotContainer == null)
            Debug.LogError(ID.ToString() + " Cannot find the Character Container Slot!");
        
        containerScript = characterSlotContainer.GetComponent<CharacterSlotContainterScript>();
        if (containerScript == null)
            Debug.LogError(ID.ToString() + " Cannot find Character Container Script!");

        // Debug.Log("Set Position: " + position);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
