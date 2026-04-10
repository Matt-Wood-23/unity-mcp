using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class BatchHandler
    {
        public static string BatchModify(string body)
        {
            var request = JsonConvert.DeserializeObject<BatchModifyRequest>(body);
            if (request == null || (request.InstanceIds == null && request.Filter == null))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'instanceIds' or 'filter' is required"
                });
            }

            try
            {
                List<GameObject> targets = ResolveTargets(request.InstanceIds, request.Filter);

                if (targets.Count == 0)
                {
                    return JsonConvert.SerializeObject(new BatchOperationResult
                    {
                        Success = false,
                        Message = "No matching GameObjects found",
                        AffectedCount = 0
                    });
                }

                int modified = 0;
                var errors = new List<string>();

                foreach (var go in targets)
                {
                    try
                    {
                        Undo.RecordObject(go, "Batch Modify");
                        Undo.RecordObject(go.transform, "Batch Modify Transform");

                        if (!string.IsNullOrEmpty(request.Tag))
                            go.tag = request.Tag;

                        if (request.Layer.HasValue)
                            go.layer = request.Layer.Value;

                        if (request.IsActive.HasValue)
                            go.SetActive(request.IsActive.Value);

                        if (request.IsStatic.HasValue)
                            go.isStatic = request.IsStatic.Value;

                        if (request.Position != null)
                            go.transform.position = request.Position.ToVector3();

                        if (request.Rotation != null)
                            go.transform.eulerAngles = request.Rotation.ToVector3();

                        if (request.Scale != null)
                            go.transform.localScale = request.Scale.ToVector3();

                        if (request.ParentId.HasValue)
                        {
                            if (request.ParentId.Value == 0)
                            {
                                Undo.SetTransformParent(go.transform, null, "Batch Unparent");
                            }
                            else
                            {
                                var parent = EditorUtility.InstanceIDToObject(request.ParentId.Value) as GameObject;
                                if (parent != null)
                                {
                                    Undo.SetTransformParent(go.transform, parent.transform, "Batch Reparent");
                                }
                            }
                        }

                        // Add component
                        if (!string.IsNullOrEmpty(request.AddComponent))
                        {
                            var type = FindComponentType(request.AddComponent);
                            if (type != null)
                            {
                                Undo.AddComponent(go, type);
                            }
                        }

                        // Remove component
                        if (!string.IsNullOrEmpty(request.RemoveComponent))
                        {
                            var type = FindComponentType(request.RemoveComponent);
                            if (type != null)
                            {
                                var comp = go.GetComponent(type);
                                if (comp != null)
                                {
                                    Undo.DestroyObjectImmediate(comp);
                                }
                            }
                        }

                        EditorUtility.SetDirty(go);
                        modified++;
                    }
                    catch (Exception e)
                    {
                        errors.Add($"{go.name}: {e.Message}");
                    }
                }

                return JsonConvert.SerializeObject(new BatchOperationResult
                {
                    Success = true,
                    Message = $"Modified {modified}/{targets.Count} GameObjects",
                    AffectedCount = modified,
                    Errors = errors.Count > 0 ? errors : null
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error in batch modify: {e.Message}"
                });
            }
        }

        public static string BatchDelete(string body)
        {
            var request = JsonConvert.DeserializeObject<BatchDeleteRequest>(body);
            if (request == null || (request.InstanceIds == null && request.Filter == null))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'instanceIds' or 'filter' is required"
                });
            }

            try
            {
                List<GameObject> targets = ResolveTargets(request.InstanceIds, request.Filter);

                if (targets.Count == 0)
                {
                    return JsonConvert.SerializeObject(new BatchOperationResult
                    {
                        Success = false,
                        Message = "No matching GameObjects found",
                        AffectedCount = 0
                    });
                }

                int deleted = 0;
                var errors = new List<string>();

                foreach (var go in targets)
                {
                    try
                    {
                        Undo.DestroyObjectImmediate(go);
                        deleted++;
                    }
                    catch (Exception e)
                    {
                        errors.Add($"{go.name}: {e.Message}");
                    }
                }

                return JsonConvert.SerializeObject(new BatchOperationResult
                {
                    Success = true,
                    Message = $"Deleted {deleted}/{targets.Count} GameObjects",
                    AffectedCount = deleted,
                    Errors = errors.Count > 0 ? errors : null
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error in batch delete: {e.Message}"
                });
            }
        }

        private static List<GameObject> ResolveTargets(List<int> instanceIds, BatchFilter filter)
        {
            var targets = new List<GameObject>();

            // By instance IDs
            if (instanceIds != null && instanceIds.Count > 0)
            {
                foreach (var id in instanceIds)
                {
                    var go = EditorUtility.InstanceIDToObject(id) as GameObject;
                    if (go != null)
                        targets.Add(go);
                }
                return targets;
            }

            // By filter
            if (filter != null)
            {
                var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

                foreach (var go in allObjects)
                {
                    bool matches = true;

                    if (!string.IsNullOrEmpty(filter.Name))
                    {
                        matches = go.name.IndexOf(filter.Name, StringComparison.OrdinalIgnoreCase) >= 0;
                    }

                    if (matches && !string.IsNullOrEmpty(filter.Tag))
                    {
                        matches = go.CompareTag(filter.Tag);
                    }

                    if (matches && !string.IsNullOrEmpty(filter.Layer))
                    {
                        matches = LayerMask.LayerToName(go.layer).Equals(filter.Layer, StringComparison.OrdinalIgnoreCase);
                    }

                    if (matches && !string.IsNullOrEmpty(filter.HasComponent))
                    {
                        matches = go.GetComponents<Component>().Any(c =>
                            c != null && c.GetType().Name.Equals(filter.HasComponent, StringComparison.OrdinalIgnoreCase));
                    }

                    if (matches && filter.ActiveOnly)
                    {
                        matches = go.activeInHierarchy;
                    }

                    if (matches)
                    {
                        targets.Add(go);
                        if (targets.Count >= (filter.MaxResults > 0 ? filter.MaxResults : 1000))
                            break;
                    }
                }
            }

            return targets;
        }

        private static Type FindComponentType(string typeName)
        {
            // Search Unity engine types
            var type = typeof(Component).Assembly.GetType($"UnityEngine.{typeName}")
                    ?? typeof(Component).Assembly.GetType(typeName);

            if (type != null) return type;

            // Search all assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetTypes().FirstOrDefault(t =>
                    t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) &&
                    typeof(Component).IsAssignableFrom(t));
                if (type != null) return type;
            }

            return null;
        }
    }
}
