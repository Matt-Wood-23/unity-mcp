using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Providers
{
    public static class SceneDataProvider
    {
        private const int DefaultMaxDepth = 3;
        private const int DetailedMaxDepth = 10;

        public static string GetSceneHierarchy()
        {
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();

            var hierarchy = new SceneHierarchyData
            {
                SceneName = scene.name,
                ScenePath = scene.path,
                IsDirty = scene.isDirty,
                RootObjectCount = rootObjects.Length,
                RootObjects = rootObjects.Select(go => BuildHierarchyNode(go, 0, DefaultMaxDepth)).ToList()
            };

            return JsonConvert.SerializeObject(hierarchy, Formatting.Indented);
        }

        public static string GetDetailedSceneData()
        {
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();

            var hierarchy = new SceneHierarchyData
            {
                SceneName = scene.name,
                ScenePath = scene.path,
                IsDirty = scene.isDirty,
                RootObjectCount = rootObjects.Length,
                RootObjects = rootObjects.Select(go => BuildHierarchyNode(go, 0, DetailedMaxDepth)).ToList()
            };

            return JsonConvert.SerializeObject(hierarchy, Formatting.Indented);
        }

        private static GameObjectNode BuildHierarchyNode(GameObject go, int depth, int maxDepth)
        {
            var node = new GameObjectNode
            {
                InstanceId = go.GetInstanceID(),
                Name = go.name,
                Tag = go.tag,
                Layer = LayerMask.LayerToName(go.layer),
                IsActive = go.activeSelf,
                IsStatic = go.isStatic,
                Components = go.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .ToList(),
                ChildCount = go.transform.childCount
            };

            if (depth < maxDepth && go.transform.childCount > 0)
            {
                node.Children = new List<GameObjectNode>();
                foreach (Transform child in go.transform)
                {
                    node.Children.Add(BuildHierarchyNode(child.gameObject, depth + 1, maxDepth));
                }
            }

            return node;
        }

        public static string GetGameObject(string instanceIdStr)
        {
            if (!int.TryParse(instanceIdStr, out int instanceId))
            {
                return JsonConvert.SerializeObject(new { error = "Invalid instance ID" });
            }

            var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (go == null)
            {
                return JsonConvert.SerializeObject(new { error = "GameObject not found", instanceId });
            }

            var data = new GameObjectDetailData
            {
                InstanceId = instanceId,
                Name = go.name,
                Tag = go.tag,
                Layer = LayerMask.LayerToName(go.layer),
                IsActive = go.activeSelf,
                IsStatic = go.isStatic,
                Transform = new TransformData
                {
                    Position = new Vector3Data(go.transform.position),
                    Rotation = new Vector3Data(go.transform.eulerAngles),
                    Scale = new Vector3Data(go.transform.localScale),
                    LocalPosition = new Vector3Data(go.transform.localPosition),
                    LocalRotation = new Vector3Data(go.transform.localEulerAngles)
                },
                Components = GetComponentsDetailed(go)
            };

            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public static string GetComponents(string instanceIdStr)
        {
            if (!int.TryParse(instanceIdStr, out int instanceId))
            {
                return JsonConvert.SerializeObject(new { error = "Invalid instance ID" });
            }

            var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (go == null)
            {
                return JsonConvert.SerializeObject(new { error = "GameObject not found", instanceId });
            }

            var result = new
            {
                GameObjectName = go.name,
                InstanceId = instanceId,
                Components = GetComponentsDetailed(go)
            };

            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }

        private static List<ComponentData> GetComponentsDetailed(GameObject go)
        {
            var result = new List<ComponentData>();

            foreach (var component in go.GetComponents<Component>())
            {
                if (component == null) continue;

                var compData = new ComponentData
                {
                    Type = component.GetType().Name,
                    FullType = component.GetType().FullName,
                    Enabled = !(component is Behaviour behaviour) || behaviour.enabled,
                    Properties = new List<PropertyData>()
                };

                // Use SerializedObject to get all serialized properties
                try
                {
                    var so = new SerializedObject(component);
                    var prop = so.GetIterator();

                    if (prop.NextVisible(true))
                    {
                        do
                        {
                            compData.Properties.Add(new PropertyData
                            {
                                Name = prop.name,
                                Type = prop.propertyType.ToString(),
                                Value = GetPropertyValue(prop)
                            });
                        } while (prop.NextVisible(false));
                    }
                }
                catch
                {
                    // Some components may not serialize properly
                }

                result.Add(compData);
            }

            return result;
        }

        private static string GetPropertyValue(SerializedProperty prop)
        {
            try
            {
                return prop.propertyType switch
                {
                    SerializedPropertyType.Integer => prop.intValue.ToString(),
                    SerializedPropertyType.Boolean => prop.boolValue.ToString(),
                    SerializedPropertyType.Float => prop.floatValue.ToString("F4"),
                    SerializedPropertyType.String => prop.stringValue ?? "",
                    SerializedPropertyType.Color => $"RGBA({prop.colorValue.r:F2}, {prop.colorValue.g:F2}, {prop.colorValue.b:F2}, {prop.colorValue.a:F2})",
                    SerializedPropertyType.Vector2 => $"({prop.vector2Value.x:F2}, {prop.vector2Value.y:F2})",
                    SerializedPropertyType.Vector3 => $"({prop.vector3Value.x:F2}, {prop.vector3Value.y:F2}, {prop.vector3Value.z:F2})",
                    SerializedPropertyType.Vector4 => $"({prop.vector4Value.x:F2}, {prop.vector4Value.y:F2}, {prop.vector4Value.z:F2}, {prop.vector4Value.w:F2})",
                    SerializedPropertyType.Rect => $"(x:{prop.rectValue.x:F2}, y:{prop.rectValue.y:F2}, w:{prop.rectValue.width:F2}, h:{prop.rectValue.height:F2})",
                    SerializedPropertyType.Enum => prop.enumValueIndex >= 0 && prop.enumValueIndex < prop.enumDisplayNames.Length
                        ? prop.enumDisplayNames[prop.enumValueIndex]
                        : prop.enumValueIndex.ToString(),
                    SerializedPropertyType.ObjectReference => prop.objectReferenceValue?.name ?? "None",
                    SerializedPropertyType.LayerMask => LayerMask.LayerToName(prop.intValue),
                    SerializedPropertyType.Quaternion => $"({prop.quaternionValue.eulerAngles.x:F2}, {prop.quaternionValue.eulerAngles.y:F2}, {prop.quaternionValue.eulerAngles.z:F2})",
                    _ => $"({prop.propertyType})"
                };
            }
            catch
            {
                return "(error reading value)";
            }
        }
    }
}
