#!/usr/bin/env node

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
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
const tools = [
  // Read operations
  {
    name: "unity_ping",
    description: "Check if Unity Editor is running and the MCP bridge is active",
    inputSchema: {
      type: "object" as const,
      properties: {},
    },
  },
  {
    name: "unity_get_project",
    description:
      "Get Unity project information including name, version, platform, and play mode status",
    inputSchema: {
      type: "object" as const,
      properties: {},
    },
  },
  {
    name: "unity_get_scene",
    description:
      "Get the current scene hierarchy with all GameObjects, their components, and children. Returns scene name, root objects, and their hierarchical structure.",
    inputSchema: {
      type: "object" as const,
      properties: {
        detailed: {
          type: "boolean",
          description: "If true, returns deeper hierarchy (up to 10 levels instead of 3)",
        },
      },
    },
  },
  {
    name: "unity_get_gameobject",
    description:
      "Get detailed information about a specific GameObject by its instance ID, including transform, all components and their serialized properties",
    inputSchema: {
      type: "object" as const,
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
    description:
      "Get all components attached to a GameObject with their full property details",
    inputSchema: {
      type: "object" as const,
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
    description:
      "Search and list project assets. Use Unity search filter syntax (e.g., 't:Prefab', 't:Material', 't:Script name')",
    inputSchema: {
      type: "object" as const,
      properties: {
        filter: {
          type: "string",
          description:
            "Unity asset search filter (e.g., 't:Prefab', 't:Texture2D', 'Player t:Script')",
        },
      },
    },
  },
  {
    name: "unity_get_scripts",
    description: "List MonoBehaviour scripts in the project, optionally filtered by name",
    inputSchema: {
      type: "object" as const,
      properties: {
        filter: {
          type: "string",
          description: "Filter scripts by name (case-insensitive)",
        },
      },
    },
  },
  {
    name: "unity_get_console",
    description:
      "Get recent Unity console logs including errors, warnings, and info messages",
    inputSchema: {
      type: "object" as const,
      properties: {},
    },
  },
  {
    name: "unity_get_selection",
    description: "Get currently selected GameObjects and assets in the Unity Editor",
    inputSchema: {
      type: "object" as const,
      properties: {},
    },
  },

  // Write operations
  {
    name: "unity_create_gameobject",
    description:
      "Create a new GameObject in the scene. Can create empty objects or primitives (Cube, Sphere, Capsule, Cylinder, Plane, Quad)",
    inputSchema: {
      type: "object" as const,
      properties: {
        name: {
          type: "string",
          description: "Name for the new GameObject",
        },
        primitiveType: {
          type: "string",
          description: "Optional primitive type: Cube, Sphere, Capsule, Cylinder, Plane, Quad",
        },
        parentId: {
          type: "number",
          description: "Instance ID of parent GameObject (optional)",
        },
        position: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" },
          },
          description: "World position",
        },
        rotation: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" },
          },
          description: "Euler rotation in degrees",
        },
        scale: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" },
          },
          description: "Local scale",
        },
      },
    },
  },
  {
    name: "unity_modify_gameobject",
    description:
      "Modify properties of an existing GameObject (name, transform, active state, etc.)",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject to modify",
        },
        name: {
          type: "string",
          description: "New name for the GameObject",
        },
        tag: {
          type: "string",
          description: "New tag for the GameObject",
        },
        layer: {
          type: "number",
          description: "New layer index for the GameObject",
        },
        isActive: {
          type: "boolean",
          description: "Set active state",
        },
        isStatic: {
          type: "boolean",
          description: "Set static flag",
        },
        position: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" },
          },
          description: "New world position",
        },
        rotation: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" },
          },
          description: "New euler rotation in degrees",
        },
        scale: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" },
          },
          description: "New local scale",
        },
        parentId: {
          type: "number",
          description: "Instance ID of new parent (use 0 to unparent)",
        },
      },
      required: ["instanceId"],
    },
  },
  {
    name: "unity_delete_gameobject",
    description: "Delete a GameObject from the scene",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject to delete",
        },
      },
      required: ["instanceId"],
    },
  },
  {
    name: "unity_add_component",
    description:
      "Add a component to a GameObject. Supports Unity built-in types and custom MonoBehaviours.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject",
        },
        componentType: {
          type: "string",
          description:
            "Type name of the component (e.g., 'Rigidbody', 'BoxCollider', 'AudioSource')",
        },
      },
      required: ["instanceId", "componentType"],
    },
  },
  {
    name: "unity_remove_component",
    description: "Remove a component from a GameObject",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject",
        },
        componentType: {
          type: "string",
          description: "Type name of the component to remove",
        },
        componentIndex: {
          type: "number",
          description:
            "If multiple components of same type exist, specify which one (0-indexed)",
        },
      },
      required: ["instanceId", "componentType"],
    },
  },
  {
    name: "unity_set_playmode",
    description: "Control Unity Editor play mode",
    inputSchema: {
      type: "object" as const,
      properties: {
        action: {
          type: "string",
          enum: ["play", "stop", "pause", "step"],
          description: "Play mode action: play, stop, pause (toggle), or step (one frame)",
        },
      },
      required: ["action"],
    },
  },
  {
    name: "unity_set_property",
    description:
      "Set a property value on a component. Supports int, float, bool, string, Vector2, Vector3, Color, enums, and object references.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject",
        },
        componentType: {
          type: "string",
          description: "Type name of the component (e.g., 'Rigidbody', 'Light', 'VirtualPiano')",
        },
        propertyName: {
          type: "string",
          description: "Name of the property to set (e.g., 'mass', 'intensity', 'overlayTransparency')",
        },
        value: {
          description:
            "The value to set. Use appropriate type: number, boolean, string, or object for Vector3 {x,y,z}, Color {r,g,b,a}",
        },
        componentIndex: {
          type: "number",
          description: "If multiple components of same type exist, specify which one (0-indexed)",
        },
      },
      required: ["instanceId", "componentType", "propertyName", "value"],
    },
  },
  {
    name: "unity_find_gameobjects",
    description:
      "Find GameObjects in the scene by name, tag, layer, or component. Returns instance IDs that can be used with other tools.",
    inputSchema: {
      type: "object" as const,
      properties: {
        name: {
          type: "string",
          description: "Find by name (partial match by default)",
        },
        tag: {
          type: "string",
          description: "Filter by tag (e.g., 'Player', 'MainCamera')",
        },
        layer: {
          type: "string",
          description: "Filter by layer name",
        },
        hasComponent: {
          type: "string",
          description: "Filter by component type (e.g., 'Rigidbody', 'Camera')",
        },
        activeOnly: {
          type: "boolean",
          description: "Only return active GameObjects",
        },
        exactMatch: {
          type: "boolean",
          description: "Require exact name match instead of partial",
        },
        maxResults: {
          type: "number",
          description: "Maximum results to return (default 50)",
        },
      },
    },
  },
  {
    name: "unity_instantiate_prefab",
    description: "Instantiate a prefab into the scene by path or name",
    inputSchema: {
      type: "object" as const,
      properties: {
        prefabPath: {
          type: "string",
          description: "Path to prefab (e.g., 'Assets/Prefabs/Enemy.prefab') or prefab name to search",
        },
        name: {
          type: "string",
          description: "Name for the instantiated object",
        },
        parentId: {
          type: "number",
          description: "Instance ID of parent GameObject",
        },
        position: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" },
          },
          description: "World position",
        },
        rotation: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" },
          },
          description: "Euler rotation in degrees",
        },
        scale: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" },
          },
          description: "Local scale",
        },
      },
      required: ["prefabPath"],
    },
  },
  {
    name: "unity_save_scene",
    description: "Save the current scene to disk",
    inputSchema: {
      type: "object" as const,
      properties: {},
    },
  },
  {
    name: "unity_load_scene",
    description: "Load a scene by path. Can load additively or replace the current scene.",
    inputSchema: {
      type: "object" as const,
      properties: {
        scenePath: {
          type: "string",
          description: "Path to the scene (e.g., 'Assets/Scenes/MainMenu.unity')",
        },
        additive: {
          type: "boolean",
          description: "If true, load scene additively without unloading current scene",
        },
        force: {
          type: "boolean",
          description: "If true, discard unsaved changes in current scene",
        },
      },
      required: ["scenePath"],
    },
  },
  {
    name: "unity_list_scenes",
    description: "List all scenes in the project build settings and Assets folder",
    inputSchema: {
      type: "object" as const,
      properties: {},
    },
  },
  {
    name: "unity_undo",
    description: "Undo the last action in Unity Editor",
    inputSchema: {
      type: "object" as const,
      properties: {},
    },
  },
  {
    name: "unity_redo",
    description: "Redo the last undone action in Unity Editor",
    inputSchema: {
      type: "object" as const,
      properties: {},
    },
  },
  {
    name: "unity_refresh_assets",
    description: "Refresh the Unity Asset Database to detect new or changed files",
    inputSchema: {
      type: "object" as const,
      properties: {},
    },
  },
  {
    name: "unity_get_material",
    description: "Get information about a material including its shader, color, and properties",
    inputSchema: {
      type: "object" as const,
      properties: {
        materialPath: {
          type: "string",
          description: "Path to material (e.g., 'Assets/Materials/Red.mat') or material name to search",
        },
      },
      required: ["materialPath"],
    },
  },
  {
    name: "unity_create_material",
    description: "Create a new material with specified shader and color",
    inputSchema: {
      type: "object" as const,
      properties: {
        name: {
          type: "string",
          description: "Name for the new material",
        },
        shader: {
          type: "string",
          description: "Shader name (e.g., 'Standard', 'Universal Render Pipeline/Lit'). Defaults to project's default shader.",
        },
        color: {
          type: "object",
          properties: {
            r: { type: "number", description: "Red (0-1)" },
            g: { type: "number", description: "Green (0-1)" },
            b: { type: "number", description: "Blue (0-1)" },
            a: { type: "number", description: "Alpha (0-1)" },
          },
          description: "Material color",
        },
        savePath: {
          type: "string",
          description: "Path to save the material (e.g., 'Assets/Materials/NewMat.mat')",
        },
      },
      required: ["name"],
    },
  },
  {
    name: "unity_modify_material",
    description: "Modify an existing material's color or shader properties",
    inputSchema: {
      type: "object" as const,
      properties: {
        materialPath: {
          type: "string",
          description: "Path to material or material name",
        },
        color: {
          type: "object",
          properties: {
            r: { type: "number" },
            g: { type: "number" },
            b: { type: "number" },
            a: { type: "number" },
          },
          description: "New material color",
        },
        propertyName: {
          type: "string",
          description: "Shader property name to set (e.g., '_Metallic', '_Smoothness')",
        },
        propertyValue: {
          description: "Value for the shader property (number, color object, or vector)",
        },
      },
      required: ["materialPath"],
    },
  },
  {
    name: "unity_assign_material",
    description: "Assign a material to a GameObject's Renderer component",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject",
        },
        materialPath: {
          type: "string",
          description: "Path to material or material name",
        },
        materialIndex: {
          type: "number",
          description: "Index of material slot to replace (for multi-material objects)",
        },
      },
      required: ["instanceId", "materialPath"],
    },
  },

  // Sprite operations
  {
    name: "unity_import_sprite",
    description: "Import an image file as a sprite asset into Unity. Configures texture settings for optimal sprite use.",
    inputSchema: {
      type: "object" as const,
      properties: {
        imagePath: {
          type: "string",
          description: "Full path to the source image file (e.g., 'E:/Generated/sprite.png')",
        },
        destinationPath: {
          type: "string",
          description: "Path in Assets folder where sprite should be saved (e.g., 'Assets/Sprites/player.png')",
        },
        settings: {
          type: "object",
          properties: {
            pixelsPerUnit: { type: "number", description: "Pixels per unit (default 100)" },
            pivotMode: { type: "string", enum: ["Center", "Bottom", "TopLeft", "Custom"], description: "Sprite pivot point" },
            pivotX: { type: "number", description: "Custom pivot X (0-1)" },
            pivotY: { type: "number", description: "Custom pivot Y (0-1)" },
            filterMode: { type: "string", enum: ["Point", "Bilinear"], description: "Filter mode (Point for pixel art)" },
            generateMipMaps: { type: "boolean", description: "Generate mipmaps (false for pixel art)" },
            spriteMode: { type: "string", enum: ["Single", "Multiple"], description: "Single sprite or sprite sheet" },
            compression: { type: "string", enum: ["None", "Compressed"], description: "Texture compression" },
            maxSize: { type: "number", description: "Maximum texture size" },
          },
          description: "Sprite import settings (all optional, defaults optimized for pixel art)",
        },
      },
      required: ["imagePath"],
    },
  },
  {
    name: "unity_configure_sprite",
    description: "Configure sprite settings on an existing texture asset in Unity",
    inputSchema: {
      type: "object" as const,
      properties: {
        assetPath: {
          type: "string",
          description: "Path to sprite asset (e.g., 'Assets/Sprites/player.png')",
        },
        settings: {
          type: "object",
          properties: {
            pixelsPerUnit: { type: "number" },
            pivotMode: { type: "string", enum: ["Center", "Bottom", "TopLeft", "Custom"] },
            pivotX: { type: "number" },
            pivotY: { type: "number" },
            filterMode: { type: "string", enum: ["Point", "Bilinear"] },
            generateMipMaps: { type: "boolean" },
            spriteMode: { type: "string", enum: ["Single", "Multiple"] },
            compression: { type: "string", enum: ["None", "Compressed"] },
            maxSize: { type: "number" },
          },
          description: "Sprite settings to apply",
        },
      },
      required: ["assetPath", "settings"],
    },
  },
  {
    name: "unity_slice_spritesheet",
    description: "Slice a sprite sheet image into multiple sprites based on a grid",
    inputSchema: {
      type: "object" as const,
      properties: {
        assetPath: {
          type: "string",
          description: "Path to sprite sheet asset (e.g., 'Assets/Sprites/character_sheet.png')",
        },
        rows: {
          type: "number",
          description: "Number of rows in the sprite sheet grid",
        },
        columns: {
          type: "number",
          description: "Number of columns in the sprite sheet grid",
        },
      },
      required: ["assetPath", "rows", "columns"],
    },
  },
  // Screenshot
  {
    name: "unity_take_screenshot",
    description: "Capture a screenshot from the Scene or Game view camera. Returns a base64-encoded image.",
    inputSchema: {
      type: "object" as const,
      properties: {
        source: {
          type: "string",
          enum: ["game", "scene"],
          description: "Which view to capture: 'game' (main camera) or 'scene' (scene view camera). Default: game",
        },
        width: {
          type: "number",
          description: "Image width in pixels (default 640)",
        },
        height: {
          type: "number",
          description: "Image height in pixels (default 480)",
        },
        format: {
          type: "string",
          enum: ["png", "jpg"],
          description: "Image format (default png)",
        },
        quality: {
          type: "number",
          description: "JPG quality 1-100 (default 85)",
        },
        savePath: {
          type: "string",
          description: "Optional file path to save the image to disk",
        },
      },
    },
  },

  // Code execution
  {
    name: "unity_execute_code",
    description:
      "Execute arbitrary C# code in the Unity Editor. Code is compiled and run at runtime. Simple statements are auto-wrapped; or provide a full class with a public static Execute() method.",
    inputSchema: {
      type: "object" as const,
      properties: {
        code: {
          type: "string",
          description:
            "C# code to execute. Simple statements (e.g., 'Debug.Log(Camera.main.transform.position);') or a full class with public static object Execute() method.",
        },
      },
      required: ["code"],
    },
  },

  // UI System
  {
    name: "unity_create_ui_element",
    description:
      "Create a UI element on a Canvas. Supported types: Canvas, Text, Button, Image, Panel, InputField, Slider, Toggle, Dropdown, ScrollView, RawImage. Auto-creates a Canvas if none exists.",
    inputSchema: {
      type: "object" as const,
      properties: {
        elementType: {
          type: "string",
          enum: ["Canvas", "Text", "Button", "Image", "Panel", "InputField", "Slider", "Toggle", "Dropdown", "ScrollView", "RawImage"],
          description: "Type of UI element to create",
        },
        name: {
          type: "string",
          description: "Name for the UI element",
        },
        parentId: {
          type: "number",
          description: "Instance ID of parent GameObject (usually a Canvas or Panel)",
        },
        text: {
          type: "string",
          description: "Text content (for Text, Button, InputField placeholder, Toggle label, Dropdown caption)",
        },
        fontSize: {
          type: "number",
          description: "Font size for text elements",
        },
        color: {
          type: "object",
          properties: {
            r: { type: "number" },
            g: { type: "number" },
            b: { type: "number" },
            a: { type: "number" },
          },
          description: "Color (RGBA 0-1)",
        },
        spritePath: {
          type: "string",
          description: "Asset path to sprite for Image elements",
        },
        anchoredPosition: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" } },
          description: "Position relative to anchor",
        },
        sizeDelta: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" } },
          description: "Size of the element",
        },
        anchorMin: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" } },
          description: "Minimum anchor (0-1)",
        },
        anchorMax: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" } },
          description: "Maximum anchor (0-1)",
        },
        pivot: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" } },
          description: "Pivot point (0-1)",
        },
      },
      required: ["elementType"],
    },
  },
  {
    name: "unity_modify_ui_element",
    description:
      "Modify properties of an existing UI element (text, color, size, anchors, interactable state)",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the UI GameObject",
        },
        text: { type: "string", description: "New text content" },
        fontSize: { type: "number", description: "New font size" },
        color: {
          type: "object",
          properties: {
            r: { type: "number" },
            g: { type: "number" },
            b: { type: "number" },
            a: { type: "number" },
          },
        },
        spritePath: { type: "string", description: "New sprite asset path" },
        alignment: {
          type: "string",
          enum: ["UpperLeft", "UpperCenter", "UpperRight", "MiddleLeft", "MiddleCenter", "MiddleRight", "LowerLeft", "LowerCenter", "LowerRight"],
          description: "Text alignment",
        },
        interactable: { type: "boolean", description: "Set interactable state (Button, InputField, etc.)" },
        anchoredPosition: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" } },
        },
        sizeDelta: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" } },
        },
        anchorMin: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" } },
        },
        anchorMax: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" } },
        },
        pivot: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" } },
        },
      },
      required: ["instanceId"],
    },
  },

  // Profiler
  {
    name: "unity_get_profiler_data",
    description:
      "Get performance and profiling data: memory usage, object counts, asset counts, rendering settings, physics config, and timing info",
    inputSchema: {
      type: "object" as const,
      properties: {},
    },
  },

  // Batch operations
  {
    name: "unity_batch_modify",
    description:
      "Modify multiple GameObjects at once. Target by instance IDs or by filter (name, tag, layer, component). Can set tag, layer, active/static state, transform, parent, and add/remove components.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceIds: {
          type: "array",
          items: { type: "number" },
          description: "Array of instance IDs to modify",
        },
        filter: {
          type: "object",
          properties: {
            name: { type: "string", description: "Filter by name (partial match)" },
            tag: { type: "string", description: "Filter by tag" },
            layer: { type: "string", description: "Filter by layer name" },
            hasComponent: { type: "string", description: "Filter by component type" },
            activeOnly: { type: "boolean" },
            maxResults: { type: "number", description: "Max objects to affect (default 1000)" },
          },
          description: "Filter to select targets (alternative to instanceIds)",
        },
        tag: { type: "string", description: "Set tag on all targets" },
        layer: { type: "number", description: "Set layer on all targets" },
        isActive: { type: "boolean", description: "Set active state" },
        isStatic: { type: "boolean", description: "Set static flag" },
        position: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } },
        },
        rotation: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } },
        },
        scale: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } },
        },
        parentId: { type: "number", description: "Reparent all targets (0 to unparent)" },
        addComponent: { type: "string", description: "Component type to add to all targets" },
        removeComponent: { type: "string", description: "Component type to remove from all targets" },
      },
    },
  },
  {
    name: "unity_batch_delete",
    description: "Delete multiple GameObjects at once. Target by instance IDs or by filter.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceIds: {
          type: "array",
          items: { type: "number" },
          description: "Array of instance IDs to delete",
        },
        filter: {
          type: "object",
          properties: {
            name: { type: "string", description: "Filter by name (partial match)" },
            tag: { type: "string", description: "Filter by tag" },
            layer: { type: "string", description: "Filter by layer name" },
            hasComponent: { type: "string", description: "Filter by component type" },
            activeOnly: { type: "boolean" },
            maxResults: { type: "number" },
          },
          description: "Filter to select targets (alternative to instanceIds)",
        },
      },
    },
  },

  // Terrain
  {
    name: "unity_create_terrain",
    description: "Create a new terrain in the scene with specified dimensions",
    inputSchema: {
      type: "object" as const,
      properties: {
        name: { type: "string", description: "Terrain name (default 'Terrain')" },
        width: { type: "number", description: "Terrain width in units (default 500)" },
        length: { type: "number", description: "Terrain length in units (default 500)" },
        height: { type: "number", description: "Max terrain height (default 100)" },
        heightmapResolution: { type: "number", description: "Heightmap resolution (default 513, must be 2^n+1)" },
        alphamapResolution: { type: "number", description: "Alphamap resolution for textures (default 512)" },
        position: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } },
        },
        savePath: { type: "string", description: "Path to save terrain data asset (default 'Assets/Terrain.asset')" },
      },
    },
  },
  {
    name: "unity_modify_terrain_height",
    description:
      "Modify terrain heightmap. Operations: set, raise, lower, smooth, flatten, perlin. Operates on a circular area defined by center and radius (normalized 0-1).",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: { type: "number", description: "Instance ID of terrain (uses active terrain if omitted)" },
        operation: {
          type: "string",
          enum: ["set", "raise", "lower", "smooth", "flatten", "perlin"],
          description: "Height operation to perform",
        },
        value: {
          type: "number",
          description: "Value: height for set/flatten (world units), amount for raise/lower, scale for perlin, iterations for smooth",
        },
        strength: { type: "number", description: "Strength multiplier (default 1.0). For perlin: amplitude" },
        areaCenterX: { type: "number", description: "Area center X (0-1 normalized, -1 for entire terrain)" },
        areaCenterZ: { type: "number", description: "Area center Z (0-1 normalized, -1 for entire terrain)" },
        areaRadius: { type: "number", description: "Area radius (0-1 normalized, default 0.1)" },
        seed: { type: "number", description: "Random seed for perlin noise" },
      },
      required: ["operation", "value"],
    },
  },
  {
    name: "unity_paint_terrain_texture",
    description:
      "Add terrain layers (textures) and paint them on the terrain. First add a texture, then paint it by layer index.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: { type: "number", description: "Instance ID of terrain (uses active terrain if omitted)" },
        texturePath: { type: "string", description: "Asset path of texture to add as terrain layer" },
        tileSize: { type: "number", description: "Texture tile size (default 10)" },
        layerIndex: { type: "number", description: "Index of terrain layer to paint" },
        centerX: { type: "number", description: "Paint center X (0-1 normalized)" },
        centerY: { type: "number", description: "Paint center Y (0-1 normalized)" },
        radius: { type: "number", description: "Paint radius (0-1 normalized)" },
        strength: { type: "number", description: "Paint strength (0-1)" },
      },
    },
  },
  {
    name: "unity_place_terrain_trees",
    description:
      "Place trees on a terrain. Add a tree prototype by prefab path, then scatter trees within an area.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: { type: "number", description: "Instance ID of terrain (uses active terrain if omitted)" },
        prefabPath: { type: "string", description: "Path to tree prefab to add as prototype" },
        prototypeIndex: { type: "number", description: "Index of existing tree prototype to use" },
        count: { type: "number", description: "Number of trees to place (default 50)" },
        minScale: { type: "number", description: "Minimum random scale (default 0.8)" },
        maxScale: { type: "number", description: "Maximum random scale (default 1.2)" },
        density: { type: "number", description: "Density multiplier (default 1.0)" },
        areaCenterX: { type: "number", description: "Area center X (0-1 normalized, default 0.5)" },
        areaCenterZ: { type: "number", description: "Area center Z (0-1 normalized, default 0.5)" },
        areaRadius: { type: "number", description: "Area radius (0-1 normalized, default 0.5)" },
        seed: { type: "number", description: "Random seed for reproducibility" },
      },
    },
  },
  {
    name: "unity_get_terrain_info",
    description:
      "Get information about a terrain: size, resolution, layers, tree prototypes, and counts",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: { type: "number", description: "Instance ID of terrain (uses active terrain if omitted)" },
      },
    },
  },

  {
    name: "unity_create_sprite_renderer",
    description: "Add a SpriteRenderer component to a GameObject with a specified sprite",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of existing GameObject (optional - creates new if not provided)",
        },
        name: {
          type: "string",
          description: "Name for new GameObject (if instanceId not provided)",
        },
        spritePath: {
          type: "string",
          description: "Path to sprite asset (e.g., 'Assets/Sprites/player.png')",
        },
        sortingLayer: {
          type: "string",
          description: "Sorting layer name",
        },
        orderInLayer: {
          type: "number",
          description: "Order in sorting layer",
        },
        color: {
          type: "object",
          properties: {
            r: { type: "number" },
            g: { type: "number" },
            b: { type: "number" },
            a: { type: "number" },
          },
          description: "Sprite tint color (RGBA 0-1)",
        },
        flipX: { type: "boolean", description: "Flip sprite horizontally" },
        flipY: { type: "boolean", description: "Flip sprite vertically" },
      },
      required: ["spritePath"],
    },
  },

  // Prefab tools
  {
    name: "unity_create_prefab",
    description:
      "Save a scene GameObject as a prefab asset. The GameObject becomes a connected prefab instance.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject to save as prefab",
        },
        savePath: {
          type: "string",
          description: "Asset path to save the prefab (e.g., 'Assets/Prefabs/Player.prefab'). Defaults to Assets/Prefabs/<name>.prefab",
        },
        replacePrefab: {
          type: "boolean",
          description: "Overwrite if a prefab already exists at that path (default false)",
        },
      },
      required: ["instanceId"],
    },
  },
  {
    name: "unity_unpack_prefab",
    description:
      "Unpack a prefab instance in the scene, disconnecting it from the prefab asset. The GameObject remains but is no longer linked to the prefab.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the prefab instance to unpack",
        },
        completely: {
          type: "boolean",
          description: "If true, unpacks all nested prefabs too (default true). If false, only unpacks the outermost prefab.",
        },
      },
      required: ["instanceId"],
    },
  },
  {
    name: "unity_apply_prefab_overrides",
    description:
      "Apply all instance overrides on a prefab instance back to the prefab asset, making them the new defaults.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the prefab instance",
        },
      },
      required: ["instanceId"],
    },
  },
  {
    name: "unity_revert_prefab_overrides",
    description:
      "Revert all instance overrides on a prefab instance, restoring it to the prefab asset's state.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the prefab instance",
        },
      },
      required: ["instanceId"],
    },
  },
  {
    name: "unity_get_prefab_info",
    description:
      "Get prefab status for a GameObject: whether it's a prefab instance, asset path, and if it has overrides.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject",
        },
      },
      required: ["instanceId"],
    },
  },

  // Particle System tools
  {
    name: "unity_create_particle_system",
    description:
      "Create a new Particle System GameObject with configurable emission, shape, color, speed, and lifetime.",
    inputSchema: {
      type: "object" as const,
      properties: {
        name: { type: "string", description: "Name for the GameObject" },
        parentId: { type: "number", description: "Instance ID of parent GameObject" },
        position: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } },
        },
        rotation: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } },
        },
        duration: { type: "number", description: "Particle system duration in seconds" },
        looping: { type: "boolean", description: "Loop the particle system" },
        startLifetime: { type: "number", description: "Particle lifetime in seconds" },
        startSpeed: { type: "number", description: "Initial particle speed" },
        startSize: { type: "number", description: "Initial particle size" },
        startColor: {
          type: "object",
          properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } },
          description: "Initial particle color (RGBA 0-1)",
        },
        maxParticles: { type: "number", description: "Maximum number of particles" },
        simulationSpace: {
          type: "string",
          enum: ["Local", "World"],
          description: "Simulation space (default Local)",
        },
        playOnAwake: { type: "boolean", description: "Auto-play on start" },
        emissionRate: { type: "number", description: "Particles emitted per second" },
        shape: {
          type: "string",
          enum: ["Cone", "Sphere", "Hemisphere", "Box", "Circle", "Edge"],
          description: "Emitter shape (default Cone)",
        },
        shapeRadius: { type: "number", description: "Shape radius" },
        shapeAngle: { type: "number", description: "Cone angle in degrees" },
        gravityModifier: { type: "number", description: "Gravity multiplier (0 = no gravity)" },
      },
    },
  },
  {
    name: "unity_modify_particle_system",
    description:
      "Modify an existing Particle System's properties. Supports min/max ranges for lifetime, speed, size, and color.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject with a ParticleSystem",
        },
        duration: { type: "number" },
        looping: { type: "boolean" },
        startLifetime: { type: "number", description: "Fixed lifetime (use startLifetimeMin/Max for random range)" },
        startLifetimeMin: { type: "number" },
        startLifetimeMax: { type: "number" },
        startSpeed: { type: "number", description: "Fixed speed (use startSpeedMin/Max for random range)" },
        startSpeedMin: { type: "number" },
        startSpeedMax: { type: "number" },
        startSize: { type: "number", description: "Fixed size (use startSizeMin/Max for random range)" },
        startSizeMin: { type: "number" },
        startSizeMax: { type: "number" },
        startColor: {
          type: "object",
          properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } },
        },
        startColorMin: {
          type: "object",
          properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } },
        },
        startColorMax: {
          type: "object",
          properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } },
        },
        maxParticles: { type: "number" },
        simulationSpace: { type: "string", enum: ["Local", "World"] },
        playOnAwake: { type: "boolean" },
        gravityModifier: { type: "number" },
        simulationSpeed: { type: "number", description: "Global simulation speed multiplier" },
        emissionRate: { type: "number" },
        shape: { type: "string", enum: ["Cone", "Sphere", "Hemisphere", "Box", "Circle", "Edge"] },
        shapeRadius: { type: "number" },
        shapeAngle: { type: "number" },
        shapeScale: {
          type: "object",
          properties: { x: { type: "number" }, y: { type: "number" }, z: { type: "number" } },
        },
        materialPath: { type: "string", description: "Asset path to particle material" },
        renderMode: {
          type: "string",
          enum: ["Billboard", "Mesh", "Stretch", "HorizontalBillboard", "VerticalBillboard"],
        },
      },
      required: ["instanceId"],
    },
  },
  {
    name: "unity_play_particle_system",
    description: "Play, pause, stop, or restart a Particle System",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject with a ParticleSystem",
        },
        action: {
          type: "string",
          enum: ["play", "stop", "pause", "restart"],
          description: "Action to perform",
        },
        withChildren: {
          type: "boolean",
          description: "Apply action to child particle systems too (default true)",
        },
      },
      required: ["instanceId", "action"],
    },
  },
  {
    name: "unity_get_particle_system_info",
    description: "Get current settings and state of a Particle System component",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject with a ParticleSystem",
        },
      },
      required: ["instanceId"],
    },
  },

  // Animation/Animator tools
  {
    name: "unity_get_animator_info",
    description:
      "Get Animator component info including parameters, layers, animation clips, and current state. Parameter values are live in Play mode.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject with an Animator component",
        },
      },
      required: ["instanceId"],
    },
  },
  {
    name: "unity_set_animator_parameter",
    description:
      "Set an Animator parameter value. Requires Play mode. Use 'trigger' type to fire a trigger (no value needed).",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject with an Animator",
        },
        parameterName: {
          type: "string",
          description: "Name of the animator parameter",
        },
        parameterType: {
          type: "string",
          enum: ["float", "int", "bool", "trigger"],
          description: "Type of the parameter",
        },
        value: {
          description:
            "Value to set (number for float/int, boolean for bool, not needed for trigger)",
        },
      },
      required: ["instanceId", "parameterName", "parameterType"],
    },
  },
  {
    name: "unity_play_animation",
    description:
      "Play an animation state on an Animator. Requires Play mode. Use unity_get_animator_info to discover available states/clips.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject with an Animator",
        },
        stateName: {
          type: "string",
          description: "Name of the animation state to play",
        },
        layer: {
          type: "number",
          description: "Animator layer index (default 0)",
        },
        normalizedTime: {
          type: "number",
          description: "Start time normalized 0-1 (optional, -1 for default)",
        },
      },
      required: ["instanceId", "stateName"],
    },
  },

  // Lighting tools
  {
    name: "unity_create_light",
    description:
      "Create a new light in the scene (Directional, Point, Spot, or Area)",
    inputSchema: {
      type: "object" as const,
      properties: {
        name: {
          type: "string",
          description: "Name for the light GameObject",
        },
        lightType: {
          type: "string",
          enum: ["Directional", "Point", "Spot", "Area"],
          description: "Type of light (default: Point)",
        },
        color: {
          type: "object",
          properties: {
            r: { type: "number", description: "Red (0-1)" },
            g: { type: "number", description: "Green (0-1)" },
            b: { type: "number", description: "Blue (0-1)" },
            a: { type: "number", description: "Alpha (0-1)" },
          },
          description: "Light color",
        },
        intensity: {
          type: "number",
          description: "Light intensity (default 1)",
        },
        range: {
          type: "number",
          description: "Light range for Point/Spot lights (default 10)",
        },
        spotAngle: {
          type: "number",
          description: "Spot light cone angle in degrees (default 30)",
        },
        shadows: {
          type: "string",
          enum: ["None", "Hard", "Soft"],
          description: "Shadow type (default: None)",
        },
        position: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" },
          },
          description: "World position",
        },
        rotation: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" },
          },
          description: "Euler rotation in degrees",
        },
        parentId: {
          type: "number",
          description: "Instance ID of parent GameObject",
        },
      },
    },
  },
  {
    name: "unity_modify_light",
    description: "Modify properties of an existing Light component",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject with a Light component",
        },
        color: {
          type: "object",
          properties: {
            r: { type: "number" },
            g: { type: "number" },
            b: { type: "number" },
            a: { type: "number" },
          },
          description: "New light color",
        },
        intensity: {
          type: "number",
          description: "New intensity value",
        },
        range: {
          type: "number",
          description: "New range (Point/Spot)",
        },
        spotAngle: {
          type: "number",
          description: "New spot angle in degrees",
        },
        shadows: {
          type: "string",
          enum: ["None", "Hard", "Soft"],
          description: "Shadow type",
        },
        lightType: {
          type: "string",
          enum: ["Directional", "Point", "Spot", "Area"],
          description: "Change light type",
        },
      },
      required: ["instanceId"],
    },
  },
  {
    name: "unity_get_light_info",
    description: "Get detailed information about a Light component on a GameObject",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject with a Light component",
        },
      },
      required: ["instanceId"],
    },
  },
  {
    name: "unity_set_environment",
    description:
      "Set environment and rendering settings: skybox material, ambient light mode/color, fog settings, reflection intensity",
    inputSchema: {
      type: "object" as const,
      properties: {
        skyboxMaterialPath: {
          type: "string",
          description: "Asset path to skybox material (e.g., 'Assets/Materials/MySkybox.mat')",
        },
        ambientMode: {
          type: "string",
          enum: ["Skybox", "Trilight", "Flat"],
          description: "Ambient lighting mode",
        },
        ambientColor: {
          type: "object",
          properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } },
          description: "Ambient light color (Flat mode)",
        },
        ambientSkyColor: {
          type: "object",
          properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } },
          description: "Sky color (Trilight mode)",
        },
        ambientEquatorColor: {
          type: "object",
          properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } },
          description: "Equator color (Trilight mode)",
        },
        ambientGroundColor: {
          type: "object",
          properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } },
          description: "Ground color (Trilight mode)",
        },
        ambientIntensity: {
          type: "number",
          description: "Ambient light intensity",
        },
        reflectionIntensity: {
          type: "number",
          description: "Reflection probe intensity",
        },
        fog: {
          type: "boolean",
          description: "Enable/disable fog",
        },
        fogColor: {
          type: "object",
          properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" }, a: { type: "number" } },
          description: "Fog color",
        },
        fogMode: {
          type: "string",
          enum: ["Linear", "Exponential", "ExponentialSquared"],
          description: "Fog mode",
        },
        fogDensity: {
          type: "number",
          description: "Fog density (Exponential modes)",
        },
        fogStartDistance: {
          type: "number",
          description: "Fog start distance (Linear mode)",
        },
        fogEndDistance: {
          type: "number",
          description: "Fog end distance (Linear mode)",
        },
      },
    },
  },
  {
    name: "unity_get_environment",
    description:
      "Get current environment settings including skybox, ambient light, fog, and reflection configuration",
    inputSchema: {
      type: "object" as const,
      properties: {},
    },
  },

  // Physics tools
  {
    name: "unity_add_rigidbody",
    description:
      "Add and configure a Rigidbody component on a GameObject. If one already exists, updates its settings.",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject",
        },
        mass: {
          type: "number",
          description: "Mass in kg (default 1)",
        },
        drag: {
          type: "number",
          description: "Linear drag (default 0)",
        },
        angularDrag: {
          type: "number",
          description: "Angular drag (default 0.05)",
        },
        useGravity: {
          type: "boolean",
          description: "Use gravity (default true)",
        },
        isKinematic: {
          type: "boolean",
          description: "Is kinematic (default false)",
        },
        collisionDetection: {
          type: "string",
          enum: ["Discrete", "Continuous", "ContinuousDynamic", "ContinuousSpeculative"],
          description: "Collision detection mode (default Discrete)",
        },
        interpolation: {
          type: "string",
          enum: ["None", "Interpolate", "Extrapolate"],
          description: "Rigidbody interpolation mode",
        },
        constraints: {
          type: "string",
          description:
            "Comma-separated constraints: FreezePositionX, FreezePositionY, FreezePositionZ, FreezeRotationX, FreezeRotationY, FreezeRotationZ, FreezePosition, FreezeRotation, FreezeAll",
        },
      },
      required: ["instanceId"],
    },
  },
  {
    name: "unity_add_collider",
    description:
      "Add a collider component to a GameObject (Box, Sphere, Capsule, or Mesh)",
    inputSchema: {
      type: "object" as const,
      properties: {
        instanceId: {
          type: "number",
          description: "Instance ID of the GameObject",
        },
        colliderType: {
          type: "string",
          enum: ["Box", "Sphere", "Capsule", "Mesh"],
          description: "Type of collider to add",
        },
        isTrigger: {
          type: "boolean",
          description: "Set as trigger collider (default false)",
        },
        physicMaterialPath: {
          type: "string",
          description: "Asset path to a PhysicMaterial",
        },
        center: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" },
          },
          description: "Collider center offset",
        },
        size: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" },
          },
          description: "Box collider size",
        },
        radius: {
          type: "number",
          description: "Sphere/Capsule radius",
        },
        height: {
          type: "number",
          description: "Capsule height",
        },
        direction: {
          type: "number",
          description: "Capsule direction axis: 0=X, 1=Y, 2=Z",
        },
      },
      required: ["instanceId", "colliderType"],
    },
  },
  {
    name: "unity_set_physics_settings",
    description:
      "Modify global physics settings (gravity, solver iterations, thresholds)",
    inputSchema: {
      type: "object" as const,
      properties: {
        gravity: {
          type: "object",
          properties: {
            x: { type: "number" },
            y: { type: "number" },
            z: { type: "number" },
          },
          description: "Gravity vector (default 0, -9.81, 0)",
        },
        bounceThreshold: {
          type: "number",
          description: "Minimum relative velocity for bouncing",
        },
        defaultContactOffset: {
          type: "number",
          description: "Default contact offset for colliders",
        },
        sleepThreshold: {
          type: "number",
          description: "Energy threshold below which objects sleep",
        },
        defaultSolverIterations: {
          type: "number",
          description: "Default solver iteration count",
        },
        defaultSolverVelocityIterations: {
          type: "number",
          description: "Default velocity solver iteration count",
        },
        autoSyncTransforms: {
          type: "boolean",
          description: "Auto-sync transforms with physics",
        },
      },
    },
  },
  {
    name: "unity_get_physics_settings",
    description:
      "Get current global physics settings including gravity, solver config, and thresholds",
    inputSchema: {
      type: "object" as const,
      properties: {},
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
        result = await unityClient.ping();
        break;

      case "unity_get_project":
        result = await unityClient.getProject();
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

      case "unity_get_scripts":
        result = await unityClient.getScripts(args?.filter as string);
        break;

      case "unity_get_console":
        result = await unityClient.getConsoleLogs();
        break;

      case "unity_get_selection":
        result = await unityClient.getCurrentSelection();
        break;

      case "unity_create_gameobject":
        result = await unityClient.createGameObject({
          name: args?.name as string,
          primitiveType: args?.primitiveType as string,
          parentId: args?.parentId as number,
          position: args?.position as { x: number; y: number; z: number },
          rotation: args?.rotation as { x: number; y: number; z: number },
          scale: args?.scale as { x: number; y: number; z: number },
        });
        break;

      case "unity_modify_gameobject":
        result = await unityClient.modifyGameObject({
          instanceId: args!.instanceId as number,
          name: args?.name as string,
          tag: args?.tag as string,
          layer: args?.layer as number,
          isActive: args?.isActive as boolean,
          isStatic: args?.isStatic as boolean,
          position: args?.position as { x: number; y: number; z: number },
          rotation: args?.rotation as { x: number; y: number; z: number },
          scale: args?.scale as { x: number; y: number; z: number },
          parentId: args?.parentId as number,
        });
        break;

      case "unity_delete_gameobject":
        result = await unityClient.deleteGameObject(args!.instanceId as number);
        break;

      case "unity_add_component":
        result = await unityClient.addComponent(
          args!.instanceId as number,
          args!.componentType as string
        );
        break;

      case "unity_remove_component":
        result = await unityClient.removeComponent(
          args!.instanceId as number,
          args!.componentType as string,
          args?.componentIndex as number
        );
        break;

      case "unity_set_playmode":
        result = await unityClient.setPlayMode(
          args!.action as "play" | "stop" | "pause" | "step"
        );
        break;

      case "unity_set_property":
        result = await unityClient.setProperty({
          instanceId: args!.instanceId as number,
          componentType: args!.componentType as string,
          propertyName: args!.propertyName as string,
          value: args!.value,
          componentIndex: args?.componentIndex as number,
        });
        break;

      case "unity_find_gameobjects":
        result = await unityClient.findGameObjects({
          name: args?.name as string,
          tag: args?.tag as string,
          layer: args?.layer as string,
          hasComponent: args?.hasComponent as string,
          activeOnly: args?.activeOnly as boolean,
          exactMatch: args?.exactMatch as boolean,
          maxResults: args?.maxResults as number,
        });
        break;

      case "unity_instantiate_prefab":
        result = await unityClient.instantiatePrefab({
          prefabPath: args!.prefabPath as string,
          name: args?.name as string,
          parentId: args?.parentId as number,
          position: args?.position as { x: number; y: number; z: number },
          rotation: args?.rotation as { x: number; y: number; z: number },
          scale: args?.scale as { x: number; y: number; z: number },
        });
        break;

      case "unity_save_scene":
        result = await unityClient.saveScene();
        break;

      case "unity_load_scene":
        result = await unityClient.loadScene({
          scenePath: args!.scenePath as string,
          additive: args?.additive as boolean,
          force: args?.force as boolean,
        });
        break;

      case "unity_list_scenes":
        result = await unityClient.listScenes();
        break;

      case "unity_undo":
        result = await unityClient.undo();
        break;

      case "unity_redo":
        result = await unityClient.redo();
        break;

      case "unity_refresh_assets":
        result = await unityClient.refreshAssets();
        break;

      case "unity_get_material":
        result = await unityClient.getMaterialInfo(args!.materialPath as string);
        break;

      case "unity_create_material":
        result = await unityClient.createMaterial({
          name: args!.name as string,
          shader: args?.shader as string,
          color: args?.color as { r: number; g: number; b: number; a?: number },
          savePath: args?.savePath as string,
        });
        break;

      case "unity_modify_material":
        result = await unityClient.modifyMaterial({
          materialPath: args!.materialPath as string,
          color: args?.color as { r: number; g: number; b: number; a?: number },
          propertyName: args?.propertyName as string,
          propertyValue: args?.propertyValue,
        });
        break;

      case "unity_assign_material":
        result = await unityClient.assignMaterial({
          instanceId: args!.instanceId as number,
          materialPath: args!.materialPath as string,
          materialIndex: args?.materialIndex as number,
        });
        break;

      // Sprite operations
      case "unity_import_sprite":
        result = await unityClient.importSprite({
          imagePath: args!.imagePath as string,
          destinationPath: args?.destinationPath as string,
          settings: args?.settings as any,
        });
        break;

      case "unity_configure_sprite":
        result = await unityClient.configureSpriteSettings({
          assetPath: args!.assetPath as string,
          settings: args!.settings as any,
        });
        break;

      case "unity_slice_spritesheet":
        result = await unityClient.sliceSpriteSheet({
          assetPath: args!.assetPath as string,
          rows: args!.rows as number,
          columns: args!.columns as number,
        });
        break;

      // Screenshot
      case "unity_take_screenshot":
        result = await unityClient.takeScreenshot({
          source: args?.source as string,
          width: args?.width as number,
          height: args?.height as number,
          format: args?.format as string,
          quality: args?.quality as number,
          savePath: args?.savePath as string,
        });
        // Return as image if base64 is present
        if (result?.Base64) {
          const imageFormat = result.Format === "jpeg" ? "image/jpeg" : "image/png";
          return {
            content: [
              {
                type: "image",
                data: result.Base64,
                mimeType: imageFormat,
              },
              {
                type: "text",
                text: `Screenshot captured: ${result.Width}x${result.Height} ${result.Format}${result.SavePath ? ` (saved to ${result.SavePath})` : ""}`,
              },
            ],
          };
        }
        break;

      // Code execution
      case "unity_execute_code":
        result = await unityClient.executeCode(args!.code as string);
        break;

      // UI
      case "unity_create_ui_element":
        result = await unityClient.createUIElement({
          elementType: args!.elementType as string,
          name: args?.name as string,
          parentId: args?.parentId as number,
          text: args?.text as string,
          fontSize: args?.fontSize as number,
          color: args?.color as { r: number; g: number; b: number; a?: number },
          spritePath: args?.spritePath as string,
          anchoredPosition: args?.anchoredPosition as { x: number; y: number },
          sizeDelta: args?.sizeDelta as { x: number; y: number },
          anchorMin: args?.anchorMin as { x: number; y: number },
          anchorMax: args?.anchorMax as { x: number; y: number },
          pivot: args?.pivot as { x: number; y: number },
        });
        break;

      case "unity_modify_ui_element":
        result = await unityClient.modifyUIElement({
          instanceId: args!.instanceId as number,
          text: args?.text as string,
          fontSize: args?.fontSize as number,
          color: args?.color as { r: number; g: number; b: number; a?: number },
          spritePath: args?.spritePath as string,
          alignment: args?.alignment as string,
          interactable: args?.interactable as boolean,
          anchoredPosition: args?.anchoredPosition as { x: number; y: number },
          sizeDelta: args?.sizeDelta as { x: number; y: number },
          anchorMin: args?.anchorMin as { x: number; y: number },
          anchorMax: args?.anchorMax as { x: number; y: number },
          pivot: args?.pivot as { x: number; y: number },
        });
        break;

      // Profiler
      case "unity_get_profiler_data":
        result = await unityClient.getProfilerData();
        break;

      // Batch operations
      case "unity_batch_modify":
        result = await unityClient.batchModify({
          instanceIds: args?.instanceIds as number[],
          filter: args?.filter as any,
          tag: args?.tag as string,
          layer: args?.layer as number,
          isActive: args?.isActive as boolean,
          isStatic: args?.isStatic as boolean,
          position: args?.position as { x: number; y: number; z: number },
          rotation: args?.rotation as { x: number; y: number; z: number },
          scale: args?.scale as { x: number; y: number; z: number },
          parentId: args?.parentId as number,
          addComponent: args?.addComponent as string,
          removeComponent: args?.removeComponent as string,
        });
        break;

      case "unity_batch_delete":
        result = await unityClient.batchDelete({
          instanceIds: args?.instanceIds as number[],
          filter: args?.filter as any,
        });
        break;

      // Terrain
      case "unity_create_terrain":
        result = await unityClient.createTerrain({
          name: args?.name as string,
          width: args?.width as number,
          length: args?.length as number,
          height: args?.height as number,
          heightmapResolution: args?.heightmapResolution as number,
          alphamapResolution: args?.alphamapResolution as number,
          position: args?.position as { x: number; y: number; z: number },
          savePath: args?.savePath as string,
        });
        break;

      case "unity_modify_terrain_height":
        result = await unityClient.modifyTerrainHeight({
          instanceId: args?.instanceId as number,
          operation: args!.operation as string,
          value: args!.value as number,
          strength: args?.strength as number,
          areaCenterX: args?.areaCenterX as number,
          areaCenterZ: args?.areaCenterZ as number,
          areaRadius: args?.areaRadius as number,
          seed: args?.seed as number,
        });
        break;

      case "unity_paint_terrain_texture":
        result = await unityClient.paintTerrainTexture({
          instanceId: args?.instanceId as number,
          texturePath: args?.texturePath as string,
          tileSize: args?.tileSize as number,
          layerIndex: args?.layerIndex as number,
          centerX: args?.centerX as number,
          centerY: args?.centerY as number,
          radius: args?.radius as number,
          strength: args?.strength as number,
        });
        break;

      case "unity_place_terrain_trees":
        result = await unityClient.placeTerrainTrees({
          instanceId: args?.instanceId as number,
          prefabPath: args?.prefabPath as string,
          prototypeIndex: args?.prototypeIndex as number,
          count: args?.count as number,
          minScale: args?.minScale as number,
          maxScale: args?.maxScale as number,
          density: args?.density as number,
          areaCenterX: args?.areaCenterX as number,
          areaCenterZ: args?.areaCenterZ as number,
          areaRadius: args?.areaRadius as number,
          seed: args?.seed as number,
        });
        break;

      case "unity_get_terrain_info":
        result = await unityClient.getTerrainInfo(args?.instanceId as number);
        break;

      case "unity_create_sprite_renderer":
        result = await unityClient.createSpriteRenderer({
          instanceId: args?.instanceId as number,
          name: args?.name as string,
          spritePath: args!.spritePath as string,
          sortingLayer: args?.sortingLayer as string,
          orderInLayer: args?.orderInLayer as number,
          color: args?.color as { r: number; g: number; b: number; a?: number },
          flipX: args?.flipX as boolean,
          flipY: args?.flipY as boolean,
        });
        break;

      // Prefab
      case "unity_create_prefab":
        result = await unityClient.createPrefab({
          instanceId: args!.instanceId as number,
          savePath: args?.savePath as string,
          replacePrefab: args?.replacePrefab as boolean,
        });
        break;

      case "unity_unpack_prefab":
        result = await unityClient.unpackPrefab({
          instanceId: args!.instanceId as number,
          completely: args?.completely as boolean,
        });
        break;

      case "unity_apply_prefab_overrides":
        result = await unityClient.applyPrefabOverrides(args!.instanceId as number);
        break;

      case "unity_revert_prefab_overrides":
        result = await unityClient.revertPrefabOverrides(args!.instanceId as number);
        break;

      case "unity_get_prefab_info":
        result = await unityClient.getPrefabInfo(args!.instanceId as number);
        break;

      // Particle System
      case "unity_create_particle_system":
        result = await unityClient.createParticleSystem({
          name: args?.name as string,
          parentId: args?.parentId as number,
          position: args?.position as { x: number; y: number; z: number },
          rotation: args?.rotation as { x: number; y: number; z: number },
          duration: args?.duration as number,
          looping: args?.looping as boolean,
          startLifetime: args?.startLifetime as number,
          startSpeed: args?.startSpeed as number,
          startSize: args?.startSize as number,
          startColor: args?.startColor as { r: number; g: number; b: number; a?: number },
          maxParticles: args?.maxParticles as number,
          simulationSpace: args?.simulationSpace as string,
          playOnAwake: args?.playOnAwake as boolean,
          emissionRate: args?.emissionRate as number,
          shape: args?.shape as string,
          shapeRadius: args?.shapeRadius as number,
          shapeAngle: args?.shapeAngle as number,
          gravityModifier: args?.gravityModifier as number,
        });
        break;

      case "unity_modify_particle_system":
        result = await unityClient.modifyParticleSystem({
          instanceId: args!.instanceId as number,
          duration: args?.duration as number,
          looping: args?.looping as boolean,
          startLifetime: args?.startLifetime as number,
          startLifetimeMin: args?.startLifetimeMin as number,
          startLifetimeMax: args?.startLifetimeMax as number,
          startSpeed: args?.startSpeed as number,
          startSpeedMin: args?.startSpeedMin as number,
          startSpeedMax: args?.startSpeedMax as number,
          startSize: args?.startSize as number,
          startSizeMin: args?.startSizeMin as number,
          startSizeMax: args?.startSizeMax as number,
          startColor: args?.startColor as { r: number; g: number; b: number; a?: number },
          startColorMin: args?.startColorMin as { r: number; g: number; b: number; a?: number },
          startColorMax: args?.startColorMax as { r: number; g: number; b: number; a?: number },
          maxParticles: args?.maxParticles as number,
          simulationSpace: args?.simulationSpace as string,
          playOnAwake: args?.playOnAwake as boolean,
          gravityModifier: args?.gravityModifier as number,
          simulationSpeed: args?.simulationSpeed as number,
          emissionRate: args?.emissionRate as number,
          shape: args?.shape as string,
          shapeRadius: args?.shapeRadius as number,
          shapeAngle: args?.shapeAngle as number,
          shapeScale: args?.shapeScale as { x: number; y: number; z: number },
          materialPath: args?.materialPath as string,
          renderMode: args?.renderMode as string,
        });
        break;

      case "unity_play_particle_system":
        result = await unityClient.playParticleSystem({
          instanceId: args!.instanceId as number,
          action: args!.action as string,
          withChildren: args?.withChildren as boolean,
        });
        break;

      case "unity_get_particle_system_info":
        result = await unityClient.getParticleSystemInfo(args!.instanceId as number);
        break;

      // Animation/Animator
      case "unity_get_animator_info":
        result = await unityClient.getAnimatorInfo(args!.instanceId as number);
        break;

      case "unity_set_animator_parameter":
        result = await unityClient.setAnimatorParameter({
          instanceId: args!.instanceId as number,
          parameterName: args!.parameterName as string,
          parameterType: args!.parameterType as string,
          value: args?.value,
        });
        break;

      case "unity_play_animation":
        result = await unityClient.playAnimation({
          instanceId: args!.instanceId as number,
          stateName: args!.stateName as string,
          layer: args?.layer as number,
          normalizedTime: args?.normalizedTime as number,
        });
        break;

      // Lighting
      case "unity_create_light":
        result = await unityClient.createLight({
          name: args?.name as string,
          lightType: args?.lightType as string,
          color: args?.color as { r: number; g: number; b: number; a?: number },
          intensity: args?.intensity as number,
          range: args?.range as number,
          spotAngle: args?.spotAngle as number,
          shadows: args?.shadows as string,
          position: args?.position as { x: number; y: number; z: number },
          rotation: args?.rotation as { x: number; y: number; z: number },
          parentId: args?.parentId as number,
        });
        break;

      case "unity_modify_light":
        result = await unityClient.modifyLight({
          instanceId: args!.instanceId as number,
          color: args?.color as { r: number; g: number; b: number; a?: number },
          intensity: args?.intensity as number,
          range: args?.range as number,
          spotAngle: args?.spotAngle as number,
          shadows: args?.shadows as string,
          lightType: args?.lightType as string,
        });
        break;

      case "unity_get_light_info":
        result = await unityClient.getLightInfo(args!.instanceId as number);
        break;

      case "unity_set_environment":
        result = await unityClient.setEnvironment({
          skyboxMaterialPath: args?.skyboxMaterialPath as string,
          ambientMode: args?.ambientMode as string,
          ambientColor: args?.ambientColor as { r: number; g: number; b: number; a?: number },
          ambientSkyColor: args?.ambientSkyColor as { r: number; g: number; b: number; a?: number },
          ambientEquatorColor: args?.ambientEquatorColor as { r: number; g: number; b: number; a?: number },
          ambientGroundColor: args?.ambientGroundColor as { r: number; g: number; b: number; a?: number },
          ambientIntensity: args?.ambientIntensity as number,
          reflectionIntensity: args?.reflectionIntensity as number,
          fog: args?.fog as boolean,
          fogColor: args?.fogColor as { r: number; g: number; b: number; a?: number },
          fogMode: args?.fogMode as string,
          fogDensity: args?.fogDensity as number,
          fogStartDistance: args?.fogStartDistance as number,
          fogEndDistance: args?.fogEndDistance as number,
        });
        break;

      case "unity_get_environment":
        result = await unityClient.getEnvironment();
        break;

      // Physics
      case "unity_add_rigidbody":
        result = await unityClient.addRigidbody({
          instanceId: args!.instanceId as number,
          mass: args?.mass as number,
          drag: args?.drag as number,
          angularDrag: args?.angularDrag as number,
          useGravity: args?.useGravity as boolean,
          isKinematic: args?.isKinematic as boolean,
          collisionDetection: args?.collisionDetection as string,
          interpolation: args?.interpolation as string,
          constraints: args?.constraints as string,
        });
        break;

      case "unity_add_collider":
        result = await unityClient.addCollider({
          instanceId: args!.instanceId as number,
          colliderType: args!.colliderType as string,
          isTrigger: args?.isTrigger as boolean,
          physicMaterialPath: args?.physicMaterialPath as string,
          center: args?.center as { x: number; y: number; z: number },
          size: args?.size as { x: number; y: number; z: number },
          radius: args?.radius as number,
          height: args?.height as number,
          direction: args?.direction as number,
        });
        break;

      case "unity_set_physics_settings":
        result = await unityClient.setPhysicsSettings({
          gravity: args?.gravity as { x: number; y: number; z: number },
          bounceThreshold: args?.bounceThreshold as number,
          defaultContactOffset: args?.defaultContactOffset as number,
          sleepThreshold: args?.sleepThreshold as number,
          defaultSolverIterations: args?.defaultSolverIterations as number,
          defaultSolverVelocityIterations: args?.defaultSolverVelocityIterations as number,
          autoSyncTransforms: args?.autoSyncTransforms as boolean,
        });
        break;

      case "unity_get_physics_settings":
        result = await unityClient.getPhysicsSettings();
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
