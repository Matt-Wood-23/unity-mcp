using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Handlers
{
    public static class TilemapHandler
    {
        public static string CreateTilemap(string body)
        {
            var request = JsonConvert.DeserializeObject<CreateTilemapRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                string name = request.Name ?? "Tilemap";

                // Create or find Grid parent
                GameObject gridGo;
                if (request.ParentId.HasValue)
                {
                    gridGo = EditorUtility.InstanceIDToObject(request.ParentId.Value) as GameObject;
                    if (gridGo == null)
                        return Error($"Parent GameObject not found: {request.ParentId}");
                }
                else
                {
                    gridGo = new GameObject(name + " Grid");
                    Undo.RegisterCreatedObjectUndo(gridGo, $"Create Grid {name}");
                    var grid = gridGo.AddComponent<Grid>();

                    if (request.CellSize.HasValue)
                        grid.cellSize = new Vector3(request.CellSize.Value, request.CellSize.Value, 0f);

                    if (!string.IsNullOrEmpty(request.Orientation))
                    {
                        switch (request.Orientation.ToUpper())
                        {
                            case "XZ":
                                grid.cellLayout = GridLayout.CellLayout.Rectangle;
                                grid.cellSwizzle = GridLayout.CellSwizzle.XZY;
                                break;
                            case "HEXFLAT":
                                grid.cellLayout = GridLayout.CellLayout.Hexagon;
                                grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
                                break;
                            case "HEXPOINT":
                                grid.cellLayout = GridLayout.CellLayout.Hexagon;
                                break;
                            default:
                                grid.cellLayout = GridLayout.CellLayout.Rectangle;
                                break;
                        }
                    }

                    if (request.Position != null)
                        gridGo.transform.position = request.Position.ToVector3();
                }

                // Create tilemap child
                var tilemapGo = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(tilemapGo, $"Create Tilemap {name}");
                Undo.SetTransformParent(tilemapGo.transform, gridGo.transform, $"Parent Tilemap {name}");
                tilemapGo.transform.localPosition = Vector3.zero;

                tilemapGo.AddComponent<Tilemap>();
                tilemapGo.AddComponent<TilemapRenderer>();

                return JsonConvert.SerializeObject(new TilemapCreateResult
                {
                    Success = true,
                    Message = $"Tilemap '{name}' created",
                    InstanceId = tilemapGo.GetInstanceID(),
                    GridInstanceId = gridGo.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                return Error($"Error creating tilemap: {e.Message}");
            }
        }

        public static string SetTile(string body)
        {
            var request = JsonConvert.DeserializeObject<SetTileRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                    return Error($"GameObject not found: {request.InstanceId}");

                var tilemap = go.GetComponent<Tilemap>();
                if (tilemap == null)
                    return Error($"No Tilemap component on '{go.name}'");

                TileBase tile = null;
                if (!string.IsNullOrEmpty(request.TilePath))
                {
                    tile = AssetDatabase.LoadAssetAtPath<TileBase>(request.TilePath);
                    if (tile == null)
                        return Error($"Tile asset not found at: {request.TilePath}");
                }

                var cellPos = new Vector3Int(request.X, request.Y, request.Z);
                Undo.RecordObject(tilemap, "Set Tile");
                tilemap.SetTile(cellPos, tile); // null clears the tile

                EditorUtility.SetDirty(tilemap);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = tile != null
                        ? $"Tile set at ({request.X}, {request.Y}, {request.Z})"
                        : $"Tile cleared at ({request.X}, {request.Y}, {request.Z})"
                });
            }
            catch (Exception e)
            {
                return Error($"Error setting tile: {e.Message}");
            }
        }

        public static string FillTiles(string body)
        {
            var request = JsonConvert.DeserializeObject<FillTilesRequest>(body);
            if (request == null)
                return Error("Invalid request body");

            try
            {
                var go = EditorUtility.InstanceIDToObject(request.InstanceId) as GameObject;
                if (go == null)
                    return Error($"GameObject not found: {request.InstanceId}");

                var tilemap = go.GetComponent<Tilemap>();
                if (tilemap == null)
                    return Error($"No Tilemap component on '{go.name}'");

                TileBase tile = null;
                if (!string.IsNullOrEmpty(request.TilePath))
                {
                    tile = AssetDatabase.LoadAssetAtPath<TileBase>(request.TilePath);
                    if (tile == null)
                        return Error($"Tile asset not found at: {request.TilePath}");
                }

                Undo.RecordObject(tilemap, "Fill Tiles");
                int count = 0;
                for (int x = request.XMin; x <= request.XMax; x++)
                {
                    for (int y = request.YMin; y <= request.YMax; y++)
                    {
                        tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                        count++;
                    }
                }

                EditorUtility.SetDirty(tilemap);

                return JsonConvert.SerializeObject(new OperationResult
                {
                    Success = true,
                    Message = $"Filled {count} tiles in region ({request.XMin},{request.YMin}) to ({request.XMax},{request.YMax})"
                });
            }
            catch (Exception e)
            {
                return Error($"Error filling tiles: {e.Message}");
            }
        }

        private static string Error(string msg) =>
            JsonConvert.SerializeObject(new OperationResult { Success = false, Message = msg });
    }
}
