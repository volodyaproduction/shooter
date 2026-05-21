using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Программно собирает сцену MainMenu.unity: главное меню (Старт / Имя /
// Лидерборд / Выход) + диалог смены имени (NameInputDialog). Выбора сложности
// больше нет — единый пресет в Configs/Difficulty_Normal.asset.
//
// Помимо UI на сцену кладётся объект LeaderboardClient (DontDestroyOnLoad)
// — после первого захода в меню он переживёт переходы в Game и Leaderboard.
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

        // 4. LeaderboardClient (живёт между сценами)
        var clientGO = new GameObject("LeaderboardClient");
        clientGO.AddComponent<LeaderboardClient>();

        // 5. Canvas
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

        // 6. Заголовок
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

        // 7. Главная панель
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

        menu.nameButton = UiHelpers.Button(
            parent: mainPanel.transform,
            name: "NameButton",
            font: font,
            label: "Имя: не задано",
            color: new Color(0.5f, 0.5f, 0.6f),
            anchoredPos: new Vector2(0, -50),
            size: new Vector2(440, 100));
        menu.nameButtonLabel = menu.nameButton.GetComponentInChildren<Text>();

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

        // 8. NameInputDialog (используется для смены имени из меню)
        var dialog = CreateNameInputDialog(canvasGO.transform, font);
        menu.nameDialog = dialog;

        EditorUtility.SetDirty(menu);

        // 9. Сохраняем сцену и регистрируем первой в BuildSettings
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

    public static NameInputDialog CreateNameInputDialog(Transform canvasTr, Font font)
    {
        var go = new GameObject("NameInputDialog", typeof(RectTransform));
        go.transform.SetParent(canvasTr, false);
        UiHelpers.StretchFull(go.GetComponent<RectTransform>());
        var dialog = go.AddComponent<NameInputDialog>();

        var rootGO = new GameObject("Root");
        rootGO.transform.SetParent(go.transform, false);
        var rootImg = rootGO.AddComponent<Image>();
        rootImg.color = new Color(0, 0, 0, 0.7f);
        UiHelpers.StretchFull(rootGO.GetComponent<RectTransform>());
        rootGO.SetActive(false);
        dialog.root = rootGO;

        var box = new GameObject("Box");
        box.transform.SetParent(rootGO.transform, false);
        var boxImg = box.AddComponent<Image>();
        boxImg.color = new Color(0.18f, 0.22f, 0.30f, 1f);
        var boxRT = box.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.anchoredPosition = Vector2.zero;
        boxRT.sizeDelta = new Vector2(960, 660);

        UiHelpers.Text(
            parent: box.transform,
            name: "Title",
            font: font,
            fontSize: 56,
            anchor: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(0, 240),
            size: new Vector2(900, 80),
            alignment: TextAnchor.MiddleCenter,
            text: "Никнейм");

        dialog.hintText = UiHelpers.Text(
            parent: box.transform,
            name: "Hint",
            font: font,
            fontSize: 26,
            anchor: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(0, 120),
            size: new Vector2(900, 140),
            alignment: TextAnchor.MiddleCenter,
            text: string.Empty);
        dialog.hintText.horizontalOverflow = HorizontalWrapMode.Wrap;

        dialog.nameField = CreateInputField(
            parent: box.transform,
            font: font,
            anchoredPos: new Vector2(0, -10),
            size: new Vector2(820, 100));

        dialog.errorText = UiHelpers.Text(
            parent: box.transform,
            name: "Error",
            font: font,
            fontSize: 30,
            anchor: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(0, -110),
            size: new Vector2(900, 60),
            alignment: TextAnchor.MiddleCenter,
            text: string.Empty);
        dialog.errorText.color = new Color(1f, 0.6f, 0.6f);

        dialog.submitButton = UiHelpers.Button(
            parent: box.transform,
            name: "SubmitButton",
            font: font,
            label: "OK",
            color: new Color(0.25f, 0.65f, 0.95f),
            anchoredPos: new Vector2(0, -220),
            size: new Vector2(360, 100));

        EditorUtility.SetDirty(dialog);
        return dialog;
    }

    public static InputField CreateInputField(Transform parent, Font font,
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

        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(go.transform, false);
        var ph = phGO.AddComponent<Text>();
        ph.font = font;
        ph.fontSize = 44;
        ph.color = new Color(0.5f, 0.5f, 0.5f);
        ph.alignment = TextAnchor.MiddleLeft;
        ph.text = "@vova";
        var phRT = phGO.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(20, 5);
        phRT.offsetMax = new Vector2(-20, -5);

        var input = go.AddComponent<InputField>();
        input.textComponent = text;
        input.placeholder = ph;
        input.characterLimit = 24;
        input.contentType = InputField.ContentType.Standard;
        return input;
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
