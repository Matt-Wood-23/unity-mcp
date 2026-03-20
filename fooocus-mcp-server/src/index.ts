#!/usr/bin/env node

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import { fooocusClient } from "./fooocus-client.js";
import { SPRITE_PRESETS } from "./presets.js";
import { SpriteStyle } from "./types.js";

const server = new Server(
  {
    name: "fooocus-mcp-server",
    version: "1.0.0",
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

const tools = [
  {
    name: "fooocus_ping",
    description: "Check if Fooocus is running and accessible",
    inputSchema: {
      type: "object" as const,
      properties: {},
    },
  },
  {
    name: "fooocus_generate_sprite",
    description: "Generate a sprite image using Fooocus AI. Supports various pixel art and 2D game art styles.",
    inputSchema: {
      type: "object" as const,
      properties: {
        prompt: {
          type: "string",
          description: "Description of the sprite to generate (e.g., 'a warrior character with sword', 'red mushroom enemy')",
        },
        negative_prompt: {
          type: "string",
          description: "Things to avoid in the generation (optional)",
        },
        style: {
          type: "string",
          enum: Object.keys(SPRITE_PRESETS),
          description: "Art style preset: pixel_art_16x16, pixel_art_32x32, pixel_art_64x64, pixel_art_generic, retro_game, modern_2d, hand_drawn, flat_color",
        },
        output_path: {
          type: "string",
          description: "Full path where the sprite should be saved (e.g., 'E:/MyGame/Assets/Sprites/player.png')",
        },
        width: {
          type: "number",
          description: "Image width in pixels (default based on style)",
        },
        height: {
          type: "number",
          description: "Image height in pixels (default based on style)",
        },
        seed: {
          type: "number",
          description: "Random seed for reproducible generation (-1 for random)",
        },
      },
      required: ["prompt", "output_path"],
    },
  },
  {
    name: "fooocus_list_styles",
    description: "List available sprite generation style presets with their descriptions",
    inputSchema: {
      type: "object" as const,
      properties: {},
    },
  },
];

server.setRequestHandler(ListToolsRequestSchema, async () => {
  return { tools };
});

server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    let result: any;

    switch (name) {
      case "fooocus_ping":
        result = await fooocusClient.ping();
        break;

      case "fooocus_generate_sprite":
        result = await fooocusClient.generateSprite({
          prompt: args!.prompt as string,
          negativePrompt: args?.negative_prompt as string,
          style: (args?.style as SpriteStyle) || "pixel_art_generic",
          outputPath: args!.output_path as string,
          width: args?.width as number,
          height: args?.height as number,
          seed: args?.seed as number,
        });
        break;

      case "fooocus_list_styles":
        const styles = Object.entries(SPRITE_PRESETS).map(([name, preset]) => ({
          name,
          description: preset.promptPrefix,
          recommendedSize: preset.recommendedSize,
        }));
        result = { styles };
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
          text: `Error: ${errorMessage}. Make sure Fooocus is running with: run.bat --listen --port 7865`,
        },
      ],
      isError: true,
    };
  }
});

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("Fooocus MCP Server running on stdio");
}

main().catch(console.error);
