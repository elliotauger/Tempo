using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Editor utility: creates all Tempo scenes from the Unity menu bar.
/// Menu → Tempo → Setup All Scenes
/// </summary>
public class SceneSetup : MonoBehaviour
{
    const string ScenesFolder = "Assets/Scenes";

    [MenuItem("Tempo/Setup All Scenes")]
    static void SetupAllScenes()
    {
        EnsureFolder(ScenesFolder);
        CreateSplashScene();
        CreateMainMenuScene();
        CreateSwingScene();
        SetBuildSettings();
        Debug.Log("[Tempo] All scenes created. Open Scenes/SplashScene to start.");
    }

    [MenuItem("Tempo/Create Splash Scene Only")]
    static void CreateSplashScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camObj = new GameObject("Main Camera");
        var cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.06f, 0.04f);

        // Splash controller
        var splash = new GameObject("SplashController");
        splash.AddComponent<SplashScreen>();

        EnsureFolder(ScenesFolder);
        EditorSceneManager.SaveScene(scene, ScenesFolder + "/SplashScene.unity");
        Debug.Log("[Tempo] SplashScene created.");
    }

    [MenuItem("Tempo/Create Main Menu Scene Only")]
    static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camObj = new GameObject("Main Camera");
        var cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.08f, 0.04f);

        // Menu controller — the MainMenu script builds all UI from code
        var menu = new GameObject("MainMenuController");
        menu.AddComponent<MainMenu>();

        EnsureFolder(ScenesFolder);
        EditorSceneManager.SaveScene(scene, ScenesFolder + "/MainMenuScene.unity");
        Debug.Log("[Tempo] MainMenuScene created.");
    }

    [MenuItem("Tempo/Create Swing Scene Only")]
    static void CreateSwingScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ──
        var camObj = new GameObject("Main Camera");
        var cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.Skybox;
        camObj.transform.position = new Vector3(0f, 2.5f, 5f);
        camObj.transform.rotation = Quaternion.Euler(15f, 180f, 0f);

        // ── Directional Light ──
        var lightObj = new GameObject("Directional Light");
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.88f);
        light.intensity = 1.2f;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // ── Fairway ground ──
        var fairway = GameObject.CreatePrimitive(PrimitiveType.Plane);
        fairway.name = "Fairway";
        fairway.transform.position = Vector3.zero;
        fairway.transform.localScale = new Vector3(5f, 1f, 30f);
        // Apply fairway material (URP compatible)
        string matPath = "Assets/Materials";
        EnsureFolder(matPath);

        var fairwayShader = Shader.Find("Custom/FairwayStripesURP");
        if (fairwayShader != null)
        {
            var mat = new Material(fairwayShader);
            fairway.GetComponent<Renderer>().sharedMaterial = mat;
            AssetDatabase.CreateAsset(mat, matPath + "/FairwayMaterial.mat");
        }
        else
        {
            // Fallback: URP Lit green material
            var mat = CreateURPMaterial("FairwayFallback", new Color(0.15f, 0.45f, 0.1f));
            fairway.GetComponent<Renderer>().sharedMaterial = mat;
            AssetDatabase.CreateAsset(mat, matPath + "/FairwayMaterial.mat");
        }

        // ── Body Pivot (parent for arms + club) ──
        var bodyPivot = new GameObject("BodyPivot");
        bodyPivot.transform.position = new Vector3(0f, 1f, 0f);

        // Left arm
        var leftArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        leftArm.name = "LeftArm";
        leftArm.transform.SetParent(bodyPivot.transform);
        leftArm.transform.localPosition = new Vector3(-0.2f, 0f, 0f);
        leftArm.transform.localScale = new Vector3(0.08f, 0.4f, 0.08f);
        SetColor(leftArm, new Color(0.85f, 0.75f, 0.65f));

        // Right arm
        var rightArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        rightArm.name = "RightArm";
        rightArm.transform.SetParent(bodyPivot.transform);
        rightArm.transform.localPosition = new Vector3(0.2f, 0f, 0f);
        rightArm.transform.localScale = new Vector3(0.08f, 0.4f, 0.08f);
        SetColor(rightArm, new Color(0.85f, 0.75f, 0.65f));

        // Club shaft
        var clubShaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        clubShaft.name = "ClubShaft";
        clubShaft.transform.SetParent(bodyPivot.transform);
        clubShaft.transform.localPosition = new Vector3(0f, -0.5f, -0.15f);
        clubShaft.transform.localScale = new Vector3(0.03f, 0.55f, 0.03f);
        SetColor(clubShaft, new Color(0.7f, 0.7f, 0.7f));

        // Club head
        var clubHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        clubHead.name = "ClubHead";
        clubHead.transform.SetParent(clubShaft.transform);
        clubHead.transform.localPosition = new Vector3(0f, -1f, 0f);
        clubHead.transform.localScale = new Vector3(3f, 0.8f, 5f);
        SetColor(clubHead, new Color(0.5f, 0.5f, 0.55f));

        // ── Tee ──
        var tee = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tee.name = "Tee";
        tee.transform.position = new Vector3(0f, 0.04f, 0f);
        tee.transform.localScale = new Vector3(0.02f, 0.04f, 0.02f);
        SetColor(tee, new Color(0.9f, 0.85f, 0.7f));

        // ── Ball ──
        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Ball";
        ball.transform.position = new Vector3(0f, 0.1f, 0f);
        ball.transform.localScale = new Vector3(0.043f, 0.043f, 0.043f);
        SetColor(ball, Color.white);

        // ── Flag (down the fairway) ──
        var flag = new GameObject("Flag");
        flag.transform.position = new Vector3(0f, 0f, -120f);

        var flagPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        flagPole.name = "Pole";
        flagPole.transform.SetParent(flag.transform);
        flagPole.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        flagPole.transform.localScale = new Vector3(0.02f, 1.5f, 0.02f);
        SetColor(flagPole, Color.white);

        var flagCloth = GameObject.CreatePrimitive(PrimitiveType.Quad);
        flagCloth.name = "FlagCloth";
        flagCloth.transform.SetParent(flag.transform);
        flagCloth.transform.localPosition = new Vector3(0.2f, 2.8f, 0f);
        flagCloth.transform.localScale = new Vector3(0.4f, 0.25f, 1f);
        SetColor(flagCloth, Color.red);

        // ── UI (Canvas with shot label + power bar) ──
        var canvasObj = new GameObject("SwingUI");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Shot label
        var shotLabelObj = new GameObject("ShotLabel");
        shotLabelObj.transform.SetParent(canvasObj.transform, false);
        var shotRect = shotLabelObj.AddComponent<RectTransform>();
        shotRect.anchorMin = new Vector2(0.5f, 0.85f);
        shotRect.anchorMax = new Vector2(0.5f, 0.85f);
        shotRect.sizeDelta = new Vector2(600f, 100f);
        var shotText = shotLabelObj.AddComponent<UnityEngine.UI.Text>();
        shotText.text = "";
        shotText.fontSize = 48;
        shotText.alignment = TextAnchor.MiddleCenter;
        shotText.color = Color.white;
        shotText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        shotLabelObj.SetActive(false);

        // Power bar
        var powerBarObj = new GameObject("PowerBar");
        powerBarObj.transform.SetParent(canvasObj.transform, false);
        var pbRect = powerBarObj.AddComponent<RectTransform>();
        pbRect.anchorMin = new Vector2(0.5f, 0.08f);
        pbRect.anchorMax = new Vector2(0.5f, 0.08f);
        pbRect.sizeDelta = new Vector2(400f, 30f);

        // Slider background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(powerBarObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.7f);

        // Slider fill area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(powerBarObj.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        var fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImg = fillObj.AddComponent<UnityEngine.UI.Image>();
        fillImg.color = new Color(0.2f, 0.7f, 0.2f);

        var slider = powerBarObj.AddComponent<UnityEngine.UI.Slider>();
        slider.fillRect = fillRect;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = false;
        powerBarObj.SetActive(false);

        // Instruction label
        var instrObj = new GameObject("InstructionLabel");
        instrObj.transform.SetParent(canvasObj.transform, false);
        var instrRect = instrObj.AddComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0.5f, 0.15f);
        instrRect.anchorMax = new Vector2(0.5f, 0.15f);
        instrRect.sizeDelta = new Vector2(600f, 60f);
        var instrText = instrObj.AddComponent<UnityEngine.UI.Text>();
        instrText.text = "drag right to swing";
        instrText.fontSize = 28;
        instrText.alignment = TextAnchor.MiddleCenter;
        instrText.color = new Color(0.7f, 0.8f, 0.65f, 0.7f);
        instrText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ── SwingMechanic controller ──
        var controller = new GameObject("SwingController");
        var swing = controller.AddComponent<SwingMechanic>();
        swing.mainCamera = cam;
        swing.bodyPivot = bodyPivot.transform;
        swing.clubShaft = clubShaft.transform;
        swing.ball = ball.transform;
        swing.tee = tee.transform;
        swing.flag = flag.transform;
        swing.shotLabel = shotText;
        swing.powerBar = slider;
        swing.instructionLabel = instrText;

        EnsureFolder(ScenesFolder);
        EditorSceneManager.SaveScene(scene, ScenesFolder + "/SwingScene.unity");
        Debug.Log("[Tempo] SwingScene created.");
    }

    static void SetBuildSettings()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(ScenesFolder + "/SplashScene.unity", true),
            new EditorBuildSettingsScene(ScenesFolder + "/MainMenuScene.unity", true),
            new EditorBuildSettingsScene(ScenesFolder + "/SwingScene.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[Tempo] Build Settings updated: SplashScene → MainMenuScene → SwingScene");
    }

    // ── Helpers ──

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path);
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    static void SetColor(GameObject obj, Color color)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateURPMaterial(obj.name, color);
        }
    }

    static Material CreateURPMaterial(string name, Color color)
    {
        // Try URP Lit first, fall back to Standard
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.name = name + "_Mat";
        mat.SetColor("_BaseColor", color); // URP uses _BaseColor
        mat.color = color; // Standard uses _Color (mat.color maps to it)
        return mat;
    }
}
