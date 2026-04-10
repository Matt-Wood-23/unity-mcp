using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class LayerTagHandler
    {
        public static string GetLayersAndTags(string body)
        {
            try
            {
                var layers = new List<LayerInfo>();
                for (int i = 0; i < 32; i++)
                {
                    string layerName = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(layerName))
                        layers.Add(new LayerInfo { Index = i, Name = layerName });
                }

                var tags = new List<string>(UnityEditorInternal.InternalEditorUtility.tags);

                return JsonConvert.SerializeObject(new LayersAndTagsResult
                {
                    Success = true,
                    Layers = layers,
                    Tags = tags
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new LayersAndTagsResult
                {
                    Success = false,
                    Message = $"Error: {e.Message}"
                });
            }
        }

        public static string AddLayer(string body)
        {
            var request = JsonConvert.DeserializeObject<AddLayerRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.LayerName))
                return Error("Layer name is required");

            try
            {
                // Check if layer already exists
                for (int i = 0; i < 32; i++)
                {
                    if (LayerMask.LayerToName(i) == request.LayerName)
                        return JsonConvert.SerializeObject(new LayerAddResult
                        {
                            Success = true,
                            Message = $"Layer '{request.LayerName}' already exists",
                            LayerIndex = i
                        });
                }

                // Find first empty user layer slot (8-31)
                var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                var layersProp = tagManager.FindProperty("layers");

                int newIndex = -1;
                for (int i = 8; i < layersProp.arraySize; i++)
                {
                    var layerProp = layersProp.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(layerProp.stringValue))
                    {
                        layerProp.stringValue = request.LayerName;
                        newIndex = i;
                        break;
                    }
                }

                if (newIndex == -1)
                    return Error("No empty layer slots available (layers 8-31 are all used)");

                tagManager.ApplyModifiedProperties();

                return JsonConvert.SerializeObject(new LayerAddResult
                {
                    Success = true,
                    Message = $"Layer '{request.LayerName}' added at index {newIndex}",
                    LayerIndex = newIndex
                });
            }
            catch (Exception e)
            {
                return Error($"Error adding layer: {e.Message}");
            }
        }

        public static string AddTag(string body)
        {
            var request = JsonConvert.DeserializeObject<AddTagRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.TagName))
                return Error("Tag name is required");

            try
            {
                // Check if already exists
                string[] existingTags = UnityEditorInternal.InternalEditorUtility.tags;
                foreach (var tag in existingTags)
                {
                    if (tag == request.TagName)
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = true,
                            Message = $"Tag '{request.TagName}' already exists"
                        });
                }

                var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                var tagsProp = tagManager.FindProperty("tags");

                int newIndex = tagsProp.arraySize;
                tagsProp.InsertArrayElementAtIndex(newIndex);
                tagsProp.GetArrayElementAtIndex(newIndex).stringValue = request.TagName;
                tagManager.ApplyModifiedProperties();

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Tag '{request.TagName}' added"
                });
            }
            catch (Exception e)
            {
                return Error($"Error adding tag: {e.Message}");
            }
        }

        private static string Error(string msg) =>
            JsonConvert.SerializeObject(new OperationResult { Success = false, Message = msg });
    }
}
