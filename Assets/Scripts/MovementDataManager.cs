using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// AI Aided
// --- 1. THE DATA CLASSES ---
// These classes are a "blueprint" that must match your JSON structure.
// Unity's [System.Serializable] tag tells the JsonUtility to read them.

/// <summary>
/// Matches the structure of the parametersf or each "attack" entry.
/// i.e. "kick": {"startupFrames": . . . }
/// </summary>
[System.Serializable]
public class MoveDetails
{
    // Make sure variable names (e.g., "startupFrames")
    // exactly match the keys in your JSON file!
    public string input;
    public int speed;
    public int startupFrames;
    public int activeFrames;
    public int recoveryFrames;
    public int totalFrames;
    
    // These fields are in some moves but not others.
    // The parser will just leave them as default (0 or null) if not found.
    public float distance;
    public int damage;
    public int onHitAdvantage;
    public int onBlockAdvantage;

    // Use List<string> for JSON arrays
    public List<string> specialEffects;
}

/// <summary>
/// Matches the structure for the "movement" entry of the 'MoveDatabase'.
/// i.e. "movement": { . . . }.
/// </summary>
[System.Serializable]
public class MovementContainer
{
    // These variable names MUST match the JSON keys
    public MoveDetails walkForward;
    public MoveDetails walkBack;
    public MoveDetails forwardDash;
    public MoveDetails backDash;
    public MoveDetails jumpNeutral;
}

/// <summary>
/// Matches the structure for the "attacks" entry in the 'MoveDatabase'.
/// </summary>
[System.Serializable]
public class AttackContainer
{
    // These variable names MUST match the JSON keys
    public MoveDetails lightPunch;
    public MoveDetails heavySlash;
}

/// <summary>
/// Matches the structure of the JSON for a specific character entry in the 'MoveDatabase'.
/// i.e. "Kensei": { . . . }.
/// </summary>
[System.Serializable]
public class CharacterData
{
    // These variable names MUST match the JSON keys
    public MovementContainer movement;
    public AttackContainer attacks;
}

/// <summary>
/// The overall database for all characters.
/// Matches the structure of the JSON 'MoveDatabase'. i.e. { . . . }.
/// </summary>
[System.Serializable]
public class MoveDatabase
{
    // This top-level variable MUST match the "Kensei" key in your JSON
    public CharacterData Kensei;
    // If you add a new character "Monk" to your JSON,
    // you would add: public CharacterData Monk;
}

/// <summary>
/// --- 2. THE DATABASE MANAGER ---
/// This script loads and holds all the move data for the whole game.
/// </summary>
public class MovementDataManager : MonoBehaviour
{
    public static MovementDataManager Instance; // A static instance to access it from anywhere

    [Tooltip("Drag your move_database.json file here (must be a TextAsset)")]
    public TextAsset moveDatabaseFile;
    
    public MoveDatabase moveDB; // This variable will hold all your parsed data

    void Awake()
    {
        // --- This is the JSON Parsing! ---
        if (moveDatabaseFile != null)
        {
            // This one line reads the text and converts it into your C# classes.
            moveDB = JsonUtility.FromJson<MoveDatabase>(moveDatabaseFile.text);
            
            // Set up the static instance
            Instance = this;
            
            // You can log to check if it worked!
            Debug.Log("Move Database Loaded!");
            Debug.Log($"Kensei's Light Punch Startup: {moveDB.Kensei.attacks.lightPunch.startupFrames} frames");
        }
        else
        {
            Debug.LogError("Move Database file is not assigned in the Inspector!");
        }
    }

    /// <summary>
    /// A helper function to get move data easily from other scripts.
    /// Note: This is a simple way; a real game might use dictionaries for speed.
    /// </summary>
    /// <param name="characterName">The name of the character you want to select moves from.</param>
    /// <param name="moveName">The name of the move you want to access.</param>
    /// <returns>The move details for the selected move name.</returns>
    public MoveDetails GetMove(string characterName, string moveName)
    {
        if (characterName == "Kensei")
        {
            if (moveName == "lightPunch")
            {
                return moveDB.Kensei.attacks.lightPunch;
            }
            if (moveName == "forwardDash")
            {
                return moveDB.Kensei.movement.forwardDash;
            }
            // ... you would add more "if" checks for each move ...
        }
        
        Debug.LogWarning($"Move not found: {characterName} / {moveName}");
        return null;
    }
}

// // --- 3. EXAMPLE PLAYER SCRIPT ---
// // This script shows how to *use* the data from the DatabaseManager
// public class PlayerController : MonoBehaviour
// {
//     private bool isBusy = false; // A simple state to prevent new moves while busy

//     void Update()
//     {
//         // Check for input (e.g., "J" key for Light Punch)
//         if (Input.GetKeyDown(KeyCode.J) && !isBusy)
//         {
//             // Start the move!
//             StartCoroutine(PerformMove("lightPunch"));
//         }
//     }

//     IEnumerator PerformMove(string moveName)
//     {
//         isBusy = true;
//         Debug.Log($"--- Performing: {moveName} ---");

//         // 1. Get the move data from our database
//         // We use the static Instance so we don't need a reference.
//         MoveDetails moveData = DatabaseManager.Instance.GetMove("Kensei", moveName);

//         if (moveData == null)
//         {
//             isBusy = false;
//             yield break; // Exit if move data isn't found
//         }

//         // Convert frames (at 60fps) to seconds for Unity's timer
//         float startupTime = moveData.startupFrames / 60f;
//         float activeTime = moveData.activeFrames / 60f;
//         float recoveryTime = moveData.recoveryFrames / 60f;

//         // 2. STATE: STARTUP
//         Debug.Log($"STARTUP for {startupTime} seconds...");
//         // You could play an animation here
//         yield return new WaitForSeconds(startupTime);

//         // 3. STATE: ACTIVE
//         Debug.Log($"ACTIVE for {activeTime} seconds...");
//         // This is where you would turn on the hitbox
//         // e.g., hitbox.SetActive(true);
//         yield return new WaitForSeconds(activeTime);

//         // 4. STATE: RECOVERY
//         Debug.Log($"RECOVERY for {recoveryTime} seconds...");
//         // Turn off the hitbox
//         // e.g., hitbox.SetActive(false);
//         yield return new WaitForSeconds(recoveryTime);

//         // 5. STATE: IDLE
//         Debug.Log("--- Move Finished. Returning to Idle. ---");
//         isBusy = false;
//     }
// }
