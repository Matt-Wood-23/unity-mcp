export interface SpriteGenerationRequest {
  prompt: string;
  negativePrompt?: string;
  style?: SpriteStyle;
  outputPath: string;
  width?: number;
  height?: number;
  seed?: number;
}

export interface SpriteGenerationResult {
  success: boolean;
  filePath?: string;
  error?: string;
  seed?: number;
}

export type SpriteStyle =
  | "pixel_art_16x16"
  | "pixel_art_32x32"
  | "pixel_art_64x64"
  | "pixel_art_generic"
  | "retro_game"
  | "modern_2d"
  | "hand_drawn"
  | "flat_color";

export interface StylePreset {
  promptPrefix: string;
  promptSuffix: string;
  negativePrompt: string;
  recommendedSize: { width: number; height: number };
  styles: string[];
}
