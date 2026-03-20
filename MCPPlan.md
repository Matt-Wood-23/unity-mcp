Let me put together a comprehensive implementation plan.

## Unity Editor MCP Server - Implementation Plan

---

### Architecture Overview

```
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│   Claude Code   │ ←MCP──→ │   MCP Server    │ ←HTTP─→ │  Unity Editor   │
│                 │         │   (Node.js)     │         │  (C# EditorWin) │
└─────────────────┘         └─────────────────┘         └─────────────────┘
                                    │
                                    ↓
                            JSON responses with
                            scene data, hierarchy,
                            components, logs, etc.
```

---

### Part 1: Unity Side (C# Editor Package)

**Location:** Create as a Unity Package or just drop in `Assets/Editor/UnityMCPBridge/`

#### 1.1 Core HTTP Server

```csharp
// UnityMCPServer.cs
using UnityEngine;
using UnityEditor;
using System;
using System.Net;
using System.Threading;
using System.Text;

[InitializeOnLoad]
public static class UnityMCPServer
{
    private static HttpListener listener;
    private static Thread serverThread;
    private static bool isRunning;
    private const int PORT = 6850; // Arbitrary, configurable

    static UnityMCPServer()
    {
        StartServer();
        EditorApplication.quitting += StopServer;
    }

    private static void StartServer()
    {
        if (isRunning) return;
        
        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{PORT}/");
        listener.Start();
        isRunning = true;

        serverThread = new Thread(HandleRequests) { IsBackground = true };
        serverThread.Start();
        
        Debug.Log($"[Unity MCP Bridge] Server started on port {PORT}");
    }

    private static void HandleRequests()
    {
        while (isRunning)
        {
            try
            {
                var context = listener.GetContext();
                // Must dispatch to main thread for Unity API access
                EditorApplication.delayCall += () => ProcessRequest(context);
            }
            catch (Exception e)
            {
                if (isRunning) Debug.LogError($"[Unity MCP Bridge] {e.Message}");
            }
        }
    }

    private static void ProcessRequest(HttpListenerContext context)
    {
        var response = context.Response;
        var path = context.Request.Url.AbsolutePath;
        
        string json = RouteRequest(path, context.Request);
        
        byte[] buffer = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json";
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.Close();
    }

    private static string RouteRequest(string path, HttpListenerRequest request)
    {
        return path switch
        {
            "/ping" => "{\"status\":\"ok\",\"editor\":\"Unity\"}",
            "/scene" => SceneDataProvider.GetSceneHierarchy(),
            "/scene/detailed" => SceneDataProvider.GetDetailedSceneData(),
            "/gameobject" => SceneDataProvider.GetGameObject(request.QueryString["id"]),
            "/components" => SceneDataProvider.GetComponents(request.QueryString["id"]),
            "/assets" => AssetDataProvider.GetProjectAssets(request.QueryString["filter"]),
            "/console" => ConsoleDataProvider.GetConsoleLogs(),
            "/project" => ProjectDataProvider.GetProjectInfo(),
            "/selection" => SelectionDataProvider.GetCurrentSelection(),
            "/scripts" => ScriptDataProvider.GetScripts(request.QueryString["filter"]),
            _ => "{\"error\":\"Unknown endpoint\"}"
        };
    }

    private static void StopServer()
    {
        isRunning = false;
        listener?.Stop();
    }
}
```

#### 1.2 Data Providers

