import { Client } from "@gradio/client";
import * as fs from "fs";
import * as path from "path";
import { SpriteGenerationRequest, SpriteGenerationResult, SpriteStyle } from "./types.js";
import { SPRITE_PRESETS, buildPrompt } from "./presets.js";

const FOOOCUS_URL = process.env.FOOOCUS_URL || "http://127.0.0.1:7865";

export class FooocusClient {
  private client: Client | null = null;
  private baseUrl: string;

  constructor(url: string = FOOOCUS_URL) {
    this.baseUrl = url;
  }

  async connect(): Promise<void> {
    try {
      this.client = await Client.connect(this.baseUrl);
    } catch (error) {
      throw new Error(`Failed to connect to Fooocus at ${this.baseUrl}: ${error}`);
    }
  }

  async ping(): Promise<{ connected: boolean; message: string }> {
    try {
      if (!this.client) {
        await this.connect();
      }
      return { connected: true, message: `Connected to Fooocus at ${this.baseUrl}` };
    } catch (error) {
      return {
        connected: false,
        message: `Cannot connect to Fooocus. Ensure it's running with: run.bat --listen --port 7865`
      };
    }
  }

  async generateSprite(request: SpriteGenerationRequest): Promise<SpriteGenerationResult> {
    try {
      if (!this.client) {
        await this.connect();
      }

      const style = request.style || "pixel_art_generic";
      const preset = SPRITE_PRESETS[style];
      const { prompt, negativePrompt } = buildPrompt(request.prompt, style);

      const finalNegativePrompt = request.negativePrompt
        ? `${negativePrompt}, ${request.negativePrompt}`
        : negativePrompt;

      const width = request.width || preset.recommendedSize.width;
      const height = request.height || preset.recommendedSize.height;
      const seed = request.seed ?? -1;

      // Call the Fooocus generation endpoint
      // Note: The exact API endpoint and parameters may vary based on Fooocus version
      const result = await this.client!.predict("/generate_image", {
        prompt: prompt,
        negative_prompt: finalNegativePrompt,
        style_selections: preset.styles,
        performance_selection: "Quality",
        aspect_ratios_selection: this.getAspectRatio(width, height),
        image_number: 1,
        output_format: "png",
        image_seed: seed.toString(),
        sharpness: 2.0,
        guidance_scale: 7.0,
      });

      // Extract file path from result
      const outputData = result.data as any;
      if (outputData && Array.isArray(outputData) && outputData.length > 0) {
        // Fooocus typically returns an array with image data or file paths
        const firstResult = outputData[0];
        let generatedPath: string | null = null;

        // Handle different response formats
        if (typeof firstResult === "string") {
          generatedPath = firstResult;
        } else if (firstResult?.path) {
          generatedPath = firstResult.path;
        } else if (firstResult?.url) {
          // If URL is returned, we need to download it
          generatedPath = await this.downloadImage(firstResult.url, request.outputPath);
        } else if (firstResult?.data) {
          // Base64 data
          generatedPath = await this.saveBase64Image(firstResult.data, request.outputPath);
        }

        if (generatedPath) {
          // Ensure output directory exists
          const outputDir = path.dirname(request.outputPath);
          if (!fs.existsSync(outputDir)) {
            fs.mkdirSync(outputDir, { recursive: true });
          }

          // If the generated path is different from output path, copy it
          if (generatedPath !== request.outputPath && fs.existsSync(generatedPath)) {
            fs.copyFileSync(generatedPath, request.outputPath);
          }

          return {
            success: true,
            filePath: request.outputPath,
            seed: seed
          };
        }
      }

      return {
        success: false,
        error: "No image data returned from Fooocus"
      };
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error.message : String(error)
      };
    }
  }

  private getAspectRatio(width: number, height: number): string {
    const ratio = width / height;
    if (Math.abs(ratio - 1) < 0.1) return "1024*1024";
    if (ratio > 1.3) return "1280*768";
    if (ratio < 0.77) return "768*1280";
    return "1024*1024";
  }

  private async downloadImage(url: string, outputPath: string): Promise<string> {
    const response = await fetch(url);
    const buffer = await response.arrayBuffer();
    const outputDir = path.dirname(outputPath);
    if (!fs.existsSync(outputDir)) {
      fs.mkdirSync(outputDir, { recursive: true });
    }
    fs.writeFileSync(outputPath, Buffer.from(buffer));
    return outputPath;
  }

  private async saveBase64Image(base64Data: string, outputPath: string): Promise<string> {
    // Remove data URL prefix if present
    const base64 = base64Data.replace(/^data:image\/\w+;base64,/, "");
    const buffer = Buffer.from(base64, "base64");
    const outputDir = path.dirname(outputPath);
    if (!fs.existsSync(outputDir)) {
      fs.mkdirSync(outputDir, { recursive: true });
    }
    fs.writeFileSync(outputPath, buffer);
    return outputPath;
  }

  async listStyles(): Promise<string[]> {
    return Object.keys(SPRITE_PRESETS);
  }
}

export const fooocusClient = new FooocusClient();
