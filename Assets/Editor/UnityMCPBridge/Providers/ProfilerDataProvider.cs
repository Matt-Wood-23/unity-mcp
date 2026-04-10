using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Providers
{
    public static class ProfilerDataProvider
    {
        public static string GetProfilerData()
        {
            try
            {
                var data = new ProfilerData();

                // Memory info
                data.Memory = new MemoryData
                {
                    TotalAllocatedMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f),
                    TotalReservedMB = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f),
                    TotalUnusedReservedMB = Profiler.GetTotalUnusedReservedMemoryLong() / (1024f * 1024f),
                    MonoUsedMB = Profiler.GetMonoUsedSizeLong() / (1024f * 1024f),
                    MonoHeapMB = Profiler.GetMonoHeapSizeLong() / (1024f * 1024f),
                    GfxDriverAllocatedMB = Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024f * 1024f),
                    TempAllocatorMB = Profiler.GetTempAllocatorSize() / (1024f * 1024f)
                };

                // Rendering stats (available during play mode or from last frame)
                data.Rendering = new RenderingData
                {
                    CurrentResolution = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}@{Screen.currentResolution.refreshRateRatio}",
                    ScreenResolution = $"{Screen.width}x{Screen.height}",
                    QualityLevel = QualitySettings.names[QualitySettings.GetQualityLevel()],
                    VSyncCount = QualitySettings.vSyncCount,
                    TargetFrameRate = Application.targetFrameRate,
                    MaxTextureSize = QualitySettings.globalTextureMipmapLimit
                };

                // Object counts
                data.ObjectCounts = new ObjectCountData
                {
                    GameObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length,
                    Cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length,
                    Lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length,
                    Renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length,
                    Rigidbodies = UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length,
                    AudioSources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length,
                    ParticleSystems = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length,
                    Canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length,
                    Animators = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Length
                };

                // Asset counts
                data.AssetCounts = new AssetCountData
                {
                    Materials = AssetDatabase.FindAssets("t:Material").Length,
                    Textures = AssetDatabase.FindAssets("t:Texture").Length,
                    Meshes = AssetDatabase.FindAssets("t:Mesh").Length,
                    AudioClips = AssetDatabase.FindAssets("t:AudioClip").Length,
                    Prefabs = AssetDatabase.FindAssets("t:Prefab").Length,
                    Scripts = AssetDatabase.FindAssets("t:Script").Length,
                    Shaders = AssetDatabase.FindAssets("t:Shader").Length,
                    Animations = AssetDatabase.FindAssets("t:AnimationClip").Length,
                    ScriptableObjects = AssetDatabase.FindAssets("t:ScriptableObject").Length
                };

                // Time info (useful during play mode)
                data.Time = new TimeData
                {
                    IsPlaying = Application.isPlaying,
                    TimeSinceStartup = (float)EditorApplication.timeSinceStartup,
                    RealtimeSinceStartup = Time.realtimeSinceStartup,
                    DeltaTime = Time.deltaTime,
                    FixedDeltaTime = Time.fixedDeltaTime,
                    TimeScale = Time.timeScale,
                    FrameCount = Time.frameCount
                };

                // Physics settings
                data.Physics = new PhysicsData
                {
                    Gravity = new Vector3Data(UnityEngine.Physics.gravity),
                    DefaultSolverIterations = UnityEngine.Physics.defaultSolverIterations,
                    DefaultSolverVelocityIterations = UnityEngine.Physics.defaultSolverVelocityIterations,
                    BounceThreshold = UnityEngine.Physics.bounceThreshold,
                    DefaultContactOffset = UnityEngine.Physics.defaultContactOffset,
                    SimulationMode = UnityEngine.Physics.simulationMode.ToString()
                };

                return JsonConvert.SerializeObject(new
                {
                    Success = true,
                    data.Memory,
                    data.Rendering,
                    data.ObjectCounts,
                    data.AssetCounts,
                    data.Time,
                    data.Physics
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error getting profiler data: {e.Message}"
                });
            }
        }
    }
}
