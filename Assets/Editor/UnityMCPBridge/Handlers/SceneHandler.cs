using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class SceneHandler
    {
        public static string FindGameObjects(string body)
        {
            var request = JsonConvert.DeserializeObject<FindGameObjectsRequest>(body);
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
                var results = new List<FoundGameObject>();
                var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

                foreach (var go in allObjects)
                {
                    bool matches = true;

                    // Filter by name (supports partial match)
                    if (!string.IsNullOrEmpty(request.Name))
                    {
                        if (request.ExactMatch)
                        {
                            matches = go.name.Equals(request.Name, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            matches = go.name.IndexOf(request.Name, StringComparison.OrdinalIgnoreCase) >= 0;
                        }
                    }

                    // Filter by tag
                    if (matches && !string.IsNullOrEmpty(request.Tag))
                    {
                        matches = go.CompareTag(request.Tag);
                    }

                    // Filter by layer
                    if (matches && !string.IsNullOrEmpty(request.Layer))
                    {
                        matches = LayerMask.LayerToName(go.layer).Equals(request.Layer, StringComparison.OrdinalIgnoreCase);
                    }

                    // Filter by component type
                    if (matches && !string.IsNullOrEmpty(request.HasComponent))
                    {
                        var hasComp = go.GetComponents<Component>().Any(c =>
                            c != null && c.GetType().Name.Equals(request.HasComponent, StringComparison.OrdinalIgnoreCase));
                        matches = hasComp;
                    }

                    // Filter active only
                    if (matches && request.ActiveOnly)
                    {
                        matches = go.activeInHierarchy;
                    }

                    if (matches)
                    {
                        results.Add(new FoundGameObject
                        {
                            InstanceId = go.GetInstanceID(),
                            Name = go.name,
                            Path = GetGameObjectPath(go),
                            Tag = go.tag,
                            Layer = LayerMask.LayerToName(go.layer),
                            IsActive = go.activeInHierarchy
                        });

                        // Limit results
                        if (results.Count >= (request.MaxResults > 0 ? request.MaxResults : 50))
                            break;
                    }
                }

                return JsonConvert.SerializeObject(new FindGameObjectsResult
                {
                    Success = true,
                    Count = results.Count,
                    GameObjects = results
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error finding GameObjects: {e.Message}"
                });
            }
        }

        public static string InstantiatePrefab(string body)
        {
            var request = JsonConvert.DeserializeObject<InstantiatePrefabRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.PrefabPath))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'prefabPath' is required"
                });
            }

            try
            {
                // Load the prefab
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(request.PrefabPath);
                if (prefab == null)
                {
                    // Try searching by name
                    var guids = AssetDatabase.FindAssets($"{request.PrefabPath} t:Prefab");
                    if (guids.Length > 0)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    }
                }

                if (prefab == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Prefab not found: {request.PrefabPath}"
                    });
                }

                // Instantiate the prefab
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

                // Set name if provided
                if (!string.IsNullOrEmpty(request.Name))
                {
                    instance.name = request.Name;
                }

                // Set parent if provided
                if (request.ParentId.HasValue)
                {
                    var parent = EditorUtility.InstanceIDToObject(request.ParentId.Value) as GameObject;
                    if (parent != null)
                    {
                        instance.transform.SetParent(parent.transform);
                    }
                }

                // Set transform
                if (request.Position != null)
                {
                    instance.transform.position = request.Position.ToVector3();
                }
                if (request.Rotation != null)
                {
                    instance.transform.eulerAngles = request.Rotation.ToVector3();
                }
                if (request.Scale != null)
                {
                    instance.transform.localScale = request.Scale.ToVector3();
                }

                Undo.RegisterCreatedObjectUndo(instance, $"Instantiate {prefab.name}");

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Instantiated prefab '{prefab.name}' as '{instance.name}'",
                    InstanceId = instance.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error instantiating prefab: {e.Message}"
                });
            }
        }

        public static string SaveScene(string body)
        {
            try
            {
                var scene = SceneManager.GetActiveScene();

                if (string.IsNullOrEmpty(scene.path))
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = "Scene has no path - use Save As in Unity first"
                    });
                }

                bool saved = EditorSceneManager.SaveScene(scene);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = saved,
                    Message = saved
                        ? $"Saved scene '{scene.name}'"
                        : "Failed to save scene"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error saving scene: {e.Message}"
                });
            }
        }

        public static string MarkSceneDirty(string body)
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Marked scene '{scene.name}' as dirty"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error marking scene dirty: {e.Message}"
                });
            }
        }

        public static string LoadScene(string body)
        {
            var request = JsonConvert.DeserializeObject<LoadSceneRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.ScenePath))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'scenePath' is required"
                });
            }

            try
            {
                // Check if current scene has unsaved changes
                var currentScene = SceneManager.GetActiveScene();
                if (currentScene.isDirty && !request.Force)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = "Current scene has unsaved changes. Save first or use force=true"
                    });
                }

                // Try to find the scene
                string scenePath = request.ScenePath;
                if (!scenePath.EndsWith(".unity"))
                {
                    // Search for scene by name
                    var guids = AssetDatabase.FindAssets($"{request.ScenePath} t:Scene");
                    if (guids.Length > 0)
                    {
                        scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    }
                }

                if (!System.IO.File.Exists(scenePath))
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Scene not found: {request.ScenePath}"
                    });
                }

                var mode = request.Additive
                    ? OpenSceneMode.Additive
                    : OpenSceneMode.Single;

                var scene = EditorSceneManager.OpenScene(scenePath, mode);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = scene.IsValid(),
                    Message = scene.IsValid()
                        ? $"Loaded scene '{scene.name}'"
                        : "Failed to load scene"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error loading scene: {e.Message}"
                });
            }
        }

        public static string ListScenes(string body)
        {
            try
            {
                var guids = AssetDatabase.FindAssets("t:Scene");
                var scenes = guids.Select(guid =>
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    return new SceneInfo
                    {
                        Path = path,
                        Name = System.IO.Path.GetFileNameWithoutExtension(path)
                    };
                })
                .Where(s => s.Path.StartsWith("Assets/")) // Only project scenes
                .ToList();

                return JsonConvert.SerializeObject(new
                {
                    Success = true,
                    Count = scenes.Count,
                    Scenes = scenes
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error listing scenes: {e.Message}"
                });
            }
        }

        public static string PerformUndo(string body)
        {
            try
            {
                UnityEditor.Undo.PerformUndo();
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = "Undo performed"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error performing undo: {e.Message}"
                });
            }
        }

        public static string PerformRedo(string body)
        {
            try
            {
                UnityEditor.Undo.PerformRedo();
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = "Redo performed"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error performing redo: {e.Message}"
                });
            }
        }

        public static string RefreshAssets(string body)
        {
            try
            {
                AssetDatabase.Refresh();
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = "Asset database refreshed"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error refreshing assets: {e.Message}"
                });
            }
        }

        private static string GetGameObjectPath(GameObject go)
        {
            var path = go.name;
            var parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
