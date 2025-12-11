using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

// NOTE: 11.5 (x-axis) center of the canvas on horizontal
//      -6.35 (y-axis) center of the canvas on the vertical

/// <summary>
/// CharacterSlotScript manages the properties of it's specific instance of the 'Character Slot' object.
/// It's ID and position, relative to the screen, is assigned at the start of execution, with the help of the
/// 'CharacterSlotContainerScript'.
/// </summary>
public class CharacterSlotScript : MonoBehaviour
{
    [Header("Slot Attributes")]
    [Tooltip("The name of the character in this slot.")]
    public string characterName;
    [Tooltip("The 'character slot container' assigns the ID to the slot at the start of execution.")]
    public int ID = -1;
    [Tooltip("The 'character slot container' assigns the position to the slot at the start of execution.")]
    public Vector3 position;
    [Tooltip("Assigned to true or false based on the mouse's position over the slot.")]
    public bool isHovering;
    public bool playHover;
    public bool isSelected;
    [Header("Sound")]
    public AudioSource Audio;
    public AudioClip SelectSfx;
    public AudioClip HoverSfx;
    private GameObject characterSlotContainer;
    private CharacterSlotContainterScript containerScript;
    private GameObject myBorder;


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
    /// Used to get the original position of this instance.
    /// The original position is determined by the 'original position' object.
    /// </summary>
    /// <returns>A transform object for the original position.</returns>
    public Transform GetOriginalPosition()
    {
        Transform originalPosition = transform.GetChild(2);          // the border component should be the first component
        //Debug.Log("Border Name: <color=yellow>" + border.name + "</color>");

        if (originalPosition == null)
            Debug.LogError("Could not find the original position!");
        
        return originalPosition;
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
    /// Sets the background color for this instance.
    /// </summary>
    /// <param name="color">The 'Color' object used to set the background color of this instance.</param>
    public void SetBackgroundColor(Color color)
    {
        GameObject background = GetBackground();
        SpriteRenderer backgroundRenderer = background.GetComponent<SpriteRenderer>();

        if (backgroundRenderer == null)
            Debug.LogError("Could not find the 'Background' object Sprite Renderer!");

        // Set the color
        backgroundRenderer.color = color;
    }

    /// <summary>
    /// Sets the border color for this instance.
    /// </summary>
    /// <param name="color">The 'Color' object used to set the border color of this instance.</param>
    public void SetBorderColor(Color color)
    {
        GameObject border;

        // Grab our instance of the border if we don't have it already.
        if (myBorder == null)
            border = GetBorder();
        else
            border = myBorder;
        
        SpriteRenderer borderRenderer = border.GetComponent<SpriteRenderer>();

        if (borderRenderer == null)
            Debug.LogError("Could not find the 'Border' object Sprite Renderer!");

        // Set the color
        borderRenderer.color = color;
    }
    
    /// <summary>
    /// Translates the position of this instance's background and border object by a certain amount.
    /// </summary>
    /// <param name="moveAmount">The amount specified to move the border and background.</param>
    public void MoveBorderBackground(float moveAmount)
    {
        GameObject border;
        if (myBorder == null)
            border = GetBorder();
        else
            border = myBorder;

        GameObject background = GetBackground();

        Vector3 movePosition = new Vector3(0, moveAmount, 0) + transform.position;
        background.transform.position = Vector3.Lerp(
            background.transform.position,
            movePosition,
            0.1f
        );  // smooth translation

        border.transform.position = Vector3.Lerp(
            border.transform.position,
            movePosition,
            0.1f
        );  // smooth translation
    }

    /// <summary>
    /// Resets the position of the 'Border' and 'Background' objects to the original position of this slot instance.
    /// </summary>
    public void ResetBorderBackgroundPosition()
    {
        GameObject border;
        if (myBorder == null)
            border = GetBorder();
        else
            border = myBorder;

        GameObject background = GetBackground();

        Transform originalPosition = GetOriginalPosition();
        // background.transform.position = originalPosition.position;
        // border.transform.position = originalPosition.position;

        background.transform.position = Vector3.Lerp(
            background.transform.position,
            originalPosition.position,
            0.1f
        );  // smooth reset translation

        border.transform.position = Vector3.Lerp(
            border.transform.position,
            originalPosition.position,
            0.1f
        );  // smooth reset translation
    }

    public void UpdateOrderInLayer(int amount)
    {
        SpriteRenderer bgSpriteRenderer = GetBackground().GetComponent<SpriteRenderer>();
        SpriteRenderer bdSpriteRenderer = GetBorder().GetComponent<SpriteRenderer>();

        bgSpriteRenderer.sortingOrder += 2 + amount;
        bdSpriteRenderer.sortingOrder += 2 + amount;
    }

    /// <summary>
    /// Returns the character name of the slot. Used for selecting characters.
    /// </summary>
    /// <returns>If the name of the slot is not empty, returns the name of the character as a string. If it is, returns "NoName".</returns>
    public string GetCharacterName()
    {
        if (name != null || name != "")
        {
            return characterName;
        }
        return "NoName";
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
        isSelected = false;
        myBorder = GetBorder();
    }

    // Update is called once per frame
    void Update()
    {
        // Vector3 mousePosition = Input.mousePosition;
        // Debug.Log(mousePosition);

        if (isHovering)
        {
            SetBorderColor(Color.white);
            MoveBorderBackground(1f);
        } else
        {
            SetBorderColor(Color.black);
            ResetBorderBackgroundPosition();
        }

        // if (playHover)
        // {
        //     HoverSfx.Play();
        //     playHover = false;
        // }

        // character is being selected
        if (isHovering && Input.GetMouseButtonUp(0))
        {
            isSelected = true;

            // if (HoverSfx.isPlaying)
            //     HoverSfx.Stop();
            // if (SelectSfx.isPlaying)
            //     SelectSfx.Stop();
            
            Audio.PlayOneShot(SelectSfx);
        } 
        else if (!isHovering && Input.GetMouseButtonUp(0))
        {
            isSelected = false;
        }

        if (isSelected)
        {
            SetBorderColor(Color.yellow);
        }
    }

    void OnMouseEnter()
    {
        Debug.Log("Hovering over <color=blue>" + name + "</color> !");
        isHovering = true;
        Audio.PlayOneShot(HoverSfx);
    }

    void OnMouseExit()
    {
        Debug.Log("No longer hovering over <color=blue>" + name + "</color> !");
        isHovering = false;
    }
}
