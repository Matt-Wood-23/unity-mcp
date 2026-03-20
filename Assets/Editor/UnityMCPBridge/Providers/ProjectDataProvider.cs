using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Providers
{
    public static class ProjectDataProvider
    {
        public static string GetProjectInfo()
        {
            var data = new ProjectInfo
            {
                ProductName = Application.productName,
                CompanyName = Application.companyName,
                Version = Application.version,
                UnityVersion = Application.unityVersion,
                Platform = EditorUserBuildSettings.activeBuildTarget.ToString(),
                ProjectPath = Application.dataPath,
                IsPlaying = EditorApplication.isPlaying,
                IsPaused = EditorApplication.isPaused
            };

            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
    }
}
