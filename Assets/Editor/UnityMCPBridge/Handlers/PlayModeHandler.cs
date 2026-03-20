using UnityEditor;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class PlayModeHandler
    {
        public static string SetPlayMode(string body)
        {
            var request = JsonConvert.DeserializeObject<PlayModeRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.Action))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'action' is required"
                });
            }

            var action = request.Action.ToLower();

            switch (action)
            {
                case "play":
                    if (EditorApplication.isPlaying)
                    {
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = true,
                            Message = "Already in play mode"
                        });
                    }
                    EditorApplication.isPlaying = true;
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = true,
                        Message = "Entering play mode"
                    });

                case "stop":
                    if (!EditorApplication.isPlaying)
                    {
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = true,
                            Message = "Already stopped"
                        });
                    }
                    EditorApplication.isPlaying = false;
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = true,
                        Message = "Stopping play mode"
                    });

                case "pause":
                    if (!EditorApplication.isPlaying)
                    {
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = false,
                            Message = "Cannot pause - not in play mode"
                        });
                    }
                    EditorApplication.isPaused = !EditorApplication.isPaused;
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = true,
                        Message = EditorApplication.isPaused ? "Paused" : "Resumed"
                    });

                case "step":
                    if (!EditorApplication.isPlaying)
                    {
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = false,
                            Message = "Cannot step - not in play mode"
                        });
                    }
                    EditorApplication.Step();
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = true,
                        Message = "Stepped one frame"
                    });

                default:
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Unknown action: {action}. Valid actions: play, stop, pause, step"
                    });
            }
        }
    }
}
