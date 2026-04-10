using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Providers
{
    public static class ScreenshotProvider
    {
        public static string TakeScreenshot(string body)
        {
            var request = JsonConvert.DeserializeObject<TakeScreenshotRequest>(body);
            if (request == null)
            {
                request = new TakeScreenshotRequest();
            }

            try
            {
                string source = request.Source?.ToLower() ?? "game";
                int width = request.Width > 0 ? request.Width : 640;
                int height = request.Height > 0 ? request.Height : 480;

                Texture2D screenshot = null;

                if (source == "scene")
                {
                    screenshot = CaptureSceneView(width, height);
                }
                else
                {
                    screenshot = CaptureGameView(width, height);
                }

                if (screenshot == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Failed to capture {source} view. Make sure the {source} view window is open."
                    });
                }

                byte[] imageBytes;
                string format;

                if (request.Format?.ToLower() == "jpg" || request.Format?.ToLower() == "jpeg")
                {
                    imageBytes = screenshot.EncodeToJPG(request.Quality > 0 ? request.Quality : 85);
                    format = "jpeg";
                }
                else
                {
                    imageBytes = screenshot.EncodeToPNG();
                    format = "png";
                }

                string base64 = Convert.ToBase64String(imageBytes);
                UnityEngine.Object.DestroyImmediate(screenshot);

                // If savePath is specified, also save to disk
                if (!string.IsNullOrEmpty(request.SavePath))
                {
                    string savePath = request.SavePath;
                    var directory = System.IO.Path.GetDirectoryName(savePath);
                    if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                    {
                        System.IO.Directory.CreateDirectory(directory);
                    }
                    System.IO.File.WriteAllBytes(savePath, imageBytes);
                }

                return JsonConvert.SerializeObject(new ScreenshotResult
                {
                    Success = true,
                    Message = $"Captured {source} view ({width}x{height})",
                    Width = width,
                    Height = height,
                    Format = format,
                    Base64 = base64,
                    SavePath = request.SavePath
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error capturing screenshot: {e.Message}"
                });
            }
        }

        private static Texture2D CaptureSceneView(int width, int height)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                sceneView = SceneView.sceneViews.Count > 0 ? (SceneView)SceneView.sceneViews[0] : null;
            }

            if (sceneView == null || sceneView.camera == null)
                return null;

            var camera = sceneView.camera;
            var renderTexture = new RenderTexture(width, height, 24);
            var previous = camera.targetTexture;

            camera.targetTexture = renderTexture;
            camera.Render();
            camera.targetTexture = previous;

            RenderTexture.active = renderTexture;
            var screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(renderTexture);

            return screenshot;
        }

        private static Texture2D CaptureGameView(int width, int height)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                // Try to find any camera
                camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            }

            if (camera == null)
                return null;

            var renderTexture = new RenderTexture(width, height, 24);
            var previous = camera.targetTexture;

            camera.targetTexture = renderTexture;
            camera.Render();
            camera.targetTexture = previous;

            RenderTexture.active = renderTexture;
            var screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(renderTexture);

            return screenshot;
        }
    }
}
