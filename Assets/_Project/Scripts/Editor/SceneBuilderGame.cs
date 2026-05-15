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

        hud.scoreText = MakeText(
            parent: hudGO.transform,
            name: "ScoreText",
            font: font,
            fontSize: 48,
            anchor: new Vector2(0, 1),
            pivot: new Vector2(0, 1),
            anchoredPos: new Vector2(40, -30),
            size: new Vector2(600, 80),
            alignment: TextAnchor.UpperLeft,
            startText: "Счёт: 0");

        hud.timerText = MakeText(
            parent: hudGO.transform,
            name: "TimerText",
            font: font,
            fontSize: 56,
            anchor: new Vector2(0.5f, 1),
            pivot: new Vector2(0.5f, 1),
            anchoredPos: new Vector2(0, -30),
            size: new Vector2(400, 80),
            alignment: TextAnchor.UpperCenter,
            startText: "Время: 0.0");

        EditorUtility.SetDirty(hud);

        // 2. GameOverPanel
        var panel = CreateGameOverPanel(canvasGO.transform, font);
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
        StretchFull(rootRT);
        go.root = rootGO;

        // 2. Title
        go.titleText = MakeText(
            parent: rootGO.transform,
            name: "Title",
            font: font,
            fontSize: 96,
            anchor: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(0, 160),
            size: new Vector2(900, 140),
            alignment: TextAnchor.MiddleCenter,
            startText: "Время вышло");

        // 3. Финальный счёт
        go.finalScoreText = MakeText(
            parent: rootGO.transform,
            name: "FinalScore",
            font: font,
            fontSize: 64,
            anchor: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(0, 30),
            size: new Vector2(800, 100),
            alignment: TextAnchor.MiddleCenter,
            startText: "Итог: 0");

        // 4. Кнопка Заново
        go.restartButton = MakeButton(
            parent: rootGO.transform,
            name: "RestartButton",
            font: font,
            label: "Заново",
            color: new Color(0.2f, 0.6f, 0.9f),
            anchoredPos: new Vector2(0, -120),
            size: new Vector2(360, 110));

        // 5. Кнопка Меню (на Шаге 1 сцены MainMenu нет; скрипт сам проверит)
        go.menuButton = MakeButton(
            parent: rootGO.transform,
            name: "MenuButton",
            font: font,
            label: "В меню",
            color: new Color(0.4f, 0.4f, 0.4f),
            anchoredPos: new Vector2(0, -250),
            size: new Vector2(360, 90));

        EditorUtility.SetDirty(go);
        return go;
    }

    static Text MakeText(Transform parent, string name, Font font,
        int fontSize, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos,
        Vector2 size, TextAnchor alignment, string startText)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = startText;
        t.font = font;
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = alignment;

        // Контур, чтобы текст был читаем на любом фоне
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return t;
    }

    static Button MakeButton(Transform parent, string name, Font font,
        string label, Color color, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = font;
        text.fontSize = 44;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        var textRT = textGO.GetComponent<RectTransform>();
        StretchFull(textRT);

        return btn;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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
