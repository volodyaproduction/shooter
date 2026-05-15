using UnityEditor;
using UnityEngine;

// Главный entry-point CLI. Вызывается из терминала командой:
//   Unity -batchmode -projectPath ... -executeMethod Bootstrap.BuildAll -quit
// Порядок: ассеты (PNG, конфиги, префабы) → сцена Game → BuildSettings.
public static class Bootstrap
{
    [MenuItem("Tools/Build All")]
    public static void BuildAll()
    {
        // 1. Генерируем PNG-плейсхолдеры, аудио-плейсхолдеры, конфиги, префабы
        AssetForge.BuildAll();

        // 2. Собираем игровые сцены
        SceneBuilderGame.Build();
        SceneBuilderMainMenu.Build();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Bootstrap] BuildAll завершён.");
    }
}
