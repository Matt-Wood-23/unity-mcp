using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class SpriteHandler
    {
        /// <summary>
        /// Import an image file as a sprite asset
        /// </summary>
        public static string ImportAsSprite(string body)
        {
            var request = JsonConvert.DeserializeObject<ImportSpriteRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.ImagePath))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'ImagePath' is required"
                });
            }

            try
            {
                // Validate source file exists
                if (!File.Exists(request.ImagePath))
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Source image not found: {request.ImagePath}"
                    });
                }

                // Determine destination path
                string destPath = request.DestinationPath;
                if (string.IsNullOrEmpty(destPath))
                {
                    string fileName = Path.GetFileName(request.ImagePath);
                    destPath = $"Assets/Sprites/{fileName}";
                }

                // Ensure path starts with Assets/
                if (!destPath.StartsWith("Assets/"))
                {
                    destPath = "Assets/" + destPath;
                }

                // Ensure directory exists
                string directory = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
                {
                    CreateFolderRecursive(directory);
                }

                // Copy file to Assets folder
                string fullDestPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), destPath);
                File.Copy(request.ImagePath, fullDestPath, true);

                // Import and configure as sprite
                AssetDatabase.ImportAsset(destPath);

                var importer = AssetImporter.GetAtPath(destPath) as TextureImporter;
                if (importer == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = "Failed to get TextureImporter for asset"
                    });
                }

                // Apply sprite settings
                ConfigureTextureImporter(importer, request.Settings);
                importer.SaveAndReimport();

                return JsonConvert.SerializeObject(new SpriteImportResult
                {
                    Success = true,
                    Message = $"Imported sprite to {destPath}",
                    AssetPath = destPath
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error importing sprite: {e.Message}"
                });
            }
        }

        /// <summary>
        /// Configure sprite settings on an existing texture asset
        /// </summary>
        public static string ConfigureSpriteSettings(string body)
        {
            var request = JsonConvert.DeserializeObject<ConfigureSpriteRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.AssetPath))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'AssetPath' is required"
                });
            }

            try
            {
                var importer = AssetImporter.GetAtPath(request.AssetPath) as TextureImporter;
                if (importer == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"No TextureImporter found at: {request.AssetPath}"
                    });
                }

                ConfigureTextureImporter(importer, request.Settings);
                importer.SaveAndReimport();

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Configured sprite settings for {request.AssetPath}"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error configuring sprite: {e.Message}"
                });
            }
        }

        /// <summary>
        /// Slice a sprite sheet into multiple sprites
        /// </summary>
        public static string SliceSpriteSheet(string body)
        {
            var request = JsonConvert.DeserializeObject<SliceSpriteSheetRequest>(body);
            if (request == null || string.IsNullOrEmpty(request.AssetPath))
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = "Invalid request body - 'AssetPath' is required"
                });
            }

            try
            {
                var importer = AssetImporter.GetAtPath(request.AssetPath) as TextureImporter;
                if (importer == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"No TextureImporter found at: {request.AssetPath}"
                    });
                }

                // Set to multiple sprite mode
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.isReadable = true;

                // Load the texture to get dimensions
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(request.AssetPath);
                if (texture == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = "Failed to load texture"
                    });
                }

                int rows = request.Rows > 0 ? request.Rows : 1;
                int cols = request.Columns > 0 ? request.Columns : 1;
                int cellWidth = texture.width / cols;
                int cellHeight = texture.height / rows;

                var spriteRects = new SpriteMetaData[rows * cols];
                int index = 0;

                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        spriteRects[index] = new SpriteMetaData
                        {
                            name = $"{Path.GetFileNameWithoutExtension(request.AssetPath)}_{index}",
                            rect = new Rect(col * cellWidth, (rows - 1 - row) * cellHeight, cellWidth, cellHeight),
                            alignment = (int)SpriteAlignment.Center,
                            pivot = new Vector2(0.5f, 0.5f)
                        };
                        index++;
                    }
                }

                importer.spritesheet = spriteRects;
                importer.SaveAndReimport();

                return JsonConvert.SerializeObject(new SliceSpriteSheetResult
                {
                    Success = true,
                    Message = $"Sliced sprite sheet into {rows * cols} sprites",
                    SpriteCount = rows * cols,
                    CellWidth = cellWidth,
                    CellHeight = cellHeight
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error slicing sprite sheet: {e.Message}"
                });
            }
        }

        /// <summary>
        /// Create a SpriteRenderer component on a GameObject with a specified sprite
        /// </summary>
        public static string CreateSpriteRenderer(string body)
        {
            var request = JsonConvert.DeserializeObject<CreateSpriteRendererRequest>(body);
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
                GameObject go;

                if (request.InstanceId.HasValue)
                {
                    go = EditorUtility.InstanceIDToObject(request.InstanceId.Value) as GameObject;
                    if (go == null)
                    {
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = false,
                            Message = $"GameObject not found with instance ID: {request.InstanceId}"
                        });
                    }
                }
                else
                {
                    // Create new GameObject
                    go = new GameObject(request.Name ?? "New Sprite");
                    Undo.RegisterCreatedObjectUndo(go, $"Create {go.name}");
                }

                // Add or get SpriteRenderer
                var renderer = go.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    renderer = Undo.AddComponent<SpriteRenderer>(go);
                }
                else
                {
                    Undo.RecordObject(renderer, "Modify SpriteRenderer");
                }

                // Load and assign sprite
                if (!string.IsNullOrEmpty(request.SpritePath))
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(request.SpritePath);
                    if (sprite == null)
                    {
                        // Try loading as texture and getting first sprite
                        var sprites = AssetDatabase.LoadAllAssetsAtPath(request.SpritePath);
                        foreach (var obj in sprites)
                        {
                            if (obj is Sprite s)
                            {
                                sprite = s;
                                break;
                            }
                        }
                    }

                    if (sprite != null)
                    {
                        renderer.sprite = sprite;
                    }
                    else
                    {
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = false,
                            Message = $"Sprite not found at: {request.SpritePath}"
                        });
                    }
                }

                // Apply optional settings
                if (request.SortingLayer != null)
                {
                    renderer.sortingLayerName = request.SortingLayer;
                }
                if (request.OrderInLayer.HasValue)
                {
                    renderer.sortingOrder = request.OrderInLayer.Value;
                }
                if (request.Color != null)
                {
                    renderer.color = new Color(
                        request.Color.R,
                        request.Color.G,
                        request.Color.B,
                        request.Color.A
                    );
                }
                if (request.FlipX.HasValue)
                {
                    renderer.flipX = request.FlipX.Value;
                }
                if (request.FlipY.HasValue)
                {
                    renderer.flipY = request.FlipY.Value;
                }

                EditorUtility.SetDirty(go);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Created SpriteRenderer on '{go.name}'",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error creating SpriteRenderer: {e.Message}"
                });
            }
        }

        private static void ConfigureTextureImporter(TextureImporter importer, SpriteImportSettings settings)
        {
            importer.textureType = TextureImporterType.Sprite;

            settings ??= new SpriteImportSettings();

            // Sprite mode
            importer.spriteImportMode = settings.SpriteMode == "Multiple"
                ? SpriteImportMode.Multiple
                : SpriteImportMode.Single;

            // Pixels per unit
            importer.spritePixelsPerUnit = settings.PixelsPerUnit > 0 ? settings.PixelsPerUnit : 100;

            // Pivot
            switch (settings.PivotMode?.ToLower())
            {
                case "center":
                    importer.spritePivot = new Vector2(0.5f, 0.5f);
                    break;
                case "bottom":
                case "bottomcenter":
                    importer.spritePivot = new Vector2(0.5f, 0f);
                    break;
                case "topleft":
                    importer.spritePivot = new Vector2(0f, 1f);
                    break;
                case "custom":
                    importer.spritePivot = new Vector2(
                        settings.PivotX ?? 0.5f,
                        settings.PivotY ?? 0.5f
                    );
                    break;
                default:
                    importer.spritePivot = new Vector2(0.5f, 0.5f);
                    break;
            }

            // Filter mode (Point for pixel art)
            importer.filterMode = settings.FilterMode?.ToLower() == "point"
                ? FilterMode.Point
                : FilterMode.Bilinear;

            // Compression
            importer.textureCompression = settings.Compression?.ToLower() == "none"
                ? TextureImporterCompression.Uncompressed
                : TextureImporterCompression.Compressed;

            // Generate mipmaps
            importer.mipmapEnabled = settings.GenerateMipMaps ?? false;

            // Max size
            if (settings.MaxSize.HasValue && settings.MaxSize.Value > 0)
            {
                importer.maxTextureSize = settings.MaxSize.Value;
            }
        }

        private static void CreateFolderRecursive(string path)
        {
            string[] folders = path.Split('/');
            string currentPath = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }
    }
}
