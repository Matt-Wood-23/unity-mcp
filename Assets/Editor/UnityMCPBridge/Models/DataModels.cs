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
}
