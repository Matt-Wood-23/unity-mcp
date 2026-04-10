using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using UnityMCPBridge.Providers;
using UnityMCPBridge.Handlers;

namespace UnityMCPBridge
{
    public static class RequestRouter
    {
        public static string Route(HttpListenerRequest request)
        {
            var path = request.Url.AbsolutePath.ToLower();
            var method = request.HttpMethod.ToUpper();

            try
            {
                // GET endpoints (read operations)
                if (method == "GET")
                {
                    return path switch
                    {
                        "/ping" => JsonConvert.SerializeObject(new { status = "ok", editor = "Unity", port = UnityMCPServer.Port }),
                        "/project" => ProjectDataProvider.GetProjectInfo(),
                        "/scene" => SceneDataProvider.GetSceneHierarchy(),
                        "/scene/detailed" => SceneDataProvider.GetDetailedSceneData(),
                        "/gameobject" => SceneDataProvider.GetGameObject(request.QueryString["id"]),
                        "/components" => SceneDataProvider.GetComponents(request.QueryString["id"]),
                        "/assets" => AssetDataProvider.GetProjectAssets(request.QueryString["filter"]),
                        "/scripts" => AssetDataProvider.GetScripts(request.QueryString["filter"]),
                        "/console" => ConsoleDataProvider.GetConsoleLogs(
                            typeFilter: request.QueryString["type"],
                            search: request.QueryString["search"],
                            maxCount: int.TryParse(request.QueryString["count"], out int n) ? n : 100),
                        "/selection" => SelectionDataProvider.GetCurrentSelection(),
                        "/material" => MaterialHandler.GetMaterialInfo(request.QueryString["path"]),
                        "/profiler" => ProfilerDataProvider.GetProfilerData(),
                        "/editor/layers-and-tags" => LayerTagHandler.GetLayersAndTags(""),
                        _ => JsonConvert.SerializeObject(new { error = "Unknown endpoint", path })
                    };
                }

                // POST endpoints (write operations)
                if (method == "POST")
                {
                    string body = ReadRequestBody(request);

                    return path switch
                    {
                        "/gameobject/create" => GameObjectHandler.Create(body),
                        "/gameobject/modify" => GameObjectHandler.Modify(body),
                        "/gameobject/delete" => GameObjectHandler.Delete(body),
                        "/component/add" => GameObjectHandler.AddComponent(body),
                        "/component/remove" => GameObjectHandler.RemoveComponent(body),
                        "/property/set" => ComponentPropertyHandler.SetProperty(body),
                        "/playmode" => PlayModeHandler.SetPlayMode(body),
                        "/find" => SceneHandler.FindGameObjects(body),
                        "/prefab/instantiate" => SceneHandler.InstantiatePrefab(body),
                        "/scene/save" => SceneHandler.SaveScene(body),
                        "/scene/load" => SceneHandler.LoadScene(body),
                        "/scene/list" => SceneHandler.ListScenes(body),
                        "/undo" => SceneHandler.PerformUndo(body),
                        "/redo" => SceneHandler.PerformRedo(body),
                        "/assets/refresh" => SceneHandler.RefreshAssets(body),
                        "/material/create" => MaterialHandler.CreateMaterial(body),
                        "/material/modify" => MaterialHandler.ModifyMaterial(body),
                        "/material/assign" => MaterialHandler.AssignMaterial(body),
                        "/sprite/import" => SpriteHandler.ImportAsSprite(body),
                        "/sprite/configure" => SpriteHandler.ConfigureSpriteSettings(body),
                        "/sprite/slice" => SpriteHandler.SliceSpriteSheet(body),
                        "/sprite/renderer/create" => SpriteHandler.CreateSpriteRenderer(body),
                        // Screenshot
                        "/screenshot" => ScreenshotProvider.TakeScreenshot(body),
                        // Code execution
                        "/code/execute" => CodeExecutionHandler.ExecuteCode(body),
                        // UI
                        "/ui/create" => UIHandler.CreateUIElement(body),
                        "/ui/modify" => UIHandler.ModifyUIElement(body),
                        // Batch operations
                        "/batch/modify" => BatchHandler.BatchModify(body),
                        "/batch/delete" => BatchHandler.BatchDelete(body),
                        // Terrain
                        "/terrain/create" => TerrainHandler.CreateTerrain(body),
                        "/terrain/height" => TerrainHandler.ModifyTerrainHeight(body),
                        "/terrain/paint" => TerrainHandler.PaintTerrainTexture(body),
                        "/terrain/trees" => TerrainHandler.PlaceTerrainTrees(body),
                        "/terrain/info" => TerrainHandler.GetTerrainInfo(body),
                        // Animation
                        "/animator/info" => AnimationHandler.GetAnimatorInfo(body),
                        "/animator/parameter" => AnimationHandler.SetAnimatorParameter(body),
                        "/animator/play" => AnimationHandler.PlayAnimation(body),
                        // Lighting & Environment
                        "/light/create" => LightingHandler.CreateLight(body),
                        "/light/modify" => LightingHandler.ModifyLight(body),
                        "/light/info" => LightingHandler.GetLightInfo(body),
                        "/environment/set" => LightingHandler.SetEnvironment(body),
                        "/environment/get" => LightingHandler.GetEnvironment(body),
                        // Prefab
                        "/prefab/create" => PrefabHandler.CreatePrefab(body),
                        "/prefab/unpack" => PrefabHandler.UnpackPrefab(body),
                        "/prefab/apply" => PrefabHandler.ApplyPrefabOverrides(body),
                        "/prefab/revert" => PrefabHandler.RevertPrefabOverrides(body),
                        "/prefab/info" => PrefabHandler.GetPrefabInfo(body),
                        // Particle System
                        "/particles/create" => ParticleSystemHandler.CreateParticleSystem(body),
                        "/particles/modify" => ParticleSystemHandler.ModifyParticleSystem(body),
                        "/particles/play" => ParticleSystemHandler.PlayParticleSystem(body),
                        "/particles/info" => ParticleSystemHandler.GetParticleSystemInfo(body),
                        // Physics
                        "/physics/rigidbody" => PhysicsHandler.AddRigidbody(body),
                        "/physics/collider" => PhysicsHandler.AddCollider(body),
                        "/physics/settings/set" => PhysicsHandler.SetPhysicsSettings(body),
                        "/physics/settings/get" => PhysicsHandler.GetPhysicsSettings(body),
                        // Audio
                        "/audio/source/add" => AudioHandler.AddAudioSource(body),
                        "/audio/source/modify" => AudioHandler.ModifyAudioSource(body),
                        "/audio/source/info" => AudioHandler.GetAudioSourceInfo(body),
                        "/audio/source/play" => AudioHandler.PlayAudio(body),
                        // Camera
                        "/camera/info" => CameraHandler.GetCameraInfo(body),
                        "/camera/modify" => CameraHandler.ModifyCamera(body),
                        // TextMeshPro
                        "/tmp/create" => TextMeshProHandler.CreateTMPText(body),
                        "/tmp/modify" => TextMeshProHandler.ModifyTMPText(body),
                        // Layer/Tag
                        "/editor/layer/add" => LayerTagHandler.AddLayer(body),
                        "/editor/tag/add" => LayerTagHandler.AddTag(body),
                        // Console
                        "/console/clear" => ConsoleDataProvider.ClearLogs(),
                        // NavMesh
                        "/navmesh/bake" => NavMeshHandler.BakeNavMesh(body),
                        "/navmesh/clear" => NavMeshHandler.ClearNavMesh(body),
                        "/navmesh/agent/add" => NavMeshHandler.AddNavMeshAgent(body),
                        "/navmesh/obstacle/add" => NavMeshHandler.AddNavMeshObstacle(body),
                        // 2D Physics
                        "/physics2d/rigidbody" => Physics2DHandler.AddRigidbody2D(body),
                        "/physics2d/collider" => Physics2DHandler.AddCollider2D(body),
                        // Tilemap
                        "/tilemap/create" => TilemapHandler.CreateTilemap(body),
                        "/tilemap/tile/set" => TilemapHandler.SetTile(body),
                        "/tilemap/tile/fill" => TilemapHandler.FillTiles(body),
                        // Animation Clips
                        "/animation/clip/create" => AnimationClipHandler.CreateAnimationClip(body),
                        "/animation/clip/keyframes" => AnimationClipHandler.AddKeyframes(body),
                        "/animation/clip/info" => AnimationClipHandler.GetClipInfo(body),
                        // Build
                        "/build/settings" => BuildHandler.GetBuildSettings(body),
                        "/build/scenes" => BuildHandler.SetBuildScenes(body),
                        "/build/target" => BuildHandler.SwitchBuildTarget(body),
                        "/build/player" => BuildHandler.BuildPlayer(body),
                        // Post-processing
                        "/postprocessing/create" => PostProcessingHandler.CreateVolume(body),
                        "/postprocessing/modify" => PostProcessingHandler.ModifyVolume(body),
                        _ => JsonConvert.SerializeObject(new { error = "Unknown endpoint", path })
                    };
                }

                return JsonConvert.SerializeObject(new { error = "Method not allowed", method });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message, type = e.GetType().Name });
            }
        }

        private static string ReadRequestBody(HttpListenerRequest request)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            return reader.ReadToEnd();
        }
    }
}