```csharp
// SceneDataProvider.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public static class SceneDataProvider
{
    public static string GetSceneHierarchy()
    {
        var scene = SceneManager.GetActiveScene();
        var rootObjects = scene.GetRootGameObjects();
        
        var hierarchy = new SceneHierarchyData
        {
            sceneName = scene.name,
            scenePath = scene.path,
            isDirty = scene.isDirty,
            rootObjects = rootObjects.Select(go => BuildHierarchyNode(go, 0, 3)).ToList()
        };
        
        return JsonUtility.ToJson(hierarchy, true);
    }

    private static GameObjectNode BuildHierarchyNode(GameObject go, int depth, int maxDepth)
    {
        var node = new GameObjectNode
        {
            instanceId = go.GetInstanceID(),
            name = go.name,
            tag = go.tag,
            layer = LayerMask.LayerToName(go.layer),
            isActive = go.activeSelf,
            isStatic = go.isStatic,
            components = go.GetComponents<Component>()
                          .Where(c => c != null)
                          .Select(c => c.GetType().Name).ToList(),
            childCount = go.transform.childCount
        };

        if (depth < maxDepth && go.transform.childCount > 0)
        {
            node.children = new List<GameObjectNode>();
            foreach (Transform child in go.transform)
            {
                node.children.Add(BuildHierarchyNode(child.gameObject, depth + 1, maxDepth));
            }
        }

        return node;
    }

    public static string GetGameObject(string instanceIdStr)
    {
        if (!int.TryParse(instanceIdStr, out int instanceId))
            return "{\"error\":\"Invalid instance ID\"}";

        var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
        if (go == null)
            return "{\"error\":\"GameObject not found\"}";

        var data = new GameObjectDetailData
        {
            instanceId = instanceId,
            name = go.name,
            tag = go.tag,
            layer = LayerMask.LayerToName(go.layer),
            isActive = go.activeSelf,
            isStatic = go.isStatic,
            transform = new TransformData
            {
                position = go.transform.position,
                rotation = go.transform.eulerAngles,
                scale = go.transform.localScale,
                localPosition = go.transform.localPosition,
                localRotation = go.transform.localEulerAngles
            },
            components = GetComponentsDetailed(go)
        };

        return JsonUtility.ToJson(data, true);
    }

    public static string GetComponents(string instanceIdStr)
    {
        if (!int.TryParse(instanceIdStr, out int instanceId))
            return "{\"error\":\"Invalid instance ID\"}";

        var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
        if (go == null)
            return "{\"error\":\"GameObject not found\"}";

        var components = new ComponentListData
        {
            gameObjectName = go.name,
            components = GetComponentsDetailed(go)
        };

        return JsonUtility.ToJson(components, true);
    }

    private static List<ComponentData> GetComponentsDetailed(GameObject go)
    {
        var result = new List<ComponentData>();
        
        foreach (var component in go.GetComponents<Component>())
        {
            if (component == null) continue;
            
            var compData = new ComponentData
            {
                type = component.GetType().Name,
                fullType = component.GetType().FullName,
                enabled = !(component is Behaviour behaviour) || behaviour.enabled,
                properties = new List<PropertyData>()
            };

            // Use SerializedObject to get all serialized properties
            var so = new SerializedObject(component);
            var prop = so.GetIterator();
            
            if (prop.NextVisible(true))
            {
                do
                {
                    compData.properties.Add(new PropertyData
                    {
                        name = prop.name,
                        type = prop.propertyType.ToString(),
                        value = GetPropertyValue(prop)
                    });
                } while (prop.NextVisible(false));
            }

            result.Add(compData);
        }

        return result;
    }

    private static string GetPropertyValue(SerializedProperty prop)
    {
        return prop.propertyType switch
        {
            SerializedPropertyType.Integer => prop.intValue.ToString(),
            SerializedPropertyType.Boolean => prop.boolValue.ToString(),
            SerializedPropertyType.Float => prop.floatValue.ToString(),
            SerializedPropertyType.String => prop.stringValue,
            SerializedPropertyType.Color => prop.colorValue.ToString(),
            SerializedPropertyType.Vector2 => prop.vector2Value.ToString(),
            SerializedPropertyType.Vector3 => prop.vector3Value.ToString(),
            SerializedPropertyType.Vector4 => prop.vector4Value.ToString(),
            SerializedPropertyType.Rect => prop.rectValue.ToString(),
            SerializedPropertyType.Enum => prop.enumDisplayNames[prop.enumValueIndex],
            SerializedPropertyType.ObjectReference => prop.objectReferenceValue?.name ?? "None",
            _ => $"({prop.propertyType})"
        };
    }
}

// Data classes for JSON serialization
[System.Serializable]
public class SceneHierarchyData
{
    public string sceneName;
    public string scenePath;
    public bool isDirty;
    public List<GameObjectNode> rootObjects;
}

[System.Serializable]
public class GameObjectNode
{
    public int instanceId;
    public string name;
    public string tag;
    public string layer;
    public bool isActive;
    public bool isStatic;
    public List<string> components;
    public int childCount;
    public List<GameObjectNode> children;
}

[System.Serializable]
public class GameObjectDetailData
{
    public int instanceId;
    public string name;
    public string tag;
    public string layer;
    public bool isActive;
    public bool isStatic;
    public TransformData transform;
    public List<ComponentData> components;
}

[System.Serializable]
public class TransformData
{
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public Vector3 localPosition;
    public Vector3 localRotation;
}

[System.Serializable]
public class ComponentData
{
    public string type;
    public string fullType;
    public bool enabled;
    public List<PropertyData> properties;
}

[System.Serializable]
public class PropertyData
{
    public string name;
    public string type;
    public string value;
}

[System.Serializable]
public class ComponentListData
{
    public string gameObjectName;
    public List<ComponentData> components;
}
```

