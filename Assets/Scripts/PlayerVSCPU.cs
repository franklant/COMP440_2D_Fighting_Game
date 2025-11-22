using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class InvisibleSceneButton : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Exact name of the scene to load (must be added to Build Settings).")]
    public string sceneToLoad;

    [Header("Flash Settings")]
    [Tooltip("Light that will flash when the button is pressed.")]
    public Light flashLight;

    [Tooltip("How long the flash lasts (seconds).")]
    public float flashDuration = 0.15f;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError("InvisibleSceneButton needs to be on a UI Button!");
            return;
        }

        // Make the button invisible (no graphic)
        Image img = GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = 0f;          // fully transparent
            img.color = c;
        }

        // Optional: remove button highlight/pressed visuals
        button.transition = Selectable.Transition.None;

        button.onClick.AddListener(OnButtonClicked);

        // Start with flash light OFF
        if (flashLight != null)
        {
            flashLight.enabled = false;
        }
    }

    void OnButtonClicked()
    {
        StartCoroutine(FlashAndLoad());
    }

    private IEnumerator FlashAndLoad()
    {
        // Turn light on briefly
        if (flashLight != null)
        {
            flashLight.enabled = true;
            yield return new WaitForSeconds(flashDuration);
            flashLight.enabled = false;
        }

        // Load the new scene
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("No sceneToLoad set on InvisibleSceneButton.");
        }
    }
}
