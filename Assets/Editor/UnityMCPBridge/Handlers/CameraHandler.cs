using UnityEngine;
using UnityEditor;
using System;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class CameraHandler
    {
        public static string GetCameraInfo(string body)
        {
            var request = JsonConvert.DeserializeObject<InstanceIdRequest>(body);
            if (request == null)
                return JsonConvert.SerializeObject(new CameraInfoResult { Success = false, Message = "Invalid request body" });

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                    return JsonConvert.SerializeObject(new CameraInfoResult { Success = false, Message = $"GameObject not found: {request.InstanceId}" });

                var cam = go.GetComponent<Camera>();
                if (cam == null)
                    return JsonConvert.SerializeObject(new CameraInfoResult { Success = false, Message = $"No Camera component on '{go.name}'" });

                string rtPath = cam.targetTexture != null ? AssetDatabase.GetAssetPath(cam.targetTexture) : null;

                return JsonConvert.SerializeObject(new CameraInfoResult
                {
                    Success = true,
                    FieldOfView = cam.fieldOfView,
                    NearClipPlane = cam.nearClipPlane,
                    FarClipPlane = cam.farClipPlane,
                    IsOrthographic = cam.orthographic,
                    OrthographicSize = cam.orthographicSize,
                    Depth = cam.depth,
                    CullingMask = cam.cullingMask,
                    ClearFlags = cam.clearFlags.ToString(),
                    BackgroundColor = new ColorData
                    {
                        R = cam.backgroundColor.r,
                        G = cam.backgroundColor.g,
                        B = cam.backgroundColor.b,
                        A = cam.backgroundColor.a
                    },
                    IsMainCamera = cam == Camera.main,
                    RenderTexture = rtPath,
                    AllowHDR = cam.allowHDR,
                    AllowMSAA = cam.allowMSAA,
                    RenderingPath = cam.renderingPath.ToString()
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new CameraInfoResult { Success = false, Message = $"Error: {e.Message}" });
            }
        }

        public static string ModifyCamera(string body)
        {
            var request = JsonConvert.DeserializeObject<ModifyCameraRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                    return Error($"GameObject not found: {request.InstanceId}");

                var cam = go.GetComponent<Camera>();
                if (cam == null)
                    return Error($"No Camera component on '{go.name}'");

                Undo.RecordObject(cam, $"Modify Camera {go.name}");

                if (request.FieldOfView.HasValue) cam.fieldOfView = request.FieldOfView.Value;
                if (request.NearClipPlane.HasValue) cam.nearClipPlane = request.NearClipPlane.Value;
                if (request.FarClipPlane.HasValue) cam.farClipPlane = request.FarClipPlane.Value;
                if (request.OrthographicSize.HasValue) cam.orthographicSize = request.OrthographicSize.Value;
                if (request.Depth.HasValue) cam.depth = request.Depth.Value;
                if (request.CullingMask.HasValue) cam.cullingMask = request.CullingMask.Value;
                if (request.AllowHDR.HasValue) cam.allowHDR = request.AllowHDR.Value;
                if (request.AllowMSAA.HasValue) cam.allowMSAA = request.AllowMSAA.Value;

                if (!string.IsNullOrEmpty(request.ProjectionType))
                {
                    cam.orthographic = request.ProjectionType.ToLower() == "orthographic";
                }

                if (!string.IsNullOrEmpty(request.ClearFlags))
                {
                    cam.clearFlags = request.ClearFlags.ToLower() switch
                    {
                        "skybox" => CameraClearFlags.Skybox,
                        "solidcolor" or "color" => CameraClearFlags.SolidColor,
                        "depth" => CameraClearFlags.Depth,
                        "nothing" or "none" => CameraClearFlags.Nothing,
                        _ => cam.clearFlags
                    };
                }

                if (request.BackgroundColor != null)
                {
                    cam.backgroundColor = new Color(
                        request.BackgroundColor.R,
                        request.BackgroundColor.G,
                        request.BackgroundColor.B,
                        request.BackgroundColor.A);
                }

                if (!string.IsNullOrEmpty(request.RenderTexturePath))
                {
                    var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(request.RenderTexturePath);
                    if (rt != null)
                        cam.targetTexture = rt;
                    else
                        return Error($"RenderTexture not found at: {request.RenderTexturePath}");
                }

                if (request.ClearRenderTexture == true)
                    cam.targetTexture = null;

                EditorUtility.SetDirty(cam);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Camera modified on '{go.name}'"
                });
            }
            catch (Exception e)
            {
                return Error($"Error modifying camera: {e.Message}");
            }
        }

        private static string Error(string msg) =>
            JsonConvert.SerializeObject(new OperationResult { Success = false, Message = msg });
    }
}
