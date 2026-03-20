using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class GameObjectHandler
    {
        public static string Create(string body)
        {
            var request = JsonConvert.DeserializeObject<CreateGameObjectRequest>(body);
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
                GameObject go;

                // Create primitive or empty GameObject
                if (!string.IsNullOrEmpty(request.PrimitiveType))
                {
                    if (Enum.TryParse<PrimitiveType>(request.PrimitiveType, true, out var primitiveType))
                    {
                        go = GameObject.CreatePrimitive(primitiveType);
                    }
                    else
                    {
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = false,
                            Message = $"Unknown primitive type: {request.PrimitiveType}. Valid types: Sphere, Capsule, Cylinder, Cube, Plane, Quad"
                        });
                    }
                }
                else
                {
                    go = new GameObject();
                }

                // Set name
                if (!string.IsNullOrEmpty(request.Name))
                {
                    go.name = request.Name;
                }

                // Set parent
                if (request.ParentId.HasValue)
                {
                    var parent = EditorUtility.InstanceIDToObject(request.ParentId.Value) as GameObject;
                    if (parent != null)
                    {
                        go.transform.SetParent(parent.transform);
                    }
                }

                // Set transform
                if (request.Position != null)
                {
                    go.transform.position = request.Position.ToVector3();
                }
                if (request.Rotation != null)
                {
                    go.transform.eulerAngles = request.Rotation.ToVector3();
                }
                if (request.Scale != null)
                {
                    go.transform.localScale = request.Scale.ToVector3();
                }

                // Register undo
                Undo.RegisterCreatedObjectUndo(go, $"Create {go.name}");

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Created GameObject '{go.name}'",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error creating GameObject: {e.Message}"
                });
            }
        }

        public static string Modify(string body)
        {
            var request = JsonConvert.DeserializeObject<ModifyGameObjectRequest>(body);
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
                Undo.RecordObject(go, $"Modify {go.name}");
                Undo.RecordObject(go.transform, $"Modify {go.name} Transform");

                if (!string.IsNullOrEmpty(request.Name))
                {
                    go.name = request.Name;
                }

                if (!string.IsNullOrEmpty(request.Tag))
                {
                    go.tag = request.Tag;
                }

                if (request.Layer.HasValue)
                {
                    go.layer = request.Layer.Value;
                }

                if (request.IsActive.HasValue)
                {
                    go.SetActive(request.IsActive.Value);
                }

                if (request.IsStatic.HasValue)
                {
                    go.isStatic = request.IsStatic.Value;
                }

                if (request.Position != null)
                {
                    go.transform.position = request.Position.ToVector3();
                }

                if (request.Rotation != null)
                {
                    go.transform.eulerAngles = request.Rotation.ToVector3();
                }

                if (request.Scale != null)
                {
                    go.transform.localScale = request.Scale.ToVector3();
                }

                if (request.ParentId.HasValue)
                {
                    var parent = EditorUtility.InstanceIDToObject(request.ParentId.Value) as GameObject;
                    if (parent != null)
                    {
                        Undo.SetTransformParent(go.transform, parent.transform, $"Reparent {go.name}");
                    }
                    else if (request.ParentId.Value == 0)
                    {
                        // Special case: ParentId of 0 means unparent
                        Undo.SetTransformParent(go.transform, null, $"Unparent {go.name}");
                    }
                }

                EditorUtility.SetDirty(go);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Modified GameObject '{go.name}'",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error modifying GameObject: {e.Message}"
                });
            }
        }

        public static string Delete(string body)
        {
            var request = JsonConvert.DeserializeObject<DeleteGameObjectRequest>(body);
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
                var name = go.name;
                Undo.DestroyObjectImmediate(go);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Deleted GameObject '{name}'"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error deleting GameObject: {e.Message}"
                });
            }
        }

        public static string AddComponent(string body)
        {
            var request = JsonConvert.DeserializeObject<AddComponentRequest>(body);
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
                // Try to find the component type
                var componentType = FindComponentType(request.ComponentType);
                if (componentType == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Component type not found: {request.ComponentType}"
                    });
                }

                var component = Undo.AddComponent(go, componentType);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Added component '{componentType.Name}' to '{go.name}'",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error adding component: {e.Message}"
                });
            }
        }

        public static string RemoveComponent(string body)
        {
            var request = JsonConvert.DeserializeObject<RemoveComponentRequest>(body);
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
                var componentType = FindComponentType(request.ComponentType);
                if (componentType == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Component type not found: {request.ComponentType}"
                    });
                }

                var components = go.GetComponents(componentType);
                if (components.Length == 0)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"No component of type '{request.ComponentType}' found on '{go.name}'"
                    });
                }

                var index = request.ComponentIndex ?? 0;
                if (index >= components.Length)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Component index {index} out of range (found {components.Length} components)"
                    });
                }

                Undo.DestroyObjectImmediate(components[index]);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Removed component '{componentType.Name}' from '{go.name}'",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error removing component: {e.Message}"
                });
            }
        }

        private static Type FindComponentType(string typeName)
        {
            // Try common Unity types first
            var unityTypes = new[]
            {
                typeof(Rigidbody), typeof(BoxCollider), typeof(SphereCollider),
                typeof(CapsuleCollider), typeof(MeshCollider), typeof(CharacterController),
                typeof(AudioSource), typeof(AudioListener), typeof(Camera),
                typeof(Light), typeof(Animator), typeof(Animation),
                typeof(ParticleSystem), typeof(TrailRenderer), typeof(LineRenderer),
                typeof(Canvas), typeof(CanvasRenderer),
                typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(CircleCollider2D)
            };

            foreach (var type in unityTypes)
            {
                if (type.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                {
                    return type;
                }
            }

            // Search all assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetTypes().FirstOrDefault(t =>
                        t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) &&
                        typeof(Component).IsAssignableFrom(t));

                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                    // Some assemblies may not be loadable
                }
            }

            return null;
        }
    }
}