```csharp
// ConsoleDataProvider.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
public static class ConsoleDataProvider
{
    private static List<LogEntry> logs = new List<LogEntry>();
    private const int MAX_LOGS = 100;

    static ConsoleDataProvider()
    {
        Application.logMessageReceived += OnLogMessage;
    }

    private static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        logs.Add(new LogEntry
        {
            message = condition,
            stackTrace = stackTrace,
            type = type.ToString(),
            timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff")
        });

        if (logs.Count > MAX_LOGS)
            logs.RemoveAt(0);
    }

    public static string GetConsoleLogs()
    {
        var data = new ConsoleData { logs = logs };
        return JsonUtility.ToJson(data, true);
    }

    public static void ClearLogs() => logs.Clear();
}

[System.Serializable]
public class ConsoleData
{
    public List<LogEntry> logs;
}

[System.Serializable]
public class LogEntry
{
    public string message;
    public string stackTrace;
    public string type;
    public string timestamp;
}
```

```csharp
// AssetDataProvider.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public static class AssetDataProvider
{
    public static string GetProjectAssets(string filter)
    {
        var searchFilter = string.IsNullOrEmpty(filter) ? "t:Object" : filter;
        var guids = AssetDatabase.FindAssets(searchFilter);
        
        var assets = guids.Take(200).Select(guid =>
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            return new AssetInfo
            {
                guid = guid,
                path = path,
                name = asset?.name ?? System.IO.Path.GetFileName(path),
                type = asset?.GetType().Name ?? "Unknown"
            };
        }).ToList();

        var data = new AssetListData
        {
            filter = searchFilter,
            count = assets.Count,
            totalFound = guids.Length,
            assets = assets
        };

        return JsonUtility.ToJson(data, true);
    }
}

[System.Serializable]
public class AssetListData
{
    public string filter;
    public int count;
    public int totalFound;
    public List<AssetInfo> assets;
}

[System.Serializable]
public class AssetInfo
{
    public string guid;
    public string path;
    public string name;
    public string type;
}
```

```csharp
// ProjectDataProvider.cs
using UnityEngine;
using UnityEditor;

public static class ProjectDataProvider
{
    public static string GetProjectInfo()
    {
        var data = new ProjectInfo
        {
            productName = Application.productName,
            companyName = Application.companyName,
            version = Application.version,
            unityVersion = Application.unityVersion,
            platform = EditorUserBuildSettings.activeBuildTarget.ToString(),
            projectPath = Application.dataPath,
            isPlaying = EditorApplication.isPlaying,
            isPaused = EditorApplication.isPaused
        };

        return JsonUtility.ToJson(data, true);
    }
}

[System.Serializable]
public class ProjectInfo
{
    public string productName;
    public string companyName;
    public string version;
    public string unityVersion;
    public string platform;
    public string projectPath;
    public bool isPlaying;
    public bool isPaused;
}
```

