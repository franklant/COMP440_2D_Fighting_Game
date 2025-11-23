using UnityEngine;
using System.Collections;

public class GameFeelManager : MonoBehaviour
{
    public static GameFeelManager instance;

    private float stopTimeRemaining = 0f;
    private bool isFrozen = false;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void HitStop(float duration)
    {
        // DEBUG LOG: Verify the signal reached this script
        Debug.Log($"[GameFeel] HITSTOP TRIGGERED for {duration} seconds");

        stopTimeRemaining = duration;

        if (!isFrozen)
        {
            StartCoroutine(DoHitStop());
        }
    }

    IEnumerator DoHitStop()
    {
        isFrozen = true;
        float originalScale = Time.timeScale;
        
        // FREEZE
        Time.timeScale = 0.0f;

        while (stopTimeRemaining > 0)
        {
            stopTimeRemaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        // UNFREEZE
        Time.timeScale = originalScale;
        isFrozen = false;
    }

    // --- DEBUGGER ---
    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 40;
        style.fontStyle = FontStyle.Bold;
        
        // Color Logic: Red if Frozen, Green if Normal
        style.normal.textColor = (Time.timeScale == 0) ? Color.red : Color.green;

        string status = (Time.timeScale == 0) ? "FROZEN (Hitstop)" : "RUNNING";
        
        // Draw on top right
        GUI.Label(new Rect(Screen.width - 400, 20, 400, 50), "TIME SCALE: " + Time.timeScale, style);
        GUI.Label(new Rect(Screen.width - 400, 70, 400, 50), status, style);
    }
}