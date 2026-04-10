using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class TerrainHandler
    {
        public static string CreateTerrain(string body)
        {
            var request = JsonConvert.DeserializeObject<CreateTerrainRequest>(body);
            if (request == null)
            {
                request = new CreateTerrainRequest();
            }

            try
            {
                float width = request.Width > 0 ? request.Width : 500;
                float length = request.Length > 0 ? request.Length : 500;
                float height = request.Height > 0 ? request.Height : 100;
                int heightmapResolution = request.HeightmapResolution > 0 ? request.HeightmapResolution : 513;
                int alphamapResolution = request.AlphamapResolution > 0 ? request.AlphamapResolution : 512;

                // Ensure power of 2 + 1 for heightmap
                heightmapResolution = NearestPowerOfTwoPlusOne(heightmapResolution);

                var terrainData = new TerrainData
                {
                    heightmapResolution = heightmapResolution,
                    alphamapResolution = alphamapResolution,
                    size = new Vector3(width, height, length)
                };

                // Save terrain data as asset
                string savePath = request.SavePath ?? "Assets/Terrain.asset";
                if (!savePath.EndsWith(".asset"))
                    savePath += ".asset";

                var directory = System.IO.Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(terrainData, savePath);

                var go = Terrain.CreateTerrainGameObject(terrainData);
                go.name = request.Name ?? "Terrain";

                if (request.Position != null)
                {
                    go.transform.position = request.Position.ToVector3();
                }

                Undo.RegisterCreatedObjectUndo(go, "Create Terrain");

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Created terrain '{go.name}' ({width}x{length}x{height})",
                    InstanceId = go.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error creating terrain: {e.Message}"
                });
            }
        }

        public static string ModifyTerrainHeight(string body)
        {
            var request = JsonConvert.DeserializeObject<ModifyTerrainHeightRequest>(body);
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
                Terrain terrain = FindTerrain(request.InstanceId);
                if (terrain == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = "Terrain not found"
                    });
                }

                var data = terrain.terrainData;
                Undo.RecordObject(data, "Modify Terrain Height");

                string operation = request.Operation?.ToLower() ?? "set";

                switch (operation)
                {
                    case "set":
                        SetHeights(data, request);
                        break;

                    case "raise":
                        RaiseLower(data, request, 1f);
                        break;

                    case "lower":
                        RaiseLower(data, request, -1f);
                        break;

                    case "smooth":
                        SmoothHeights(data, request);
                        break;

                    case "flatten":
                        FlattenHeights(data, request);
                        break;

                    case "perlin":
                        ApplyPerlinNoise(data, request);
                        break;

                    default:
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = false,
                            Message = $"Unknown operation: '{request.Operation}'. Supported: set, raise, lower, smooth, flatten, perlin"
                        });
                }

                EditorUtility.SetDirty(data);
                terrain.Flush();

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Terrain height modified ({operation})"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error modifying terrain height: {e.Message}"
                });
            }
        }

        public static string PaintTerrainTexture(string body)
        {
            var request = JsonConvert.DeserializeObject<PaintTerrainTextureRequest>(body);
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
                Terrain terrain = FindTerrain(request.InstanceId);
                if (terrain == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = "Terrain not found"
                    });
                }

                var data = terrain.terrainData;
                Undo.RecordObject(data, "Paint Terrain Texture");

                // Add terrain layer if texture path provided
                if (!string.IsNullOrEmpty(request.TexturePath))
                {
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(request.TexturePath);
                    if (texture == null)
                    {
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = false,
                            Message = $"Texture not found: {request.TexturePath}"
                        });
                    }

                    var layers = new List<TerrainLayer>(data.terrainLayers);
                    var newLayer = new TerrainLayer
                    {
                        diffuseTexture = texture,
                        tileSize = new Vector2(
                            request.TileSize > 0 ? request.TileSize : 10,
                            request.TileSize > 0 ? request.TileSize : 10
                        )
                    };

                    // Save terrain layer as asset
                    string layerPath = $"Assets/TerrainLayer_{layers.Count}.terrainlayer";
                    AssetDatabase.CreateAsset(newLayer, layerPath);

                    layers.Add(newLayer);
                    data.terrainLayers = layers.ToArray();
                }

                // Paint at specified area if layerIndex given
                if (request.LayerIndex >= 0 && request.LayerIndex < data.terrainLayers.Length)
                {
                    int alphamapWidth = data.alphamapWidth;
                    int alphamapHeight = data.alphamapHeight;
                    int layerCount = data.alphamapLayers;

                    // Determine paint area (normalized 0-1 coordinates)
                    float cx = Mathf.Clamp01(request.CenterX);
                    float cy = Mathf.Clamp01(request.CenterY);
                    float radius = Mathf.Clamp01(request.Radius > 0 ? request.Radius : 0.1f);
                    float strength = Mathf.Clamp01(request.Strength > 0 ? request.Strength : 1f);

                    int startX = Mathf.Max(0, (int)((cx - radius) * alphamapWidth));
                    int startY = Mathf.Max(0, (int)((cy - radius) * alphamapHeight));
                    int endX = Mathf.Min(alphamapWidth, (int)((cx + radius) * alphamapWidth));
                    int endY = Mathf.Min(alphamapHeight, (int)((cy + radius) * alphamapHeight));
                    int sizeX = endX - startX;
                    int sizeY = endY - startY;

                    if (sizeX > 0 && sizeY > 0)
                    {
                        var alphamaps = data.GetAlphamaps(startX, startY, sizeX, sizeY);

                        for (int y = 0; y < sizeY; y++)
                        {
                            for (int x = 0; x < sizeX; x++)
                            {
                                float nx = (startX + x) / (float)alphamapWidth;
                                float ny = (startY + y) / (float)alphamapHeight;
                                float dist = Mathf.Sqrt((nx - cx) * (nx - cx) + (ny - cy) * (ny - cy));

                                if (dist <= radius)
                                {
                                    float falloff = 1f - (dist / radius);
                                    float paintStrength = strength * falloff;

                                    // Reduce other layers
                                    for (int l = 0; l < layerCount; l++)
                                    {
                                        if (l == request.LayerIndex)
                                        {
                                            alphamaps[y, x, l] = Mathf.Lerp(alphamaps[y, x, l], 1f, paintStrength);
                                        }
                                        else
                                        {
                                            alphamaps[y, x, l] = Mathf.Lerp(alphamaps[y, x, l], 0f, paintStrength);
                                        }
                                    }

                                    // Normalize
                                    float sum = 0;
                                    for (int l = 0; l < layerCount; l++)
                                        sum += alphamaps[y, x, l];
                                    if (sum > 0)
                                    {
                                        for (int l = 0; l < layerCount; l++)
                                            alphamaps[y, x, l] /= sum;
                                    }
                                }
                            }
                        }

                        data.SetAlphamaps(startX, startY, alphamaps);
                    }
                }

                EditorUtility.SetDirty(data);
                terrain.Flush();

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Terrain texture painted. Total layers: {data.terrainLayers.Length}"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error painting terrain texture: {e.Message}"
                });
            }
        }

        public static string PlaceTerrainTrees(string body)
        {
            var request = JsonConvert.DeserializeObject<PlaceTerrainTreesRequest>(body);
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
                Terrain terrain = FindTerrain(request.InstanceId);
                if (terrain == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = "Terrain not found"
                    });
                }

                var data = terrain.terrainData;
                Undo.RecordObject(data, "Place Terrain Trees");

                // Add tree prototype if prefab provided
                int prototypeIndex = request.PrototypeIndex;
                if (!string.IsNullOrEmpty(request.PrefabPath))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(request.PrefabPath);
                    if (prefab == null)
                    {
                        return JsonConvert.SerializeObject(new OperationResult
                        {
                            Success = false,
                            Message = $"Prefab not found: {request.PrefabPath}"
                        });
                    }

                    var prototypes = new List<TreePrototype>(data.treePrototypes);
                    prototypes.Add(new TreePrototype { prefab = prefab });
                    data.treePrototypes = prototypes.ToArray();
                    prototypeIndex = prototypes.Count - 1;
                }

                if (prototypeIndex < 0 || prototypeIndex >= data.treePrototypes.Length)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = $"Invalid tree prototype index: {prototypeIndex}. Available: {data.treePrototypes.Length}"
                    });
                }

                // Place trees
                int count = request.Count > 0 ? request.Count : 50;
                float minScale = request.MinScale > 0 ? request.MinScale : 0.8f;
                float maxScale = request.MaxScale > 0 ? request.MaxScale : 1.2f;
                float density = request.Density > 0 ? request.Density : 1f;

                // Area (normalized 0-1)
                float areaX = Mathf.Clamp01(request.AreaCenterX > 0 ? request.AreaCenterX : 0.5f);
                float areaZ = Mathf.Clamp01(request.AreaCenterZ > 0 ? request.AreaCenterZ : 0.5f);
                float areaRadius = Mathf.Clamp01(request.AreaRadius > 0 ? request.AreaRadius : 0.5f);

                var instances = new List<TreeInstance>(data.treeInstances);
                var random = new System.Random(request.Seed > 0 ? request.Seed : Environment.TickCount);

                for (int i = 0; i < count; i++)
                {
                    // Random point within circle
                    float angle = (float)(random.NextDouble() * Math.PI * 2);
                    float dist = (float)Math.Sqrt(random.NextDouble()) * areaRadius;
                    float px = areaX + Mathf.Cos(angle) * dist;
                    float pz = areaZ + Mathf.Sin(angle) * dist;

                    if (px < 0 || px > 1 || pz < 0 || pz > 1) continue;

                    float scale = Mathf.Lerp(minScale, maxScale, (float)random.NextDouble());
                    float rotY = (float)(random.NextDouble() * 360);

                    instances.Add(new TreeInstance
                    {
                        position = new Vector3(px, 0, pz), // y is set by terrain
                        widthScale = scale,
                        heightScale = scale,
                        rotation = rotY * Mathf.Deg2Rad,
                        color = Color.white,
                        lightmapColor = Color.white,
                        prototypeIndex = prototypeIndex
                    });
                }

                data.treeInstances = instances.ToArray();
                EditorUtility.SetDirty(data);
                terrain.Flush();

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Placed {count} trees on terrain. Total trees: {instances.Count}"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error placing trees: {e.Message}"
                });
            }
        }

        public static string GetTerrainInfo(string body)
        {
            var request = JsonConvert.DeserializeObject<TerrainInfoRequest>(body);

            try
            {
                Terrain terrain = null;
                if (request?.InstanceId > 0)
                {
                    terrain = FindTerrain(request.InstanceId);
                }
                else
                {
                    terrain = Terrain.activeTerrain;
                }

                if (terrain == null)
                {
                    return JsonConvert.SerializeObject(new OperationResult
                    {
                        Success = false,
                        Message = "No terrain found in scene"
                    });
                }

                var data = terrain.terrainData;
                var layers = data.terrainLayers.Select((l, i) => new
                {
                    Index = i,
                    Texture = l.diffuseTexture != null ? l.diffuseTexture.name : "None",
                    TileSize = new { l.tileSize.x, l.tileSize.y }
                }).ToList();

                var treePrototypes = data.treePrototypes.Select((t, i) => new
                {
                    Index = i,
                    Prefab = t.prefab != null ? t.prefab.name : "None"
                }).ToList();

                return JsonConvert.SerializeObject(new
                {
                    Success = true,
                    InstanceId = terrain.gameObject.GetInstanceID(),
                    Name = terrain.gameObject.name,
                    Position = new Vector3Data(terrain.transform.position),
                    Size = new { data.size.x, data.size.y, data.size.z },
                    HeightmapResolution = data.heightmapResolution,
                    AlphamapResolution = data.alphamapResolution,
                    TreeInstanceCount = data.treeInstanceCount,
                    TreePrototypes = treePrototypes,
                    TerrainLayers = layers,
                    DetailResolution = data.detailResolution
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = false,
                    Message = $"Error getting terrain info: {e.Message}"
                });
            }
        }

        // Helper methods

        private static Terrain FindTerrain(int instanceId)
        {
            if (instanceId > 0)
            {
                var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
                if (go != null)
                    return go.GetComponent<Terrain>();
            }
            return Terrain.activeTerrain;
        }

        private static void SetHeights(TerrainData data, ModifyTerrainHeightRequest request)
        {
            int res = data.heightmapResolution;
            float normalizedHeight = Mathf.Clamp01(request.Value / data.size.y);

            if (request.AreaCenterX >= 0 && request.AreaCenterZ >= 0 && request.AreaRadius > 0)
            {
                // Set within area
                ApplyToArea(data, request, (existing, falloff) => normalizedHeight);
            }
            else
            {
                // Set entire heightmap
                var heights = new float[res, res];
                for (int y = 0; y < res; y++)
                    for (int x = 0; x < res; x++)
                        heights[y, x] = normalizedHeight;
                data.SetHeights(0, 0, heights);
            }
        }

        private static void RaiseLower(TerrainData data, ModifyTerrainHeightRequest request, float direction)
        {
            float amount = (request.Value * direction) / data.size.y;
            ApplyToArea(data, request, (existing, falloff) =>
                Mathf.Clamp01(existing + amount * falloff));
        }

        private static void SmoothHeights(TerrainData data, ModifyTerrainHeightRequest request)
        {
            int res = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, res, res);
            int iterations = Mathf.Max(1, (int)request.Value);

            float cx = Mathf.Clamp01(request.AreaCenterX >= 0 ? request.AreaCenterX : 0.5f);
            float cz = Mathf.Clamp01(request.AreaCenterZ >= 0 ? request.AreaCenterZ : 0.5f);
            float radius = request.AreaRadius > 0 ? request.AreaRadius : 1f;

            for (int iter = 0; iter < iterations; iter++)
            {
                var smoothed = (float[,])heights.Clone();
                for (int y = 1; y < res - 1; y++)
                {
                    for (int x = 1; x < res - 1; x++)
                    {
                        float nx = x / (float)res;
                        float ny = y / (float)res;
                        float dist = Mathf.Sqrt((nx - cx) * (nx - cx) + (ny - cz) * (ny - cz));

                        if (dist <= radius)
                        {
                            float avg = (
                                heights[y - 1, x - 1] + heights[y - 1, x] + heights[y - 1, x + 1] +
                                heights[y, x - 1] + heights[y, x] + heights[y, x + 1] +
                                heights[y + 1, x - 1] + heights[y + 1, x] + heights[y + 1, x + 1]
                            ) / 9f;
                            float falloff = 1f - (dist / radius);
                            smoothed[y, x] = Mathf.Lerp(heights[y, x], avg, falloff);
                        }
                    }
                }
                heights = smoothed;
            }

            data.SetHeights(0, 0, heights);
        }

        private static void FlattenHeights(TerrainData data, ModifyTerrainHeightRequest request)
        {
            float targetHeight = Mathf.Clamp01(request.Value / data.size.y);
            ApplyToArea(data, request, (existing, falloff) =>
                Mathf.Lerp(existing, targetHeight, falloff));
        }

        private static void ApplyPerlinNoise(TerrainData data, ModifyTerrainHeightRequest request)
        {
            int res = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, res, res);
            float scale = request.Value > 0 ? request.Value : 20f;
            float amplitude = request.Strength > 0 ? request.Strength : 0.1f;
            float offsetX = request.Seed * 100f;
            float offsetZ = request.Seed * 200f;

            float cx = request.AreaCenterX >= 0 ? request.AreaCenterX : 0.5f;
            float cz = request.AreaCenterZ >= 0 ? request.AreaCenterZ : 0.5f;
            float radius = request.AreaRadius > 0 ? request.AreaRadius : 1f;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float nx = x / (float)res;
                    float ny = y / (float)res;
                    float dist = Mathf.Sqrt((nx - cx) * (nx - cx) + (ny - cz) * (ny - cz));

                    if (dist <= radius)
                    {
                        float falloff = 1f - (dist / radius);
                        float noise = Mathf.PerlinNoise(
                            (x / (float)res) * scale + offsetX,
                            (y / (float)res) * scale + offsetZ
                        );
                        heights[y, x] = Mathf.Clamp01(heights[y, x] + noise * amplitude * falloff);
                    }
                }
            }

            data.SetHeights(0, 0, heights);
        }

        private static void ApplyToArea(TerrainData data, ModifyTerrainHeightRequest request,
            Func<float, float, float> operation)
        {
            int res = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, res, res);

            float cx = Mathf.Clamp01(request.AreaCenterX >= 0 ? request.AreaCenterX : 0.5f);
            float cz = Mathf.Clamp01(request.AreaCenterZ >= 0 ? request.AreaCenterZ : 0.5f);
            float radius = request.AreaRadius > 0 ? request.AreaRadius : 1f;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float nx = x / (float)res;
                    float ny = y / (float)res;
                    float dist = Mathf.Sqrt((nx - cx) * (nx - cx) + (ny - cz) * (ny - cz));

                    if (dist <= radius)
                    {
                        float falloff = 1f - (dist / radius);
                        heights[y, x] = operation(heights[y, x], falloff);
                    }
                }
            }

            data.SetHeights(0, 0, heights);
        }

        private static int NearestPowerOfTwoPlusOne(int value)
        {
            int[] valid = { 33, 65, 129, 257, 513, 1025, 2049, 4097 };
            foreach (var v in valid)
            {
                if (value <= v) return v;
            }
            return 513;
        }
    }
}
