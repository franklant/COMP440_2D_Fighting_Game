using UnityEditor;
using UnityEngine;

public class SpawnCharacterScript : MonoBehaviour
{
    [Header("Characters to Spawn")]
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
                    // GameObject IGojo = PrefabUtility.InstantiatePrefab(Gojo) as GameObject;
                    // IGojo.transform.position = transform.position;
                    Instantiate(Gojo, transform.position, transform.rotation);
                    break;
                case "Sukuna":
                    // GameObject ISukuna = PrefabUtility.InstantiatePrefab(Sukuna) as GameObject;
                    // ISukuna.transform.position = transform.position;
                    Instantiate(Sukuna, transform.position, transform.rotation);
                    break;
                case "Naruto":
                    // GameObject INaruto = PrefabUtility.InstantiatePrefab(Naruto) as GameObject;
                    // INaruto.transform.position = transform.position;
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
