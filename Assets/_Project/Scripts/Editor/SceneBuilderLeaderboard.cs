using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Сборка сцены Leaderboard.unity: ScrollRect с вертикальным списком всех
// игроков, медали для топ-3, кнопка Назад. Сам список собирается во время
// исполнения LeaderboardView.cs из ответа /api/leaderboard-top.
public static class SceneBuilderLeaderboard
{
    public const string ScenePath = "Assets/_Project/Scenes/Leaderboard.unity";

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

        // 5. Заголовок
        UiHelpers.Text(
            parent: canvasGO.transform,
            name: "Title",
            font: font,
            fontSize: 100,
            anchor: new Vector2(0.5f, 1f),
            pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0, -40),
            size: new Vector2(900, 140),
            alignment: TextAnchor.MiddleCenter,
            text: "Лидерборд");

        // 6. ScrollRect-контейнер
        var view = canvasGO.AddComponent<LeaderboardView>();
        view.font = font;
        BuildScrollRect(canvasGO, view);

        // 7. Кнопка возврата
        view.backButton = UiHelpers.Button(
            parent: canvasGO.transform,
            name: "BackButton",
            font: font,
            label: "В меню",
            color: new Color(0.25f, 0.65f, 0.95f),
            anchoredPos: new Vector2(0, -480),
            size: new Vector2(360, 100));

        EditorUtility.SetDirty(view);

        // 8. Сохраняем сцену и добавляем в BuildSettings
        EditorSceneManager.MarkSceneDirty(scene);
        EnsureDir(System.IO.Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddScene(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[SceneBuilderLeaderboard] Сцена сохранена: " + ScenePath);
    }

    static void BuildScrollRect(GameObject canvasGO, LeaderboardView view)
    {
        // 1. Внешняя рамка ScrollRect — растянута на центральной части экрана
        var scrollGO = new GameObject("Scroll", typeof(RectTransform));
        scrollGO.transform.SetParent(canvasGO.transform, false);
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRT.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRT.pivot = new Vector2(0.5f, 0.5f);
        scrollRT.anchoredPosition = new Vector2(0, 30);
        scrollRT.sizeDelta = new Vector2(1200, 700);

        var scrollBg = scrollGO.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.25f);

        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        // Маска чтобы строки не вылезали за рамку — Mask + Image на той же GO
        var mask = scrollGO.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // 2. Viewport — обязательный компонент Scroll Rect
        var viewportGO = new GameObject("Viewport", typeof(RectTransform));
        viewportGO.transform.SetParent(scrollGO.transform, false);
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        UiHelpers.StretchFull(viewportRT);
        viewportGO.AddComponent<Image>().color =
            new Color(1f, 1f, 1f, 0.001f);  // прозрачная заглушка для Mask
        viewportGO.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewportRT;

        // 3. Content — куда LeaderboardView вставляет строки
        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childAlignment = TextAnchor.UpperCenter;

        var fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentRT;
        view.content = contentRT;

        // 4. Текст статуса/загрузки поверх скролла
        view.statusText = UiHelpers.Text(
            parent: canvasGO.transform,
            name: "StatusText",
            font: view.font,
            fontSize: 36,
            anchor: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(0, 30),
            size: new Vector2(900, 80),
            alignment: TextAnchor.MiddleCenter,
            text: "Загрузка...");
    }

    static void EnsureDir(string path)
    {
        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);
    }

    static void AddScene(string scenePath)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == scenePath);
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
