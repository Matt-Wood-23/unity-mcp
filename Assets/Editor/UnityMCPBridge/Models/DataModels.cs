using System.Collections.Generic;
using UnityEngine;

namespace UnityMCPBridge.Models
{
    // Scene Data
    public class SceneHierarchyData
    {
        public string SceneName { get; set; }
        public string ScenePath { get; set; }
        public bool IsDirty { get; set; }
        public int RootObjectCount { get; set; }
        public List<GameObjectNode> RootObjects { get; set; }
    }

    public class GameObjectNode
    {
        public int InstanceId { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public string Layer { get; set; }
        public bool IsActive { get; set; }
        public bool IsStatic { get; set; }
        public List<string> Components { get; set; }
        public int ChildCount { get; set; }
        public List<GameObjectNode> Children { get; set; }
    }

    public class GameObjectDetailData
    {
        public int InstanceId { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public string Layer { get; set; }
        public bool IsActive { get; set; }
        public bool IsStatic { get; set; }
        public TransformData Transform { get; set; }
        public List<ComponentData> Components { get; set; }
    }

    public class TransformData
    {
        public Vector3Data Position { get; set; }
        public Vector3Data Rotation { get; set; }
        public Vector3Data Scale { get; set; }
        public Vector3Data LocalPosition { get; set; }
        public Vector3Data LocalRotation { get; set; }
    }

    public class Vector3Data
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3Data() { }

        public Vector3Data(Vector3 v)
        {
            X = v.x;
            Y = v.y;
            Z = v.z;
        }

        public Vector3 ToVector3() => new Vector3(X, Y, Z);
    }

    // Component Data
    public class ComponentData
    {
        public string Type { get; set; }
        public string FullType { get; set; }
        public bool Enabled { get; set; }
        public List<PropertyData> Properties { get; set; }
    }

    public class PropertyData
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }

    // Project Data
    public class ProjectInfo
    {
        public string ProductName { get; set; }
        public string CompanyName { get; set; }
        public string Version { get; set; }
        public string UnityVersion { get; set; }
        public string Platform { get; set; }
        public string ProjectPath { get; set; }
        public bool IsPlaying { get; set; }
        public bool IsPaused { get; set; }
    }

    // Console Data
    public class ConsoleData
    {
        public int TotalCount { get; set; }
        public List<LogEntry> Logs { get; set; }
    }

    public class LogEntry
    {
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Type { get; set; }
        public string Timestamp { get; set; }
    }

    // Asset Data
    public class AssetListData
    {
        public string Filter { get; set; }
        public int Count { get; set; }
        public int TotalFound { get; set; }
        public List<AssetInfo> Assets { get; set; }
    }

    public class AssetInfo
    {
        public string Guid { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class ScriptListData
    {
        public int Count { get; set; }
        public List<ScriptInfo> Scripts { get; set; }
    }

    public class ScriptInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string ClassName { get; set; }
    }

    // Selection Data
    public class SelectionData
    {
        public SelectedObject ActiveGameObject { get; set; }
        public List<SelectedObject> SelectedObjects { get; set; }
        public List<string> SelectedAssetPaths { get; set; }
    }

    public class SelectedObject
    {
        public int InstanceId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }

    // Request Models (for POST operations)
    public class CreateGameObjectRequest
    {
        public string Name { get; set; }
        public string PrimitiveType { get; set; } // Optional: Cube, Sphere, Capsule, etc.
        public int? ParentId { get; set; }
        public Vector3Data Position { get; set; }
        public Vector3Data Rotation { get; set; }
        public Vector3Data Scale { get; set; }
    }

    public class ModifyGameObjectRequest
    {
        public int InstanceId { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public int? Layer { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsStatic { get; set; }
        public Vector3Data Position { get; set; }
        public Vector3Data Rotation { get; set; }
        public Vector3Data Scale { get; set; }
        public int? ParentId { get; set; }
    }

    public class DeleteGameObjectRequest
    {
        public int InstanceId { get; set; }
    }

    public class AddComponentRequest
    {
        public int InstanceId { get; set; }
        public string ComponentType { get; set; }
    }

    public class RemoveComponentRequest
    {
        public int InstanceId { get; set; }
        public string ComponentType { get; set; }
        public int? ComponentIndex { get; set; } // If multiple of same type
    }

    public class PlayModeRequest
    {
        public string Action { get; set; } // "play", "pause", "stop", "step"
    }

    public class SetPropertyRequest
    {
        public int InstanceId { get; set; }
        public string ComponentType { get; set; }
        public string PropertyName { get; set; }
        public object Value { get; set; }
        public int? ComponentIndex { get; set; } // If multiple of same type
    }

    public class FindGameObjectsRequest
    {
        public string Name { get; set; }
        public string Tag { get; set; }
        public string Layer { get; set; }
        public string HasComponent { get; set; }
        public bool ActiveOnly { get; set; }
        public bool ExactMatch { get; set; }
        public int MaxResults { get; set; }
    }

    public class InstantiatePrefabRequest
    {
        public string PrefabPath { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public Vector3Data Position { get; set; }
        public Vector3Data Rotation { get; set; }
        public Vector3Data Scale { get; set; }
    }

    public class LoadSceneRequest
    {
        public string ScenePath { get; set; }
        public bool Additive { get; set; }
        public bool Force { get; set; }
    }

    public class CreateMaterialRequest
    {
        public string Name { get; set; }
        public string Shader { get; set; }
        public ColorData Color { get; set; }
        public string SavePath { get; set; }
    }

    public class ModifyMaterialRequest
    {
        public string MaterialPath { get; set; }
        public ColorData Color { get; set; }
        public string PropertyName { get; set; }
        public object PropertyValue { get; set; }
    }

    public class AssignMaterialRequest
    {
        public int InstanceId { get; set; }
        public string MaterialPath { get; set; }
        public int? MaterialIndex { get; set; }
    }

    public class ColorData
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public float A { get; set; } = 1f;
    }

    // Response Models
    public class SceneInfo
    {
        public string Path { get; set; }
        public string Name { get; set; }
    }

    public class MaterialPropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }
    public class FoundGameObject
    {
        public int InstanceId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Tag { get; set; }
        public string Layer { get; set; }
        public bool IsActive { get; set; }
    }

