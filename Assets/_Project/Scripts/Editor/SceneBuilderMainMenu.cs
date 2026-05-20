using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Программно собирает сцену MainMenu.unity с двумя панелями:
// главное меню (Старт/Сложность/Лидерборд/Выход) и выбор сложности.
public static class SceneBuilderMainMenu
{
    public const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";

    public static void Build()
    {
        // 1. Пустая сцена
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Камера
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.13f, 0.16f, 0.22f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.nearClipPlane = 0.3f;
        camGO.transform.position = new Vector3(0, 0, -10);
        camGO.AddComponent<AudioListener>();

        // 3. EventSystem
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // 4. Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var font = UiHelpers.LoadProjectFont();

        var menuRoot = new GameObject("Menu", typeof(RectTransform));
        menuRoot.transform.SetParent(canvasGO.transform, false);
        UiHelpers.StretchFull(menuRoot.GetComponent<RectTransform>());
        var menu = menuRoot.AddComponent<MenuController>();

        // 5. Заголовок (общий для обеих панелей)
        UiHelpers.Text(
            parent: canvasGO.transform,
            name: "Title",
            font: font,
            fontSize: 110,
            anchor: new Vector2(0.5f, 1f),
            pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0, -80),
            size: new Vector2(1400, 180),
            alignment: TextAnchor.MiddleCenter,
            text: "Кликер-шутер");

        // 6. Главная панель
        var mainPanel = CreatePanel(menuRoot.transform, "MainPanel");
        menu.mainPanel = mainPanel;
        menu.startButton = UiHelpers.Button(
            parent: mainPanel.transform,
            name: "StartButton",
            font: font,
            label: "Старт",
            color: new Color(0.25f, 0.65f, 0.95f),
            anchoredPos: new Vector2(0, 80),
            size: new Vector2(440, 110));
        menu.difficultyButton = UiHelpers.Button(
            parent: mainPanel.transform,
            name: "DifficultyButton",
            font: font,
            label: "Сложность: Нормально",
            color: new Color(0.5f, 0.5f, 0.6f),
            anchoredPos: new Vector2(0, -50),
            size: new Vector2(440, 100));
        // Текст внутри кнопки уже создан UiHelpers.Button — получим ссылку
        menu.difficultyButtonLabel =
            menu.difficultyButton.GetComponentInChildren<Text>();
        menu.leaderboardButton = UiHelpers.Button(
            parent: mainPanel.transform,
            name: "LeaderboardButton",
            font: font,
            label: "Лидерборд",
            color: new Color(0.5f, 0.5f, 0.6f),
            anchoredPos: new Vector2(0, -170),
            size: new Vector2(440, 100));
        menu.exitButton = UiHelpers.Button(
            parent: mainPanel.transform,
            name: "ExitButton",
            font: font,
            label: "Выход",
            color: new Color(0.55f, 0.35f, 0.35f),
            anchoredPos: new Vector2(0, -290),
            size: new Vector2(440, 100));

        // 7. Панель выбора сложности
        var diffPanel = CreatePanel(menuRoot.transform, "DifficultyPanel");
        menu.difficultyPanel = diffPanel;
        diffPanel.SetActive(false);
        menu.easyButton = UiHelpers.Button(
            parent: diffPanel.transform,
            name: "EasyButton",
            font: font,
            label: "Легко",
            color: new Color(0.35f, 0.7f, 0.4f),
            anchoredPos: new Vector2(0, 110),
            size: new Vector2(440, 100));
        menu.normalButton = UiHelpers.Button(
            parent: diffPanel.transform,
            name: "NormalButton",
            font: font,
            label: "Нормально",
            color: new Color(0.55f, 0.55f, 0.85f),
            anchoredPos: new Vector2(0, -10),
            size: new Vector2(440, 100));
        menu.hardButton = UiHelpers.Button(
            parent: diffPanel.transform,
            name: "HardButton",
            font: font,
            label: "Сложно",
            color: new Color(0.85f, 0.45f, 0.45f),
            anchoredPos: new Vector2(0, -130),
            size: new Vector2(440, 100));
        menu.backButton = UiHelpers.Button(
            parent: diffPanel.transform,
            name: "BackButton",
            font: font,
            label: "Назад",
            color: new Color(0.4f, 0.4f, 0.45f),
            anchoredPos: new Vector2(0, -250),
            size: new Vector2(440, 90));

        EditorUtility.SetDirty(menu);

        // 8. Сохраняем сцену и регистрируем как первую в BuildSettings
        EditorSceneManager.MarkSceneDirty(scene);
        EnsureDir(System.IO.Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneAsFirst(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[SceneBuilderMainMenu] Сцена сохранена: " + ScenePath);
    }

    static GameObject CreatePanel(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    static void EnsureDir(string path)
    {
        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);
    }

    static void AddSceneAsFirst(string scenePath)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == scenePath);
        list.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
