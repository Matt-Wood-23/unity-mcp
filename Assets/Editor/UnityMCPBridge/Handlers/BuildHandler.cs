using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class BuildHandler
    {
        public static string GetBuildSettings(string body)
        {
            try
            {
                var scenes = new List<BuildSceneInfo>();
                foreach (var scene in EditorBuildSettings.scenes)
                {
                    scenes.Add(new BuildSceneInfo
                    {
                        Path = scene.path,
                        Enabled = scene.enabled,
                        BuildIndex = scenes.Count
                    });
                }

                return JsonConvert.SerializeObject(new BuildSettingsResult
                {
                    Success = true,
                    ActiveBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                    ActiveBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup.ToString(),
                    Scenes = scenes,
                    DevelopmentBuild = EditorUserBuildSettings.development,
                    ConnectWithProfiler = EditorUserBuildSettings.connectProfiler,
                    AllowDebugging = EditorUserBuildSettings.allowDebugging
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new BuildSettingsResult
                {
                    Success = false,
                    Message = $"Error: {e.Message}"
                });
            }
        }

        public static string SetBuildScenes(string body)
        {
            var request = JsonConvert.DeserializeObject<SetBuildScenesRequest>(body);
            if (request == null || request.ScenePaths == null)
                return Error("Invalid request body. Provide scenePaths array.");

            try
            {
                var scenes = new List<EditorBuildSettingsScene>();

                if (request.AddToExisting == true)
                {
                    scenes.AddRange(EditorBuildSettings.scenes);
                }

                foreach (var path in request.ScenePaths)
                {
                    // Accept either full path or scene name
                    string scenePath = path;
                    if (!path.StartsWith("Assets/"))
                    {
                        var guids = AssetDatabase.FindAssets($"{path} t:Scene");
                        if (guids.Length > 0)
                            scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    }

                    // Avoid duplicates
                    bool exists = false;
                    foreach (var s in scenes)
                        if (s.path == scenePath) { exists = true; break; }

                    if (!exists)
                        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                }

                EditorBuildSettings.scenes = scenes.ToArray();

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Build scene list updated ({scenes.Count} scenes)"
                });
            }
            catch (Exception e)
            {
                return Error($"Error setting build scenes: {e.Message}");
            }
        }

        public static string SwitchBuildTarget(string body)
        {
            var request = JsonConvert.DeserializeObject<SwitchBuildTargetRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.BuildTarget))
                return Error("buildTarget is required");

            try
            {
                if (!TryParseBuildTarget(request.BuildTarget, out BuildTarget target, out BuildTargetGroup group))
                    return Error($"Unknown build target '{request.BuildTarget}'. Valid targets: StandaloneWindows64, StandaloneOSX, StandaloneLinux64, Android, iOS, WebGL, PS4, PS5, XboxOne");

                bool success = EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = success,
                    Message = success
                        ? $"Switched build target to {request.BuildTarget}"
                        : $"Failed to switch to {request.BuildTarget}"
                });
            }
            catch (Exception e)
            {
                return Error($"Error switching build target: {e.Message}");
            }
        }

        public static string BuildPlayer(string body)
        {
            var request = JsonConvert.DeserializeObject<BuildPlayerRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.OutputPath))
                return Error("outputPath is required");

            try
            {
                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;

                if (!string.IsNullOrEmpty(request.BuildTarget))
                {
                    if (!TryParseBuildTarget(request.BuildTarget, out target, out group))
                        return Error($"Unknown build target: {request.BuildTarget}");
                }

                var options = BuildOptions.None;
                if (request.Development) options |= BuildOptions.Development;
                if (request.AutoRunPlayer) options |= BuildOptions.AutoRunPlayer;
                if (request.ConnectWithProfiler) options |= BuildOptions.ConnectWithProfiler;

                var buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = GetEnabledScenePaths(),
                    locationPathName = request.OutputPath,
                    target = target,
#pragma warning disable CS0618 // targetGroup is deprecated in Unity 2023.1+ but needed for older versions
                    targetGroup = group,
#pragma warning restore CS0618
                    options = options
                };

                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                var summary = report.summary;

                return JsonConvert.SerializeObject(new BuildPlayerResult
                {
                    Success = summary.result == BuildResult.Succeeded,
                    Message = $"Build {summary.result}: {summary.totalErrors} errors, {summary.totalWarnings} warnings",
                    OutputPath = summary.outputPath,
                    BuildTime = (float)summary.totalTime.TotalSeconds,
                    TotalSize = summary.totalSize,
                    ErrorCount = summary.totalErrors,
                    WarningCount = summary.totalWarnings
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return Error($"Error building player: {e.Message}");
            }
        }

        private static string[] GetEnabledScenePaths()
        {
            var paths = new List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled) paths.Add(s.path);
            return paths.ToArray();
        }

        private static bool TryParseBuildTarget(string name, out BuildTarget target, out BuildTargetGroup group)
        {
            switch (name.ToLower())
            {
                case "standalonewindows" or "windows":
                    target = BuildTarget.StandaloneWindows; group = BuildTargetGroup.Standalone; return true;
                case "standalonewindows64" or "windows64":
                    target = BuildTarget.StandaloneWindows64; group = BuildTargetGroup.Standalone; return true;
                case "standaloneosx" or "osx" or "mac":
                    target = BuildTarget.StandaloneOSX; group = BuildTargetGroup.Standalone; return true;
                case "standalonelinux64" or "linux":
                    target = BuildTarget.StandaloneLinux64; group = BuildTargetGroup.Standalone; return true;
                case "android":
                    target = BuildTarget.Android; group = BuildTargetGroup.Android; return true;
                case "ios":
                    target = BuildTarget.iOS; group = BuildTargetGroup.iOS; return true;
                case "webgl":
                    target = BuildTarget.WebGL; group = BuildTargetGroup.WebGL; return true;
                case "ps4":
                    target = BuildTarget.PS4; group = BuildTargetGroup.PS4; return true;
                case "ps5":
                    target = BuildTarget.PS5; group = BuildTargetGroup.PS5; return true;
                case "xboxone":
                    target = BuildTarget.XboxOne; group = BuildTargetGroup.XboxOne; return true;
                default:
                    target = BuildTarget.StandaloneWindows64;
                    group = BuildTargetGroup.Standalone;
                    return false;
            }
        }

        private static string Error(string msg) =>
            JsonConvert.SerializeObject(new OperationResult { Success = false, Message = msg });
    }
}
