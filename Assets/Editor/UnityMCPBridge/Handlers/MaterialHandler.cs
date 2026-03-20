using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class MaterialHandler
    {
        public static string CreateMaterial(string body)
        {
            var request = JsonConvert.DeserializeObject<CreateMaterialRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.Name))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'name' is required"
                });
            }

            try
            {
                // Find shader
                Shader shader = null;
                if (!string.IsNullOrEmpty(request.Shader))
                {
                    shader = Shader.Find(request.Shader);
                    if (shader == null)
                    {
                        // Try common shader names
                        var shaderNames = new[]
                        {
                            request.Shader,
                            $"Standard",
                            $"Universal Render Pipeline/Lit",
                            $"Universal Render Pipeline/Simple Lit",
                            $"Universal Render Pipeline/Unlit"
                        };

                        foreach (var name in shaderNames)
                        {
                            shader = Shader.Find(name);
                            if (shader != null) break;
                        }
                    }
                }

                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Lit")
                          ?? Shader.Find("Standard");
                }

                if (shader == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = "Could not find a valid shader"
                    });
                }

                var material = new Material(shader);
                material.name = request.Name;

                // Set color if provided
                if (request.Color != null)
                {
                    var color = new Color(
                        request.Color.R,
                        request.Color.G,
                        request.Color.B,
                        request.Color.A
                    );
                    material.color = color;
                }

                // Determine save path
                string path = request.SavePath;
                if (string.IsNullOrEmpty(path))
                {
                    path = $"Assets/{request.Name}.mat";
                }
                if (!path.EndsWith(".mat"))
                {
                    path += ".mat";
                }

                // Ensure directory exists
                var directory = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(material, path);
                AssetDatabase.SaveAssets();

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Created material '{request.Name}' at {path}"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error creating material: {e.Message}"
                });
            }
        }

        public static string ModifyMaterial(string body)
        {
            var request = JsonConvert.DeserializeObject<ModifyMaterialRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.MaterialPath))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'materialPath' is required"
                });
            }

            try
            {
                // Find material
                Material material = null;
                if (request.MaterialPath.StartsWith("Assets/"))
                {
                    material = AssetDatabase.LoadAssetAtPath<Material>(request.MaterialPath);
                }
                else
                {
                    // Search by name
                    var guids = AssetDatabase.FindAssets($"{request.MaterialPath} t:Material");
                    if (guids.Length > 0)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    }
                }

                if (material == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Material not found: {request.MaterialPath}"
                    });
                }

                Undo.RecordObject(material, $"Modify Material {material.name}");

                // Set color
                if (request.Color != null)
                {
                    var color = new Color(
                        request.Color.R,
                        request.Color.G,
                        request.Color.B,
                        request.Color.A
                    );
                    material.color = color;
                }

                // Set shader property
                if (!string.IsNullOrEmpty(request.PropertyName) && request.PropertyValue != null)
                {
                    SetMaterialProperty(material, request.PropertyName, request.PropertyValue);
                }

                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Modified material '{material.name}'"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error modifying material: {e.Message}"
                });
            }
        }

        public static string GetMaterialInfo(string materialPath)
        {
            try
            {
                Material material = null;
                if (materialPath.StartsWith("Assets/"))
                {
                    material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                }
                else
                {
                    var guids = AssetDatabase.FindAssets($"{materialPath} t:Material");
                    if (guids.Length > 0)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    }
                }

                if (material == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Material not found: {materialPath}"
                    });
                }

                var properties = new List<MaterialPropertyInfo>();
                var shader = material.shader;

                for (int i = 0; i < shader.GetPropertyCount(); i++)
                {
                    var propName = shader.GetPropertyName(i);
                    var propType = shader.GetPropertyType(i);

                    properties.Add(new MaterialPropertyInfo
                    {
                        Name = propName,
                        Type = propType.ToString(),
                        Value = GetPropertyValueString(material, propName, propType)
                    });
                }

                return JsonConvert.SerializeObject(new
                {
                    Success = true,
                    Name = material.name,
                    Shader = shader.name,
                    Color = new { material.color.r, material.color.g, material.color.b, material.color.a },
                    Properties = properties
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error getting material info: {e.Message}"
                });
            }
        }

        public static string AssignMaterial(string body)
        {
            var request = JsonConvert.DeserializeObject<AssignMaterialRequest>(body);
            if (request == null)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body"
                });
            }

            try
            {
                // Find material
                Material material = null;
                if (request.MaterialPath.StartsWith("Assets/"))
                {
                    material = AssetDatabase.LoadAssetAtPath<Material>(request.MaterialPath);
                }
                else
                {
                    var guids = AssetDatabase.FindAssets($"{request.MaterialPath} t:Material");
                    if (guids.Length > 0)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    }
                }

                if (material == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Material not found: {request.MaterialPath}"
                    });
                }

                // Find GameObject
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"GameObject not found: {request.InstanceId}"
                    });
                }

                var renderer = go.GetComponent<Renderer>();
                if (renderer == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"GameObject '{go.name}' has no Renderer component"
                    });
                }

                Undo.RecordObject(renderer, $"Assign Material to {go.name}");

                if (request.MaterialIndex.HasValue && request.MaterialIndex.Value < renderer.sharedMaterials.Length)
                {
                    var materials = renderer.sharedMaterials;
                    materials[request.MaterialIndex.Value] = material;
                    renderer.sharedMaterials = materials;
                }
                else
                {
                    renderer.sharedMaterial = material;
                }

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Assigned material '{material.name}' to '{go.name}'"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error assigning material: {e.Message}"
                });
            }
        }

        private static void SetMaterialProperty(Material material, string propertyName, object value)
        {
            var shader = material.shader;
            int propIndex = shader.FindPropertyIndex(propertyName);
            if (propIndex < 0) return;

            var propType = shader.GetPropertyType(propIndex);
            var jValue = value as JToken;

            switch (propType)
            {
                case UnityEngine.Rendering.ShaderPropertyType.Color:
                    if (jValue != null)
                    {
                        var color = new Color(
                            jValue["r"]?.Value<float>() ?? jValue["R"]?.Value<float>() ?? 0,
                            jValue["g"]?.Value<float>() ?? jValue["G"]?.Value<float>() ?? 0,
                            jValue["b"]?.Value<float>() ?? jValue["B"]?.Value<float>() ?? 0,
                            jValue["a"]?.Value<float>() ?? jValue["A"]?.Value<float>() ?? 1
                        );
                        material.SetColor(propertyName, color);
                    }
                    break;

                case UnityEngine.Rendering.ShaderPropertyType.Float:
                case UnityEngine.Rendering.ShaderPropertyType.Range:
                    material.SetFloat(propertyName, Convert.ToSingle(value));
                    break;

                case UnityEngine.Rendering.ShaderPropertyType.Int:
                    material.SetInt(propertyName, Convert.ToInt32(value));
                    break;

                case UnityEngine.Rendering.ShaderPropertyType.Vector:
                    if (jValue != null)
                    {
                        var vector = new Vector4(
                            jValue["x"]?.Value<float>() ?? jValue["X"]?.Value<float>() ?? 0,
                            jValue["y"]?.Value<float>() ?? jValue["Y"]?.Value<float>() ?? 0,
                            jValue["z"]?.Value<float>() ?? jValue["Z"]?.Value<float>() ?? 0,
                            jValue["w"]?.Value<float>() ?? jValue["W"]?.Value<float>() ?? 0
                        );
                        material.SetVector(propertyName, vector);
                    }
                    break;
            }
        }

        private static string GetPropertyValueString(Material material, string name, UnityEngine.Rendering.ShaderPropertyType type)
        {
            try
            {
                return type switch
                {
                    UnityEngine.Rendering.ShaderPropertyType.Color => material.GetColor(name).ToString(),
                    UnityEngine.Rendering.ShaderPropertyType.Float => material.GetFloat(name).ToString("F4"),
                    UnityEngine.Rendering.ShaderPropertyType.Range => material.GetFloat(name).ToString("F4"),
                    UnityEngine.Rendering.ShaderPropertyType.Int => material.GetInt(name).ToString(),
                    UnityEngine.Rendering.ShaderPropertyType.Vector => material.GetVector(name).ToString(),
                    UnityEngine.Rendering.ShaderPropertyType.Texture => material.GetTexture(name)?.name ?? "None",
                    _ => "(unknown)"
                };
            }
            catch
            {
                return "(error)";
            }
        }
    }
}
