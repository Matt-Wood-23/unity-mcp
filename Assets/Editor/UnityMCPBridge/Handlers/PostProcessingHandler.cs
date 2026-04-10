using UnityEngine;
using UnityEditor;
using System;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

#if USING_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace UnityMCPBridge.Handlers
{
    /// <summary>
    /// Post-processing volume handler.
    /// Requires Universal Render Pipeline (URP).
    /// To enable: add USING_URP to Project Settings > Player > Scripting Define Symbols.
    /// </summary>
    public static class PostProcessingHandler
    {
        public static string CreateVolume(string body)
        {
#if !USING_URP
            return NotAvailable();
#else
            var request = JsonConvert.DeserializeObject<CreateVolumeRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                string name = request.Name ?? "Post-Process Volume";
                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, $"Create Volume {name}");

                if (request.ParentId.HasValue)
                {
                    var parent = EditorUtility.InstanceIDToObject(request.ParentId.Value) as GameObject;
                    if (parent != null)
                        Undo.SetTransformParent(go.transform, parent.transform, $"Parent {name}");
                }

                var volume = go.AddComponent<Volume>();
                volume.isGlobal = request.IsGlobal;
                volume.priority = request.Priority;
                volume.blendDistance = request.BlendDistance;
                volume.weight = request.Weight;

                // Create and assign a VolumeProfile
                string profileName = request.ProfileName ?? (name + " Profile");
                string profilePath = request.ProfileSavePath ?? $"Assets/Settings/{profileName}.asset";

                string dir = System.IO.Path.GetDirectoryName(profilePath).Replace('\\', '/');
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

                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
                volume.sharedProfile = profile;
                AssetDatabase.SaveAssets();

                return JsonConvert.SerializeObject(new VolumeCreateResult
                {
                    Success = true,
                    Message = $"Volume '{name}' created with profile at {profilePath}",
                    InstanceId = go.GetInstanceID(),
                    ProfilePath = profilePath
                });
            }
            catch (Exception e)
            {
                return Error($"Error creating volume: {e.Message}");
            }
#endif
        }

        public static string ModifyVolume(string body)
        {
#if !USING_URP
            return NotAvailable();
#else
            var request = JsonConvert.DeserializeObject<ModifyVolumeRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                    return Error($"GameObject not found: {request.InstanceId}");

                var volume = go.GetComponent<Volume>();
                if (volume == null)
                    return Error($"No Volume component on '{go.name}'");

                var profile = volume.sharedProfile;
                if (profile == null)
                    return Error($"Volume '{go.name}' has no profile assigned");

                Undo.RecordObject(profile, $"Modify Volume {go.name}");

                // Bloom
                if (request.BloomEnabled.HasValue || request.BloomIntensity.HasValue ||
                    request.BloomThreshold.HasValue || request.BloomScatter.HasValue)
                {
                    if (!profile.TryGet<Bloom>(out var bloom))
                        bloom = profile.Add<Bloom>();
                    bloom.active = request.BloomEnabled ?? bloom.active;
                    if (request.BloomIntensity.HasValue) bloom.intensity.Override(request.BloomIntensity.Value);
                    if (request.BloomThreshold.HasValue) bloom.threshold.Override(request.BloomThreshold.Value);
                    if (request.BloomScatter.HasValue) bloom.scatter.Override(request.BloomScatter.Value);
                }

                // Color Adjustments
                if (request.ColorAdjustmentsEnabled.HasValue || request.PostExposure.HasValue ||
                    request.Contrast.HasValue || request.Saturation.HasValue || request.HueShift.HasValue)
                {
                    if (!profile.TryGet<ColorAdjustments>(out var ca))
                        ca = profile.Add<ColorAdjustments>();
                    ca.active = request.ColorAdjustmentsEnabled ?? ca.active;
                    if (request.PostExposure.HasValue) ca.postExposure.Override(request.PostExposure.Value);
                    if (request.Contrast.HasValue) ca.contrast.Override(request.Contrast.Value);
                    if (request.Saturation.HasValue) ca.saturation.Override(request.Saturation.Value);
                    if (request.HueShift.HasValue) ca.hueShift.Override(request.HueShift.Value);
                }

                // Vignette
                if (request.VignetteEnabled.HasValue || request.VignetteIntensity.HasValue ||
                    request.VignetteSmoothness.HasValue)
                {
                    if (!profile.TryGet<Vignette>(out var vignette))
                        vignette = profile.Add<Vignette>();
                    vignette.active = request.VignetteEnabled ?? vignette.active;
                    if (request.VignetteIntensity.HasValue) vignette.intensity.Override(request.VignetteIntensity.Value);
                    if (request.VignetteSmoothness.HasValue) vignette.smoothness.Override(request.VignetteSmoothness.Value);
                }

                // Depth of Field
                if (request.DepthOfFieldEnabled.HasValue || request.FocusDistance.HasValue ||
                    request.Aperture.HasValue || request.FocalLength.HasValue)
                {
                    if (!profile.TryGet<DepthOfField>(out var dof))
                        dof = profile.Add<DepthOfField>();
                    dof.active = request.DepthOfFieldEnabled ?? dof.active;
                    if (request.FocusDistance.HasValue) dof.focusDistance.Override(request.FocusDistance.Value);
                    if (request.Aperture.HasValue) dof.aperture.Override(request.Aperture.Value);
                    if (request.FocalLength.HasValue) dof.focalLength.Override((int)request.FocalLength.Value);
                }

                // Tonemapping
                if (request.TonemappingEnabled.HasValue || !string.IsNullOrEmpty(request.TonemappingMode))
                {
                    if (!profile.TryGet<Tonemapping>(out var tone))
                        tone = profile.Add<Tonemapping>();
                    tone.active = request.TonemappingEnabled ?? tone.active;
                    if (!string.IsNullOrEmpty(request.TonemappingMode))
                    {
                        tone.mode.Override(request.TonemappingMode.ToLower() switch
                        {
                            "aces" => TonemappingMode.ACES,
                            "neutral" => TonemappingMode.Neutral,
                            _ => TonemappingMode.None
                        });
                    }
                }

                // Motion Blur
                if (request.MotionBlurEnabled.HasValue || request.MotionBlurIntensity.HasValue)
                {
                    if (!profile.TryGet<MotionBlur>(out var mb))
                        mb = profile.Add<MotionBlur>();
                    mb.active = request.MotionBlurEnabled ?? mb.active;
                    if (request.MotionBlurIntensity.HasValue) mb.intensity.Override(request.MotionBlurIntensity.Value);
                }

                // Film Grain
                if (request.FilmGrainEnabled.HasValue || request.FilmGrainIntensity.HasValue)
                {
                    if (!profile.TryGet<FilmGrain>(out var grain))
                        grain = profile.Add<FilmGrain>();
                    grain.active = request.FilmGrainEnabled ?? grain.active;
                    if (request.FilmGrainIntensity.HasValue) grain.intensity.Override(request.FilmGrainIntensity.Value);
                }

                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Volume profile modified on '{go.name}'"
                });
            }
            catch (Exception e)
            {
                return Error($"Error modifying volume: {e.Message}");
            }
#endif
        }

        private static string Error(string msg) =>
            JsonConvert.SerializeObject(new OperationResult { Success = false, Message = msg });

        private static string NotAvailable() =>
            JsonConvert.SerializeObject(new OperationResult
            {
                Success = false,
                Message = "Post-processing requires Universal Render Pipeline (URP). Install URP and add 'USING_URP' to Project Settings > Player > Scripting Define Symbols."
            });
    }
}
