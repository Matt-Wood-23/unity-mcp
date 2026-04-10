using UnityEngine;
using UnityEditor;
using System;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class AudioHandler
    {
        public static string AddAudioSource(string body)
        {
            var request = JsonConvert.DeserializeObject<AddAudioSourceRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                    return Error($"GameObject not found: {request.InstanceId}");

                AudioSource source = go.GetComponent<AudioSource>();
                if (source == null)
                    source = Undo.AddComponent<AudioSource>(go);
                else
                    Undo.RecordObject(source, $"Configure AudioSource {go.name}");

                ApplyAudioSourceSettings(source, request);
                EditorUtility.SetDirty(go);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"AudioSource configured on '{go.name}'",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return Error($"Error configuring AudioSource: {e.Message}");
            }
        }

        public static string ModifyAudioSource(string body)
        {
            var request = JsonConvert.DeserializeObject<ModifyAudioSourceRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                    return Error($"GameObject not found: {request.InstanceId}");

                var source = go.GetComponent<AudioSource>();
                if (source == null)
                    return Error($"No AudioSource on '{go.name}'");

                // Validate clip path before applying any mutations
                AudioClip newClip = null;
                if (!string.IsNullOrEmpty(request.ClipPath))
                {
                    newClip = AssetDatabase.LoadAssetAtPath<AudioClip>(request.ClipPath);
                    if (newClip == null)
                        return Error($"AudioClip not found at: {request.ClipPath}");
                }

                Undo.RecordObject(source, $"Modify AudioSource {go.name}");

                if (request.Volume.HasValue) source.volume = request.Volume.Value;
                if (request.Pitch.HasValue) source.pitch = request.Pitch.Value;
                if (request.Loop.HasValue) source.loop = request.Loop.Value;
                if (request.PlayOnAwake.HasValue) source.playOnAwake = request.PlayOnAwake.Value;
                if (request.Mute.HasValue) source.mute = request.Mute.Value;
                if (request.SpatialBlend.HasValue) source.spatialBlend = request.SpatialBlend.Value;
                if (request.MinDistance.HasValue) source.minDistance = request.MinDistance.Value;
                if (request.MaxDistance.HasValue) source.maxDistance = request.MaxDistance.Value;
                if (request.Priority.HasValue) source.priority = request.Priority.Value;
                if (request.StereoPan.HasValue) source.panStereo = request.StereoPan.Value;
                if (request.ReverbZoneMix.HasValue) source.reverbZoneMix = request.ReverbZoneMix.Value;
                if (!string.IsNullOrEmpty(request.RolloffMode))
                {
                    source.rolloffMode = request.RolloffMode.ToLower() switch
                    {
                        "linear" => AudioRolloffMode.Linear,
                        "custom" => AudioRolloffMode.Custom,
                        _ => AudioRolloffMode.Logarithmic
                    };
                }

                if (newClip != null)
                    source.clip = newClip;

                EditorUtility.SetDirty(source);
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"AudioSource modified on '{go.name}'"
                });
            }
            catch (Exception e)
            {
                return Error($"Error modifying AudioSource: {e.Message}");
            }
        }

        public static string GetAudioSourceInfo(string body)
        {
            var request = JsonConvert.DeserializeObject<InstanceIdRequest>(body);
            if (request == null)
                return JsonConvert.SerializeObject(new AudioSourceInfoResult { Success = false, Message = "Invalid request body" });

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                    return JsonConvert.SerializeObject(new AudioSourceInfoResult { Success = false, Message = $"GameObject not found: {request.InstanceId}" });

                var source = go.GetComponent<AudioSource>();
                if (source == null)
                    return JsonConvert.SerializeObject(new AudioSourceInfoResult { Success = false, Message = $"No AudioSource on '{go.name}'" });

                return JsonConvert.SerializeObject(new AudioSourceInfoResult
                {
                    Success = true,
                    ClipName = source.clip?.name,
                    ClipPath = source.clip != null ? AssetDatabase.GetAssetPath(source.clip) : null,
                    Volume = source.volume,
                    Pitch = source.pitch,
                    Loop = source.loop,
                    PlayOnAwake = source.playOnAwake,
                    Mute = source.mute,
                    SpatialBlend = source.spatialBlend,
                    MinDistance = source.minDistance,
                    MaxDistance = source.maxDistance,
                    Priority = source.priority,
                    StereoPan = source.panStereo,
                    ReverbZoneMix = source.reverbZoneMix,
                    RolloffMode = source.rolloffMode.ToString(),
                    IsPlaying = source.isPlaying
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new AudioSourceInfoResult { Success = false, Message = $"Error: {e.Message}" });
            }
        }

        public static string PlayAudio(string body)
        {
            var request = JsonConvert.DeserializeObject<PlayAudioRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                    return Error($"GameObject not found: {request.InstanceId}");

                var source = go.GetComponent<AudioSource>();
                if (source == null)
                    return Error($"No AudioSource on '{go.name}'");

                if (!Application.isPlaying)
                    return Error("Audio playback control requires Play mode. Use unity_set_playmode to enter Play mode first.");

                switch (request.Action?.ToLower())
                {
                    case "play": source.Play(); break;
                    case "stop": source.Stop(); break;
                    case "pause": source.Pause(); break;
                    case "unpause": source.UnPause(); break;
                    default:
                        return Error($"Unknown action '{request.Action}'. Use: play, stop, pause, unpause");
                }

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"AudioSource '{request.Action}' on '{go.name}'"
                });
            }
            catch (Exception e)
            {
                return Error($"Error controlling audio: {e.Message}");
            }
        }

        private static void ApplyAudioSourceSettings(AudioSource source, AddAudioSourceRequest request)
        {
            source.volume = request.Volume;
            source.pitch = request.Pitch;
            source.loop = request.Loop;
            source.playOnAwake = request.PlayOnAwake;
            source.mute = request.Mute;
            source.spatialBlend = request.SpatialBlend;
            source.minDistance = request.MinDistance;
            source.maxDistance = request.MaxDistance;
            source.priority = request.Priority;
            source.panStereo = request.StereoPan;
            if (request.ReverbZoneMix.HasValue) source.reverbZoneMix = request.ReverbZoneMix.Value;

            if (!string.IsNullOrEmpty(request.ClipPath))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(request.ClipPath);
                if (clip != null) source.clip = clip;
            }

            if (!string.IsNullOrEmpty(request.RolloffMode))
            {
                source.rolloffMode = request.RolloffMode.ToLower() switch
                {
                    "linear" => AudioRolloffMode.Linear,
                    "custom" => AudioRolloffMode.Custom,
                    _ => AudioRolloffMode.Logarithmic
                };
            }
        }

        private static string Error(string msg) =>
            JsonConvert.SerializeObject(new OperationResult { Success = false, Message = msg });
    }
}
