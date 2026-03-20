using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class ComponentPropertyHandler
    {
        public static string SetProperty(string body)
        {
            var request = JsonConvert.DeserializeObject<SetPropertyRequest>(body);
            if (request == null)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body"
                });
            }

            var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
            if (go == null)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"GameObject not found with instance ID: {request.InstanceId}"
                });
            }

            try
            {
                // Find the component
                Component component = null;
                var components = go.GetComponents<Component>();

                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    if (comp.GetType().Name.Equals(request.ComponentType, StringComparison.OrdinalIgnoreCase) ||
                        comp.GetType().FullName.Equals(request.ComponentType, StringComparison.OrdinalIgnoreCase))
                    {
                        if (request.ComponentIndex.HasValue)
                        {
                            if (request.ComponentIndex.Value == 0)
                            {
                                component = comp;
                                break;
                            }
                            request.ComponentIndex--;
                        }
                        else
                        {
                            component = comp;
                            break;
                        }
                    }
                }

                if (component == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Component '{request.ComponentType}' not found on '{go.name}'"
                    });
                }

                // Use SerializedObject to set the property
                var serializedObject = new SerializedObject(component);
                var property = serializedObject.FindProperty(request.PropertyName);

                if (property == null)
                {
                    // Try to find by display name or partial match
                    var iterator = serializedObject.GetIterator();
                    if (iterator.NextVisible(true))
                    {
                        do
                        {
                            if (iterator.name.Equals(request.PropertyName, StringComparison.OrdinalIgnoreCase) ||
                                iterator.displayName.Equals(request.PropertyName, StringComparison.OrdinalIgnoreCase))
                            {
                                property = serializedObject.FindProperty(iterator.name);
                                break;
                            }
                        } while (iterator.NextVisible(false));
                    }
                }

                if (property == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Property '{request.PropertyName}' not found on component '{request.ComponentType}'"
                    });
                }

                Undo.RecordObject(component, $"Set {request.PropertyName}");

                // Set the value based on property type
                bool success = SetPropertyValue(property, request.Value);

                if (!success)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Failed to set property '{request.PropertyName}' of type {property.propertyType}"
                    });
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(component);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Set '{request.PropertyName}' on '{request.ComponentType}' to '{request.Value}'"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error setting property: {e.Message}"
                });
            }
        }

        private static bool SetPropertyValue(SerializedProperty property, object value)
        {
            try
            {
                var valueStr = value?.ToString() ?? "";
                var jValue = value as JToken;

                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        property.intValue = Convert.ToInt32(value);
                        return true;

                    case SerializedPropertyType.Boolean:
                        if (value is bool b)
                            property.boolValue = b;
                        else
                            property.boolValue = bool.Parse(valueStr);
                        return true;

                    case SerializedPropertyType.Float:
                        property.floatValue = Convert.ToSingle(value);
                        return true;

                    case SerializedPropertyType.String:
                        property.stringValue = valueStr;
                        return true;

                    case SerializedPropertyType.Color:
                        if (jValue != null && jValue.Type == JTokenType.Object)
                        {
                            var r = jValue["r"]?.Value<float>() ?? jValue["R"]?.Value<float>() ?? 0;
                            var g = jValue["g"]?.Value<float>() ?? jValue["G"]?.Value<float>() ?? 0;
                            var bVal = jValue["b"]?.Value<float>() ?? jValue["B"]?.Value<float>() ?? 0;
                            var a = jValue["a"]?.Value<float>() ?? jValue["A"]?.Value<float>() ?? 1;
                            property.colorValue = new Color(r, g, bVal, a);
                            return true;
                        }
                        return false;

                    case SerializedPropertyType.Vector2:
                        if (jValue != null && jValue.Type == JTokenType.Object)
                        {
                            var x = jValue["x"]?.Value<float>() ?? jValue["X"]?.Value<float>() ?? 0;
                            var y = jValue["y"]?.Value<float>() ?? jValue["Y"]?.Value<float>() ?? 0;
                            property.vector2Value = new Vector2(x, y);
                            return true;
                        }
                        return false;

                    case SerializedPropertyType.Vector3:
                        if (jValue != null && jValue.Type == JTokenType.Object)
                        {
                            var x = jValue["x"]?.Value<float>() ?? jValue["X"]?.Value<float>() ?? 0;
                            var y = jValue["y"]?.Value<float>() ?? jValue["Y"]?.Value<float>() ?? 0;
                            var z = jValue["z"]?.Value<float>() ?? jValue["Z"]?.Value<float>() ?? 0;
                            property.vector3Value = new Vector3(x, y, z);
                            return true;
                        }
                        return false;

                    case SerializedPropertyType.Vector4:
                        if (jValue != null && jValue.Type == JTokenType.Object)
                        {
                            var x = jValue["x"]?.Value<float>() ?? jValue["X"]?.Value<float>() ?? 0;
                            var y = jValue["y"]?.Value<float>() ?? jValue["Y"]?.Value<float>() ?? 0;
                            var z = jValue["z"]?.Value<float>() ?? jValue["Z"]?.Value<float>() ?? 0;
                            var w = jValue["w"]?.Value<float>() ?? jValue["W"]?.Value<float>() ?? 0;
                            property.vector4Value = new Vector4(x, y, z, w);
                            return true;
                        }
                        return false;

                    case SerializedPropertyType.Enum:
                        if (int.TryParse(valueStr, out int enumIndex))
                        {
                            property.enumValueIndex = enumIndex;
                            return true;
                        }
                        // Try to find enum by name
                        for (int i = 0; i < property.enumDisplayNames.Length; i++)
                        {
                            if (property.enumDisplayNames[i].Equals(valueStr, StringComparison.OrdinalIgnoreCase) ||
                                property.enumNames[i].Equals(valueStr, StringComparison.OrdinalIgnoreCase))
                            {
                                property.enumValueIndex = i;
                                return true;
                            }
                        }
                        return false;

                    case SerializedPropertyType.ObjectReference:
                        // Find asset by name or path
                        if (string.IsNullOrEmpty(valueStr) || valueStr.ToLower() == "null" || valueStr.ToLower() == "none")
                        {
                            property.objectReferenceValue = null;
                            return true;
                        }

                        // Try to find by path first
                        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(valueStr);
                        if (asset != null)
                        {
                            property.objectReferenceValue = asset;
                            return true;
                        }

                        // Try to find by name in project
                        var guids = AssetDatabase.FindAssets(valueStr);
                        if (guids.Length > 0)
                        {
                            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                            asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                            if (asset != null)
                            {
                                property.objectReferenceValue = asset;
                                return true;
                            }
                        }
                        return false;

                    case SerializedPropertyType.LayerMask:
                        property.intValue = Convert.ToInt32(value);
                        return true;

                    case SerializedPropertyType.Rect:
                        if (jValue != null && jValue.Type == JTokenType.Object)
                        {
                            var x = jValue["x"]?.Value<float>() ?? jValue["X"]?.Value<float>() ?? 0;
                            var y = jValue["y"]?.Value<float>() ?? jValue["Y"]?.Value<float>() ?? 0;
                            var w = jValue["width"]?.Value<float>() ?? jValue["Width"]?.Value<float>() ?? jValue["w"]?.Value<float>() ?? 0;
                            var h = jValue["height"]?.Value<float>() ?? jValue["Height"]?.Value<float>() ?? jValue["h"]?.Value<float>() ?? 0;
                            property.rectValue = new Rect(x, y, w, h);
                            return true;
                        }
                        return false;

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
