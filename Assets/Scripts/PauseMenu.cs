using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool isPaused = false;

    public GameObject PauseMenuUI;
    public GameObject CommandListUI;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            AudioManager.Instance.UIClicks();

            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Resume()
    {
        PauseMenuUI.SetActive(false);
        CommandListUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        AudioManager.Instance.UIClicks();
    }

    public void Pause()
    {
        PauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        AudioManager.Instance.UIClicks();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        AudioManager.Instance.UIClicks();
        AudioManager.Instance.RestartMusic();
    }

    public void OpenCommandList()
    {
        PauseMenuUI.SetActive(false);
        CommandListUI.SetActive(true);
        AudioManager.Instance.UIClicks();

    }

    public void CloseCommandList()
    {
        CommandListUI.SetActive(false);
        PauseMenuUI.SetActive(true);
        AudioManager.Instance.UIClicks();
    }
    public void Quit()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameMenu");
        AudioManager.Instance.UIClicks();
        AudioManager.Instance.StopMusic();
    }
}

