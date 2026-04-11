using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class PhysicsHandler
    {
        public static string AddRigidbody(string body)
        {
            var request = JsonConvert.DeserializeObject<AddRigidbodyRequest>(body);
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

                // Check if already has Rigidbody
                var existing = go.GetComponent<Rigidbody>();
                if (existing != null)
                {
                    // Modify existing instead
                    Undo.RecordObject(existing, $"Modify Rigidbody on {go.name}");
                    ConfigureRigidbody(existing, request);
                    EditorUtility.SetDirty(existing);

                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = true,
                        Message = $"Updated existing Rigidbody on '{go.name}'",
                        InstanceId = go.GetInstanceID()
                    });
                }

                var rb = Undo.AddComponent<Rigidbody>(go);
                ConfigureRigidbody(rb, request);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Added Rigidbody to '{go.name}' (mass={request.Mass}, gravity={request.UseGravity}, kinematic={request.IsKinematic})",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error adding Rigidbody: {e.Message}"
                });
            }
        }

        public static string AddCollider(string body)
        {
            var request = JsonConvert.DeserializeObject<AddColliderRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.ColliderType))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'colliderType' is required"
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

                Collider collider;
                var type = request.ColliderType.ToLower();

                switch (type)
                {
                    case "box":
                        var box = Undo.AddComponent<BoxCollider>(go);
                        if (request.Center != null)
                            box.center = request.Center.ToVector3();
                        if (request.Size != null)
                            box.size = request.Size.ToVector3();
                        collider = box;
                        break;

                    case "sphere":
                        var sphere = Undo.AddComponent<SphereCollider>(go);
                        if (request.Center != null)
                            sphere.center = request.Center.ToVector3();
                        if (request.Radius.HasValue)
                            sphere.radius = request.Radius.Value;
                        collider = sphere;
                        break;

                    case "capsule":
                        var capsule = Undo.AddComponent<CapsuleCollider>(go);
                        if (request.Center != null)
                            capsule.center = request.Center.ToVector3();
                        if (request.Radius.HasValue)
                            capsule.radius = request.Radius.Value;
                        if (request.Height.HasValue)
                            capsule.height = request.Height.Value;
                        if (request.Direction.HasValue)
                            capsule.direction = request.Direction.Value;
                        collider = capsule;
                        break;

                    case "mesh":
                        var meshFilter = go.GetComponent<MeshFilter>();
                        if (meshFilter == null || meshFilter.sharedMesh == null)
                        {
                            return JsonConvert.SerializeObject(new OperationResult
                            {
                                Success = false,
                                Message = $"GameObject '{go.name}' needs a MeshFilter with a mesh for MeshCollider"
                            });
                        }
                        var mesh = Undo.AddComponent<MeshCollider>(go);
                        collider = mesh;
                        break;

                    default:
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = false,
                            Message = $"Unknown collider type: '{request.ColliderType}'. Use: Box, Sphere, Capsule, Mesh"
                        });
                }

                collider.isTrigger = request.IsTrigger;

                // Assign physics material
                if (!string.IsNullOrEmpty(request.PhysicMaterialPath))
                {
                    var physicMat = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(request.PhysicMaterialPath);
                    if (physicMat != null)
                    {
                        collider.sharedMaterial = physicMat;
                    }
                }

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Added {request.ColliderType}Collider to '{go.name}' (trigger={request.IsTrigger})",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error adding collider: {e.Message}"
                });
            }
        }

        public static string SetPhysicsSettings(string body)
        {
            var request = JsonConvert.DeserializeObject<SetPhysicsSettingsRequest>(body);
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
                if (request.Gravity != null)
                    Physics.gravity = request.Gravity.ToVector3();
                if (request.BounceThreshold.HasValue)
                    Physics.bounceThreshold = request.BounceThreshold.Value;
                if (request.DefaultContactOffset.HasValue)
                    Physics.defaultContactOffset = request.DefaultContactOffset.Value;
                if (request.SleepThreshold.HasValue)
                    Physics.sleepThreshold = request.SleepThreshold.Value;
                if (request.DefaultSolverIterations.HasValue)
                    Physics.defaultSolverIterations = request.DefaultSolverIterations.Value;
                if (request.DefaultSolverVelocityIterations.HasValue)
                    Physics.defaultSolverVelocityIterations = request.DefaultSolverVelocityIterations.Value;
                if (request.AutoSyncTransforms.HasValue)
                    Physics.autoSyncTransforms = request.AutoSyncTransforms.Value;

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Physics settings updated (gravity={Physics.gravity})"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error setting physics settings: {e.Message}"
                });
            }
        }

        public static string GetPhysicsSettings(string body)
        {
            try
            {
                return JsonConvert.SerializeObject(new PhysicsSettingsResult
                {
                    Success = true,
                    Gravity = new Vector3Data(Physics.gravity),
                    BounceThreshold = Physics.bounceThreshold,
                    DefaultContactOffset = Physics.defaultContactOffset,
                    SleepThreshold = Physics.sleepThreshold,
                    DefaultSolverIterations = Physics.defaultSolverIterations,
                    DefaultSolverVelocityIterations = Physics.defaultSolverVelocityIterations,
                    AutoSyncTransforms = Physics.autoSyncTransforms,
                    SimulationMode = Physics.simulationMode.ToString()
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new PhysicsSettingsResult
                {
                    Success = false,
                    Message = $"Error getting physics settings: {e.Message}"
                });
            }
        }

        public static string GetCollisionMatrix(string body)
        {
            try
            {
                var pairs = new System.Collections.Generic.List<Models.CollisionMatrixLayerPair>();
                var layers = new System.Collections.Generic.List<Models.LayerInfo>();

                // Collect named layers
                for (int i = 0; i < 32; i++)
                {
                    var name = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(name))
                    {
                        layers.Add(new Models.LayerInfo { Index = i, Name = name });
                    }
                }

                // Only report pairs between named layers (avoids 32x32 noise)
                for (int i = 0; i < layers.Count; i++)
                {
                    for (int j = i; j < layers.Count; j++)
                    {
                        var l1 = layers[i].Index;
                        var l2 = layers[j].Index;
                        pairs.Add(new Models.CollisionMatrixLayerPair
                        {
                            Layer1 = l1,
                            Layer1Name = layers[i].Name,
                            Layer2 = l2,
                            Layer2Name = layers[j].Name,
                            Collide = !Physics.GetIgnoreLayerCollision(l1, l2)
                        });
                    }
                }

                return JsonConvert.SerializeObject(new Models.CollisionMatrixResult
                {
                    Success = true,
                    Layers = layers,
                    Pairs = pairs
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new Models.CollisionMatrixResult
                {
                    Success = false,
                    Message = $"Error getting collision matrix: {e.Message}"
                });
            }
        }

        public static string SetCollisionMatrix(string body)
        {
            var request = JsonConvert.DeserializeObject<Models.SetCollisionMatrixRequest>(body);
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
                int changedCount = 0;

                // Bulk enable/disable all
                if (request.EnableAll == true || request.DisableAll == true)
                {
                    bool ignore = request.DisableAll == true;
                    for (int i = 0; i < 32; i++)
                    {
                        for (int j = i; j < 32; j++)
                        {
                            Physics.IgnoreLayerCollision(i, j, ignore);
                        }
                    }
                    changedCount = 528; // 32*33/2
                }

                // Set specific pairs (applied after bulk, so can be used as overrides)
                if (request.Entries != null)
                {
                    foreach (var entry in request.Entries)
                    {
                        if (entry.Layer1 < 0 || entry.Layer1 > 31 || entry.Layer2 < 0 || entry.Layer2 > 31)
                        {
                            return JsonConvert.SerializeObject(new OperationResult
                            {
                                Success = false,
                                Message = $"Layer index out of range (0-31): {entry.Layer1}, {entry.Layer2}"
                            });
                        }
                        Physics.IgnoreLayerCollision(entry.Layer1, entry.Layer2, !entry.Collide);
                        changedCount++;
                    }
                }

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Collision matrix updated ({changedCount} pairs modified)"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error setting collision matrix: {e.Message}"
                });
            }
        }

        private static void ConfigureRigidbody(Rigidbody rb, AddRigidbodyRequest request)
        {
            rb.mass = request.Mass;
            rb.linearDamping = request.Drag;
            rb.angularDamping = request.AngularDrag;
            rb.useGravity = request.UseGravity;
            rb.isKinematic = request.IsKinematic;

            // Collision detection
            if (!string.IsNullOrEmpty(request.CollisionDetection))
            {
                rb.collisionDetectionMode = request.CollisionDetection.ToLower() switch
                {
                    "continuous" => CollisionDetectionMode.Continuous,
                    "continuousdynamic" => CollisionDetectionMode.ContinuousDynamic,
                    "continuousspeculative" => CollisionDetectionMode.ContinuousSpeculative,
                    _ => CollisionDetectionMode.Discrete
                };
            }

            // Interpolation
            if (!string.IsNullOrEmpty(request.Interpolation))
            {
                rb.interpolation = request.Interpolation.ToLower() switch
                {
                    "interpolate" => RigidbodyInterpolation.Interpolate,
                    "extrapolate" => RigidbodyInterpolation.Extrapolate,
                    _ => RigidbodyInterpolation.None
                };
            }

            // Constraints
            if (!string.IsNullOrEmpty(request.Constraints))
            {
                RigidbodyConstraints constraints = RigidbodyConstraints.None;
                var parts = request.Constraints.Split(',');
                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (Enum.TryParse<RigidbodyConstraints>(trimmed, true, out var flag))
                    {
                        constraints |= flag;
                    }
                }
                rb.constraints = constraints;
            }
        }
    }
}