```csharp
// SelectionDataProvider.cs
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public static class SelectionDataProvider
{
    public static string GetCurrentSelection()
    {
        var data = new SelectionData
        {
            activeGameObject = Selection.activeGameObject != null
                ? new SelectedObject
                {
                    instanceId = Selection.activeGameObject.GetInstanceID(),
                    name = Selection.activeGameObject.name,
                    type = "GameObject"
                }
                : null,
            selectedObjects = Selection.gameObjects.Select(go => new SelectedObject
            {
                instanceId = go.GetInstanceID(),
                name = go.name,
                type = "GameObject"
            }).ToList(),
            selectedAssetPaths = Selection.assetGUIDs
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToList()
        };

        return JsonUtility.ToJson(data, true);
    }
}

[System.Serializable]
public class SelectionData
{
    public SelectedObject activeGameObject;
    public List<SelectedObject> selectedObjects;
    public List<string> selectedAssetPaths;
}

[System.Serializable]
public class SelectedObject
{
    public int instanceId;
    public string name;
    public string type;
}
```

```csharp
// ScriptDataProvider.cs
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public static class ScriptDataProvider
{
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
                name = script.name,
                path = path,
                className = script.GetClass()?.FullName ?? script.name
            });
        }

        var data = new ScriptListData
        {
            count = scripts.Count,
            scripts = scripts.Take(100).ToList()
        };

        return JsonUtility.ToJson(data, true);
    }

    public static string GetScriptContent(string path)
    {
        if (!File.Exists(path))
            return "{\"error\":\"Script not found\"}";

        return JsonUtility.ToJson(new ScriptContent
        {
            path = path,
            content = File.ReadAllText(path)
        }, true);
    }
}

[System.Serializable]
public class ScriptListData
{
    public int count;
    public List<ScriptInfo> scripts;
}

[System.Serializable]
public class ScriptInfo
{
    public string name;
    public string path;
    public string className;
}

[System.Serializable]
public class ScriptContent
{
    public string path;
    public string content;
}
```

---

### Part 2: MCP Server (Node.js)

**Location:** Separate repo/folder, e.g., `unity-mcp-server/`

#### 2.1 Project Structure

```
unity-mcp-server/
├── package.json
├── tsconfig.json
├── src/
│   ├── index.ts
│   ├── unity-client.ts
│   └── tools/
│       ├── scene-tools.ts
│       ├── asset-tools.ts
│       ├── console-tools.ts
│       └── project-tools.ts
```

#### 2.2 package.json

```json
{
  "name": "unity-mcp-server",
  "version": "1.0.0",
  "description": "MCP server for Unity Editor integration",
  "type": "module",
  "main": "dist/index.js",
  "bin": {
    "unity-mcp-server": "./dist/index.js"
  },
  "scripts": {
    "build": "tsc",
    "start": "node dist/index.js",
    "dev": "tsx src/index.ts"
  },
  "dependencies": {
    "@modelcontextprotocol/sdk": "^0.5.0",
    "node-fetch": "^3.3.2"
  },
  "devDependencies": {
    "@types/node": "^20.0.0",
    "typescript": "^5.0.0",
    "tsx": "^4.0.0"
  }
}
```

#### 2.3 Unity Client

```typescript
// src/unity-client.ts
const UNITY_PORT = 6850;
const UNITY_BASE_URL = `http://localhost:${UNITY_PORT}`;

export class UnityClient {
  private baseUrl: string;

  constructor(port: number = UNITY_PORT) {
    this.baseUrl = `http://localhost:${port}`;
  }

  async ping(): Promise<boolean> {
    try {
      const response = await fetch(`${this.baseUrl}/ping`);
      return response.ok;
    } catch {
      return false;
    }
  }

