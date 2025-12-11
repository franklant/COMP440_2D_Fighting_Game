using UnityEngine;

public class SpawnCharacterScript : MonoBehaviour
{
    [Header("UI & Managers")]
    public StatsUIHandler statsUIHandler; 
    // 1. NEW: Reference to the RoundManager
    public RoundManager roundManager; 

    [Header("Characters to Spawn")]
    public GameObject Gojo;
    public GameObject Sukuna;
    public GameObject Naruto;
    public GameObject Madara;
    public GameObject Luffy;

    void Start()
    {
        string selectedCharacter = PlayerPrefs.GetString("selectedCharacter");
        GameObject prefabToSpawn = null;

        // 2. Select Prefab
        if (!string.IsNullOrEmpty(selectedCharacter))
        {
            switch (selectedCharacter)
            {
                case "Gojo": prefabToSpawn = Gojo; break;
                case "Sukuna": prefabToSpawn = Sukuna; break;
                case "Naruto": prefabToSpawn = Naruto; break;
                case "Madara": prefabToSpawn = Madara; break;
                case "Luffy": prefabToSpawn = Luffy; break;
                default:
                    Debug.LogWarning($"Character '{selectedCharacter}' not found in Spawn list!");
                    break;
            }
        }
        else
        {
            Debug.LogError("No selected character found in PlayerPrefs.");
            return;
        }

        // 3. Spawn and Connect
        if (prefabToSpawn != null)
        {
            // Spawn the character
            GameObject newCharacter = Instantiate(prefabToSpawn, transform.position, transform.rotation);
            
            // IMPORTANT: Force the tag to Player1 so hitboxes know who this is
            newCharacter.tag = "Player1"; 

            // Get the stats component
            FighterStatsManager characterStats = newCharacter.GetComponent<FighterStatsManager>();

            // Connect to Health Bar UI
            if (statsUIHandler != null && characterStats != null)
            {
                statsUIHandler.SetTarget(characterStats);
            }

            // 4. NEW: Connect to Round Manager
            if (roundManager != null && characterStats != null)
            {
                roundManager.player1 = characterStats;     // Assign the stats
                roundManager.p1Name = selectedCharacter;   // Assign the name (e.g., "Luffy")
                Debug.Log($"Assigned {selectedCharacter} to RoundManager Player 1.");
            }
            else
            {
                Debug.LogWarning("RoundManager not assigned in Inspector!");
            }
        }
    }
}