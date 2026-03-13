using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drop this on an empty GameObject in a scene called "SplashScene".
/// It builds the entire splash screen UI from code and transitions to MainMenuScene.
/// </summary>
public class SplashScreen : MonoBehaviour
{
    [Header("Settings")]
    public string nextSceneName = "MainMenuScene";
    public float logoHoldTime = 1.8f;
    public float fadeTime = 0.6f;

    // Generated UI
    Canvas canvas;
    CanvasGroup canvasGroup;
    GameObject logoContainer;
    UnityEngine.UI.Text studioText;
    UnityEngine.UI.Text taglineText;
    GameObject accentLine;

    // State
    enum SplashState { FadeIn, Hold, FadeOut, Done }
    SplashState state = SplashState.FadeIn;
    float timer;
    bool skipping;

    void Start()
    {
        BuildUI();
        canvasGroup.alpha = 0f;
        timer = 0f;

        // Dark background
        Camera.main.backgroundColor = new Color(0.04f, 0.06f, 0.04f);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
    }

    void Update()
    {
        // Tap/click to skip
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            if (!skipping)
            {
                skipping = true;
                state = SplashState.FadeOut;
                timer = 0f;
            }
        }

        timer += Time.deltaTime;

        switch (state)
        {
            case SplashState.FadeIn:
                canvasGroup.alpha = Mathf.Clamp01(timer / fadeTime);
                // Subtle scale-up on studio text
                float scaleT = Mathf.Clamp01(timer / fadeTime);
                float s = Mathf.Lerp(0.85f, 1f, EaseOutCubic(scaleT));
                logoContainer.transform.localScale = Vector3.one * s;

                if (timer >= fadeTime)
                {
                    canvasGroup.alpha = 1f;
                    state = SplashState.Hold;
                    timer = 0f;
                }
                break;

            case SplashState.Hold:
                // Gentle pulse on accent line
                float pulse = Mathf.Lerp(0.5f, 1f, (Mathf.Sin(timer * 2f) + 1f) / 2f);
                if (accentLine != null)
                {
                    var img = accentLine.GetComponent<UnityEngine.UI.Image>();
                    Color c = img.color;
                    c.a = pulse;
                    img.color = c;
                }

                if (timer >= logoHoldTime)
                {
                    state = SplashState.FadeOut;
                    timer = 0f;
                }
                break;

            case SplashState.FadeOut:
                canvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeTime);
                if (timer >= fadeTime)
                {
                    state = SplashState.Done;
                    SceneManager.LoadScene(nextSceneName);
                }
                break;
        }
    }

    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("SplashCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
            UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution =
            new Vector2(1080, 1920);
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();

        // Logo container (for scale animation)
        logoContainer = new GameObject("LogoContainer");
        logoContainer.transform.SetParent(canvasObj.transform, false);
        var containerRect = logoContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        // Studio name
        studioText = CreateText(logoContainer.transform, "StudioText",
            "TEMPO STUDIOS", 42, new Color(0.85f, 0.92f, 0.82f),
            new Vector2(0.5f, 0.55f), FontStyle.Bold);

        // Accent line under studio name
        accentLine = CreateRect(logoContainer.transform, "AccentLine",
            new Color(0.3f, 0.7f, 0.25f, 0.8f),
            new Vector2(0.5f, 0.50f), new Vector2(120f, 2f));

        // Tagline
        taglineText = CreateText(logoContainer.transform, "Tagline",
            "precision in every swing", 20, new Color(0.55f, 0.65f, 0.5f),
            new Vector2(0.5f, 0.45f), FontStyle.Italic);
    }

    UnityEngine.UI.Text CreateText(Transform parent, string name, string content,
        int fontSize, Color color, Vector2 anchorPos, FontStyle style = FontStyle.Normal)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(800f, 80f);

        var text = obj.AddComponent<UnityEngine.UI.Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);

        return text;
    }

    GameObject CreateRect(Transform parent, string name, Color color,
        Vector2 anchorPos, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        var img = obj.AddComponent<UnityEngine.UI.Image>();
        img.color = color;

        return obj;
    }

    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
