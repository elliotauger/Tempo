using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    public Text titleText;
    public Text subtitleText;
    public Button playButton;
    public Text versionText;
    public CanvasGroup canvasGroup;

    [Header("Settings")]
    public string swingSceneName = "SwingScene";

    private float pulseTimer;
    private bool transitioning;

    void Start()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayPressed);
    }

    void Update()
    {
        if (transitioning) return;

        // Pulse the play button alpha
        pulseTimer += Time.deltaTime;
        if (playButton != null)
        {
            float alpha = Mathf.Lerp(0.5f, 1f, (Mathf.Sin(pulseTimer * 3f) + 1f) / 2f);
            var colors = playButton.colors;
            Color c = playButton.image.color;
            c.a = alpha;
            playButton.image.color = c;
        }
    }

    void OnPlayPressed()
    {
        if (transitioning) return;
        transitioning = true;
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f - (elapsed / duration);
            yield return null;
        }

        SceneManagement.LoadScene(swingSceneName);
    }

    // Helper to handle both old and new scene loading
    static class SceneManagement
    {
        public static void LoadScene(string name)
        {
            SceneManager.LoadScene(name);
        }
    }
}
