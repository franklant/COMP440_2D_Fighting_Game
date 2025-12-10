using UnityEngine;

public class SpawnCharacterScript : MonoBehaviour
{
    [Header("UI Connection")]
    // DRAG YOUR UI OBJECT (The one with StatsUIHandler) HERE
    public StatsUIHandler statsUIHandler; 

    [Header("Characters to Spawn")]
    public GameObject Gojo;
    public GameObject Sukuna;
    public GameObject Naruto;
    // NEW CHARACTERS
    public GameObject Madara;
    public GameObject Luffy;

    void Start()
    {
        string selectedCharacter = PlayerPrefs.GetString("selectedCharacter");
        GameObject prefabToSpawn = null;

        // 1. Choose the character
        if (!string.IsNullOrEmpty(selectedCharacter))
        {
            switch (selectedCharacter)
            {
                case "Gojo":
                    prefabToSpawn = Gojo;
                    break;
                case "Sukuna":
                    prefabToSpawn = Sukuna;
                    break;
                case "Naruto":
                    prefabToSpawn = Naruto;
                    break;
                // NEW CASES
                case "Madara":
                    prefabToSpawn = Madara;
                    break;
                case "Luffy":
                    prefabToSpawn = Luffy;
                    break;
                default:
                    Debug.LogWarning("Selected character '" + selectedCharacter + "' not found in Spawn list!");
                    break;
            }
        }
        else
        {
            Debug.LogError("Could not find selected character in PlayerPrefs");
            return;
        }

        // 2. Spawn and Connect
        if (prefabToSpawn != null)
        {
            // Spawn the character
            GameObject newCharacter = Instantiate(prefabToSpawn, transform.position, transform.rotation);

            // Find the Stats Manager on the new character
            FighterStatsManager characterStats = newCharacter.GetComponent<FighterStatsManager>();

            // Connect it to the UI
            if (statsUIHandler != null && characterStats != null)
            {
                statsUIHandler.SetTarget(characterStats);
                Debug.Log($"Spawned {selectedCharacter} and connected to UI.");
            }
            else
            {
                Debug.LogWarning("Missing StatsUIHandler ref or FighterStatsManager on character!");
            }
        }
    }
}