using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Drop this on an empty GameObject in "MainMenuScene".
/// Builds the entire main menu UI from code — no manual setup needed.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Scene Settings")]
    public string swingSceneName = "SwingScene";

    // Colors
    static readonly Color DarkGreen = new Color(0.04f, 0.08f, 0.04f);
    static readonly Color GolfGreen = new Color(0.15f, 0.55f, 0.2f);
    static readonly Color AccentGold = new Color(0.85f, 0.75f, 0.35f);
    static readonly Color TextWhite = new Color(0.92f, 0.95f, 0.9f);
    static readonly Color TextDim = new Color(0.5f, 0.6f, 0.48f);
    static readonly Color ButtonBg = new Color(0.12f, 0.35f, 0.12f);
    static readonly Color ButtonHover = new Color(0.18f, 0.5f, 0.18f);

    // Generated references
    Canvas canvas;
    CanvasGroup masterGroup;
    Text titleText;
    Text subtitleText;
    RectTransform titleUnderline;
    Button playButton;
    Text playButtonText;
    Button settingsButton;
    Button creditsButton;
    Text versionText;
    GameObject settingsPanel;
    CanvasGroup settingsPanelGroup;

    // Animation
    float pulseTimer;
    float titleBobTimer;
    bool transitioning;
    bool menuReady;
    float introTimer;

    // Settings panel
    bool settingsOpen;
    float settingsAnimTimer;
    bool settingsAnimating;
    bool settingsTargetOpen;

    // Credits
    bool creditsOpen;
    GameObject creditsPanel;
    CanvasGroup creditsPanelGroup;
    float creditsAnimTimer;
    bool creditsAnimating;

    void Start()
    {
        Camera.main.backgroundColor = DarkGreen;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;

        BuildUI();

        // Start with everything invisible for intro animation
        masterGroup.alpha = 0f;
        introTimer = 0f;
        menuReady = false;
    }

    void Update()
    {
        // Intro fade
        if (!menuReady)
        {
            introTimer += Time.deltaTime;
            float delay = 0.2f;
            if (introTimer > delay)
            {
                float t = Mathf.Clamp01((introTimer - delay) / 0.8f);
                masterGroup.alpha = EaseOutCubic(t);
                if (t >= 1f) menuReady = true;
            }
            return;
        }

        if (transitioning) return;

        // Play button pulse
        pulseTimer += Time.deltaTime;
        if (playButton != null)
        {
            float glow = Mathf.Lerp(0.7f, 1f, (Mathf.Sin(pulseTimer * 2.5f) + 1f) / 2f);
            var img = playButton.GetComponent<Image>();
            img.color = Color.Lerp(ButtonBg, GolfGreen, (Mathf.Sin(pulseTimer * 2.5f) + 1f) / 2f);

            // Subtle scale pulse
            float scale = Mathf.Lerp(1f, 1.03f, (Mathf.Sin(pulseTimer * 2.5f) + 1f) / 2f);
            playButton.transform.localScale = Vector3.one * scale;
        }

        // Title underline shimmer
        if (titleUnderline != null)
        {
            var img = titleUnderline.GetComponent<Image>();
            float shimmer = Mathf.Lerp(0.4f, 0.9f, (Mathf.Sin(pulseTimer * 1.5f + 0.5f) + 1f) / 2f);
            Color c = AccentGold;
            c.a = shimmer;
            img.color = c;
        }

        // Settings panel animation
        if (settingsAnimating)
        {
            settingsAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(settingsAnimTimer / 0.3f);
            t = EaseOutCubic(t);

            if (settingsTargetOpen)
            {
                settingsPanelGroup.alpha = t;
                settingsPanel.GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(0, Mathf.Lerp(30f, 0f, t));
            }
            else
            {
                settingsPanelGroup.alpha = 1f - t;
                settingsPanel.GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(0, Mathf.Lerp(0f, 30f, t));
            }

            if (settingsAnimTimer >= 0.3f)
            {
                settingsAnimating = false;
                settingsOpen = settingsTargetOpen;
                if (!settingsOpen) settingsPanel.SetActive(false);
            }
        }

        // Credits panel animation
        if (creditsAnimating)
        {
            creditsAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(creditsAnimTimer / 0.3f);
            t = EaseOutCubic(t);

            if (creditsOpen)
            {
                creditsPanelGroup.alpha = t;
            }
            else
            {
                creditsPanelGroup.alpha = 1f - t;
            }

            if (creditsAnimTimer >= 0.3f)
            {
                creditsAnimating = false;
                if (!creditsOpen) creditsPanel.SetActive(false);
            }
        }
    }

    // ── UI Construction ─────────────────────────────────────────

    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("MenuCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        masterGroup = canvasObj.AddComponent<CanvasGroup>();

        // Full-screen dark overlay for depth
        CreateFullscreenImage(canvasObj.transform, "BgOverlay", DarkGreen);

        // Decorative top stripe
        CreateStripe(canvasObj.transform, "TopStripe",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -60), new Vector2(0, 0),
            new Color(0.1f, 0.3f, 0.1f, 0.4f));

        // Decorative bottom stripe
        CreateStripe(canvasObj.transform, "BottomStripe",
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 0), new Vector2(0, 50),
            new Color(0.1f, 0.3f, 0.1f, 0.3f));

        // ── Title area ──
        titleText = CreateText(canvasObj.transform, "Title", "TEMPO",
            96, TextWhite, new Vector2(0.5f, 0.78f), FontStyle.Bold);
        titleText.GetComponent<RectTransform>().sizeDelta = new Vector2(900f, 130f);

        // Title underline
        var underlineObj = CreateRect(canvasObj.transform, "TitleUnderline",
            AccentGold, new Vector2(0.5f, 0.735f), new Vector2(200f, 3f));
        titleUnderline = underlineObj.GetComponent<RectTransform>();

        subtitleText = CreateText(canvasObj.transform, "Subtitle",
            "master your tempo", 28, TextDim,
            new Vector2(0.5f, 0.70f), FontStyle.Italic);

        // ── Buttons ──

        // Play button (large, prominent)
        playButton = CreateButton(canvasObj.transform, "PlayButton",
            "PLAY", new Vector2(0.5f, 0.50f), new Vector2(420f, 90f),
            GolfGreen, TextWhite, 36, OnPlayPressed);

        // Add rounded corner feel with larger font
        playButtonText = playButton.GetComponentInChildren<Text>();

        // Settings button
        settingsButton = CreateButton(canvasObj.transform, "SettingsButton",
            "SETTINGS", new Vector2(0.5f, 0.41f), new Vector2(320f, 68f),
            ButtonBg, TextDim, 26, OnSettingsPressed);

        // Credits button
        creditsButton = CreateButton(canvasObj.transform, "CreditsButton",
            "CREDITS", new Vector2(0.5f, 0.34f), new Vector2(320f, 68f),
            ButtonBg, TextDim, 26, OnCreditsPressed);

        // Version text
        versionText = CreateText(canvasObj.transform, "Version",
            "v0.1.0", 18, new Color(0.35f, 0.4f, 0.33f),
            new Vector2(0.5f, 0.05f), FontStyle.Normal);

        // ── Settings Panel (hidden) ──
        BuildSettingsPanel(canvasObj.transform);

        // ── Credits Panel (hidden) ──
        BuildCreditsPanel(canvasObj.transform);
    }

    void BuildSettingsPanel(Transform parent)
    {
        settingsPanel = new GameObject("SettingsPanel");
        settingsPanel.transform.SetParent(parent, false);

        var panelRect = settingsPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.25f);
        panelRect.anchorMax = new Vector2(0.9f, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImg = settingsPanel.AddComponent<Image>();
        panelImg.color = new Color(0.06f, 0.12f, 0.06f, 0.95f);

        settingsPanelGroup = settingsPanel.AddComponent<CanvasGroup>();
        settingsPanelGroup.alpha = 0f;

        // Panel title
        CreateText(settingsPanel.transform, "SettingsTitle", "SETTINGS",
            38, TextWhite, new Vector2(0.5f, 0.88f), FontStyle.Bold);

        // Divider
        CreateRect(settingsPanel.transform, "SettingsDivider",
            new Color(0.3f, 0.6f, 0.3f, 0.3f),
            new Vector2(0.5f, 0.82f), new Vector2(400f, 2f));

        // Sound volume label + indicator
        CreateText(settingsPanel.transform, "SoundLabel", "SOUND",
            24, TextDim, new Vector2(0.25f, 0.70f), FontStyle.Normal);
        CreateText(settingsPanel.transform, "SoundValue", "ON",
            24, GolfGreen, new Vector2(0.75f, 0.70f), FontStyle.Bold);

        // Music volume label + indicator
        CreateText(settingsPanel.transform, "MusicLabel", "MUSIC",
            24, TextDim, new Vector2(0.25f, 0.58f), FontStyle.Normal);
        CreateText(settingsPanel.transform, "MusicValue", "ON",
            24, GolfGreen, new Vector2(0.75f, 0.58f), FontStyle.Bold);

        // Vibration
        CreateText(settingsPanel.transform, "VibrationLabel", "VIBRATION",
            24, TextDim, new Vector2(0.25f, 0.46f), FontStyle.Normal);
        CreateText(settingsPanel.transform, "VibrationValue", "ON",
            24, GolfGreen, new Vector2(0.75f, 0.46f), FontStyle.Bold);

        // Controls hint
        CreateText(settingsPanel.transform, "ControlsHint",
            "drag to swing  \u00b7  tap to skip",
            20, new Color(0.45f, 0.55f, 0.42f),
            new Vector2(0.5f, 0.28f), FontStyle.Italic);

        // Close button
        CreateButton(settingsPanel.transform, "CloseSettings",
            "BACK", new Vector2(0.5f, 0.12f), new Vector2(200f, 56f),
            ButtonBg, TextDim, 24, OnSettingsClose);

        settingsPanel.SetActive(false);
    }

    void BuildCreditsPanel(Transform parent)
    {
        creditsPanel = new GameObject("CreditsPanel");
        creditsPanel.transform.SetParent(parent, false);

        var panelRect = creditsPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.25f);
        panelRect.anchorMax = new Vector2(0.9f, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImg = creditsPanel.AddComponent<Image>();
        panelImg.color = new Color(0.06f, 0.12f, 0.06f, 0.95f);

        creditsPanelGroup = creditsPanel.AddComponent<CanvasGroup>();
        creditsPanelGroup.alpha = 0f;

        // Title
        CreateText(creditsPanel.transform, "CreditsTitle", "CREDITS",
            38, TextWhite, new Vector2(0.5f, 0.88f), FontStyle.Bold);

        CreateRect(creditsPanel.transform, "CreditsDivider",
            new Color(0.3f, 0.6f, 0.3f, 0.3f),
            new Vector2(0.5f, 0.82f), new Vector2(400f, 2f));

        CreateText(creditsPanel.transform, "CreditsDev", "DEVELOPMENT",
            22, AccentGold, new Vector2(0.5f, 0.72f), FontStyle.Bold);

        CreateText(creditsPanel.transform, "CreditsDevName", "Tempo Studios",
            26, TextWhite, new Vector2(0.5f, 0.64f), FontStyle.Normal);

        CreateText(creditsPanel.transform, "CreditsDesign", "DESIGN",
            22, AccentGold, new Vector2(0.5f, 0.52f), FontStyle.Bold);

        CreateText(creditsPanel.transform, "CreditsDesignName", "Tempo Studios",
            26, TextWhite, new Vector2(0.5f, 0.44f), FontStyle.Normal);

        CreateText(creditsPanel.transform, "CreditsEngine", "Built with Unity",
            20, TextDim, new Vector2(0.5f, 0.30f), FontStyle.Italic);

        CreateButton(creditsPanel.transform, "CloseCredits",
            "BACK", new Vector2(0.5f, 0.12f), new Vector2(200f, 56f),
            ButtonBg, TextDim, 24, OnCreditsClose);

        creditsPanel.SetActive(false);
    }

    // ── Button handlers ─────────────────────────────────────────

    void OnPlayPressed()
    {
        if (transitioning || settingsOpen || creditsOpen) return;
        transitioning = true;
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            masterGroup.alpha = 1f - EaseInCubic(t);
            yield return null;
        }

        SceneManager.LoadScene(swingSceneName);
    }

    void OnSettingsPressed()
    {
        if (transitioning || creditsOpen) return;
        settingsPanel.SetActive(true);
        settingsTargetOpen = true;
        settingsAnimTimer = 0f;
        settingsAnimating = true;
    }

    void OnSettingsClose()
    {
        settingsTargetOpen = false;
        settingsAnimTimer = 0f;
        settingsAnimating = true;
    }

    void OnCreditsPressed()
    {
        if (transitioning || settingsOpen) return;
        creditsPanel.SetActive(true);
        creditsOpen = true;
        creditsAnimTimer = 0f;
        creditsAnimating = true;
    }

    void OnCreditsClose()
    {
        creditsOpen = false;
        creditsAnimTimer = 0f;
        creditsAnimating = true;
    }

    // ── UI Helpers ──────────────────────────────────────────────

    Text CreateText(Transform parent, string name, string content,
        int fontSize, Color color, Vector2 anchorPos, FontStyle style = FontStyle.Normal)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(800f, 80f);

        var text = obj.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
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

        var img = obj.AddComponent<Image>();
        img.color = color;

        return obj;
    }

    void CreateFullscreenImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = obj.AddComponent<Image>();
        img.color = color;
    }

    void CreateStripe(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var img = obj.AddComponent<Image>();
        img.color = color;
    }

    Button CreateButton(Transform parent, string name, string label,
        Vector2 anchorPos, Vector2 size, Color bgColor, Color textColor,
        int fontSize, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        var img = obj.AddComponent<Image>();
        img.color = bgColor;

        var btn = obj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        colors.fadeDuration = 0.1f;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        // Button text
        CreateText(obj.transform, name + "Text", label,
            fontSize, textColor, new Vector2(0.5f, 0.5f), FontStyle.Bold);

        return btn;
    }

    // ── Easing ──────────────────────────────────────────────────

    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    float EaseInCubic(float t)
    {
        return t * t * t;
    }
}
