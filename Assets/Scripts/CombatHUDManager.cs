using UnityEngine;
using TMPro;
using System.Collections;

public class CombatHUDManager : MonoBehaviour
{
    public static CombatHUDManager Instance;

    [Header("Round Timer")]
    public TextMeshProUGUI timerText;
    public float roundTimeSeconds = 99f;
    private bool timerRunning = false;

    [Header("Contextual Popups")]
    [Tooltip("Text object to flash for First Attack/Counter etc")]
    public TextMeshProUGUI announcementText; 
    public AnimationCurve popupScaleCurve; // Optional for juice

    private bool firstAttackTriggered = false;

    private void Awake()
    {
        // Singleton Setup
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        StartRound();
    }

    private void Update()
    {
        if (timerRunning)
        {
            roundTimeSeconds -= Time.deltaTime;
            if (roundTimeSeconds <= 0)
            {
                roundTimeSeconds = 0;
                timerRunning = false;
                // Trigger Round Over logic here
            }
            UpdateTimerDisplay();
        }
    }

    void UpdateTimerDisplay()
    {
        if(timerText) timerText.text = Mathf.CeilToInt(roundTimeSeconds).ToString();
    }

    public void StartRound()
    {
        timerRunning = true;
        firstAttackTriggered = false;
        if(announcementText) announcementText.text = "";
    }

    // --- Global Event Triggers ---

    /// <summary>
    /// Checks if First Attack has happened yet. If not, announces it for the given player name.
    /// </summary>
    public void CheckFirstAttack(string playerName)
    {
        if (!firstAttackTriggered)
        {
            firstAttackTriggered = true;
            ShowAnnouncement("FIRST ATTACK!", Color.yellow);
        }
    }

    public void ShowCounterHit()
    {
        ShowAnnouncement("COUNTER HIT!", Color.red);
    }

    public void ShowReversal()
    {
        ShowAnnouncement("REVERSAL!", Color.cyan);
    }

    // Helper to show text and fade it out
    private void ShowAnnouncement(string text, Color color)
    {
        if (announcementText == null) return;

        announcementText.text = text;
        announcementText.color = color;
        announcementText.gameObject.SetActive(true);
        
        StopCoroutine("AnimateAnnouncement");
        StartCoroutine("AnimateAnnouncement");
    }

    IEnumerator AnimateAnnouncement()
    {
        float duration = 1.5f;
        float timer = 0;
        Vector3 originalScale = Vector3.one;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // Pop effect
            float scale = 1f;
            if (popupScaleCurve.length > 0) 
                scale = popupScaleCurve.Evaluate(timer / 0.3f); // Quick pop in 0.3s
            
            announcementText.transform.localScale = originalScale * scale;
            yield return null;
        }

        announcementText.text = "";
        announcementText.gameObject.SetActive(false);
    }
}