  async getSceneHierarchy(): Promise<any> {
    return this.get('/scene');
  }

  async getDetailedScene(): Promise<any> {
    return this.get('/scene/detailed');
  }

  async getGameObject(instanceId: number): Promise<any> {
    return this.get(`/gameobject?id=${instanceId}`);
  }

  async getComponents(instanceId: number): Promise<any> {
    return this.get(`/components?id=${instanceId}`);
  }

  async getAssets(filter?: string): Promise<any> {
    const query = filter ? `?filter=${encodeURIComponent(filter)}` : '';
    return this.get(`/assets${query}`);
  }

  async getConsoleLogs(): Promise<any> {
    return this.get('/console');
  }

  async getProjectInfo(): Promise<any> {
    return this.get('/project');
  }

  async getCurrentSelection(): Promise<any> {
    return this.get('/selection');
  }

  async getScripts(filter?: string): Promise<any> {
    const query = filter ? `?filter=${encodeURIComponent(filter)}` : '';
    return this.get(`/scripts${query}`);
  }

  private async get(endpoint: string): Promise<any> {
    const response = await fetch(`${this.baseUrl}${endpoint}`);
    if (!response.ok) {
      throw new Error(`Unity request failed: ${response.statusText}`);
    }
    return response.json();
  }
}

export const unityClient = new UnityClient();
```

#### 2.4 Main MCP Server

```typescript
// src/index.ts
#!/usr/bin/env node

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  ToolSchema,
} from "@modelcontextprotocol/sdk/types.js";
import { unityClient } from "./unity-client.js";

const server = new Server(
  {
    name: "unity-mcp-server",
    version: "1.0.0",
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

// Define available tools
const tools: ToolSchema[] = [
  {
    name: "unity_ping",
    description: "Check if Unity Editor is running and the bridge is active",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_get_scene",
    description: "Get the current scene hierarchy with all GameObjects, their components, and children. Returns scene name, root objects, and their hierarchical structure.",
    inputSchema: {
      type: "object",
      properties: {
        detailed: {
          type: "boolean",
          description: "If true, returns more detailed component data",
        },
      },
    },
  },
  {
    name: "unity_get_gameobject",
    description: "Get detailed information about a specific GameObject by its instance ID, including all components and their serialized properties",
    inputSchema: {
      type: "object",
      properties: {
        instanceId: {
          type: "number",
          description: "The Unity instance ID of the GameObject",
        },
      },
      required: ["instanceId"],
    },
  },
  {
    name: "unity_get_components",
    description: "Get all components attached to a GameObject with their full property details",
    inputSchema: {
      type: "object",
      properties: {
        instanceId: {
          type: "number",
          description: "The Unity instance ID of the GameObject",
        },
      },
      required: ["instanceId"],
    },
  },
  {
    name: "unity_get_assets",
    description: "Search and list project assets. Use Unity search filter syntax (e.g., 't:Prefab', 't:Material', 't:Script name')",
    inputSchema: {
      type: "object",
      properties: {
        filter: {
          type: "string",
          description: "Unity asset search filter (e.g., 't:Prefab', 't:Texture2D', 'Player t:Script')",
        },
      },
    },
  },
  {
    name: "unity_get_console",
    description: "Get recent Unity console logs including errors, warnings, and info messages",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_get_project",
    description: "Get Unity project information including name, version, platform, and play mode status",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_get_selection",
    description: "Get currently selected GameObjects and assets in the Unity Editor",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "unity_get_scripts",
    description: "List MonoBehaviour scripts in the project, optionally filtered by name",
    inputSchema: {
      type: "object",
      properties: {
        filter: {
          type: "string",
          description: "Filter scripts by name (case-insensitive)",
        },
      },
    },
  },
];

// Handle tool listing
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return { tools };
});

