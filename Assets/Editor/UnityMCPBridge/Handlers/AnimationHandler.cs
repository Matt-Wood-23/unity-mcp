using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class AnimationHandler
    {
        public static string GetAnimatorInfo(string body)
        {
            var request = JsonConvert.DeserializeObject<GetAnimatorInfoRequest>(body);
            if (request == null)
            {
                return JsonConvert.SerializeObject(new AnimatorInfoResult
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
                    return JsonConvert.SerializeObject(new AnimatorInfoResult
                    {
                        Success = false,
                        Message = $"GameObject not found: {request.InstanceId}"
                    });
                }

                var animator = go.GetComponent<Animator>();
                if (animator == null)
                {
                    return JsonConvert.SerializeObject(new AnimatorInfoResult
                    {
                        Success = false,
                        HasAnimator = false,
                        Message = $"No Animator component on '{go.name}'"
                    });
                }

                var result = new AnimatorInfoResult
                {
                    Success = true,
                    HasAnimator = true,
                    Speed = animator.speed,
                    Parameters = new List<AnimatorParameterInfo>(),
                    Layers = new List<AnimatorLayerInfo>(),
                    Clips = new List<AnimatorClipInfo>()
                };

                // Get controller info
                var controller = animator.runtimeAnimatorController;
                if (controller != null)
                {
                    result.ControllerName = controller.name;

                    // Get animation clips from the controller
                    var clips = controller.animationClips;
                    if (clips != null)
                    {
                        foreach (var clip in clips)
                        {
                            if (clip != null)
                            {
                                result.Clips.Add(new AnimatorClipInfo
                                {
                                    Name = clip.name,
                                    Length = clip.length,
                                    IsLooping = clip.isLooping,
                                    FrameRate = clip.frameRate
                                });
                            }
                        }
                    }
                }

                // Read parameters - works both in edit and play mode if controller is assigned
                if (animator.parameterCount > 0)
                {
                    foreach (var param in animator.parameters)
                    {
                        var paramInfo = new AnimatorParameterInfo
                        {
                            Name = param.name,
                            Type = param.type.ToString()
                        };

                        // Read current values (only meaningful in play mode)
                        if (Application.isPlaying)
                        {
                            switch (param.type)
                            {
                                case AnimatorControllerParameterType.Float:
                                    paramInfo.Value = animator.GetFloat(param.name).ToString("F4");
                                    break;
                                case AnimatorControllerParameterType.Int:
                                    paramInfo.Value = animator.GetInteger(param.name).ToString();
                                    break;
                                case AnimatorControllerParameterType.Bool:
                                    paramInfo.Value = animator.GetBool(param.name).ToString();
                                    break;
                                case AnimatorControllerParameterType.Trigger:
                                    paramInfo.Value = "(trigger)";
                                    break;
                            }
                        }
                        else
                        {
                            paramInfo.Value = $"(default: {param.defaultFloat}/{param.defaultInt}/{param.defaultBool})";
                        }

                        result.Parameters.Add(paramInfo);
                    }
                }

                // Read layers
                for (int i = 0; i < animator.layerCount; i++)
                {
                    result.Layers.Add(new AnimatorLayerInfo
                    {
                        Index = i,
                        Name = animator.GetLayerName(i),
                        Weight = animator.GetLayerWeight(i)
                    });
                }

                // Current state info (play mode only)
                if (Application.isPlaying && animator.isInitialized)
                {
                    result.IsPlaying = true;
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    result.CurrentStateNormalizedTime = stateInfo.normalizedTime;
                }

                result.Message = Application.isPlaying
                    ? "Animator info retrieved (play mode - live values)"
                    : "Animator info retrieved (edit mode - parameter values are defaults)";

                return JsonConvert.SerializeObject(result, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new AnimatorInfoResult
                {
                    Success = false,
                    Message = $"Error getting animator info: {e.Message}"
                });
            }
        }

        public static string SetAnimatorParameter(string body)
        {
            var request = JsonConvert.DeserializeObject<SetAnimatorParameterRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.ParameterName))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'parameterName' and 'parameterType' are required"
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

                var animator = go.GetComponent<Animator>();
                if (animator == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"No Animator component on '{go.name}'"
                    });
                }

                if (!Application.isPlaying)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = "Cannot set animator parameters outside of Play mode. Use unity_set_playmode to enter play mode first."
                    });
                }

                Undo.RecordObject(animator, $"Set Animator Parameter {request.ParameterName}");

                var paramType = request.ParameterType?.ToLower() ?? "";
                switch (paramType)
                {
                    case "float":
                        animator.SetFloat(request.ParameterName, Convert.ToSingle(request.Value));
                        break;
                    case "int":
                        animator.SetInteger(request.ParameterName, Convert.ToInt32(request.Value));
                        break;
                    case "bool":
                        animator.SetBool(request.ParameterName, Convert.ToBoolean(request.Value));
                        break;
                    case "trigger":
                        animator.SetTrigger(request.ParameterName);
                        break;
                    default:
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = false,
                            Message = $"Unknown parameter type: '{request.ParameterType}'. Use: float, int, bool, trigger"
                        });
                }

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Set animator parameter '{request.ParameterName}' ({paramType}) on '{go.name}'"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error setting animator parameter: {e.Message}"
                });
            }
        }

        public static string PlayAnimation(string body)
        {
            var request = JsonConvert.DeserializeObject<PlayAnimationRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.StateName))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'stateName' is required"
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

                var animator = go.GetComponent<Animator>();
                if (animator == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"No Animator component on '{go.name}'"
                    });
                }

                if (!Application.isPlaying)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = "Cannot play animations outside of Play mode. Use unity_set_playmode to enter play mode first."
                    });
                }

                int layer = request.Layer;
                float normalizedTime = request.NormalizedTime;

                if (normalizedTime < 0)
                {
                    animator.Play(request.StateName, layer);
                }
                else
                {
                    animator.Play(request.StateName, layer, normalizedTime);
                }

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Playing animation state '{request.StateName}' on '{go.name}' (layer {layer})"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error playing animation: {e.Message}"
                });
            }
        }
    }
}
