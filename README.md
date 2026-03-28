# Unity MCP Server

An MCP (Model Context Protocol) server that enables AI assistants like Claude to view and interact with the Unity Editor in real-time. Create GameObjects, modify components, control play mode, manage materials, and more - all through natural language.

![UnityMCP Demo](UnityMCP.gif)

## Features

- **29 MCP Tools** for comprehensive Unity Editor control
- **Real-time scene inspection** - View hierarchy, components, and properties
- **Full CRUD operations** - Create, modify, delete GameObjects and components
- **Material system** - Create, modify, and assign materials
- **Scene management** - Load, save, and list scenes
- **Play mode control** - Start, stop, pause, and step through gameplay
- **Undo/Redo support** - All operations are undoable
- **Thread-safe** - Properly handles Unity's main thread requirements

## Architecture

```
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│  Claude Code /  │         │   MCP Server    │         │  Unity Editor   │
│  AI Assistant   │ ←─MCP─→ │   (Node.js)     │ ←─HTTP─→│  (C# Bridge)    │
└─────────────────┘         └─────────────────┘         └─────────────────┘
```

## Quick Start

### 1. Unity Setup

Copy the `Assets/Editor/UnityMCPBridge/` folder into your Unity project's `Assets/Editor/` directory.

**Requirements:**
- Unity 6+ (uses modern APIs like `FindObjectsByType`)
- Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`) - usually included by default

The server auto-starts when Unity opens. Check the console for:
```
[UnityMCP] Server started on port 6850
```

**Verify it's working:**
```bash
curl http://localhost:6850/ping
# Returns: {"status":"ok","editor":"Unity","port":6850}
```

### 2. MCP Server Setup

```bash
cd unity-mcp-server
npm install
npm run build
```

### 3. Configure Claude Code

Add to your MCP settings file (`~/.claude.json` or via settings UI):

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

Restart Claude Code after adding the configuration.

## Available Tools (29 Total)

### Scene & Hierarchy

| Tool | Description |
|------|-------------|
| `unity_ping` | Check if Unity Editor is connected |
| `unity_get_project` | Get project info, version, and play mode status |
| `unity_get_scene` | Get scene hierarchy with GameObjects and components |
| `unity_get_gameobject` | Get detailed info about a specific GameObject |
| `unity_get_components` | Get all components on a GameObject with properties |
| `unity_get_selection` | Get currently selected objects in the editor |
| `unity_find_gameobjects` | Search for GameObjects by name, tag, layer, or component |

### GameObject Operations

| Tool | Description |
|------|-------------|
| `unity_create_gameobject` | Create empty or primitive GameObjects (Cube, Sphere, etc.) |
| `unity_modify_gameobject` | Change name, transform, parent, active state, etc. |
| `unity_delete_gameobject` | Delete a GameObject from the scene |

### Component Operations

| Tool | Description |
|------|-------------|
| `unity_add_component` | Add any component (Rigidbody, Collider, custom scripts) |
| `unity_remove_component` | Remove a component from a GameObject |
| `unity_set_property` | Set any serialized property on a component |

### Materials

| Tool | Description |
|------|-------------|
| `unity_get_material` | Get material info including shader and all properties |
| `unity_create_material` | Create a new material with specified shader and color |
| `unity_modify_material` | Change material color or shader properties |
| `unity_assign_material` | Assign a material to a GameObject's renderer |

### Scene Management

| Tool | Description |
|------|-------------|
| `unity_list_scenes` | List all scenes in the project |
| `unity_load_scene` | Load a scene (single or additive) |
| `unity_save_scene` | Save the current scene |

### Assets

| Tool | Description |
|------|-------------|
| `unity_get_assets` | Search project assets with Unity filter syntax |
| `unity_get_scripts` | List MonoBehaviour scripts in the project |
| `unity_instantiate_prefab` | Instantiate a prefab into the scene |
| `unity_refresh_assets` | Refresh the Asset Database |

### Editor Control

| Tool | Description |
|------|-------------|
| `unity_set_playmode` | Control play mode (play, stop, pause, step) |
| `unity_undo` | Undo the last operation |
| `unity_redo` | Redo the last undone operation |
| `unity_get_console` | Get recent console logs, warnings, and errors |

## Example Usage

Once configured, you can use natural language with Claude:

**Scene Inspection:**
- "Show me the scene hierarchy"
- "What components are on the Main Camera?"
- "Find all GameObjects with a Rigidbody"

**Creating Objects:**
- "Create a red cube at position (0, 5, 0)"
- "Add a Sphere named 'Ball' as a child of the Player"
- "Instantiate the Enemy prefab at the spawn point"

**Modifying Objects:**
- "Move the Player to position (10, 0, 5)"
- "Add a Rigidbody to the Cube and set its mass to 5"
- "Change the light intensity to 2.5"

**Materials:**
- "Create a blue metallic material called 'Ocean'"
- "Assign the Glass material to all windows"
- "Make the floor material more reflective"

**Play Mode:**
- "Start play mode"
- "Pause the game"
- "Step one frame forward"

**Scene Management:**
- "Save the current scene"
- "Load the MainMenu scene"
- "What scenes are in this project?"

## API Reference

### HTTP Endpoints (Unity Bridge)

**GET Endpoints:**
- `/ping` - Health check
- `/project` - Project information
- `/scene` - Scene hierarchy
- `/scene/detailed` - Deep scene hierarchy (10 levels)
- `/gameobject?id={instanceId}` - GameObject details
- `/components?id={instanceId}` - Component list
- `/assets?filter={filter}` - Asset search
- `/scripts?filter={filter}` - Script list
- `/console` - Console logs
- `/selection` - Current selection
- `/material?path={path}` - Material info

**POST Endpoints:**
- `/gameobject/create` - Create GameObject
- `/gameobject/modify` - Modify GameObject
- `/gameobject/delete` - Delete GameObject
- `/component/add` - Add component
- `/component/remove` - Remove component
- `/property/set` - Set property value
- `/playmode` - Control play mode
- `/find` - Find GameObjects
- `/prefab/instantiate` - Instantiate prefab
- `/scene/save` - Save scene
- `/scene/load` - Load scene
- `/scene/list` - List scenes
- `/undo` - Perform undo
- `/redo` - Perform redo
- `/assets/refresh` - Refresh Asset Database
- `/material/create` - Create material
- `/material/modify` - Modify material
- `/material/assign` - Assign material

## Troubleshooting

### "Cannot connect to Unity Editor"
- Make sure Unity is open and focused (first request may timeout if Unity is in background)
- Check Unity console for "[UnityMCP] Server started on port 6850"
- Try `curl http://localhost:6850/ping` to verify the bridge is running

