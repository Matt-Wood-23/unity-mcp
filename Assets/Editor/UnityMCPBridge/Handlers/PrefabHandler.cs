using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class PrefabHandler
    {
        public static string CreatePrefab(string body)
        {
            var request = JsonConvert.DeserializeObject<CreatePrefabRequest>(body);
            if (request == null)
            {
                return JsonConvert.SerializeObject(new PrefabInfoResult
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
                    return JsonConvert.SerializeObject(new PrefabInfoResult
                    {
                        Success = false,
                        Message = $"GameObject not found: {request.InstanceId}"
                    });
                }

                // Determine save path
                string savePath = request.SavePath;
                if (string.IsNullOrEmpty(savePath))
                {
                    savePath = $"Assets/Prefabs/{go.name}.prefab";
                }
                if (!savePath.EndsWith(".prefab"))
                    savePath += ".prefab";

                // Ensure directory exists
                var directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
                {
                    Directory.CreateDirectory(directory);
                    AssetDatabase.Refresh();
                }

                // Check if prefab already exists
                bool exists = File.Exists(Path.Combine(Directory.GetCurrentDirectory(), savePath));
                if (exists && !request.ReplacePrefab)
                {
                    return JsonConvert.SerializeObject(new PrefabInfoResult
                    {
                        Success = false,
                        Message = $"Prefab already exists at '{savePath}'. Set replacePrefab=true to overwrite.",
                        PrefabPath = savePath
                    });
                }

                // Save as prefab
                bool saveSuccess;
                var prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(go, savePath, InteractionMode.AutomatedAction, out saveSuccess);

                if (!saveSuccess || prefabAsset == null)
                {
                    return JsonConvert.SerializeObject(new PrefabInfoResult
                    {
                        Success = false,
                        Message = $"Failed to save prefab at '{savePath}'"
                    });
                }

                return JsonConvert.SerializeObject(new PrefabInfoResult
                {
                    Success = true,
                    Message = $"Saved '{go.name}' as prefab at '{savePath}'",
                    PrefabPath = savePath,
                    IsPrefabInstance = true,
                    PrefabStatus = "Connected",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new PrefabInfoResult
                {
                    Success = false,
                    Message = $"Error creating prefab: {e.Message}"
                });
            }
        }

        public static string UnpackPrefab(string body)
        {
            var request = JsonConvert.DeserializeObject<UnpackPrefabRequest>(body);
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

                if (!PrefabUtility.IsPartOfPrefabInstance(go))
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"'{go.name}' is not a prefab instance"
                    });
                }

                // Get prefab root if we're given a child
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                if (root == null) root = go;

                Undo.RegisterFullObjectHierarchyUndo(root, $"Unpack Prefab {root.name}");

                var mode = request.Completely
                    ? PrefabUnpackMode.Completely
                    : PrefabUnpackMode.OutermostRoot;

                PrefabUtility.UnpackPrefabInstance(root, mode, InteractionMode.AutomatedAction);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Unpacked prefab '{root.name}' ({(request.Completely ? "completely" : "outermost root only")})",
                    InstanceId = root.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error unpacking prefab: {e.Message}"
                });
            }
        }

        public static string ApplyPrefabOverrides(string body)
        {
            var request = JsonConvert.DeserializeObject<ApplyPrefabOverridesRequest>(body);
            if (request == null)
            {
                return JsonConvert.SerializeObject(new PrefabInfoResult
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
                    return JsonConvert.SerializeObject(new PrefabInfoResult
                    {
                        Success = false,
                        Message = $"GameObject not found: {request.InstanceId}"
                    });
                }

                if (!PrefabUtility.IsPartOfPrefabInstance(go))
                {
                    return JsonConvert.SerializeObject(new PrefabInfoResult
                    {
                        Success = false,
                        Message = $"'{go.name}' is not a prefab instance"
                    });
                }

                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go) ?? go;
                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);

                PrefabUtility.ApplyPrefabInstance(root, InteractionMode.AutomatedAction);

                return JsonConvert.SerializeObject(new PrefabInfoResult
                {
                    Success = true,
                    Message = $"Applied overrides from '{root.name}' to prefab asset at '{prefabPath}'",
                    PrefabPath = prefabPath,
                    IsPrefabInstance = true,
                    HasOverrides = false,
                    PrefabStatus = "Connected",
                    InstanceId = root.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new PrefabInfoResult
                {
                    Success = false,
                    Message = $"Error applying prefab overrides: {e.Message}"
                });
            }
        }

        public static string RevertPrefabOverrides(string body)
        {
            var request = JsonConvert.DeserializeObject<RevertPrefabOverridesRequest>(body);
            if (request == null)
            {
                return JsonConvert.SerializeObject(new PrefabInfoResult
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
                    return JsonConvert.SerializeObject(new PrefabInfoResult
                    {
                        Success = false,
                        Message = $"GameObject not found: {request.InstanceId}"
                    });
                }

                if (!PrefabUtility.IsPartOfPrefabInstance(go))
                {
                    return JsonConvert.SerializeObject(new PrefabInfoResult
                    {
                        Success = false,
                        Message = $"'{go.name}' is not a prefab instance"
                    });
                }

                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go) ?? go;
                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);

                PrefabUtility.RevertPrefabInstance(root, InteractionMode.AutomatedAction);

                return JsonConvert.SerializeObject(new PrefabInfoResult
                {
                    Success = true,
                    Message = $"Reverted '{root.name}' to prefab asset state",
                    PrefabPath = prefabPath,
                    IsPrefabInstance = true,
                    HasOverrides = false,
                    PrefabStatus = "Connected",
                    InstanceId = root.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new PrefabInfoResult
                {
                    Success = false,
                    Message = $"Error reverting prefab overrides: {e.Message}"
                });
            }
        }

        public static string GetPrefabInfo(string body)
        {
            var request = JsonConvert.DeserializeObject<GetLightInfoRequest>(body); // reuse simple InstanceId model
            if (request == null)
            {
                return JsonConvert.SerializeObject(new PrefabInfoResult
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
                    return JsonConvert.SerializeObject(new PrefabInfoResult
                    {
                        Success = false,
                        Message = $"GameObject not found: {request.InstanceId}"
                    });
                }

                bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(go);
                bool isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(go);
                string prefabPath = null;
                bool hasOverrides = false;
                string status = "None";

                if (isPrefabInstance)
                {
                    prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    hasOverrides = PrefabUtility.HasPrefabInstanceAnyOverrides(go, false);
                    var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(go);
                    status = prefabStatus.ToString();
                }
                else if (isPrefabAsset)
                {
                    prefabPath = AssetDatabase.GetAssetPath(go);
                    status = "Asset";
                }

                return JsonConvert.SerializeObject(new PrefabInfoResult
                {
                    Success = true,
                    IsPrefabInstance = isPrefabInstance,
                    HasOverrides = hasOverrides,
                    PrefabPath = prefabPath,
                    PrefabStatus = status,
                    InstanceId = go.GetInstanceID(),
                    Message = isPrefabInstance
                        ? $"'{go.name}' is a prefab instance{(hasOverrides ? " with overrides" : "")}"
                        : $"'{go.name}' is not a prefab instance"
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new PrefabInfoResult
                {
                    Success = false,
                    Message = $"Error getting prefab info: {e.Message}"
                });
            }
        }
    }
}
