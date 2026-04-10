using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class LightingHandler
    {
        public static string CreateLight(string body)
        {
            var request = JsonConvert.DeserializeObject<CreateLightRequest>(body);
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
                string name = request.Name ?? $"{request.LightType} Light";
                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, $"Create Light {name}");

                var light = go.AddComponent<Light>();

                // Set light type
                light.type = ParseLightType(request.LightType);

                // Set color
                if (request.Color != null)
                {
                    light.color = new Color(request.Color.R, request.Color.G, request.Color.B, request.Color.A);
                }

                light.intensity = request.Intensity;
                light.range = request.Range;
                light.spotAngle = request.SpotAngle;

                // Set shadows
                light.shadows = ParseShadowType(request.Shadows);

                // Set transform
                if (request.Position != null)
                    go.transform.position = request.Position.ToVector3();
                if (request.Rotation != null)
                    go.transform.eulerAngles = request.Rotation.ToVector3();

                // Set parent
                if (request.ParentId.HasValue)
                {
                    var parent = EditorUtility.InstanceIDToObject(request.ParentId.Value) as GameObject;
                    if (parent != null)
                    {
                        Undo.SetTransformParent(go.transform, parent.transform, $"Parent Light {name}");
                    }
                }

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Created {request.LightType} light '{name}'",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error creating light: {e.Message}"
                });
            }
        }

        public static string ModifyLight(string body)
        {
            var request = JsonConvert.DeserializeObject<ModifyLightRequest>(body);
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
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"GameObject not found: {request.InstanceId}"
                    });
                }

                var light = go.GetComponent<Light>();
                if (light == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"No Light component on '{go.name}'"
                    });
                }

                Undo.RecordObject(light, $"Modify Light {go.name}");

                if (request.Color != null)
                    light.color = new Color(request.Color.R, request.Color.G, request.Color.B, request.Color.A);
                if (request.Intensity.HasValue)
                    light.intensity = request.Intensity.Value;
                if (request.Range.HasValue)
                    light.range = request.Range.Value;
                if (request.SpotAngle.HasValue)
                    light.spotAngle = request.SpotAngle.Value;
                if (!string.IsNullOrEmpty(request.Shadows))
                    light.shadows = ParseShadowType(request.Shadows);
                if (!string.IsNullOrEmpty(request.LightType))
                    light.type = ParseLightType(request.LightType);

                EditorUtility.SetDirty(light);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Modified light on '{go.name}'"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error modifying light: {e.Message}"
                });
            }
        }

        public static string GetLightInfo(string body)
        {
            var request = JsonConvert.DeserializeObject<GetLightInfoRequest>(body);
            if (request == null)
            {
                return JsonConvert.SerializeObject(new LightInfoResult
                {
                    Success = false,
                    Message = "Invalid request body"
                });
            }

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                {
                    return JsonConvert.SerializeObject(new LightInfoResult
                    {
                        Success = false,
                        Message = $"GameObject not found: {request.InstanceId}"
                    });
                }

                var light = go.GetComponent<Light>();
                if (light == null)
                {
                    return JsonConvert.SerializeObject(new LightInfoResult
                    {
                        Success = false,
                        Message = $"No Light component on '{go.name}'"
                    });
                }

                return JsonConvert.SerializeObject(new LightInfoResult
                {
                    Success = true,
                    InstanceId = go.GetInstanceID(),
                    Name = go.name,
                    LightType = light.type.ToString(),
                    Color = new ColorData
                    {
                        R = light.color.r,
                        G = light.color.g,
                        B = light.color.b,
                        A = light.color.a
                    },
                    Intensity = light.intensity,
                    Range = light.range,
                    SpotAngle = light.spotAngle,
                    Shadows = light.shadows.ToString(),
                    ShadowStrength = light.shadowStrength
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new LightInfoResult
                {
                    Success = false,
                    Message = $"Error getting light info: {e.Message}"
                });
            }
        }

        public static string SetEnvironment(string body)
        {
            var request = JsonConvert.DeserializeObject<SetEnvironmentRequest>(body);
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
                // Skybox
                if (!string.IsNullOrEmpty(request.SkyboxMaterialPath))
                {
                    var skybox = AssetDatabase.LoadAssetAtPath<Material>(request.SkyboxMaterialPath);
                    if (skybox != null)
                    {
                        RenderSettings.skybox = skybox;
                    }
                    else
                    {
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = false,
                            Message = $"Skybox material not found: {request.SkyboxMaterialPath}"
                        });
                    }
                }

                // Ambient mode
                if (!string.IsNullOrEmpty(request.AmbientMode))
                {
                    switch (request.AmbientMode.ToLower())
                    {
                        case "skybox":
                            RenderSettings.ambientMode = AmbientMode.Skybox;
                            break;
                        case "trilight":
                            RenderSettings.ambientMode = AmbientMode.Trilight;
                            break;
                        case "flat":
                        case "custom":
                            RenderSettings.ambientMode = AmbientMode.Flat;
                            break;
                    }
                }

                // Ambient colors
                if (request.AmbientColor != null)
                    RenderSettings.ambientLight = new Color(request.AmbientColor.R, request.AmbientColor.G, request.AmbientColor.B, request.AmbientColor.A);
                if (request.AmbientSkyColor != null)
                    RenderSettings.ambientSkyColor = new Color(request.AmbientSkyColor.R, request.AmbientSkyColor.G, request.AmbientSkyColor.B, request.AmbientSkyColor.A);
                if (request.AmbientEquatorColor != null)
                    RenderSettings.ambientEquatorColor = new Color(request.AmbientEquatorColor.R, request.AmbientEquatorColor.G, request.AmbientEquatorColor.B, request.AmbientEquatorColor.A);
                if (request.AmbientGroundColor != null)
                    RenderSettings.ambientGroundColor = new Color(request.AmbientGroundColor.R, request.AmbientGroundColor.G, request.AmbientGroundColor.B, request.AmbientGroundColor.A);

                if (request.AmbientIntensity.HasValue)
                    RenderSettings.ambientIntensity = request.AmbientIntensity.Value;
                if (request.ReflectionIntensity.HasValue)
                    RenderSettings.reflectionIntensity = request.ReflectionIntensity.Value;

                // Fog
                if (request.Fog.HasValue)
                    RenderSettings.fog = request.Fog.Value;
                if (request.FogColor != null)
                    RenderSettings.fogColor = new Color(request.FogColor.R, request.FogColor.G, request.FogColor.B, request.FogColor.A);
                if (!string.IsNullOrEmpty(request.FogMode))
                {
                    switch (request.FogMode.ToLower())
                    {
                        case "linear":
                            RenderSettings.fogMode = FogMode.Linear;
                            break;
                        case "exponential":
                            RenderSettings.fogMode = FogMode.Exponential;
                            break;
                        case "exponentialsquared":
                            RenderSettings.fogMode = FogMode.ExponentialSquared;
                            break;
                    }
                }
                if (request.FogDensity.HasValue)
                    RenderSettings.fogDensity = request.FogDensity.Value;
                if (request.FogStartDistance.HasValue)
                    RenderSettings.fogStartDistance = request.FogStartDistance.Value;
                if (request.FogEndDistance.HasValue)
                    RenderSettings.fogEndDistance = request.FogEndDistance.Value;

                // Mark scene dirty so changes are saveable
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = "Environment settings updated"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error setting environment: {e.Message}"
                });
            }
        }

        public static string GetEnvironment(string body)
        {
            try
            {
                var result = new EnvironmentInfoResult
                {
                    Success = true,
                    SkyboxMaterial = RenderSettings.skybox != null ? RenderSettings.skybox.name : "None",
                    AmbientMode = RenderSettings.ambientMode.ToString(),
                    AmbientColor = new ColorData
                    {
                        R = RenderSettings.ambientLight.r,
                        G = RenderSettings.ambientLight.g,
                        B = RenderSettings.ambientLight.b,
                        A = RenderSettings.ambientLight.a
                    },
                    AmbientSkyColor = new ColorData
                    {
                        R = RenderSettings.ambientSkyColor.r,
                        G = RenderSettings.ambientSkyColor.g,
                        B = RenderSettings.ambientSkyColor.b,
                        A = RenderSettings.ambientSkyColor.a
                    },
                    AmbientEquatorColor = new ColorData
                    {
                        R = RenderSettings.ambientEquatorColor.r,
                        G = RenderSettings.ambientEquatorColor.g,
                        B = RenderSettings.ambientEquatorColor.b,
                        A = RenderSettings.ambientEquatorColor.a
                    },
                    AmbientGroundColor = new ColorData
                    {
                        R = RenderSettings.ambientGroundColor.r,
                        G = RenderSettings.ambientGroundColor.g,
                        B = RenderSettings.ambientGroundColor.b,
                        A = RenderSettings.ambientGroundColor.a
                    },
                    AmbientIntensity = RenderSettings.ambientIntensity,
                    ReflectionIntensity = RenderSettings.reflectionIntensity,
                    Fog = RenderSettings.fog,
                    FogColor = new ColorData
                    {
                        R = RenderSettings.fogColor.r,
                        G = RenderSettings.fogColor.g,
                        B = RenderSettings.fogColor.b,
                        A = RenderSettings.fogColor.a
                    },
                    FogMode = RenderSettings.fogMode.ToString(),
                    FogDensity = RenderSettings.fogDensity,
                    FogStartDistance = RenderSettings.fogStartDistance,
                    FogEndDistance = RenderSettings.fogEndDistance
                };

                return JsonConvert.SerializeObject(result, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new EnvironmentInfoResult
                {
                    Success = false,
                    Message = $"Error getting environment info: {e.Message}"
                });
            }
        }

        private static LightType ParseLightType(string type)
        {
            if (string.IsNullOrEmpty(type)) return LightType.Point;
            return type.ToLower() switch
            {
                "directional" => LightType.Directional,
                "point" => LightType.Point,
                "spot" => LightType.Spot,
                "area" or "rectangle" => LightType.Rectangle,
                _ => LightType.Point
            };
        }

        private static LightShadows ParseShadowType(string shadows)
        {
            if (string.IsNullOrEmpty(shadows)) return LightShadows.None;
            return shadows.ToLower() switch
            {
                "hard" => LightShadows.Hard,
                "soft" => LightShadows.Soft,
                _ => LightShadows.None
            };
        }
    }
}
