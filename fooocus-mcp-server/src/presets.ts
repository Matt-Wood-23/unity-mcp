import { SpriteStyle, StylePreset } from "./types.js";

export const SPRITE_PRESETS: Record<SpriteStyle, StylePreset> = {
  pixel_art_16x16: {
    promptPrefix: "16x16 pixel art sprite, retro video game style,",
    promptSuffix: ", pixelated, crisp edges, limited color palette, game asset",
    negativePrompt: "blurry, antialiased, smooth gradients, realistic, 3d render, photograph",
    recommendedSize: { width: 512, height: 512 },
    styles: ["Fooocus V2", "SAI Pixel Art"]
  },
  pixel_art_32x32: {
    promptPrefix: "32x32 pixel art sprite, classic game style,",
    promptSuffix: ", pixelated, sharp edges, retro color palette, game asset",
    negativePrompt: "blurry, antialiased, smooth, realistic, 3d, photograph, high resolution details",
    recommendedSize: { width: 512, height: 512 },
    styles: ["Fooocus V2", "SAI Pixel Art"]
  },
  pixel_art_64x64: {
    promptPrefix: "64x64 pixel art sprite, detailed pixel art,",
    promptSuffix: ", pixelated, clean pixels, vibrant colors, game asset, sprite sheet ready",
    negativePrompt: "blurry, smooth gradients, realistic, 3d render, photograph, antialiasing",
    recommendedSize: { width: 512, height: 512 },
    styles: ["Fooocus V2", "SAI Pixel Art"]
  },
  pixel_art_generic: {
    promptPrefix: "pixel art sprite,",
    promptSuffix: ", pixelated style, game asset, crisp pixels",
    negativePrompt: "blurry, smooth, realistic, 3d, photograph",
    recommendedSize: { width: 512, height: 512 },
    styles: ["Fooocus V2", "SAI Pixel Art"]
  },
  retro_game: {
    promptPrefix: "retro video game sprite, 16-bit era style,",
    promptSuffix: ", game asset, nostalgic, classic gaming aesthetic",
    negativePrompt: "modern, realistic, 3d render, photograph, blurry",
    recommendedSize: { width: 512, height: 512 },
    styles: ["Fooocus V2"]
  },
  modern_2d: {
    promptPrefix: "modern 2D game sprite, clean vector style,",
    promptSuffix: ", game asset, polished, professional game art",
    negativePrompt: "3d render, photograph, realistic, pixelated, blurry",
    recommendedSize: { width: 1024, height: 1024 },
    styles: ["Fooocus V2", "SAI Enhance"]
  },
  hand_drawn: {
    promptPrefix: "hand-drawn game sprite, illustrated style,",
    promptSuffix: ", game asset, artistic, sketch-like, charming",
    negativePrompt: "3d render, photograph, realistic, pixelated",
    recommendedSize: { width: 1024, height: 1024 },
    styles: ["Fooocus V2", "MRE Spontaneous Picture"]
  },
  flat_color: {
    promptPrefix: "flat color game sprite, simple shapes,",
    promptSuffix: ", game asset, minimal shading, clean design, vector-like",
    negativePrompt: "3d render, photograph, realistic, detailed textures, gradients",
    recommendedSize: { width: 1024, height: 1024 },
    styles: ["Fooocus V2", "Ads Advertising"]
  }
};

export function buildPrompt(userPrompt: string, style: SpriteStyle): { prompt: string; negativePrompt: string } {
  const preset = SPRITE_PRESETS[style];
  return {
    prompt: `${preset.promptPrefix} ${userPrompt} ${preset.promptSuffix}`,
    negativePrompt: preset.negativePrompt
  };
}
