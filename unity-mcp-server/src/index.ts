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
