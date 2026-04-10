using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class AnimationClipHandler
    {
        public static string CreateAnimationClip(string body)
        {
            var request = JsonConvert.DeserializeObject<CreateAnimationClipRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                string name = request.Name ?? "NewAnimation";
                string savePath = request.SavePath ?? $"Assets/Animations/{name}.anim";

                // Ensure directory exists
                string dir = System.IO.Path.GetDirectoryName(savePath).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    string[] parts = dir.Split('/');
                    string current = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string next = current + "/" + parts[i];
                        if (!AssetDatabase.IsValidFolder(next))
                            AssetDatabase.CreateFolder(current, parts[i]);
                        current = next;
                    }
                }

                var clip = new AnimationClip();
                clip.name = name;
                if (request.FrameRate.HasValue) clip.frameRate = request.FrameRate.Value;
                if (request.IsLooping.HasValue)
                {
                    var settings = AnimationUtility.GetAnimationClipSettings(clip);
                    settings.loopTime = request.IsLooping.Value;
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                }

                AssetDatabase.CreateAsset(clip, savePath);
                AssetDatabase.SaveAssets();

                return JsonConvert.SerializeObject(new AnimationClipResult
                {
                    Success = true,
                    Message = $"Animation clip '{name}' created at {savePath}",
                    ClipPath = savePath
                });
            }
            catch (Exception e)
            {
                return Error($"Error creating animation clip: {e.Message}");
            }
        }

        public static string AddKeyframes(string body)
        {
            var request = JsonConvert.DeserializeObject<AddKeyframesRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(request.ClipPath);
                if (clip == null)
                    return Error($"AnimationClip not found at: {request.ClipPath}");

                if (request.Keyframes == null || request.Keyframes.Count == 0)
                    return Error("No keyframes provided");

                var keyframes = new Keyframe[request.Keyframes.Count];
                for (int i = 0; i < request.Keyframes.Count; i++)
                    keyframes[i] = new Keyframe(request.Keyframes[i].Time, request.Keyframes[i].Value);

                var curve = new AnimationCurve(keyframes);

                // Smooth tangents if requested
                if (request.SmoothTangents)
                    for (int i = 0; i < curve.length; i++)
                        curve.SmoothTangents(i, 0f);

                // Determine the binding type
                Type bindingType = ResolveBindingType(request.BindingType);

                var binding = new EditorCurveBinding
                {
                    path = request.GameObjectPath ?? "",
                    type = bindingType,
                    propertyName = request.PropertyPath
                };

                Undo.RecordObject(clip, "Add Animation Keyframes");
                AnimationUtility.SetEditorCurve(clip, binding, curve);
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Added {keyframes.Length} keyframe(s) to '{request.PropertyPath}' on clip '{clip.name}'"
                });
            }
            catch (Exception e)
            {
                return Error($"Error adding keyframes: {e.Message}");
            }
        }

        public static string GetClipInfo(string body)
        {
            var request = JsonConvert.DeserializeObject<GetClipInfoRequest>(body);
            if (request == null)
                return JsonConvert.SerializeObject(new AnimationClipInfoResult { Success = false, Message = "Invalid request body" });

            try
            {
                AnimationClip clip = null;

                if (!string.IsNullOrEmpty(request.ClipPath))
                    clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(request.ClipPath);

                if (clip == null && request.InstanceId.HasValue)
                    clip = EditorUtility.InstanceIDToObject(request.InstanceId.Value) as AnimationClip;

                if (clip == null)
                    return JsonConvert.SerializeObject(new AnimationClipInfoResult { Success = false, Message = "AnimationClip not found" });

                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                var bindings = AnimationUtility.GetCurveBindings(clip);

                var bindingList = new List<string>();
                foreach (var b in bindings)
                    bindingList.Add($"{b.path}/{b.type.Name}/{b.propertyName}");

                return JsonConvert.SerializeObject(new AnimationClipInfoResult
                {
                    Success = true,
                    Name = clip.name,
                    Length = clip.length,
                    FrameRate = clip.frameRate,
                    IsLooping = settings.loopTime,
                    WrapMode = clip.wrapMode.ToString(),
                    CurveCount = bindings.Length,
                    CurveBindings = bindingList
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new AnimationClipInfoResult { Success = false, Message = $"Error: {e.Message}" });
            }
        }

        private static Type ResolveBindingType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return typeof(Transform);
            return typeName.ToLower() switch
            {
                "transform" => typeof(Transform),
                "light" => typeof(Light),
                "camera" => typeof(Camera),
                "rigidbody" => typeof(Rigidbody),
                "meshrenderer" => typeof(MeshRenderer),
                "spriterenderer" => typeof(SpriteRenderer),
                "audiosource" => typeof(AudioSource),
                _ => typeof(Transform)
            };
        }

        private static string Error(string msg) =>
            JsonConvert.SerializeObject(new OperationResult { Success = false, Message = msg });
    }
}
