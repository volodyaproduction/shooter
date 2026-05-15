using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Скрипт сборки: WebGL (для Vercel) и Windows-Standalone (.exe).
// Запускается:
//   Unity -batchmode -projectPath . -executeMethod BuildScript.BuildWebGL -quit
//   Unity -batchmode -projectPath . -executeMethod BuildScript.BuildWindows -quit
public static class BuildScript
{
    const string WebOutDir = "web";
    const string WinOutDir = "build/Windows";
    const string WinExeName = "Shooter.exe";

    public static void BuildAll()
    {
        BuildWindows();
        BuildWebGL();
    }

    public static void BuildWindows()
    {
        EnsureDirectory(WinOutDir);

        PlayerSettings.companyName = "Shooter";
        PlayerSettings.productName = "Shooter";

        var options = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = Path.Combine(WinOutDir, WinExeName),
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        ReportResult("Windows", report);
    }

    public static void BuildWebGL()
    {
        EnsureDirectory(WebOutDir);

        // 1. WebGL-настройки: Brotli + Fallback=ON (Vercel без vercel.json),
        //    потоки выкл (без COOP/COEP), exception support выкл (меньше билд)
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.initialMemorySize = 64;
        PlayerSettings.runInBackground = false;
        PlayerSettings.companyName = "Shooter";
        PlayerSettings.productName = "Shooter";

        // 2. Low quality — выкидываем тяжёлые шейдеры
        SetQualityLow();

        var options = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = WebOutDir,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        ReportResult("WebGL", report);
    }

    static string[] GetEnabledScenes()
    {
        var list = new List<string>();
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.enabled) list.Add(s.path);
        }
        if (list.Count == 0)
            Debug.LogError("[BuildScript] В BuildSettings нет ни одной сцены.");
        return list.ToArray();
    }

    static void SetQualityLow()
    {
        var levels = QualitySettings.names;
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].Equals("Low", System.StringComparison.OrdinalIgnoreCase))
            {
                QualitySettings.SetQualityLevel(i, true);
                return;
            }
        }
    }

    static void ReportResult(string label, BuildReport report)
    {
        var summary = report.summary;
        Debug.Log($"[BuildScript] {label}: {summary.result}, "
                  + $"размер {summary.totalSize / 1024 / 1024} МБ, "
                  + $"время {summary.totalTime}");
        if (summary.result != BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
        }
    }

    static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }
}
