using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class BuildScript
{
    [MenuItem("Build/Build WebGL")]
    public static void BuildWebGL()
    {
        string outputPath = Path.GetFullPath(
            Path.Combine(Application.dataPath,
                "../../../../frontend/public/unity/AquaView3D"));

        Debug.Log($"[BuildScript] Output: {outputPath}");

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes      = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = outputPath,
            target      = BuildTarget.WebGL,
            options     = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);

        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"[BuildScript] ✅ Build succeeded: {report.summary.totalSize} bytes");
        else
            Debug.LogError($"[BuildScript] ❌ Build failed: {report.summary.result}");
    }
}
