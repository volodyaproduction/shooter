using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Сборка сцены Leaderboard.unity: топ-5 (rank/name/score) + кнопка Назад.
// Сцена попадает в BuildSettings последней, после Game.
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

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 5. Заголовок
        UiHelpers.Text(
            parent: canvasGO.transform,
            name: "Title",
            font: font,
            fontSize: 110,
            anchor: new Vector2(0.5f, 1f),
            pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0, -50),
            size: new Vector2(900, 160),
            alignment: TextAnchor.MiddleCenter,
            text: "Лидерборд");

        // 6. Контейнер с LeaderboardView и пятью строками
        var viewGO = new GameObject("LeaderboardView");
        viewGO.transform.SetParent(canvasGO.transform, false);
        var view = viewGO.AddComponent<LeaderboardView>();
        view.rankTexts = new Text[LeaderboardSaveSystem.TopSize];
        view.nameTexts = new Text[LeaderboardSaveSystem.TopSize];
        view.scoreTexts = new Text[LeaderboardSaveSystem.TopSize];

        // 7. Шапка таблицы
        UiHelpers.Text(
            parent: canvasGO.transform,
            name: "HeaderRank",
            font: font,
            fontSize: 36,
            anchor: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(-340, 280),
            size: new Vector2(120, 50),
            alignment: TextAnchor.MiddleCenter,
            text: "#");
        UiHelpers.Text(
            parent: canvasGO.transform,
            name: "HeaderName",
            font: font,
            fontSize: 36,
            anchor: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(-50, 280),
            size: new Vector2(420, 50),
            alignment: TextAnchor.MiddleLeft,
            text: "Имя");
        UiHelpers.Text(
            parent: canvasGO.transform,
            name: "HeaderScore",
            font: font,
            fontSize: 36,
            anchor: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(320, 280),
            size: new Vector2(220, 50),
            alignment: TextAnchor.MiddleRight,
            text: "Очки");

        // 8. Строки (rank/name/score)
        float startY = 200f;
        float rowH = 70f;
        for (int i = 0; i < LeaderboardSaveSystem.TopSize; i++)
        {
            float y = startY - i * rowH;
            view.rankTexts[i] = UiHelpers.Text(
                parent: canvasGO.transform,
                name: $"Row{i}_Rank",
                font: font,
                fontSize: 48,
                anchor: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPos: new Vector2(-340, y),
                size: new Vector2(120, 60),
                alignment: TextAnchor.MiddleCenter,
                text: (i + 1) + ".");
            view.nameTexts[i] = UiHelpers.Text(
                parent: canvasGO.transform,
                name: $"Row{i}_Name",
                font: font,
                fontSize: 48,
                anchor: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPos: new Vector2(-50, y),
                size: new Vector2(420, 60),
                alignment: TextAnchor.MiddleLeft,
                text: "—");
            view.scoreTexts[i] = UiHelpers.Text(
                parent: canvasGO.transform,
                name: $"Row{i}_Score",
                font: font,
                fontSize: 48,
                anchor: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPos: new Vector2(320, y),
                size: new Vector2(220, 60),
                alignment: TextAnchor.MiddleRight,
                text: "—");
        }

        // 9. Кнопки внизу: Назад и (опционально) Очистить
        view.backButton = UiHelpers.Button(
            parent: canvasGO.transform,
            name: "BackButton",
            font: font,
            label: "В меню",
            color: new Color(0.25f, 0.65f, 0.95f),
            anchoredPos: new Vector2(-160, -400),
            size: new Vector2(320, 100));
        view.clearButton = UiHelpers.Button(
            parent: canvasGO.transform,
            name: "ClearButton",
            font: font,
            label: "Очистить",
            color: new Color(0.55f, 0.35f, 0.35f),
            anchoredPos: new Vector2(180, -400),
            size: new Vector2(280, 100));

        EditorUtility.SetDirty(view);

        // 10. Сохраняем сцену и добавляем в BuildSettings
        EditorSceneManager.MarkSceneDirty(scene);
        EnsureDir(System.IO.Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddScene(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[SceneBuilderLeaderboard] Сцена сохранена: " + ScenePath);
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
