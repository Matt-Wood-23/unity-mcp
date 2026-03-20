const UNITY_PORT = 6850;

export class UnityClient {
  private baseUrl: string;

  constructor(port: number = UNITY_PORT) {
    this.baseUrl = `http://localhost:${port}`;
  }

  async ping(): Promise<{ connected: boolean; message: string; port?: number }> {
    try {
      const response = await fetch(`${this.baseUrl}/ping`, {
        signal: AbortSignal.timeout(5000)
      });
      if (response.ok) {
        const data = await response.json();
        return { connected: true, message: "Unity Editor is connected", port: data.port };
      }
      return { connected: false, message: "Unity responded but with error" };
    } catch {
      return {
        connected: false,
        message: "Cannot connect to Unity Editor. Make sure Unity is open with the MCP Bridge package installed."
      };
    }
  }

  async getProject(): Promise<any> {
    return this.get("/project");
  }

  async getSceneHierarchy(): Promise<any> {
    return this.get("/scene");
  }

  async getDetailedScene(): Promise<any> {
    return this.get("/scene/detailed");
  }

  async getGameObject(instanceId: number): Promise<any> {
    return this.get(`/gameobject?id=${instanceId}`);
  }

  async getComponents(instanceId: number): Promise<any> {
    return this.get(`/components?id=${instanceId}`);
  }

  async getAssets(filter?: string): Promise<any> {
    const query = filter ? `?filter=${encodeURIComponent(filter)}` : "";
    return this.get(`/assets${query}`);
  }

  async getScripts(filter?: string): Promise<any> {
    const query = filter ? `?filter=${encodeURIComponent(filter)}` : "";
    return this.get(`/scripts${query}`);
  }

  async getConsoleLogs(): Promise<any> {
    return this.get("/console");
  }

  async getCurrentSelection(): Promise<any> {
    return this.get("/selection");
  }

  // Write operations
  async createGameObject(options: {
    name?: string;
    primitiveType?: string;
    parentId?: number;
    position?: { x: number; y: number; z: number };
    rotation?: { x: number; y: number; z: number };
    scale?: { x: number; y: number; z: number };
  }): Promise<any> {
    return this.post("/gameobject/create", {
      Name: options.name,
      PrimitiveType: options.primitiveType,
      ParentId: options.parentId,
      Position: options.position,
      Rotation: options.rotation,
      Scale: options.scale,
    });
  }

  async modifyGameObject(options: {
    instanceId: number;
    name?: string;
    tag?: string;
    layer?: number;
    isActive?: boolean;
    isStatic?: boolean;
    position?: { x: number; y: number; z: number };
    rotation?: { x: number; y: number; z: number };
    scale?: { x: number; y: number; z: number };
    parentId?: number;
  }): Promise<any> {
    return this.post("/gameobject/modify", {
      InstanceId: options.instanceId,
      Name: options.name,
      Tag: options.tag,
      Layer: options.layer,
      IsActive: options.isActive,
      IsStatic: options.isStatic,
      Position: options.position,
      Rotation: options.rotation,
      Scale: options.scale,
      ParentId: options.parentId,
    });
  }

  async deleteGameObject(instanceId: number): Promise<any> {
    return this.post("/gameobject/delete", { InstanceId: instanceId });
  }

  async addComponent(instanceId: number, componentType: string): Promise<any> {
    return this.post("/component/add", {
      InstanceId: instanceId,
      ComponentType: componentType,
    });
  }

  async removeComponent(
    instanceId: number,
    componentType: string,
    componentIndex?: number
  ): Promise<any> {
    return this.post("/component/remove", {
      InstanceId: instanceId,
      ComponentType: componentType,
      ComponentIndex: componentIndex,
    });
  }

  async setPlayMode(action: "play" | "stop" | "pause" | "step"): Promise<any> {
    return this.post("/playmode", { Action: action });
  }

  async setProperty(options: {
    instanceId: number;
    componentType: string;
    propertyName: string;
    value: any;
    componentIndex?: number;
  }): Promise<any> {
    return this.post("/property/set", {
      InstanceId: options.instanceId,
      ComponentType: options.componentType,
      PropertyName: options.propertyName,
      Value: options.value,
      ComponentIndex: options.componentIndex,
    });
  }

  async findGameObjects(options: {
    name?: string;
    tag?: string;
    layer?: string;
    hasComponent?: string;
    activeOnly?: boolean;
    exactMatch?: boolean;
    maxResults?: number;
  }): Promise<any> {
    return this.post("/find", {
      Name: options.name,
      Tag: options.tag,
      Layer: options.layer,
      HasComponent: options.hasComponent,
      ActiveOnly: options.activeOnly,
      ExactMatch: options.exactMatch,
      MaxResults: options.maxResults,
    });
  }

  async instantiatePrefab(options: {
    prefabPath: string;
    name?: string;
    parentId?: number;
    position?: { x: number; y: number; z: number };
    rotation?: { x: number; y: number; z: number };
    scale?: { x: number; y: number; z: number };
  }): Promise<any> {
    return this.post("/prefab/instantiate", {
      PrefabPath: options.prefabPath,
      Name: options.name,
      ParentId: options.parentId,
      Position: options.position,
      Rotation: options.rotation,
      Scale: options.scale,
    });
  }