// Handle tool execution
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    let result: any;

    switch (name) {
      case "unity_ping":
        const isConnected = await unityClient.ping();
        result = {
          connected: isConnected,
          message: isConnected
            ? "Unity Editor is connected and responding"
            : "Cannot connect to Unity Editor. Make sure Unity is open with the MCP Bridge package installed.",
        };
        break;

      case "unity_get_scene":
        result = args?.detailed
          ? await unityClient.getDetailedScene()
          : await unityClient.getSceneHierarchy();
        break;

      case "unity_get_gameobject":
        result = await unityClient.getGameObject(args!.instanceId as number);
        break;

      case "unity_get_components":
        result = await unityClient.getComponents(args!.instanceId as number);
        break;

      case "unity_get_assets":
        result = await unityClient.getAssets(args?.filter as string);
        break;

      case "unity_get_console":
        result = await unityClient.getConsoleLogs();
        break;

      case "unity_get_project":
        result = await unityClient.getProjectInfo();
        break;

      case "unity_get_selection":
        result = await unityClient.getCurrentSelection();
        break;

      case "unity_get_scripts":
        result = await unityClient.getScripts(args?.filter as string);
        break;

      default:
        throw new Error(`Unknown tool: ${name}`);
    }

    return {
      content: [
        {
          type: "text",
          text: JSON.stringify(result, null, 2),
        },
      ],
    };
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    return {
      content: [
        {
          type: "text",
          text: `Error: ${errorMessage}. Make sure Unity Editor is running with the MCP Bridge package installed.`,
        },
      ],
      isError: true,
    };
  }
});

// Start the server
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("Unity MCP Server running on stdio");
}

main().catch(console.error);
```

#### 2.5 tsconfig.json

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "NodeNext",
    "moduleResolution": "NodeNext",
    "outDir": "./dist",
    "rootDir": "./src",
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "declaration": true
  },
  "include": ["src/**/*"],
  "exclude": ["node_modules", "dist"]
}
```

---

### Part 3: Claude Code Configuration

Add to your Claude Code MCP settings (usually `~/.config/claude-code/mcp.json` or similar):

```json
{
  "mcpServers": {
    "unity": {
      "command": "node",
      "args": ["/path/to/unity-mcp-server/dist/index.js"]
    }
  }
}
```

Or if you want to run from source during development:

```json
{
  "mcpServers": {
    "unity": {
      "command": "npx",
      "args": ["tsx", "/path/to/unity-mcp-server/src/index.ts"]
    }
  }
}
```

---

### Part 4: Potential Extensions

Once the basic version works, you could add:

| Feature | Unity Side | MCP Tool |
|---------|------------|----------|
| **Modify GameObjects** | POST endpoints to create/delete/modify | `unity_create_gameobject`, `unity_set_property` |
| **Play Mode Control** | `EditorApplication.isPlaying = true` | `unity_play`, `unity_pause`, `unity_stop` |
| **Scene Management** | `EditorSceneManager` API | `unity_load_scene`, `unity_save_scene` |
| **Prefab Operations** | `PrefabUtility` API | `unity_instantiate_prefab` |
| **Screenshot/Preview** | `ScreenCapture` + base64 | `unity_capture_game_view` (though Claude Code is text-focused) |
| **Build** | `BuildPipeline` | `unity_build` |
| **Run Tests** | `TestRunnerApi` | `unity_run_tests` |

---

### Getting Started Steps

1. **Unity Side:**
   - Create folder `Assets/Editor/UnityMCPBridge/`
   - Add all the C# files above
   - Unity will auto-compile; check console for "Server started on port 6850"
   - Test: `curl http://localhost:6850/ping`

2. **MCP Server Side:**
   - Create the project folder and files
   - `npm install`
   - `npm run build`
   - Test standalone: `npm start` (will wait for MCP input)

3. **Wire it up:**
   - Add to Claude Code's MCP config
   - Restart Claude Code
   - Try asking Claude Code to "check if Unity is connected" or "show me the scene hierarchy"

---

Want me to adjust anything or expand on any particular section?