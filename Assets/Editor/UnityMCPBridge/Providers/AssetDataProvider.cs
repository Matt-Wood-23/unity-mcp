using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Providers
{
    public static class AssetDataProvider
    {
        private const int MaxAssets = 200;
        private const int MaxScripts = 100;

        public static string GetProjectAssets(string filter)
        {
            var searchFilter = string.IsNullOrEmpty(filter) ? "t:Object" : filter;
            var guids = AssetDatabase.FindAssets(searchFilter);

            var assets = guids.Take(MaxAssets).Select(guid =>
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                return new AssetInfo
                {
                    Guid = guid,
                    Path = path,
                    Name = asset?.name ?? Path.GetFileName(path),
                    Type = asset?.GetType().Name ?? "Unknown"
                };
            }).ToList();

            var data = new AssetListData
            {
                Filter = searchFilter,
                Count = assets.Count,
                TotalFound = guids.Length,
                Assets = assets
            };

            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public static string GetScripts(string filter)
        {
            var guids = AssetDatabase.FindAssets("t:MonoScript");
            var scripts = new List<ScriptInfo>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                // Skip Unity/Package scripts unless specifically requested
                if (path.StartsWith("Packages/") && string.IsNullOrEmpty(filter))
                    continue;

                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null) continue;

                if (!string.IsNullOrEmpty(filter) &&
                    !script.name.ToLower().Contains(filter.ToLower()))
                    continue;

                scripts.Add(new ScriptInfo
                {
                    Name = script.name,
                    Path = path,
                    ClassName = script.GetClass()?.FullName ?? script.name
                });
            }

            var data = new ScriptListData
            {
                Count = scripts.Count,
                Scripts = scripts.Take(MaxScripts).ToList()
            };

            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
    }
}