    public class FindGameObjectsResult
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public List<FoundGameObject> GameObjects { get; set; }
    }
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? InstanceId { get; set; }
    }

    // Sprite Import Models
    public class SpriteImportSettings
    {
        public int PixelsPerUnit { get; set; } = 100;
        public string PivotMode { get; set; } = "Center";  // Center, Bottom, TopLeft, Custom
        public float? PivotX { get; set; } = 0.5f;
        public float? PivotY { get; set; } = 0.5f;
        public string FilterMode { get; set; } = "Point";  // Point, Bilinear
        public bool? GenerateMipMaps { get; set; } = false;
        public string SpriteMode { get; set; } = "Single";  // Single, Multiple
        public string Compression { get; set; } = "None";  // None, Compressed
        public int? MaxSize { get; set; }
    }

    public class ImportSpriteRequest
    {
        public string ImagePath { get; set; }
        public string DestinationPath { get; set; }
        public SpriteImportSettings Settings { get; set; }
    }

    public class ConfigureSpriteRequest
    {
        public string AssetPath { get; set; }
        public SpriteImportSettings Settings { get; set; }
    }

    public class SliceSpriteSheetRequest
    {
        public string AssetPath { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
    }

    public class CreateSpriteRendererRequest
    {
        public int? InstanceId { get; set; }
        public string Name { get; set; }
        public string SpritePath { get; set; }
        public string SortingLayer { get; set; }
        public int? OrderInLayer { get; set; }
        public ColorData Color { get; set; }
        public bool? FlipX { get; set; }
        public bool? FlipY { get; set; }
    }

    // Sprite Result Models
    public class SpriteImportResult : OperationResult
    {
        public string AssetPath { get; set; }
    }

    public class SliceSpriteSheetResult : OperationResult
    {
        public int SpriteCount { get; set; }
        public int CellWidth { get; set; }
        public int CellHeight { get; set; }
    }

    // Screenshot Models
    public class TakeScreenshotRequest
    {
        public string Source { get; set; } = "game"; // "game" or "scene"
        public int Width { get; set; } = 640;
        public int Height { get; set; } = 480;
        public string Format { get; set; } = "png"; // "png" or "jpg"
        public int Quality { get; set; } = 85; // JPG quality
        public string SavePath { get; set; }
    }

