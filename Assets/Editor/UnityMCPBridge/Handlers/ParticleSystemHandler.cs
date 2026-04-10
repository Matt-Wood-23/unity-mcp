using UnityEngine;
using UnityEditor;
using System;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class ParticleSystemHandler
    {
        public static string CreateParticleSystem(string body)
        {
            var request = JsonConvert.DeserializeObject<CreateParticleSystemRequest>(body);
            if (request == null)
                request = new CreateParticleSystemRequest();

            try
            {
                string name = request.Name ?? "Particle System";
                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, $"Create Particle System {name}");

                if (request.Position != null)
                    go.transform.position = request.Position.ToVector3();
                if (request.Rotation != null)
                    go.transform.eulerAngles = request.Rotation.ToVector3();

                if (request.ParentId.HasValue)
                {
                    var parent = EditorUtility.InstanceIDToObject(request.ParentId.Value) as GameObject;
                    if (parent != null)
                        Undo.SetTransformParent(go.transform, parent.transform, $"Parent {name}");
                }

                var ps = go.AddComponent<ParticleSystem>();

                // Configure via the request
                ApplyMainModule(ps, request);
                ApplyEmissionModule(ps, request.EmissionRate);
                ApplyShapeModule(ps, request.Shape, request.ShapeRadius, request.ShapeAngle);

                // Stop auto-play so settings are visible in edit mode
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    // Default material fallback
                    renderer.sharedMaterial = GetDefaultParticleMaterial();
                }

                EditorUtility.SetDirty(go);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Created particle system '{name}'",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error creating particle system: {e.Message}"
                });
            }
        }

        public static string ModifyParticleSystem(string body)
        {
            var request = JsonConvert.DeserializeObject<ModifyParticleSystemRequest>(body);
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

                var ps = go.GetComponent<ParticleSystem>();
                if (ps == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"No ParticleSystem component on '{go.name}'"
                    });
                }

                Undo.RecordObject(ps, $"Modify Particle System {go.name}");

                // Main module
                var main = ps.main;
                if (request.Duration.HasValue) main.duration = request.Duration.Value;
                if (request.Looping.HasValue) main.loop = request.Looping.Value;
                if (request.PlayOnAwake.HasValue) main.playOnAwake = request.PlayOnAwake.Value;
                if (request.MaxParticles.HasValue) main.maxParticles = request.MaxParticles.Value;
                if (request.SimulationSpeed.HasValue) main.simulationSpeed = request.SimulationSpeed.Value;
                if (request.GravityModifier.HasValue)
                    main.gravityModifier = new ParticleSystem.MinMaxCurve(request.GravityModifier.Value);

                if (!string.IsNullOrEmpty(request.SimulationSpace))
                {
                    main.simulationSpace = request.SimulationSpace.ToLower() switch
                    {
                        "world" => ParticleSystemSimulationSpace.World,
                        _ => ParticleSystemSimulationSpace.Local
                    };
                }

                // Start lifetime
                if (request.StartLifetimeMin.HasValue && request.StartLifetimeMax.HasValue)
                    main.startLifetime = new ParticleSystem.MinMaxCurve(request.StartLifetimeMin.Value, request.StartLifetimeMax.Value);
                else if (request.StartLifetime.HasValue)
                    main.startLifetime = new ParticleSystem.MinMaxCurve(request.StartLifetime.Value);

                // Start speed
                if (request.StartSpeedMin.HasValue && request.StartSpeedMax.HasValue)
                    main.startSpeed = new ParticleSystem.MinMaxCurve(request.StartSpeedMin.Value, request.StartSpeedMax.Value);
                else if (request.StartSpeed.HasValue)
                    main.startSpeed = new ParticleSystem.MinMaxCurve(request.StartSpeed.Value);

                // Start size
                if (request.StartSizeMin.HasValue && request.StartSizeMax.HasValue)
                    main.startSize = new ParticleSystem.MinMaxCurve(request.StartSizeMin.Value, request.StartSizeMax.Value);
                else if (request.StartSize.HasValue)
                    main.startSize = new ParticleSystem.MinMaxCurve(request.StartSize.Value);

                // Start color
                if (request.StartColorMin != null && request.StartColorMax != null)
                {
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        new Color(request.StartColorMin.R, request.StartColorMin.G, request.StartColorMin.B, request.StartColorMin.A),
                        new Color(request.StartColorMax.R, request.StartColorMax.G, request.StartColorMax.B, request.StartColorMax.A)
                    );
                }
                else if (request.StartColor != null)
                {
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        new Color(request.StartColor.R, request.StartColor.G, request.StartColor.B, request.StartColor.A)
                    );
                }

                // Emission module
                if (request.EmissionRate.HasValue)
                    ApplyEmissionModule(ps, request.EmissionRate);

                // Shape module
                if (!string.IsNullOrEmpty(request.Shape) || request.ShapeRadius.HasValue || request.ShapeAngle.HasValue)
                    ApplyShapeModule(ps, request.Shape, request.ShapeRadius, request.ShapeAngle, request.ShapeScale);

                // Renderer material
                if (!string.IsNullOrEmpty(request.MaterialPath))
                {
                    var renderer = go.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                    {
                        var mat = AssetDatabase.LoadAssetAtPath<Material>(request.MaterialPath);
                        if (mat != null)
                        {
                            Undo.RecordObject(renderer, $"Set Particle Material {go.name}");
                            renderer.sharedMaterial = mat;
                        }
                    }
                }

                // Render mode
                if (!string.IsNullOrEmpty(request.RenderMode))
                {
                    var renderer = go.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                    {
                        Undo.RecordObject(renderer, $"Set Particle RenderMode {go.name}");
                        renderer.renderMode = request.RenderMode.ToLower() switch
                        {
                            "mesh" => ParticleSystemRenderMode.Mesh,
                            "stretch" => ParticleSystemRenderMode.Stretch,
                            "horizontalbillboard" => ParticleSystemRenderMode.HorizontalBillboard,
                            "verticalbillboard" => ParticleSystemRenderMode.VerticalBillboard,
                            _ => ParticleSystemRenderMode.Billboard
                        };
                    }
                }

                EditorUtility.SetDirty(ps);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Modified particle system on '{go.name}'"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error modifying particle system: {e.Message}"
                });
            }
        }

        public static string PlayParticleSystem(string body)
        {
            var request = JsonConvert.DeserializeObject<PlayParticleSystemRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.Action))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'action' is required (play, stop, pause, restart)"
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

                var ps = go.GetComponent<ParticleSystem>();
                if (ps == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"No ParticleSystem component on '{go.name}'"
                    });
                }

                bool withChildren = request.WithChildren;
                string action = request.Action.ToLower();

                switch (action)
                {
                    case "play":
                        ps.Play(withChildren);
                        break;
                    case "stop":
                        ps.Stop(withChildren, ParticleSystemStopBehavior.StopEmitting);
                        break;
                    case "pause":
                        ps.Pause(withChildren);
                        break;
                    case "restart":
                        ps.Stop(withChildren, ParticleSystemStopBehavior.StopEmittingAndClear);
                        ps.Play(withChildren);
                        break;
                    default:
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = false,
                            Message = $"Unknown action '{request.Action}'. Use: play, stop, pause, restart"
                        });
                }

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Particle system '{go.name}': {action}"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error controlling particle system: {e.Message}"
                });
            }
        }

        public static string GetParticleSystemInfo(string body)
        {
            var request = JsonConvert.DeserializeObject<GetLightInfoRequest>(body); // reuse simple InstanceId model
            if (request == null)
            {
                return JsonConvert.SerializeObject(new ParticleSystemInfoResult
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
                    return JsonConvert.SerializeObject(new ParticleSystemInfoResult
                    {
                        Success = false,
                        Message = $"GameObject not found: {request.InstanceId}"
                    });
                }

                var ps = go.GetComponent<ParticleSystem>();
                if (ps == null)
                {
                    return JsonConvert.SerializeObject(new ParticleSystemInfoResult
                    {
                        Success = false,
                        Message = $"No ParticleSystem component on '{go.name}'"
                    });
                }

                var main = ps.main;
                var emission = ps.emission;
                var shape = ps.shape;

                var startColor = main.startColor.color;

                var result = new ParticleSystemInfoResult
                {
                    Success = true,
                    IsPlaying = ps.isPlaying,
                    IsPaused = ps.isPaused,
                    IsStopped = ps.isStopped,
                    ParticleCount = ps.particleCount,
                    Time = ps.time,
                    Duration = main.duration,
                    Looping = main.loop,
                    StartLifetime = main.startLifetime.constant,
                    StartSpeed = main.startSpeed.constant,
                    StartSize = main.startSize.constant,
                    StartColor = new ColorData
                    {
                        R = startColor.r,
                        G = startColor.g,
                        B = startColor.b,
                        A = startColor.a
                    },
                    MaxParticles = main.maxParticles,
                    SimulationSpace = main.simulationSpace.ToString(),
                    PlayOnAwake = main.playOnAwake,
                    GravityModifier = main.gravityModifier.constant,
                    EmissionRate = emission.rateOverTime.constant,
                    Shape = shape.shapeType.ToString(),
                    ShapeRadius = shape.radius,
                    ShapeAngle = shape.angle
                };

                return JsonConvert.SerializeObject(result, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new ParticleSystemInfoResult
                {
                    Success = false,
                    Message = $"Error getting particle system info: {e.Message}"
                });
            }
        }

        // ---- Helpers ----

        private static void ApplyMainModule(ParticleSystem ps, CreateParticleSystemRequest req)
        {
            var main = ps.main;
            if (req.Duration.HasValue) main.duration = req.Duration.Value;
            if (req.Looping.HasValue) main.loop = req.Looping.Value;
            if (req.PlayOnAwake.HasValue) main.playOnAwake = req.PlayOnAwake.Value;
            if (req.MaxParticles.HasValue) main.maxParticles = req.MaxParticles.Value;
            if (req.GravityModifier.HasValue)
                main.gravityModifier = new ParticleSystem.MinMaxCurve(req.GravityModifier.Value);

            if (!string.IsNullOrEmpty(req.SimulationSpace))
            {
                main.simulationSpace = req.SimulationSpace.ToLower() switch
                {
                    "world" => ParticleSystemSimulationSpace.World,
                    _ => ParticleSystemSimulationSpace.Local
                };
            }
            if (req.StartLifetime.HasValue)
                main.startLifetime = new ParticleSystem.MinMaxCurve(req.StartLifetime.Value);
            if (req.StartSpeed.HasValue)
                main.startSpeed = new ParticleSystem.MinMaxCurve(req.StartSpeed.Value);
            if (req.StartSize.HasValue)
                main.startSize = new ParticleSystem.MinMaxCurve(req.StartSize.Value);
            if (req.StartColor != null)
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(req.StartColor.R, req.StartColor.G, req.StartColor.B, req.StartColor.A));
        }

        private static void ApplyEmissionModule(ParticleSystem ps, float? rate)
        {
            if (!rate.HasValue) return;
            var emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate.Value);
        }

        private static void ApplyShapeModule(ParticleSystem ps, string shape, float? radius, float? angle, Vector3Data scale = null)
        {
            if (string.IsNullOrEmpty(shape) && !radius.HasValue && !angle.HasValue && scale == null) return;
            var shapeModule = ps.shape;
            shapeModule.enabled = true;

            if (!string.IsNullOrEmpty(shape))
            {
                shapeModule.shapeType = shape.ToLower() switch
                {
                    "sphere" => ParticleSystemShapeType.Sphere,
                    "hemisphere" => ParticleSystemShapeType.Hemisphere,
                    "cone" => ParticleSystemShapeType.Cone,
                    "box" => ParticleSystemShapeType.Box,
                    "circle" => ParticleSystemShapeType.Circle,
                    "edge" or "line" => ParticleSystemShapeType.SingleSidedEdge,
                    _ => ParticleSystemShapeType.Cone
                };
            }

            if (radius.HasValue) shapeModule.radius = radius.Value;
            if (angle.HasValue) shapeModule.angle = angle.Value;
            if (scale != null) shapeModule.scale = scale.ToVector3();
        }

        private static Material GetDefaultParticleMaterial()
        {
            // Try to find a default particle material
            var guids = AssetDatabase.FindAssets("Default-Particle t:Material");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            return null;
        }
    }
}
