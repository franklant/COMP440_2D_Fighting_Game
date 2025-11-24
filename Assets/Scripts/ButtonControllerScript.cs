using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class ButtonControllerScript : MonoBehaviour
{
    [Header("Button Controller Attributes")]
    public Button button;
    public GameObject characterSlotContainer;
    public CharacterSlotContainterScript containterScript;
    public GameObject[] characterSlots;
    public bool isReady;
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
    }

    IEnumerator WaitForSlotActive()
    {
        Debug.Log("Waiting for slots to fill...");
        yield return new WaitUntil(() => containterScript.GetCharacterSlots() != null);
        characterSlots = containterScript.GetCharacterSlots();
        Debug.Log("Slots was filled!");
    }

    // Update is called once per frame
    void Update()
    {
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
            } 

            // if (!slotScript.isSelected)
            // {
            //     // button.gameObject.SetActive(false);
            //     // selectedCharacter = "";
            // }
        }
    }

    public void SwitchScene()
    {
        // Switch to the next scene (level select)
        if (isReady)
        {
            Debug.Log("Character <color=green>" + selectedCharacter + "</color> selected.");
            SceneManager.LoadScene("Player vs CPU");
        } else
        {
            Debug.Log("<color=red>No character selected.</color>");
        }
    }
}
