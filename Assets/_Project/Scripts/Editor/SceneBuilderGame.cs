using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Программно собирает Game.unity со всеми объектами: камера + EventSystem +
// Background+Playfield, Spawner, GameSession, Canvas с HUD и GameOverPanel.
// Подразумевается, что AssetForge уже создал префаб Target и конфиги.
public static class SceneBuilderGame
{
    public const string ScenePath = "Assets/_Project/Scenes/Game.unity";

    public static void Build()
    {
        // 1. Пустая сцена
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Камера + Physics2DRaycaster (ровно 1 hit — клик идёт только в
        //    ближайший по z коллайдер; Target ставится на z=-0.5, Background
        //    на z=0, поэтому Target всегда «забирает» клик)
        var camera = CreateMainCamera();

        // 3. EventSystem — обязательно для IPointerClickHandler
        CreateEventSystem();

        // 4. Фон с Playfield-компонентом
        CreateBackground();

        // 5. GameSession (SFX-источник, ссылка на сложность)
        var session = CreateGameSession();

        // 6. Spawner
        CreateSpawner();

        // 7. Canvas с HUD и GameOverPanel
        CreateCanvas();

        // 8. Сохраняем и регистрируем в BuildSettings
        EditorSceneManager.MarkSceneDirty(scene);
        EnsureDir(System.IO.Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath, makeFirst: false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SceneBuilderGame] Сцена сохранена: " + ScenePath);
    }

    // ===== Камера =====

    static Camera CreateMainCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.13f, 0.16f, 0.22f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 100f;
        go.transform.position = new Vector3(0, 0, -10);

        go.AddComponent<AudioListener>();
        var raycaster = go.AddComponent<Physics2DRaycaster>();
        raycaster.maxRayIntersections = 1;
        return cam;
    }

    // ===== EventSystem =====