### Port already in use
The server automatically tries ports 6850-6859. Check the Unity console for the actual port being used.

### HttpListener access denied (Windows)
If you get access denied errors, register the URL namespace:
```cmd
netsh http add urlacl url=http://localhost:6850/ user=Everyone
```

### Unity domain reload
The server automatically handles Unity's script recompilation and restarts gracefully.

### Property not found
- Property names are case-sensitive
- Use the serialized field name (often starts with lowercase or `m_`)
- Use `unity_get_components` to see available property names

## Project Structure

```
UnityMCP/
├── Assets/
│   └── Editor/
│       └── UnityMCPBridge/
│           ├── UnityMCPServer.cs      # HTTP server with thread-safe handling
│           ├── RequestRouter.cs        # Routes requests to handlers
│           ├── Models/
│           │   └── DataModels.cs       # Request/response data classes
│           ├── Providers/
│           │   ├── SceneDataProvider.cs
│           │   ├── AssetDataProvider.cs
│           │   ├── ConsoleDataProvider.cs
│           │   ├── ProjectDataProvider.cs
│           │   └── SelectionDataProvider.cs
│           └── Handlers/
│               ├── GameObjectHandler.cs
│               ├── PlayModeHandler.cs
│               ├── ComponentPropertyHandler.cs
│               ├── SceneHandler.cs
│               └── MaterialHandler.cs
└── unity-mcp-server/
    ├── package.json
    ├── tsconfig.json
    └── src/
        ├── index.ts                    # MCP server with tool definitions
        └── unity-client.ts             # HTTP client for Unity bridge
```

## Contributing

Contributions are welcome! Some ideas for extensions:

- [ ] Screenshot/preview capture
- [ ] Animation control
- [ ] Lighting adjustments
- [ ] Physics simulation control
- [ ] Build pipeline integration
- [ ] Test runner integration
- [ ] Asset import/export
- [ ] Terrain tools

## License

MIT License - See [LICENSE](LICENSE) file for details.

## Acknowledgments

Built with:
- [Model Context Protocol SDK](https://github.com/modelcontextprotocol/sdk)
- [Newtonsoft.Json](https://www.newtonsoft.com/json) for Unity serialization
