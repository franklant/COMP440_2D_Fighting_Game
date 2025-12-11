using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class ButtonControllerScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Button Controller Attributes")]
    public Button button;
    public GameObject characterSlotContainer;
    public CharacterSlotContainterScript containterScript;
    public GameObject[] characterSlots;
    public bool isReady;
    public bool isButtonHovered;
    public string selectedCharacter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (characterSlotContainer == null)
        {
            Debug.LogError("Cannot find character slot container in Button Controller!");
        }

        containterScript = characterSlotContainer.GetComponent<CharacterSlotContainterScript>();

        if (containterScript == null)
        {
            Debug.LogError("Could not get character container script");
        }

        StartCoroutine(WaitForSlotActive());

        if (characterSlots == null || characterSlots.Length == 0)
        {
            Debug.LogError("Could not succesfully grab character slots");
        }

        button.onClick.AddListener(SwitchScene);
        button.gameObject.SetActive(false);
    }

    IEnumerator WaitForSlotActive()
    {
        Debug.Log("Waiting for slots to fill...");
        yield return new WaitUntil(() => containterScript.GetCharacterSlots() != null);
        characterSlots = containterScript.GetCharacterSlots();
        Debug.Log("Slots was filled!");
    }

    IEnumerator WaitForSceneSwitch()
    {
        Debug.LogWarning("Waiting for a scene switch");
        if (isReady)
        {
            Debug.Log("Character <color=green>" + selectedCharacter + "</color> selected.");

            /// !! remove once characters are added !! ///
            // if (selectedCharacter == "Madara" || selectedCharacter == "Luffy")
            //     selectedCharacter = "Naruto";
            
            PlayerPrefs.SetString("selectedCharacter", selectedCharacter);  // set the selected character, to be used in the final screen
            SceneManager.LoadScene("TestSelectedCharacterScene");
            //StartCoroutine(WaitForSceneSwitch());
        } else
        {
            Debug.Log("<color=red>No character selected.</color>");
        }
        yield return new WaitForSeconds(1f * Time.deltaTime);
        Debug.LogWarning("Switch uncucessful.");
    }

    // Update is called once per frame
    void Update()
    {
        if (!isReady)
        {
            button.gameObject.SetActive(false);
            selectedCharacter = "";
        }

        foreach (GameObject characterSlot in characterSlots)
        {
            CharacterSlotScript slotScript = characterSlot.GetComponent<CharacterSlotScript>();
            if (slotScript == null) { Debug.LogError("Could not fetch character slot's script"); }

            if (slotScript.isSelected)
            {
                button.gameObject.SetActive(true);
                selectedCharacter = slotScript.characterName;
                isReady = true;
                // Debug.Log("HELLO");
                break;
            } else
            {
                isReady = false;
            } 
            //stillSelected = false;
        }


        // prevents the button from disappearing before the scene has a chance to switch.
        //if (isReady) { StartCoroutine(WaitForSceneSwitch());}
        // if (!isReady && !isButtonHovered)
        // {
        //     button.gameObject.SetActive(false);
        //     selectedCharacter = "";
        // }
    }

    public void SwitchScene()
    {
        Debug.Log("CLICK");
        //StartCoroutine(WaitForSceneSwitch());
        if (isReady)
        {
            Debug.Log("Character <color=green>" + selectedCharacter + "</color> selected.");

            /// !! remove once characters are added !! ///
            // if (selectedCharacter == "Madara" || selectedCharacter == "Luffy")
            //     selectedCharacter = "Naruto";
            
            PlayerPrefs.SetString("selectedCharacter", selectedCharacter);  // set the selected character, to be used in the final screen
            SceneManager.LoadScene("TestSelectedCharacterScene");
            //StartCoroutine(WaitForSceneSwitch());
        } else
        {
            Debug.Log("<color=red>No character selected.</color>");
        }
        // Switch to the next scene (level select)
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        Debug.LogWarning("Cursor over the button.");
        isButtonHovered = true;
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        Debug.LogWarning("Cursor no longer over the button");
        isButtonHovered = false;
    }
    
    
}