    public class ScreenshotResult : OperationResult
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; }
        public string Base64 { get; set; }
        public string SavePath { get; set; }
    }

    // Code Execution Models
    public class ExecuteCodeRequest
    {
        public string Code { get; set; }
    }

    public class CodeExecutionResult : OperationResult
    {
        public string Output { get; set; }
        public List<string> Logs { get; set; }
        public List<string> Errors { get; set; }
    }

    // UI Models
    public class Vector2Data
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    public class CreateUIElementRequest
    {
        public string ElementType { get; set; } // Canvas, Text, Button, Image, Panel, InputField, Slider, Toggle, Dropdown, ScrollView, RawImage
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public string Text { get; set; }
        public int? FontSize { get; set; }
        public ColorData Color { get; set; }
        public string SpritePath { get; set; }
        public Vector2Data AnchoredPosition { get; set; }
        public Vector2Data SizeDelta { get; set; }
        public Vector2Data AnchorMin { get; set; }
        public Vector2Data AnchorMax { get; set; }
        public Vector2Data Pivot { get; set; }
    }

    public class ModifyUIElementRequest
    {
        public int InstanceId { get; set; }
        public string Text { get; set; }
        public int? FontSize { get; set; }
        public ColorData Color { get; set; }
        public string SpritePath { get; set; }
        public string Alignment { get; set; }
        public bool? Interactable { get; set; }
        public Vector2Data AnchoredPosition { get; set; }
        public Vector2Data SizeDelta { get; set; }
        public Vector2Data AnchorMin { get; set; }
        public Vector2Data AnchorMax { get; set; }
        public Vector2Data Pivot { get; set; }
    }

    // Profiler Models
    public class ProfilerData
    {
        public MemoryData Memory { get; set; }
        public RenderingData Rendering { get; set; }
        public ObjectCountData ObjectCounts { get; set; }
        public AssetCountData AssetCounts { get; set; }
        public TimeData Time { get; set; }
        public PhysicsData Physics { get; set; }
    }

    public class MemoryData
    {
        public float TotalAllocatedMB { get; set; }
        public float TotalReservedMB { get; set; }
        public float TotalUnusedReservedMB { get; set; }
        public float MonoUsedMB { get; set; }
        public float MonoHeapMB { get; set; }
        public float GfxDriverAllocatedMB { get; set; }
        public float TempAllocatorMB { get; set; }
    }

    public class RenderingData
    {
        public string CurrentResolution { get; set; }
        public string ScreenResolution { get; set; }
        public string QualityLevel { get; set; }
        public int VSyncCount { get; set; }
        public int TargetFrameRate { get; set; }
        public int MaxTextureSize { get; set; }
    }

    public class ObjectCountData
    {
        public int GameObjects { get; set; }
        public int Cameras { get; set; }
        public int Lights { get; set; }
        public int Renderers { get; set; }
        public int Rigidbodies { get; set; }
        public int AudioSources { get; set; }
        public int ParticleSystems { get; set; }
        public int Canvases { get; set; }
        public int Animators { get; set; }
    }

    public class AssetCountData
    {
        public int Materials { get; set; }
        public int Textures { get; set; }
        public int Meshes { get; set; }
        public int AudioClips { get; set; }
        public int Prefabs { get; set; }
        public int Scripts { get; set; }
        public int Shaders { get; set; }
        public int Animations { get; set; }
        public int ScriptableObjects { get; set; }
    }

    public class TimeData
    {
        public bool IsPlaying { get; set; }
        public float TimeSinceStartup { get; set; }
        public float RealtimeSinceStartup { get; set; }
        public float DeltaTime { get; set; }
        public float FixedDeltaTime { get; set; }
        public float TimeScale { get; set; }
        public int FrameCount { get; set; }
    }

    public class PhysicsData
    {
        public Vector3Data Gravity { get; set; }
        public int DefaultSolverIterations { get; set; }
        public int DefaultSolverVelocityIterations { get; set; }
        public float BounceThreshold { get; set; }
        public float DefaultContactOffset { get; set; }
        public string SimulationMode { get; set; }
    }

    // Batch Operation Models
    public class BatchFilter
    {
        public string Name { get; set; }
        public string Tag { get; set; }
        public string Layer { get; set; }
        public string HasComponent { get; set; }
        public bool ActiveOnly { get; set; }
        public int MaxResults { get; set; }
    }

    public class BatchModifyRequest
    {
        public List<int> InstanceIds { get; set; }
        public BatchFilter Filter { get; set; }
        public string Tag { get; set; }
        public int? Layer { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsStatic { get; set; }
        public Vector3Data Position { get; set; }
        public Vector3Data Rotation { get; set; }
        public Vector3Data Scale { get; set; }
        public int? ParentId { get; set; }
        public string AddComponent { get; set; }
        public string RemoveComponent { get; set; }
    }

    public class BatchDeleteRequest
    {
        public List<int> InstanceIds { get; set; }
        public BatchFilter Filter { get; set; }
    }

    public class BatchOperationResult : OperationResult
    {
        public int AffectedCount { get; set; }
        public List<string> Errors { get; set; }
    }

    // Terrain Models
    public class CreateTerrainRequest
    {
        public string Name { get; set; }
        public float Width { get; set; } = 500;
        public float Length { get; set; } = 500;
        public float Height { get; set; } = 100;
        public int HeightmapResolution { get; set; } = 513;
        public int AlphamapResolution { get; set; } = 512;
        public Vector3Data Position { get; set; }
        public string SavePath { get; set; }
    }

    public class ModifyTerrainHeightRequest
    {
        public int InstanceId { get; set; }
        public string Operation { get; set; } // set, raise, lower, smooth, flatten, perlin
        public float Value { get; set; }
        public float Strength { get; set; } = 1f;
        public float AreaCenterX { get; set; } = -1; // Normalized 0-1, -1 means whole terrain
        public float AreaCenterZ { get; set; } = -1;
        public float AreaRadius { get; set; } = 0.1f;
        public int Seed { get; set; }
    }

    public class PaintTerrainTextureRequest
    {
        public int InstanceId { get; set; }
        public string TexturePath { get; set; }
        public float TileSize { get; set; } = 10;
        public int LayerIndex { get; set; }
        public float CenterX { get; set; } = 0.5f;
        public float CenterY { get; set; } = 0.5f;
        public float Radius { get; set; } = 0.1f;
        public float Strength { get; set; } = 1f;
    }

    public class PlaceTerrainTreesRequest
    {
        public int InstanceId { get; set; }
        public string PrefabPath { get; set; }
        public int PrototypeIndex { get; set; }
        public int Count { get; set; } = 50;
        public float MinScale { get; set; } = 0.8f;
        public float MaxScale { get; set; } = 1.2f;
        public float Density { get; set; } = 1f;
        public float AreaCenterX { get; set; } = 0.5f;
        public float AreaCenterZ { get; set; } = 0.5f;
        public float AreaRadius { get; set; } = 0.5f;
        public int Seed { get; set; }
    }

    public class TerrainInfoRequest
    {
        public int InstanceId { get; set; }
    }

    // ========== Animation/Animator Models ==========

    public class GetAnimatorInfoRequest
    {
        public int InstanceId { get; set; }
    }

    public class SetAnimatorParameterRequest
    {
        public int InstanceId { get; set; }
        public string ParameterName { get; set; }
        public string ParameterType { get; set; } // "float", "int", "bool", "trigger"
        public object Value { get; set; }
    }

    public class PlayAnimationRequest
    {
        public int InstanceId { get; set; }
        public string StateName { get; set; }
        public int Layer { get; set; }
        public float NormalizedTime { get; set; } = -1f;
    }

    public class AnimatorParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public class AnimatorLayerInfo
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public float Weight { get; set; }
    }

    public class AnimatorClipInfo
    {
        public string Name { get; set; }
        public float Length { get; set; }
        public bool IsLooping { get; set; }
        public float FrameRate { get; set; }
    }

    public class AnimatorInfoResult
    {
        public bool Success { get; set; }
        public string ControllerName { get; set; }
        public bool HasAnimator { get; set; }
        public bool IsPlaying { get; set; }
        public float Speed { get; set; }
        public List<AnimatorParameterInfo> Parameters { get; set; }
        public List<AnimatorLayerInfo> Layers { get; set; }
        public List<AnimatorClipInfo> Clips { get; set; }
        public string CurrentStateName { get; set; }
        public float CurrentStateNormalizedTime { get; set; }
        public string Message { get; set; }
    }

    // ========== Lighting & Environment Models ==========

    public class CreateLightRequest
    {
        public string Name { get; set; }
        public string LightType { get; set; } = "Point"; // Directional, Point, Spot, Area
        public ColorData Color { get; set; }
        public float Intensity { get; set; } = 1f;
        public float Range { get; set; } = 10f;
        public float SpotAngle { get; set; } = 30f;
        public string Shadows { get; set; } = "None"; // None, Hard, Soft
        public Vector3Data Position { get; set; }
        public Vector3Data Rotation { get; set; }
        public int? ParentId { get; set; }
    }

    public class ModifyLightRequest
    {
        public int InstanceId { get; set; }
        public ColorData Color { get; set; }
        public float? Intensity { get; set; }
        public float? Range { get; set; }
        public float? SpotAngle { get; set; }
        public string Shadows { get; set; }
        public string LightType { get; set; }
    }

    public class GetLightInfoRequest
    {
        public int InstanceId { get; set; }
    }

    public class LightInfoResult
    {
        public bool Success { get; set; }
        public int InstanceId { get; set; }
        public string Name { get; set; }
        public string LightType { get; set; }
        public ColorData Color { get; set; }
        public float Intensity { get; set; }
        public float Range { get; set; }
        public float SpotAngle { get; set; }
        public string Shadows { get; set; }
        public float ShadowStrength { get; set; }
        public string Message { get; set; }
    }

    public class SetEnvironmentRequest
    {
        public string SkyboxMaterialPath { get; set; }
        public string AmbientMode { get; set; } // Skybox, Trilight, Flat, Custom
        public ColorData AmbientColor { get; set; }
        public ColorData AmbientSkyColor { get; set; }
        public ColorData AmbientEquatorColor { get; set; }
        public ColorData AmbientGroundColor { get; set; }
        public float? AmbientIntensity { get; set; }
        public float? ReflectionIntensity { get; set; }
        public bool? Fog { get; set; }
        public ColorData FogColor { get; set; }
        public string FogMode { get; set; } // Linear, Exponential, ExponentialSquared
        public float? FogDensity { get; set; }
        public float? FogStartDistance { get; set; }
        public float? FogEndDistance { get; set; }
    }

    public class EnvironmentInfoResult
    {
        public bool Success { get; set; }
        public string SkyboxMaterial { get; set; }
        public string AmbientMode { get; set; }
        public ColorData AmbientColor { get; set; }
        public ColorData AmbientSkyColor { get; set; }
        public ColorData AmbientEquatorColor { get; set; }
        public ColorData AmbientGroundColor { get; set; }
        public float AmbientIntensity { get; set; }
        public float ReflectionIntensity { get; set; }
        public bool Fog { get; set; }
        public ColorData FogColor { get; set; }
        public string FogMode { get; set; }
        public float FogDensity { get; set; }
        public float FogStartDistance { get; set; }
        public float FogEndDistance { get; set; }
        public string Message { get; set; }
    }

    // ========== Physics Models ==========

    public class AddRigidbodyRequest
    {
        public int InstanceId { get; set; }
        public float Mass { get; set; } = 1f;
        public float Drag { get; set; } = 0f;
        public float AngularDrag { get; set; } = 0.05f;
        public bool UseGravity { get; set; } = true;
        public bool IsKinematic { get; set; } = false;
        public string CollisionDetection { get; set; } = "Discrete";
        public string Interpolation { get; set; } = "None";
        public string Constraints { get; set; } // comma-separated: "FreezePositionX,FreezeRotationY"
    }

    public class AddColliderRequest
    {
        public int InstanceId { get; set; }
        public string ColliderType { get; set; } // Box, Sphere, Capsule, Mesh
        public bool IsTrigger { get; set; } = false;
        public string PhysicMaterialPath { get; set; }
        public Vector3Data Center { get; set; }
        public Vector3Data Size { get; set; } // Box
        public float? Radius { get; set; } // Sphere/Capsule
        public float? Height { get; set; } // Capsule
        public int? Direction { get; set; } // Capsule: 0=X, 1=Y, 2=Z
    }

    public class SetPhysicsSettingsRequest
    {
        public Vector3Data Gravity { get; set; }
        public float? BounceThreshold { get; set; }
        public float? DefaultContactOffset { get; set; }
        public float? SleepThreshold { get; set; }
        public int? DefaultSolverIterations { get; set; }
        public int? DefaultSolverVelocityIterations { get; set; }
        public bool? AutoSyncTransforms { get; set; }
    }

    public class PhysicsSettingsResult
    {
        public bool Success { get; set; }
        public Vector3Data Gravity { get; set; }
        public float BounceThreshold { get; set; }
        public float DefaultContactOffset { get; set; }
        public float SleepThreshold { get; set; }
        public int DefaultSolverIterations { get; set; }
        public int DefaultSolverVelocityIterations { get; set; }
        public bool AutoSyncTransforms { get; set; }
        public string SimulationMode { get; set; }
        public string Message { get; set; }
    }

    // ========== Prefab Models ==========

    public class CreatePrefabRequest
    {
        public int InstanceId { get; set; }
        public string SavePath { get; set; }    // e.g. "Assets/Prefabs/Player.prefab"
        public bool ReplacePrefab { get; set; } // overwrite if exists
    }

    public class UnpackPrefabRequest
    {
        public int InstanceId { get; set; }
        public bool Completely { get; set; } = true; // unpack all nested prefabs too
    }

    public class ApplyPrefabOverridesRequest
    {
        public int InstanceId { get; set; }
    }

    public class RevertPrefabOverridesRequest
    {
        public int InstanceId { get; set; }
    }

    public class PrefabInfoResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string PrefabPath { get; set; }
        public bool IsPrefabInstance { get; set; }
        public bool HasOverrides { get; set; }
        public string PrefabStatus { get; set; }
        public int? InstanceId { get; set; }
    }

    // ========== Shared Request Models ==========

    /// <summary>Generic request with just an instance ID (reused across handlers)</summary>
    public class InstanceIdRequest
    {
        public int InstanceId { get; set; }
    }

    // ========== Audio Models ==========

    public class AddAudioSourceRequest
    {
        public int InstanceId { get; set; }
        public string ClipPath { get; set; }
        public float Volume { get; set; } = 1f;
        public float Pitch { get; set; } = 1f;
        public bool Loop { get; set; }
        public bool PlayOnAwake { get; set; } = true;
        public bool Mute { get; set; }
        public float SpatialBlend { get; set; } = 0f; // 0=2D, 1=3D
        public float MinDistance { get; set; } = 1f;
        public float MaxDistance { get; set; } = 500f;
        public int Priority { get; set; } = 128;
        public float StereoPan { get; set; } = 0f;
        public float? ReverbZoneMix { get; set; }
        public string RolloffMode { get; set; } = "Logarithmic";
    }

    public class ModifyAudioSourceRequest
    {
        public int InstanceId { get; set; }
        public string ClipPath { get; set; }
        public float? Volume { get; set; }
        public float? Pitch { get; set; }
        public bool? Loop { get; set; }
        public bool? PlayOnAwake { get; set; }
        public bool? Mute { get; set; }
        public float? SpatialBlend { get; set; }
        public float? MinDistance { get; set; }
        public float? MaxDistance { get; set; }
        public int? Priority { get; set; }
        public float? StereoPan { get; set; }
        public float? ReverbZoneMix { get; set; }
        public string RolloffMode { get; set; }
    }

    public class PlayAudioRequest
    {
        public int InstanceId { get; set; }
        public string Action { get; set; } // play, stop, pause, unpause
    }

    public class AudioSourceInfoResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ClipName { get; set; }
        public string ClipPath { get; set; }
        public float Volume { get; set; }
        public float Pitch { get; set; }
        public bool Loop { get; set; }
        public bool PlayOnAwake { get; set; }
        public bool Mute { get; set; }
        public float SpatialBlend { get; set; }
        public float MinDistance { get; set; }
        public float MaxDistance { get; set; }
        public int Priority { get; set; }
        public float StereoPan { get; set; }
        public float ReverbZoneMix { get; set; }
        public string RolloffMode { get; set; }
        public bool IsPlaying { get; set; }
    }

    // ========== Camera Models ==========

    public class ModifyCameraRequest
    {
        public int InstanceId { get; set; }
        public float? FieldOfView { get; set; }
        public float? NearClipPlane { get; set; }
        public float? FarClipPlane { get; set; }
        public string ProjectionType { get; set; } // Perspective, Orthographic
        public float? OrthographicSize { get; set; }
        public float? Depth { get; set; }
        public int? CullingMask { get; set; }
        public string ClearFlags { get; set; } // Skybox, SolidColor, Depth, Nothing
        public ColorData BackgroundColor { get; set; }
        public string RenderTexturePath { get; set; }
        public bool? ClearRenderTexture { get; set; }
        public bool? AllowHDR { get; set; }
        public bool? AllowMSAA { get; set; }
    }

    public class CameraInfoResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public float FieldOfView { get; set; }
        public float NearClipPlane { get; set; }
        public float FarClipPlane { get; set; }
        public bool IsOrthographic { get; set; }
        public float OrthographicSize { get; set; }
        public float Depth { get; set; }
        public int CullingMask { get; set; }
        public string ClearFlags { get; set; }
        public ColorData BackgroundColor { get; set; }
        public bool IsMainCamera { get; set; }
        public string RenderTexture { get; set; }
        public bool AllowHDR { get; set; }
        public bool AllowMSAA { get; set; }
        public string RenderingPath { get; set; }
    }

    // ========== TextMeshPro Models ==========

    public class CreateTMPTextRequest
    {
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public string Text { get; set; }
        public float? FontSize { get; set; }
        public ColorData Color { get; set; }
        public string Alignment { get; set; }
        public Vector2Data AnchoredPosition { get; set; }
        public Vector2Data SizeDelta { get; set; }
        public bool IsWorldSpace { get; set; }
        public Vector3Data Position { get; set; }
        public Vector3Data Rotation { get; set; }
    }

    public class ModifyTMPTextRequest
    {
        public int InstanceId { get; set; }
        public string Text { get; set; }
        public float? FontSize { get; set; }
        public ColorData Color { get; set; }
        public string Alignment { get; set; }
        public bool? Bold { get; set; }
        public bool? Italic { get; set; }
        public float? CharacterSpacing { get; set; }
        public float? LineSpacing { get; set; }
        public bool? AutoSizeFont { get; set; }
        public bool? WordWrapping { get; set; }
    }

    // ========== Layer/Tag Models ==========

    public class AddLayerRequest
    {
        public string LayerName { get; set; }
    }

    public class AddTagRequest
    {
        public string TagName { get; set; }
    }

    public class LayerInfo
    {
        public int Index { get; set; }
        public string Name { get; set; }
    }

    public class LayerAddResult : OperationResult
    {
        public int LayerIndex { get; set; }
    }

    public class LayersAndTagsResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<LayerInfo> Layers { get; set; }
        public List<string> Tags { get; set; }
    }

    // ========== NavMesh Models ==========

    public class AddNavMeshAgentRequest
    {
        public int InstanceId { get; set; }
        public float? Speed { get; set; }
        public float? AngularSpeed { get; set; }
        public float? Acceleration { get; set; }
        public float? StoppingDistance { get; set; }
        public float? Radius { get; set; }
        public float? Height { get; set; }
        public bool? AutoBraking { get; set; }
        public bool? AutoRepath { get; set; }
        public int? ObstacleAvoidanceType { get; set; } // 0=None, 1=Low, 2=Med, 3=Good, 4=High
    }

    public class AddNavMeshObstacleRequest
    {
        public int InstanceId { get; set; }
        public string Shape { get; set; } // Capsule, Box
        public float? Radius { get; set; }
        public float? Height { get; set; }
        public Vector3Data Center { get; set; }
        public Vector3Data Size { get; set; }
        public bool? Carve { get; set; }
        public bool? CarveOnlyStationary { get; set; }
    }

    // ========== 2D Physics Models ==========

    public class AddRigidbody2DRequest
    {
        public int InstanceId { get; set; }
        public float? Mass { get; set; }
        public float? LinearDrag { get; set; }
        public float? AngularDrag { get; set; }
        public float? GravityScale { get; set; }
        public bool? IsKinematic { get; set; }
        public string BodyType { get; set; } // Dynamic, Kinematic, Static
        public string CollisionDetection { get; set; }
        public string Interpolation { get; set; }
        public string Constraints { get; set; }
    }

    public class AddCollider2DRequest
    {
        public int InstanceId { get; set; }
        public string ColliderType { get; set; } // Box, Circle, Polygon, Capsule, Edge
        public bool IsTrigger { get; set; }
        public Vector2Data Offset { get; set; }
        public Vector2Data Size { get; set; }
        public float? Radius { get; set; }
        public float? Height { get; set; }
        public string CapsuleDirection { get; set; } // Vertical, Horizontal
        public string PhysicsMaterialPath { get; set; }
    }

    // ========== Tilemap Models ==========

    public class CreateTilemapRequest
    {
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public Vector3Data Position { get; set; }
        public float? CellSize { get; set; }
        public string Orientation { get; set; } = "XY"; // XY, XZ, HexFlat, HexPoint
    }

    public class TilemapCreateResult : OperationResult
    {
        public int GridInstanceId { get; set; }
    }

    public class SetTileRequest
    {
        public int InstanceId { get; set; } // Tilemap GameObject
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public string TilePath { get; set; } // null = clear tile
    }

    public class FillTilesRequest
    {
        public int InstanceId { get; set; }
        public int XMin { get; set; }
        public int YMin { get; set; }
        public int XMax { get; set; }
        public int YMax { get; set; }
        public string TilePath { get; set; }
    }

    // ========== Animation Clip Models ==========

    public class CreateAnimationClipRequest
    {
        public string Name { get; set; }
        public string SavePath { get; set; }
        public float? FrameRate { get; set; }
        public bool? IsLooping { get; set; }
    }

    public class AnimationClipResult : OperationResult
    {
        public string ClipPath { get; set; }
    }

    public class KeyframeData
    {
        public float Time { get; set; }
        public float Value { get; set; }
    }

    public class AddKeyframesRequest
    {
        public string ClipPath { get; set; }
        public string GameObjectPath { get; set; } // Path within animated hierarchy (empty = root)
        public string BindingType { get; set; } // Transform, Light, Camera, etc.
        public string PropertyPath { get; set; } // e.g. "localPosition.x", "m_LocalScale.y"
        public List<KeyframeData> Keyframes { get; set; }
        public bool SmoothTangents { get; set; }
    }

    public class GetClipInfoRequest
    {
        public string ClipPath { get; set; }
        public int? InstanceId { get; set; }
    }

    public class AnimationClipInfoResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Name { get; set; }
        public float Length { get; set; }
        public float FrameRate { get; set; }
        public bool IsLooping { get; set; }
        public string WrapMode { get; set; }
        public int CurveCount { get; set; }
        public List<string> CurveBindings { get; set; }
    }

    // ========== Build Models ==========

    public class BuildSceneInfo
    {
        public string Path { get; set; }
        public bool Enabled { get; set; }
        public int BuildIndex { get; set; }
    }

    public class BuildSettingsResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ActiveBuildTarget { get; set; }
        public string ActiveBuildTargetGroup { get; set; }
        public List<BuildSceneInfo> Scenes { get; set; }
        public bool DevelopmentBuild { get; set; }
        public bool ConnectWithProfiler { get; set; }
        public bool AllowDebugging { get; set; }
    }

    public class SetBuildScenesRequest
    {
        public List<string> ScenePaths { get; set; }
        public bool? AddToExisting { get; set; }
    }

    public class SwitchBuildTargetRequest
    {
        public string BuildTarget { get; set; }
    }

    public class BuildPlayerRequest
    {
        public string OutputPath { get; set; }
        public string BuildTarget { get; set; }
        public bool Development { get; set; }
        public bool AutoRunPlayer { get; set; }
        public bool ConnectWithProfiler { get; set; }
    }

    public class BuildPlayerResult : OperationResult
    {
        public string OutputPath { get; set; }
        public float BuildTime { get; set; }
        public ulong TotalSize { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
    }

    // ========== Post-Processing (URP) Models ==========

    public class CreateVolumeRequest
    {
        public string Name { get; set; }
        public bool IsGlobal { get; set; } = true;
        public int? ParentId { get; set; }
        public float Priority { get; set; } = 0f;
        public float BlendDistance { get; set; } = 0f;
        public float Weight { get; set; } = 1f;
        public string ProfileName { get; set; }
        public string ProfileSavePath { get; set; }
    }

    public class VolumeCreateResult : OperationResult
    {
        public string ProfilePath { get; set; }
    }

    public class ModifyVolumeRequest
    {
        public int InstanceId { get; set; }
        // Bloom
        public bool? BloomEnabled { get; set; }
        public float? BloomIntensity { get; set; }
        public float? BloomThreshold { get; set; }
        public float? BloomScatter { get; set; }
        // Color Adjustments
        public bool? ColorAdjustmentsEnabled { get; set; }
        public float? PostExposure { get; set; }
        public float? Contrast { get; set; }
        public float? Saturation { get; set; }
        public float? HueShift { get; set; }
        // Vignette
        public bool? VignetteEnabled { get; set; }
        public float? VignetteIntensity { get; set; }
        public float? VignetteSmoothness { get; set; }
        // Depth of Field
        public bool? DepthOfFieldEnabled { get; set; }
        public float? FocusDistance { get; set; }
        public float? Aperture { get; set; }
        public float? FocalLength { get; set; }
        // Tonemapping
        public bool? TonemappingEnabled { get; set; }
        public string TonemappingMode { get; set; } // None, Neutral, ACES
        // Motion Blur
        public bool? MotionBlurEnabled { get; set; }
        public float? MotionBlurIntensity { get; set; }
        // Film Grain
        public bool? FilmGrainEnabled { get; set; }
        public float? FilmGrainIntensity { get; set; }
    }

    // ========== Console Filter Models ==========

    public class FilteredConsoleData
    {
        public int TotalCount { get; set; }
        public int FilteredCount { get; set; }
        public string TypeFilter { get; set; }
        public string SearchFilter { get; set; }
        public List<LogEntry> Logs { get; set; }
    }

    // ========== Particle System Models ==========

    public class MinMaxCurveData
    {
        public float Constant { get; set; }
        public float ConstantMin { get; set; }
        public float ConstantMax { get; set; }
        public string Mode { get; set; } = "Constant"; // Constant, TwoConstants
    }

    public class MinMaxGradientData
    {
        public ColorData Color { get; set; }
        public ColorData ColorMin { get; set; }
        public ColorData ColorMax { get; set; }
        public string Mode { get; set; } = "Color"; // Color, TwoColors
    }

    public class CreateParticleSystemRequest
    {
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public Vector3Data Position { get; set; }
        public Vector3Data Rotation { get; set; }
        // Main module
        public float? Duration { get; set; }
        public bool? Looping { get; set; }
        public float? StartLifetime { get; set; }
        public float? StartSpeed { get; set; }
        public float? StartSize { get; set; }
        public ColorData StartColor { get; set; }
        public int? MaxParticles { get; set; }
        public string SimulationSpace { get; set; } // Local, World
        public bool? PlayOnAwake { get; set; }
        // Emission
        public float? EmissionRate { get; set; }
        // Shape
        public string Shape { get; set; } // Sphere, Hemisphere, Cone, Box, Circle, Edge
        public float? ShapeRadius { get; set; }
        public float? ShapeAngle { get; set; }
        // Gravity
        public float? GravityModifier { get; set; }
    }

    public class ModifyParticleSystemRequest
    {
        public int InstanceId { get; set; }
        // Main module
        public float? Duration { get; set; }
        public bool? Looping { get; set; }
        public float? StartLifetime { get; set; }
        public float? StartLifetimeMin { get; set; }
        public float? StartLifetimeMax { get; set; }
        public float? StartSpeed { get; set; }
        public float? StartSpeedMin { get; set; }
        public float? StartSpeedMax { get; set; }
        public float? StartSize { get; set; }
        public float? StartSizeMin { get; set; }
        public float? StartSizeMax { get; set; }
        public ColorData StartColor { get; set; }
        public ColorData StartColorMin { get; set; }
        public ColorData StartColorMax { get; set; }
        public int? MaxParticles { get; set; }
        public string SimulationSpace { get; set; }
        public bool? PlayOnAwake { get; set; }
        public float? GravityModifier { get; set; }
        public float? SimulationSpeed { get; set; }
        // Emission
        public float? EmissionRate { get; set; }
        // Shape
        public string Shape { get; set; }
        public float? ShapeRadius { get; set; }
        public float? ShapeAngle { get; set; }
        public Vector3Data ShapeScale { get; set; }
        // Renderer
        public string MaterialPath { get; set; }
        public string RenderMode { get; set; } // Billboard, Mesh, Stretch, HorizontalBillboard, VerticalBillboard
    }

    public class PlayParticleSystemRequest
    {
        public int InstanceId { get; set; }
        public string Action { get; set; } // play, stop, pause, restart
        public bool WithChildren { get; set; } = true;
    }

    public class ParticleSystemInfoResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool IsPlaying { get; set; }
        public bool IsPaused { get; set; }
        public bool IsStopped { get; set; }
        public int ParticleCount { get; set; }
        public float Time { get; set; }
        // Main module
        public float Duration { get; set; }
        public bool Looping { get; set; }
        public float StartLifetime { get; set; }
        public float StartSpeed { get; set; }
        public float StartSize { get; set; }
        public ColorData StartColor { get; set; }
        public int MaxParticles { get; set; }
        public string SimulationSpace { get; set; }
        public bool PlayOnAwake { get; set; }
        public float GravityModifier { get; set; }
        // Emission
        public float EmissionRate { get; set; }
        // Shape
        public string Shape { get; set; }
        public float ShapeRadius { get; set; }
        public float ShapeAngle { get; set; }
    }
}
