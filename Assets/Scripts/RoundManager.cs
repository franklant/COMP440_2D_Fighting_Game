using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // <--- REQUIRED for switching scenes
using System.Collections;

public class RoundManager : MonoBehaviour
{
    [Header("--- UI References ---")]
    public TextMeshProUGUI timerText;       
    public TextMeshProUGUI announcementText; 

    [Header("--- Fighters ---")]
    public FighterStatsManager player1;     
    public FighterStatsManager player2;     
    public string p1Name = "Luffy";         
    public string p2Name = "Opponent";      

    [Header("--- Settings ---")]
    public float roundDuration = 99f;
    public string menuSceneName = "GameMenu"; // Exact name of your menu scene
    
    private float currentTime;
    private bool isRoundOver = false;

    void Start()
    {
        currentTime = roundDuration;
        if(announcementText != null) announcementText.text = "";
    }

    void Update()
    {
        if (isRoundOver) return;

        // Timer Logic
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerUI();
        }
        else
        {
            currentTime = 0;
            UpdateTimerUI();
            EndRound();
        }
        
        // KO Logic
        if (player1.CurrentHealth <= 0 || player2.CurrentHealth <= 0)
        {
            EndRound();
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(currentTime).ToString();
            if (currentTime <= 10f) timerText.color = Color.red;
        }
    }

    void EndRound()
    {
        if (isRoundOver) return;
        isRoundOver = true;

        // 1. Determine Winner
        string winMessage = "DRAW";

        if (player1 != null && player2 != null)
        {
            if (player1.CurrentHealth > player2.CurrentHealth)
                winMessage = $"{p1Name} WINS";
            else if (player2.CurrentHealth > player1.CurrentHealth)
                winMessage = $"{p2Name} WINS";
            else
                winMessage = "DRAW GAME";
        }

        // 2. Show Text
        if (announcementText != null)
        {
            announcementText.text = winMessage;
            announcementText.gameObject.SetActive(true);
        }

        // 3. Start Transition
        StartCoroutine(LoadMenuAfterDelay());
    }

    IEnumerator LoadMenuAfterDelay()
    {
        // Wait 3 seconds so players can read "Luffy Wins"
        yield return new WaitForSeconds(3.0f);
        
        Debug.Log("Loading Menu...");
        
        // Load the scene defined in the Inspector variable
        SceneManager.LoadScene(menuSceneName);
    }
}