  async saveScene(): Promise<any> {
    return this.post("/scene/save", {});
  }

  async loadScene(options: {
    scenePath: string;
    additive?: boolean;
    force?: boolean;
  }): Promise<any> {
    return this.post("/scene/load", {
      ScenePath: options.scenePath,
      Additive: options.additive,
      Force: options.force,
    });
  }

  async listScenes(): Promise<any> {
    return this.post("/scene/list", {});
  }

  async undo(): Promise<any> {
    return this.post("/undo", {});
  }

  async redo(): Promise<any> {
    return this.post("/redo", {});
  }

  async refreshAssets(): Promise<any> {
    return this.post("/assets/refresh", {});
  }

  async getMaterialInfo(materialPath: string): Promise<any> {
    return this.get(`/material?path=${encodeURIComponent(materialPath)}`);
  }

  async createMaterial(options: {
    name: string;
    shader?: string;
    color?: { r: number; g: number; b: number; a?: number };
    savePath?: string;
  }): Promise<any> {
    return this.post("/material/create", {
      Name: options.name,
      Shader: options.shader,
      Color: options.color,
      SavePath: options.savePath,
    });
  }

  async modifyMaterial(options: {
    materialPath: string;
    color?: { r: number; g: number; b: number; a?: number };
    propertyName?: string;
    propertyValue?: any;
  }): Promise<any> {
    return this.post("/material/modify", {
      MaterialPath: options.materialPath,
      Color: options.color,
      PropertyName: options.propertyName,
      PropertyValue: options.propertyValue,
    });
  }

  async assignMaterial(options: {
    instanceId: number;
    materialPath: string;
    materialIndex?: number;
  }): Promise<any> {
    return this.post("/material/assign", {
      InstanceId: options.instanceId,
      MaterialPath: options.materialPath,
      MaterialIndex: options.materialIndex,
    });
  }

  // Sprite operations
  async importSprite(options: {
    imagePath: string;
    destinationPath?: string;
    settings?: {
      pixelsPerUnit?: number;
      pivotMode?: string;
      pivotX?: number;
      pivotY?: number;
      filterMode?: string;
      generateMipMaps?: boolean;
      spriteMode?: string;
      compression?: string;
      maxSize?: number;
    };
  }): Promise<any> {
    return this.post("/sprite/import", {
      ImagePath: options.imagePath,
      DestinationPath: options.destinationPath,
      Settings: options.settings ? {
        PixelsPerUnit: options.settings.pixelsPerUnit,
        PivotMode: options.settings.pivotMode,
        PivotX: options.settings.pivotX,
        PivotY: options.settings.pivotY,
        FilterMode: options.settings.filterMode,
        GenerateMipMaps: options.settings.generateMipMaps,
        SpriteMode: options.settings.spriteMode,
        Compression: options.settings.compression,
        MaxSize: options.settings.maxSize,
      } : undefined,
    });
  }

  async configureSpriteSettings(options: {
    assetPath: string;
    settings: {
      pixelsPerUnit?: number;
      pivotMode?: string;
      pivotX?: number;
      pivotY?: number;
      filterMode?: string;
      generateMipMaps?: boolean;
      spriteMode?: string;
      compression?: string;
      maxSize?: number;
    };
  }): Promise<any> {
    return this.post("/sprite/configure", {
      AssetPath: options.assetPath,
      Settings: {
        PixelsPerUnit: options.settings.pixelsPerUnit,
        PivotMode: options.settings.pivotMode,
        PivotX: options.settings.pivotX,
        PivotY: options.settings.pivotY,
        FilterMode: options.settings.filterMode,
        GenerateMipMaps: options.settings.generateMipMaps,
        SpriteMode: options.settings.spriteMode,
        Compression: options.settings.compression,
        MaxSize: options.settings.maxSize,
      },
    });
  }

  async sliceSpriteSheet(options: {
    assetPath: string;
    rows: number;
    columns: number;
  }): Promise<any> {
    return this.post("/sprite/slice", {
      AssetPath: options.assetPath,
      Rows: options.rows,
      Columns: options.columns,
    });
  }

  async createSpriteRenderer(options: {
    instanceId?: number;
    name?: string;
    spritePath: string;
    sortingLayer?: string;
    orderInLayer?: number;
    color?: { r: number; g: number; b: number; a?: number };
    flipX?: boolean;
    flipY?: boolean;
  }): Promise<any> {
    return this.post("/sprite/renderer/create", {
      InstanceId: options.instanceId,
      Name: options.name,
      SpritePath: options.spritePath,
      SortingLayer: options.sortingLayer,
      OrderInLayer: options.orderInLayer,
      Color: options.color,
      FlipX: options.flipX,
      FlipY: options.flipY,
    });
  }

  private async get(endpoint: string): Promise<any> {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      signal: AbortSignal.timeout(30000),
    });
    if (!response.ok) {
      throw new Error(`Unity request failed: ${response.statusText}`);
    }
    return response.json();
  }

  private async post(endpoint: string, body: any): Promise<any> {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
      signal: AbortSignal.timeout(30000),
    });
    if (!response.ok) {
      throw new Error(`Unity request failed: ${response.statusText}`);
    }
    return response.json();
  }
}

export const unityClient = new UnityClient();