    static void CreateEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        // 1. Active Input Handling = Both, поэтому подойдёт StandaloneInputModule
        //    (старый Input Manager под капотом)
        go.AddComponent<StandaloneInputModule>();
    }

    // ===== Фон/Playfield =====

    static void CreateBackground()
    {
        var go = new GameObject("Background");
        go.transform.position = new Vector3(0, 0, 0);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            AssetForge.BackgroundSpritePath);
        sr.sortingOrder = -100;
        sr.color = new Color(0.16f, 0.20f, 0.27f);
        // 64x64 PPU=128 → 0.5x0.5 unit. Растягиваем на видимую область + запас
        go.transform.localScale = new Vector3(40f, 24f, 1f);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.5f, 0.5f);   // в локальных координатах спрайта
        col.isTrigger = false;

        var pf = go.AddComponent<Playfield>();
        pf.missPenalty = 5;
        pf.missEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            AssetForge.MissEffectPrefabPath);
        pf.missClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            AssetForge.MissClipPath);
        EditorUtility.SetDirty(pf);
    }

    // ===== GameSession =====

    static GameSession CreateGameSession()
    {
        var go = new GameObject("GameSession");
        var session = go.AddComponent<GameSession>();

        var audio = go.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        session.sfxSource = audio;
        session.winClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            AssetForge.WinClipPath);

        var easyDiff = AssetDatabase.LoadAssetAtPath<DifficultyConfig>(
            AssetForge.DifficultyEasyPath);
        var normalDiff = AssetDatabase.LoadAssetAtPath<DifficultyConfig>(
            AssetForge.DifficultyNormalPath);
        var hardDiff = AssetDatabase.LoadAssetAtPath<DifficultyConfig>(
            AssetForge.DifficultyHardPath);
        session.difficulty = normalDiff;
        session.availableDifficulties = new[] { easyDiff, normalDiff, hardDiff };

        EditorUtility.SetDirty(session);
        return session;
    }

    // ===== Spawner =====

    static void CreateSpawner()
    {
        var go = new GameObject("Spawner");
        var sp = go.AddComponent<Spawner>();
        sp.targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            AssetForge.TargetPrefabPath);
        sp.normalConfig = AssetDatabase.LoadAssetAtPath<TargetTypeConfig>(
            AssetForge.NormalConfigPath);
        // Шаг 1: ловушки не спавнятся (trapChance=0). На Шаге 2 подменим конфиг.
        sp.trapConfig = AssetDatabase.LoadAssetAtPath<TargetTypeConfig>(
            AssetForge.TrapConfigPath);
        // Камера найдётся через Camera.main в OnEnable
        EditorUtility.SetDirty(sp);
    }

    // ===== Canvas (HUD + GameOverPanel) =====

    static void CreateCanvas()
    {
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. HUD: счёт и таймер
        var hudGO = new GameObject("HUD");
        hudGO.transform.SetParent(canvasGO.transform, false);
        var hud = hudGO.AddComponent<HUD>();

        hud.scoreText = UiHelpers.Text(
            parent: hudGO.transform,
            name: "ScoreText",
            font: font,
            fontSize: 48,
            anchor: new Vector2(0, 1),
            pivot: new Vector2(0, 1),
            anchoredPos: new Vector2(40, -30),
            size: new Vector2(600, 80),
            alignment: TextAnchor.UpperLeft,
            text: "Счёт: 0");

        hud.timerText = UiHelpers.Text(
            parent: hudGO.transform,
            name: "TimerText",
            font: font,
            fontSize: 56,
            anchor: new Vector2(0.5f, 1),
            pivot: new Vector2(0.5f, 1),
            anchoredPos: new Vector2(0, -30),
            size: new Vector2(400, 80),
            alignment: TextAnchor.UpperCenter,
            text: "Время: 0.0");

        EditorUtility.SetDirty(hud);

        // 2. GameOverPanel + NameInputDialog
        var panel = CreateGameOverPanel(canvasGO.transform, font);
        var dialog = CreateNameInputDialog(canvasGO.transform, font);
        panel.nameDialog = dialog;
        EditorUtility.SetDirty(panel);
    }

    static NameInputDialog CreateNameInputDialog(Transform canvasTr, Font font)
    {
        var go = new GameObject("NameInputDialog");
        go.transform.SetParent(canvasTr, false);
        var dialog = go.AddComponent<NameInputDialog>();

        // 1. Root — затемнение во весь экран
        var rootGO = new GameObject("Root");
        rootGO.transform.SetParent(go.transform, false);
        var rootImg = rootGO.AddComponent<Image>();
        rootImg.color = new Color(0, 0, 0, 0.6f);
        UiHelpers.StretchFull(rootGO.GetComponent<RectTransform>());
        dialog.root = rootGO;

        // 2. Прямоугольник диалога
        var box = new GameObject("Box");
        box.transform.SetParent(rootGO.transform, false);
        var boxImg = box.AddComponent<Image>();
        boxImg.color = new Color(0.18f, 0.22f, 0.30f, 1f);
        var boxRT = box.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.anchoredPosition = Vector2.zero;
        boxRT.sizeDelta = new Vector2(800, 460);

        // 3. Заголовок
        UiHelpers.Text(
            parent: box.transform,
            name: "Title",
            font: font,
            fontSize: 56,
            anchor: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(0, 150),
            size: new Vector2(700, 80),
            alignment: TextAnchor.MiddleCenter,
            text: "Введите имя");

        // 4. InputField
        dialog.nameField = CreateInputField(
            parent: box.transform,
            font: font,
            anchoredPos: new Vector2(0, 20),
            size: new Vector2(620, 90));

        // 5. Кнопка OK
        dialog.submitButton = UiHelpers.Button(
            parent: box.transform,
            name: "SubmitButton",
            font: font,
            label: "OK",
            color: new Color(0.25f, 0.65f, 0.95f),
            anchoredPos: new Vector2(0, -130),
            size: new Vector2(320, 100));

        EditorUtility.SetDirty(dialog);
        return dialog;
    }

    static InputField CreateInputField(Transform parent, Font font,
        Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject("NameField");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.95f);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        // Внутренний текст для ввода
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.AddComponent<Text>();
        text.font = font;
        text.fontSize = 44;
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(20, 5);
        textRT.offsetMax = new Vector2(-20, -5);

        // Placeholder
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(go.transform, false);
        var ph = phGO.AddComponent<Text>();
        ph.font = font;
        ph.fontSize = 44;
        ph.color = new Color(0.5f, 0.5f, 0.5f);
        ph.alignment = TextAnchor.MiddleLeft;
        ph.text = "Игрок";
        var phRT = phGO.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(20, 5);
        phRT.offsetMax = new Vector2(-20, -5);

        var input = go.AddComponent<InputField>();
        input.textComponent = text;
        input.placeholder = ph;
        input.characterLimit = 12;
        input.contentType = InputField.ContentType.Standard;
        return input;
    }

    static GameOverPanel CreateGameOverPanel(Transform canvasTr, Font font)
    {
        var panelGO = new GameObject("GameOverPanel");
        panelGO.transform.SetParent(canvasTr, false);
        var go = panelGO.AddComponent<GameOverPanel>();

        // 1. Корневой объект (его же будем выключать в OnEnable)
        var rootGO = new GameObject("Root");
        rootGO.transform.SetParent(panelGO.transform, false);
        var rootImg = rootGO.AddComponent<Image>();
        rootImg.color = new Color(0, 0, 0, 0.75f);
        var rootRT = rootGO.GetComponent<RectTransform>();
        UiHelpers.StretchFull(rootRT);
        go.root = rootGO;

        // 2. Title
        go.titleText = UiHelpers.Text(
            parent: rootGO.transform,
            name: "Title",
            font: font,
            fontSize: 96,
            anchor: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(0, 160),
            size: new Vector2(900, 140),
            alignment: TextAnchor.MiddleCenter,
            text: "Время вышло");

        // 3. Финальный счёт
        go.finalScoreText = UiHelpers.Text(
            parent: rootGO.transform,
            name: "FinalScore",
            font: font,
            fontSize: 64,
            anchor: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(0, 30),
            size: new Vector2(800, 100),
            alignment: TextAnchor.MiddleCenter,
            text: "Итог: 0");

        // 4. Кнопка Заново
        go.restartButton = UiHelpers.Button(
            parent: rootGO.transform,
            name: "RestartButton",
            font: font,
            label: "Заново",
            color: new Color(0.2f, 0.6f, 0.9f),
            anchoredPos: new Vector2(0, -100),
            size: new Vector2(360, 100));

        // 5. Кнопка Лидерборд
        go.leaderboardButton = UiHelpers.Button(
            parent: rootGO.transform,
            name: "LeaderboardButton",
            font: font,
            label: "Лидерборд",
            color: new Color(0.4f, 0.55f, 0.7f),
            anchoredPos: new Vector2(0, -210),
            size: new Vector2(360, 90));

        // 6. Кнопка Меню
        go.menuButton = UiHelpers.Button(
            parent: rootGO.transform,
            name: "MenuButton",
            font: font,
            label: "В меню",
            color: new Color(0.4f, 0.4f, 0.4f),
            anchoredPos: new Vector2(0, -310),
            size: new Vector2(360, 90));

        EditorUtility.SetDirty(go);
        return go;
    }

    static void EnsureDir(string path)
    {
        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);
    }

    static void AddSceneToBuildSettings(string scenePath, bool makeFirst)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        var existing = scenes.FindIndex(s => s.path == scenePath);
        var entry = new EditorBuildSettingsScene(scenePath, true);
        if (existing >= 0) scenes[existing] = entry;
        else if (makeFirst) scenes.Insert(0, entry);
        else scenes.Add(entry);
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
