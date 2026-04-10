using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class UIHandler
    {
        public static string CreateUIElement(string body)
        {
            var request = JsonConvert.DeserializeObject<CreateUIElementRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.ElementType))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'elementType' is required"
                });
            }

            try
            {
                // Ensure a Canvas exists (or find specified parent canvas)
                Canvas canvas = null;
                GameObject canvasGO = null;

                if (request.ParentId.HasValue)
                {
                    var parent = EditorUtility.InstanceIDToObject(request.ParentId.Value) as GameObject;
                    if (parent != null)
                    {
                        canvas = parent.GetComponentInParent<Canvas>();
                    }
                }

                if (canvas == null && request.ElementType.ToLower() != "canvas")
                {
                    // Find or create a canvas
                    canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                    if (canvas == null)
                    {
                        canvasGO = CreateCanvas("Canvas");
                        canvas = canvasGO.GetComponent<Canvas>();
                        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
                    }
                }

                GameObject element = null;
                string elementType = request.ElementType.ToLower();

                switch (elementType)
                {
                    case "canvas":
                        element = CreateCanvas(request.Name ?? "Canvas");
                        break;

                    case "text":
                        element = CreateText(request, canvas);
                        break;

                    case "button":
                        element = CreateButton(request, canvas);
                        break;

                    case "image":
                        element = CreateImage(request, canvas);
                        break;

                    case "panel":
                        element = CreatePanel(request, canvas);
                        break;

                    case "inputfield":
                    case "input":
                        element = CreateInputField(request, canvas);
                        break;

                    case "slider":
                        element = CreateSlider(request, canvas);
                        break;

                    case "toggle":
                        element = CreateToggle(request, canvas);
                        break;

                    case "dropdown":
                        element = CreateDropdown(request, canvas);
                        break;

                    case "scrollview":
                    case "scroll":
                        element = CreateScrollView(request, canvas);
                        break;

                    case "rawimage":
                        element = CreateRawImage(request, canvas);
                        break;

                    default:
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = false,
                            Message = $"Unknown UI element type: '{request.ElementType}'. Supported: Canvas, Text, Button, Image, Panel, InputField, Slider, Toggle, Dropdown, ScrollView, RawImage"
                        });
                }

                if (element == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = "Failed to create UI element"
                    });
                }

                // Set parent if specified and not already parented
                if (request.ParentId.HasValue)
                {
                    var parent = EditorUtility.InstanceIDToObject(request.ParentId.Value) as GameObject;
                    if (parent != null && element.transform.parent != parent.transform)
                    {
                        Undo.SetTransformParent(element.transform, parent.transform, "Set UI Parent");
                    }
                }
                else if (canvas != null && elementType != "canvas" && element.transform.parent == null)
                {
                    element.transform.SetParent(canvas.transform, false);
                }

                // Apply anchored position
                var rectTransform = element.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    if (request.AnchoredPosition != null)
                    {
                        rectTransform.anchoredPosition = new Vector2(
                            request.AnchoredPosition.X,
                            request.AnchoredPosition.Y
                        );
                    }
                    if (request.SizeDelta != null)
                    {
                        rectTransform.sizeDelta = new Vector2(
                            request.SizeDelta.X,
                            request.SizeDelta.Y
                        );
                    }
                    if (request.AnchorMin != null)
                    {
                        rectTransform.anchorMin = new Vector2(
                            request.AnchorMin.X,
                            request.AnchorMin.Y
                        );
                    }
                    if (request.AnchorMax != null)
                    {
                        rectTransform.anchorMax = new Vector2(
                            request.AnchorMax.X,
                            request.AnchorMax.Y
                        );
                    }
                    if (request.Pivot != null)
                    {
                        rectTransform.pivot = new Vector2(
                            request.Pivot.X,
                            request.Pivot.Y
                        );
                    }
                }

                Undo.RegisterCreatedObjectUndo(element, $"Create UI {request.ElementType}");

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Created UI {request.ElementType} '{element.name}'",
                    InstanceId = element.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error creating UI element: {e.Message}"
                });
            }
        }

        public static string ModifyUIElement(string body)
        {
            var request = JsonConvert.DeserializeObject<ModifyUIElementRequest>(body);
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

                Undo.RecordObject(go, $"Modify UI Element {go.name}");

                var rectTransform = go.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Undo.RecordObject(rectTransform, "Modify RectTransform");

                    if (request.AnchoredPosition != null)
                        rectTransform.anchoredPosition = new Vector2(request.AnchoredPosition.X, request.AnchoredPosition.Y);
                    if (request.SizeDelta != null)
                        rectTransform.sizeDelta = new Vector2(request.SizeDelta.X, request.SizeDelta.Y);
                    if (request.AnchorMin != null)
                        rectTransform.anchorMin = new Vector2(request.AnchorMin.X, request.AnchorMin.Y);
                    if (request.AnchorMax != null)
                        rectTransform.anchorMax = new Vector2(request.AnchorMax.X, request.AnchorMax.Y);
                    if (request.Pivot != null)
                        rectTransform.pivot = new Vector2(request.Pivot.X, request.Pivot.Y);
                }

                // Modify Text component
                var text = go.GetComponent<Text>();
                if (text != null)
                {
                    Undo.RecordObject(text, "Modify Text");
                    if (!string.IsNullOrEmpty(request.Text))
                        text.text = request.Text;
                    if (request.FontSize.HasValue)
                        text.fontSize = request.FontSize.Value;
                    if (request.Color != null)
                        text.color = new Color(request.Color.R, request.Color.G, request.Color.B, request.Color.A);
                    if (!string.IsNullOrEmpty(request.Alignment))
                        text.alignment = ParseTextAnchor(request.Alignment);
                }

                // Modify Image component
                var image = go.GetComponent<Image>();
                if (image != null)
                {
                    Undo.RecordObject(image, "Modify Image");
                    if (request.Color != null)
                        image.color = new Color(request.Color.R, request.Color.G, request.Color.B, request.Color.A);
                    if (!string.IsNullOrEmpty(request.SpritePath))
                    {
                        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(request.SpritePath);
                        if (sprite != null)
                            image.sprite = sprite;
                    }
                }

                // Modify Button interactable state
                var button = go.GetComponent<Button>();
                if (button != null && request.Interactable.HasValue)
                {
                    Undo.RecordObject(button, "Modify Button");
                    button.interactable = request.Interactable.Value;
                }

                EditorUtility.SetDirty(go);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Modified UI element '{go.name}'"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error modifying UI element: {e.Message}"
                });
            }
        }

        private static GameObject CreateCanvas(string name)
        {
            var canvasGO = new GameObject(name);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            return canvasGO;
        }

        private static GameObject CreateText(CreateUIElementRequest request, Canvas canvas)
        {
            var go = new GameObject(request.Name ?? "Text");
            go.transform.SetParent(canvas.transform, false);
            var text = go.AddComponent<Text>();
            text.text = request.Text ?? "New Text";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = request.FontSize ?? 14;
            text.color = request.Color != null
                ? new Color(request.Color.R, request.Color.G, request.Color.B, request.Color.A)
                : Color.black;
            text.alignment = TextAnchor.MiddleCenter;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 30);
            return go;
        }

        private static GameObject CreateButton(CreateUIElementRequest request, Canvas canvas)
        {
            var go = new GameObject(request.Name ?? "Button");
            go.transform.SetParent(canvas.transform, false);
            var image = go.AddComponent<Image>();
            image.color = request.Color != null
                ? new Color(request.Color.R, request.Color.G, request.Color.B, request.Color.A)
                : Color.white;
            go.AddComponent<Button>();

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 30);

            // Add child text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var text = textGO.AddComponent<Text>();
            text.text = request.Text ?? "Button";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = request.FontSize ?? 14;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;

            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return go;
        }

        private static GameObject CreateImage(CreateUIElementRequest request, Canvas canvas)
        {
            var go = new GameObject(request.Name ?? "Image");
            go.transform.SetParent(canvas.transform, false);
            var image = go.AddComponent<Image>();
            image.color = request.Color != null
                ? new Color(request.Color.R, request.Color.G, request.Color.B, request.Color.A)
                : Color.white;

            if (!string.IsNullOrEmpty(request.SpritePath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(request.SpritePath);
                if (sprite != null)
                    image.sprite = sprite;
            }

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 100);
            return go;
        }

        private static GameObject CreatePanel(CreateUIElementRequest request, Canvas canvas)
        {
            var go = new GameObject(request.Name ?? "Panel");
            go.transform.SetParent(canvas.transform, false);
            var image = go.AddComponent<Image>();
            image.color = request.Color != null
                ? new Color(request.Color.R, request.Color.G, request.Color.B, request.Color.A)
                : new Color(1, 1, 1, 0.4f);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            return go;
        }

        private static GameObject CreateInputField(CreateUIElementRequest request, Canvas canvas)
        {
            var go = new GameObject(request.Name ?? "InputField");
            go.transform.SetParent(canvas.transform, false);
            var image = go.AddComponent<Image>();
            image.color = Color.white;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 30);

            // Placeholder text
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(go.transform, false);
            var placeholder = placeholderGO.AddComponent<Text>();
            placeholder.text = request.Text ?? "Enter text...";
            placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholder.fontSize = request.FontSize ?? 14;
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.color = new Color(0, 0, 0, 0.5f);
            placeholder.alignment = TextAnchor.MiddleLeft;
            var phRect = placeholderGO.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(10, 0);
            phRect.offsetMax = new Vector2(-10, 0);

            // Text child
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var textComp = textGO.AddComponent<Text>();
            textComp.text = "";
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = request.FontSize ?? 14;
            textComp.color = Color.black;
            textComp.supportRichText = false;
            textComp.alignment = TextAnchor.MiddleLeft;
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            var inputField = go.AddComponent<InputField>();
            inputField.textComponent = textComp;
            inputField.placeholder = placeholder;

            return go;
        }

        private static GameObject CreateSlider(CreateUIElementRequest request, Canvas canvas)
        {
            var go = new GameObject(request.Name ?? "Slider");
            go.transform.SetParent(canvas.transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 20);

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(go.transform, false);
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.8f, 0.8f, 0.8f);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.sizeDelta = Vector2.zero;

            // Fill Area
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(go.transform, false);
            var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.sizeDelta = Vector2.zero;

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillImage = fillGO.AddComponent<Image>();
            fillImage.color = request.Color != null
                ? new Color(request.Color.R, request.Color.G, request.Color.B, request.Color.A)
                : new Color(0.3f, 0.6f, 1f);
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;

            // Handle
            var handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(go.transform, false);
            var handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = Vector2.zero;

            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            var handleImage = handleGO.AddComponent<Image>();
            handleImage.color = Color.white;
            var handleRect = handleGO.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            return go;
        }

        private static GameObject CreateToggle(CreateUIElementRequest request, Canvas canvas)
        {
            var go = new GameObject(request.Name ?? "Toggle");
            go.transform.SetParent(canvas.transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 20);

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(go.transform, false);
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = Color.white;
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0);
            bgRect.anchorMax = new Vector2(0, 1);
            bgRect.sizeDelta = new Vector2(20, 20);
            bgRect.anchoredPosition = new Vector2(10, 0);

            // Checkmark
            var checkGO = new GameObject("Checkmark");
            checkGO.transform.SetParent(bgGO.transform, false);
            var checkImage = checkGO.AddComponent<Image>();
            checkImage.color = request.Color != null
                ? new Color(request.Color.R, request.Color.G, request.Color.B, request.Color.A)
                : new Color(0.3f, 0.6f, 1f);
            var checkRect = checkGO.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.sizeDelta = new Vector2(-4, -4);

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var label = labelGO.AddComponent<Text>();
            label.text = request.Text ?? "Toggle";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = request.FontSize ?? 14;
            label.color = Color.black;
            label.alignment = TextAnchor.MiddleLeft;
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(25, 0);
            labelRect.offsetMax = Vector2.zero;

            var toggle = go.AddComponent<Toggle>();
            toggle.graphic = checkImage;
            toggle.targetGraphic = bgImage;

            return go;
        }

        private static GameObject CreateDropdown(CreateUIElementRequest request, Canvas canvas)
        {
            var go = new GameObject(request.Name ?? "Dropdown");
            go.transform.SetParent(canvas.transform, false);

            var image = go.AddComponent<Image>();
            image.color = Color.white;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 30);

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var label = labelGO.AddComponent<Text>();
            label.text = request.Text ?? "Option A";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = request.FontSize ?? 14;
            label.color = Color.black;
            label.alignment = TextAnchor.MiddleLeft;
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 0);
            labelRect.offsetMax = new Vector2(-25, 0);

            // Template (hidden by default)
            var templateGO = new GameObject("Template");
            templateGO.transform.SetParent(go.transform, false);
            templateGO.SetActive(false);
            var templateImage = templateGO.AddComponent<Image>();
            var templateRect = templateGO.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.sizeDelta = new Vector2(0, 150);

            // Item in template
            var itemGO = new GameObject("Item");
            itemGO.transform.SetParent(templateGO.transform, false);
            var itemToggle = itemGO.AddComponent<Toggle>();
            var itemRect = itemGO.AddComponent<RectTransform>();
            itemRect.anchorMin = Vector2.zero;
            itemRect.anchorMax = new Vector2(1, 0);
            itemRect.sizeDelta = new Vector2(0, 20);

            var itemLabelGO = new GameObject("Item Label");
            itemLabelGO.transform.SetParent(itemGO.transform, false);
            var itemLabel = itemLabelGO.AddComponent<Text>();
            itemLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemLabel.fontSize = request.FontSize ?? 14;
            itemLabel.color = Color.black;
            itemLabel.alignment = TextAnchor.MiddleLeft;
            var itemLabelRect = itemLabelGO.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(10, 0);
            itemLabelRect.offsetMax = Vector2.zero;

            var dropdown = go.AddComponent<Dropdown>();
            dropdown.template = templateRect;
            dropdown.captionText = label;
            dropdown.itemText = itemLabel;
            dropdown.options.Clear();
            dropdown.options.Add(new Dropdown.OptionData("Option A"));
            dropdown.options.Add(new Dropdown.OptionData("Option B"));
            dropdown.options.Add(new Dropdown.OptionData("Option C"));

            return go;
        }

        private static GameObject CreateScrollView(CreateUIElementRequest request, Canvas canvas)
        {
            var go = new GameObject(request.Name ?? "ScrollView");
            go.transform.SetParent(canvas.transform, false);

            var image = go.AddComponent<Image>();
            image.color = new Color(1, 1, 1, 0.4f);
            var scrollRect = go.AddComponent<ScrollRect>();

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 200);

            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(go.transform, false);
            var viewportImage = viewportGO.AddComponent<Image>();
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;
            var viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;

            // Content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300);

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;

            return go;
        }

        private static GameObject CreateRawImage(CreateUIElementRequest request, Canvas canvas)
        {
            var go = new GameObject(request.Name ?? "RawImage");
            go.transform.SetParent(canvas.transform, false);
            var rawImage = go.AddComponent<RawImage>();
            rawImage.color = request.Color != null
                ? new Color(request.Color.R, request.Color.G, request.Color.B, request.Color.A)
                : Color.white;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 100);
            return go;
        }

        private static TextAnchor ParseTextAnchor(string alignment)
        {
            return alignment?.ToLower() switch
            {
                "upperleft" => TextAnchor.UpperLeft,
                "uppercenter" => TextAnchor.UpperCenter,
                "upperright" => TextAnchor.UpperRight,
                "middleleft" => TextAnchor.MiddleLeft,
                "middlecenter" => TextAnchor.MiddleCenter,
                "middleright" => TextAnchor.MiddleRight,
                "lowerleft" => TextAnchor.LowerLeft,
                "lowercenter" => TextAnchor.LowerCenter,
                "lowerright" => TextAnchor.LowerRight,
                _ => TextAnchor.MiddleCenter,
            };
        }
    }
}
