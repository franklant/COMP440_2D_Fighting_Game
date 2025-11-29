using UnityEditor;
using UnityEngine;

public class SpawnCharacterScript : MonoBehaviour
{
    [Header("Available Characters")]
    public GameObject Gojo;
    public GameObject Sukuna;
    public GameObject Naruto;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string selectedCharacter = PlayerPrefs.GetString("selectedCharacter");

        if (selectedCharacter != null)
        {
            switch (selectedCharacter)
            {
                case "Gojo":
                    Instantiate(Gojo, transform.position, transform.rotation);
                    break;
                case "Sukuna":
                    Instantiate(Sukuna, transform.position, transform.rotation);
                    break;
                case "Naruto":
                    Instantiate(Naruto, transform.position, transform.rotation);
                    break;
                // can add more cases for characters.
            }
        } else
        {
            Debug.LogError("Could not find selected character");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
