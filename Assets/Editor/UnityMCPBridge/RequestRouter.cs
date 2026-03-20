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
                        "/console" => ConsoleDataProvider.GetConsoleLogs(),
                        "/selection" => SelectionDataProvider.GetCurrentSelection(),
                        "/material" => MaterialHandler.GetMaterialInfo(request.QueryString["path"]),
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
