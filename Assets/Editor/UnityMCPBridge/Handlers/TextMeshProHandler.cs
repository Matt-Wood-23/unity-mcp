using UnityEngine;
using UnityEditor;
using System;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

namespace UnityMCPBridge.Handlers
{
    public static class TextMeshProHandler
    {
        public static string CreateTMPText(string body)
        {
#if !UNITY_TEXTMESHPRO
            return JsonConvert.SerializeObject(new OperationResult
            {
                Success = false,
                Message = "TextMeshPro is not installed. Add 'UNITY_TEXTMESHPRO' to Scripting Define Symbols after installing the package."
            });
#else
            var request = JsonConvert.DeserializeObject<CreateTMPTextRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                string name = request.Name ?? "TMP Text";
                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, $"Create TMP Text {name}");

                // Set parent
                if (request.ParentId.HasValue)
                {
                    var parent = EditorUtility.InstanceIDToObject(request.ParentId.Value) as GameObject;
                    if (parent != null)
                        Undo.SetTransformParent(go.transform, parent.transform, $"Parent {name}");
                }

                if (request.IsWorldSpace)
                {
                    // 3D world-space text
                    var tmp = go.AddComponent<TextMeshPro>();
                    tmp.text = request.Text ?? "Sample Text";
                    if (request.FontSize.HasValue) tmp.fontSize = request.FontSize.Value;
                    if (request.Color != null)
                        tmp.color = new Color(request.Color.R, request.Color.G, request.Color.B, request.Color.A);
                    if (!string.IsNullOrEmpty(request.Alignment))
                        tmp.alignment = ParseAlignment(request.Alignment);
                    if (request.Position != null)
                        go.transform.position = request.Position.ToVector3();
                    if (request.Rotation != null)
                        go.transform.eulerAngles = request.Rotation.ToVector3();
                }
                else
                {
                    // UI text - TextMeshProUGUI auto-adds RectTransform
                    var tmp = go.AddComponent<TextMeshProUGUI>();
                    var rect = go.GetComponent<RectTransform>();
                    tmp.text = request.Text ?? "Sample Text";
                    if (request.FontSize.HasValue) tmp.fontSize = request.FontSize.Value;
                    if (request.Color != null)
                        tmp.color = new Color(request.Color.R, request.Color.G, request.Color.B, request.Color.A);
                    if (!string.IsNullOrEmpty(request.Alignment))
                        tmp.alignment = ParseAlignment(request.Alignment);
                    if (request.AnchoredPosition != null)
                        rect.anchoredPosition = new Vector2(request.AnchoredPosition.X, request.AnchoredPosition.Y);
                    if (request.SizeDelta != null)
                        rect.sizeDelta = new Vector2(request.SizeDelta.X, request.SizeDelta.Y);
                    else
                        rect.sizeDelta = new Vector2(200, 50);
                }

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Created TMP text '{name}'",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return Error($"Error creating TMP text: {e.Message}");
            }
#endif
        }

        public static string ModifyTMPText(string body)
        {
#if !UNITY_TEXTMESHPRO
            return JsonConvert.SerializeObject(new OperationResult
            {
                Success = false,
                Message = "TextMeshPro is not installed. Add 'UNITY_TEXTMESHPRO' to Scripting Define Symbols after installing the package."
            });
#else
            var request = JsonConvert.DeserializeObject<ModifyTMPTextRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                    return Error($"GameObject not found: {request.InstanceId}");

                // Try UI version first, then world-space
                TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
                if (tmp == null) tmp = go.GetComponent<TextMeshPro>();
                if (tmp == null)
                    return Error($"No TextMeshPro component on '{go.name}'");

                Undo.RecordObject(tmp, $"Modify TMP Text {go.name}");

                if (request.Text != null) tmp.text = request.Text;
                if (request.FontSize.HasValue) tmp.fontSize = request.FontSize.Value;
                if (request.Color != null)
                    tmp.color = new Color(request.Color.R, request.Color.G, request.Color.B, request.Color.A);
                if (!string.IsNullOrEmpty(request.Alignment))
                    tmp.alignment = ParseAlignment(request.Alignment);
                if (request.Bold.HasValue)
                    tmp.fontStyle = request.Bold.Value
                        ? (tmp.fontStyle | FontStyles.Bold)
                        : (tmp.fontStyle & ~FontStyles.Bold);
                if (request.Italic.HasValue)
                    tmp.fontStyle = request.Italic.Value
                        ? (tmp.fontStyle | FontStyles.Italic)
                        : (tmp.fontStyle & ~FontStyles.Italic);
                if (request.CharacterSpacing.HasValue) tmp.characterSpacing = request.CharacterSpacing.Value;
                if (request.LineSpacing.HasValue) tmp.lineSpacing = request.LineSpacing.Value;
                if (request.AutoSizeFont.HasValue) tmp.enableAutoSizing = request.AutoSizeFont.Value;
                if (request.WordWrapping.HasValue)
                {
#if UNITY_2023_1_OR_NEWER
                    tmp.textWrappingMode = request.WordWrapping.Value
                        ? TextWrappingModes.Normal
                        : TextWrappingModes.NoWrap;
#else
                    tmp.enableWordWrapping = request.WordWrapping.Value;
#endif
                }

                EditorUtility.SetDirty(tmp);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"TMP text modified on '{go.name}'"
                });
            }
            catch (Exception e)
            {
                return Error($"Error modifying TMP text: {e.Message}");
            }
#endif
        }

#if UNITY_TEXTMESHPRO
        private static TextAlignmentOptions ParseAlignment(string alignment) =>
            alignment.ToLower() switch
            {
                "left" => TextAlignmentOptions.Left,
                "center" => TextAlignmentOptions.Center,
                "right" => TextAlignmentOptions.Right,
                "justified" => TextAlignmentOptions.Justified,
                "topleft" or "top-left" => TextAlignmentOptions.TopLeft,
                "topcenter" or "top-center" or "top" => TextAlignmentOptions.Top,
                "topright" or "top-right" => TextAlignmentOptions.TopRight,
                "bottomleft" or "bottom-left" => TextAlignmentOptions.BottomLeft,
                "bottomcenter" or "bottom-center" or "bottom" => TextAlignmentOptions.Bottom,
                "bottomright" or "bottom-right" => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Center
            };
#endif

        private static string Error(string msg) =>
            JsonConvert.SerializeObject(new OperationResult { Success = false, Message = msg });
    }
}
