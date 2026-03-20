using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Providers
{
    public static class SelectionDataProvider
    {
        public static string GetCurrentSelection()
        {
            var data = new SelectionData
            {
                ActiveGameObject = Selection.activeGameObject != null
                    ? new SelectedObject
                    {
                        InstanceId = Selection.activeGameObject.GetInstanceID(),
                        Name = Selection.activeGameObject.name,
                        Type = "GameObject"
                    }
                    : null,
                SelectedObjects = Selection.gameObjects.Select(go => new SelectedObject
                {
                    InstanceId = go.GetInstanceID(),
                    Name = go.name,
                    Type = "GameObject"
                }).ToList(),
                SelectedAssetPaths = Selection.assetGUIDs
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToList()
            };

            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
    }
}
