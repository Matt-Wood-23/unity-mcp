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

  // Screenshot
  async takeScreenshot(options: {
    source?: string;
    width?: number;
    height?: number;
    format?: string;
    quality?: number;
    savePath?: string;
  }): Promise<any> {
    return this.post("/screenshot", {
      Source: options.source,
      Width: options.width,
      Height: options.height,
      Format: options.format,
      Quality: options.quality,
      SavePath: options.savePath,
    });
  }

  // Code execution
  async executeCode(code: string): Promise<any> {
    return this.post("/code/execute", { Code: code });
  }

  // UI operations
  async createUIElement(options: {
    elementType: string;
    name?: string;
    parentId?: number;
    text?: string;
    fontSize?: number;
    color?: { r: number; g: number; b: number; a?: number };
    spritePath?: string;
    anchoredPosition?: { x: number; y: number };
    sizeDelta?: { x: number; y: number };
    anchorMin?: { x: number; y: number };
    anchorMax?: { x: number; y: number };
    pivot?: { x: number; y: number };
  }): Promise<any> {
    return this.post("/ui/create", {
      ElementType: options.elementType,
      Name: options.name,
      ParentId: options.parentId,
      Text: options.text,
      FontSize: options.fontSize,
      Color: options.color ? { R: options.color.r, G: options.color.g, B: options.color.b, A: options.color.a ?? 1 } : undefined,
      SpritePath: options.spritePath,
      AnchoredPosition: options.anchoredPosition ? { X: options.anchoredPosition.x, Y: options.anchoredPosition.y } : undefined,
      SizeDelta: options.sizeDelta ? { X: options.sizeDelta.x, Y: options.sizeDelta.y } : undefined,
      AnchorMin: options.anchorMin ? { X: options.anchorMin.x, Y: options.anchorMin.y } : undefined,
      AnchorMax: options.anchorMax ? { X: options.anchorMax.x, Y: options.anchorMax.y } : undefined,
      Pivot: options.pivot ? { X: options.pivot.x, Y: options.pivot.y } : undefined,
    });
  }

  async modifyUIElement(options: {
    instanceId: number;
    text?: string;
    fontSize?: number;
    color?: { r: number; g: number; b: number; a?: number };
    spritePath?: string;
    alignment?: string;
    interactable?: boolean;
    anchoredPosition?: { x: number; y: number };
    sizeDelta?: { x: number; y: number };
    anchorMin?: { x: number; y: number };
    anchorMax?: { x: number; y: number };
    pivot?: { x: number; y: number };
  }): Promise<any> {
    return this.post("/ui/modify", {
      InstanceId: options.instanceId,
      Text: options.text,
      FontSize: options.fontSize,
      Color: options.color ? { R: options.color.r, G: options.color.g, B: options.color.b, A: options.color.a ?? 1 } : undefined,
      SpritePath: options.spritePath,
      Alignment: options.alignment,
      Interactable: options.interactable,
      AnchoredPosition: options.anchoredPosition ? { X: options.anchoredPosition.x, Y: options.anchoredPosition.y } : undefined,
      SizeDelta: options.sizeDelta ? { X: options.sizeDelta.x, Y: options.sizeDelta.y } : undefined,
      AnchorMin: options.anchorMin ? { X: options.anchorMin.x, Y: options.anchorMin.y } : undefined,
      AnchorMax: options.anchorMax ? { X: options.anchorMax.x, Y: options.anchorMax.y } : undefined,
      Pivot: options.pivot ? { X: options.pivot.x, Y: options.pivot.y } : undefined,
    });
  }

  // Profiler
  async getProfilerData(): Promise<any> {
    return this.get("/profiler");
  }

  // Batch operations
  async batchModify(options: {
    instanceIds?: number[];
    filter?: {
      name?: string;
      tag?: string;
      layer?: string;
      hasComponent?: string;
      activeOnly?: boolean;
      maxResults?: number;
    };
    tag?: string;
    layer?: number;
    isActive?: boolean;
    isStatic?: boolean;
    position?: { x: number; y: number; z: number };
    rotation?: { x: number; y: number; z: number };
    scale?: { x: number; y: number; z: number };
    parentId?: number;
    addComponent?: string;
    removeComponent?: string;
  }): Promise<any> {
    return this.post("/batch/modify", {
      InstanceIds: options.instanceIds,
      Filter: options.filter ? {
        Name: options.filter.name,
        Tag: options.filter.tag,
        Layer: options.filter.layer,
        HasComponent: options.filter.hasComponent,
        ActiveOnly: options.filter.activeOnly,
        MaxResults: options.filter.maxResults,
      } : undefined,
      Tag: options.tag,
      Layer: options.layer,
      IsActive: options.isActive,
      IsStatic: options.isStatic,
      Position: options.position,
      Rotation: options.rotation,
      Scale: options.scale,
      ParentId: options.parentId,
      AddComponent: options.addComponent,
      RemoveComponent: options.removeComponent,
    });
  }

  async batchDelete(options: {
    instanceIds?: number[];
    filter?: {
      name?: string;
      tag?: string;
      layer?: string;
      hasComponent?: string;
      activeOnly?: boolean;
      maxResults?: number;
    };
  }): Promise<any> {
    return this.post("/batch/delete", {
      InstanceIds: options.instanceIds,
      Filter: options.filter ? {
        Name: options.filter.name,
        Tag: options.filter.tag,
        Layer: options.filter.layer,
        HasComponent: options.filter.hasComponent,
        ActiveOnly: options.filter.activeOnly,
        MaxResults: options.filter.maxResults,
      } : undefined,
    });
  }

  // Terrain operations
  async createTerrain(options: {
    name?: string;
    width?: number;
    length?: number;
    height?: number;
    heightmapResolution?: number;
    alphamapResolution?: number;
    position?: { x: number; y: number; z: number };
    savePath?: string;
  }): Promise<any> {
    return this.post("/terrain/create", {
      Name: options.name,
      Width: options.width,
      Length: options.length,
      Height: options.height,
      HeightmapResolution: options.heightmapResolution,
      AlphamapResolution: options.alphamapResolution,
      Position: options.position,
      SavePath: options.savePath,
    });
  }

  async modifyTerrainHeight(options: {
    instanceId?: number;
    operation: string;
    value: number;
    strength?: number;
    areaCenterX?: number;
    areaCenterZ?: number;
    areaRadius?: number;
    seed?: number;
  }): Promise<any> {
    return this.post("/terrain/height", {
      InstanceId: options.instanceId,
      Operation: options.operation,
      Value: options.value,
      Strength: options.strength,
      AreaCenterX: options.areaCenterX,
      AreaCenterZ: options.areaCenterZ,
      AreaRadius: options.areaRadius,
      Seed: options.seed,
    });
  }

  async paintTerrainTexture(options: {
    instanceId?: number;
    texturePath?: string;
    tileSize?: number;
    layerIndex?: number;
    centerX?: number;
    centerY?: number;
    radius?: number;
    strength?: number;
  }): Promise<any> {
    return this.post("/terrain/paint", {
      InstanceId: options.instanceId,
      TexturePath: options.texturePath,
      TileSize: options.tileSize,
      LayerIndex: options.layerIndex,
      CenterX: options.centerX,
      CenterY: options.centerY,
      Radius: options.radius,
      Strength: options.strength,
    });
  }

  async placeTerrainTrees(options: {
    instanceId?: number;
    prefabPath?: string;
    prototypeIndex?: number;
    count?: number;
    minScale?: number;
    maxScale?: number;
    density?: number;
    areaCenterX?: number;
    areaCenterZ?: number;
    areaRadius?: number;
    seed?: number;
  }): Promise<any> {
    return this.post("/terrain/trees", {
      InstanceId: options.instanceId,
      PrefabPath: options.prefabPath,
      PrototypeIndex: options.prototypeIndex,
      Count: options.count,
      MinScale: options.minScale,
      MaxScale: options.maxScale,
      Density: options.density,
      AreaCenterX: options.areaCenterX,
      AreaCenterZ: options.areaCenterZ,
      AreaRadius: options.areaRadius,
      Seed: options.seed,
    });
  }

  async getTerrainInfo(instanceId?: number): Promise<any> {
    return this.post("/terrain/info", { InstanceId: instanceId });
  }

  // Animation/Animator operations
  async getAnimatorInfo(instanceId: number): Promise<any> {
    return this.post("/animator/info", { InstanceId: instanceId });
  }

  async setAnimatorParameter(options: {
    instanceId: number;
    parameterName: string;
    parameterType: string;
    value?: any;
  }): Promise<any> {
    return this.post("/animator/parameter", {
      InstanceId: options.instanceId,
      ParameterName: options.parameterName,
      ParameterType: options.parameterType,
      Value: options.value,
    });
  }

  async playAnimation(options: {
    instanceId: number;
    stateName: string;
    layer?: number;
    normalizedTime?: number;
  }): Promise<any> {
    return this.post("/animator/play", {
      InstanceId: options.instanceId,
      StateName: options.stateName,
      Layer: options.layer ?? 0,
      NormalizedTime: options.normalizedTime ?? -1,
    });
  }

  // Lighting operations
  async createLight(options: {
    name?: string;
    lightType?: string;
    color?: { r: number; g: number; b: number; a?: number };
    intensity?: number;
    range?: number;
    spotAngle?: number;
    shadows?: string;
    position?: { x: number; y: number; z: number };
    rotation?: { x: number; y: number; z: number };
    parentId?: number;
  }): Promise<any> {
    return this.post("/light/create", {
      Name: options.name,
      LightType: options.lightType,
      Color: options.color,
      Intensity: options.intensity,
      Range: options.range,
      SpotAngle: options.spotAngle,
      Shadows: options.shadows,
      Position: options.position,
      Rotation: options.rotation,
      ParentId: options.parentId,
    });
  }

  async modifyLight(options: {
    instanceId: number;
    color?: { r: number; g: number; b: number; a?: number };
    intensity?: number;
    range?: number;
    spotAngle?: number;
    shadows?: string;
    lightType?: string;
  }): Promise<any> {
    return this.post("/light/modify", {
      InstanceId: options.instanceId,
      Color: options.color,
      Intensity: options.intensity,
      Range: options.range,
      SpotAngle: options.spotAngle,
      Shadows: options.shadows,
      LightType: options.lightType,
    });
  }

  async getLightInfo(instanceId: number): Promise<any> {
    return this.post("/light/info", { InstanceId: instanceId });
  }

  async setEnvironment(options: {
    skyboxMaterialPath?: string;
    ambientMode?: string;
    ambientColor?: { r: number; g: number; b: number; a?: number };
    ambientSkyColor?: { r: number; g: number; b: number; a?: number };
    ambientEquatorColor?: { r: number; g: number; b: number; a?: number };
    ambientGroundColor?: { r: number; g: number; b: number; a?: number };
    ambientIntensity?: number;
    reflectionIntensity?: number;
    fog?: boolean;
    fogColor?: { r: number; g: number; b: number; a?: number };
    fogMode?: string;
    fogDensity?: number;
    fogStartDistance?: number;
    fogEndDistance?: number;
  }): Promise<any> {
    return this.post("/environment/set", {
      SkyboxMaterialPath: options.skyboxMaterialPath,
      AmbientMode: options.ambientMode,
      AmbientColor: options.ambientColor,
      AmbientSkyColor: options.ambientSkyColor,
      AmbientEquatorColor: options.ambientEquatorColor,
      AmbientGroundColor: options.ambientGroundColor,
      AmbientIntensity: options.ambientIntensity,
      ReflectionIntensity: options.reflectionIntensity,
      Fog: options.fog,
      FogColor: options.fogColor,
      FogMode: options.fogMode,
      FogDensity: options.fogDensity,
      FogStartDistance: options.fogStartDistance,
      FogEndDistance: options.fogEndDistance,
    });
  }

  async getEnvironment(): Promise<any> {
    return this.post("/environment/get", {});
  }

  // Physics operations
  async addRigidbody(options: {
    instanceId: number;
    mass?: number;
    drag?: number;
    angularDrag?: number;
    useGravity?: boolean;
    isKinematic?: boolean;
    collisionDetection?: string;
    interpolation?: string;
    constraints?: string;
  }): Promise<any> {
    return this.post("/physics/rigidbody", {
      InstanceId: options.instanceId,
      Mass: options.mass,
      Drag: options.drag,
      AngularDrag: options.angularDrag,
      UseGravity: options.useGravity,
      IsKinematic: options.isKinematic,
      CollisionDetection: options.collisionDetection,
      Interpolation: options.interpolation,
      Constraints: options.constraints,
    });
  }

  async addCollider(options: {
    instanceId: number;
    colliderType: string;
    isTrigger?: boolean;
    physicMaterialPath?: string;
    center?: { x: number; y: number; z: number };
    size?: { x: number; y: number; z: number };
    radius?: number;
    height?: number;
    direction?: number;
  }): Promise<any> {
    return this.post("/physics/collider", {
      InstanceId: options.instanceId,
      ColliderType: options.colliderType,
      IsTrigger: options.isTrigger,
      PhysicMaterialPath: options.physicMaterialPath,
      Center: options.center,
      Size: options.size,
      Radius: options.radius,
      Height: options.height,
      Direction: options.direction,
    });
  }

  async setPhysicsSettings(options: {
    gravity?: { x: number; y: number; z: number };
    bounceThreshold?: number;
    defaultContactOffset?: number;
    sleepThreshold?: number;
    defaultSolverIterations?: number;
    defaultSolverVelocityIterations?: number;
    autoSyncTransforms?: boolean;
  }): Promise<any> {
    return this.post("/physics/settings/set", {
      Gravity: options.gravity,
      BounceThreshold: options.bounceThreshold,
      DefaultContactOffset: options.defaultContactOffset,
      SleepThreshold: options.sleepThreshold,
      DefaultSolverIterations: options.defaultSolverIterations,
      DefaultSolverVelocityIterations: options.defaultSolverVelocityIterations,
      AutoSyncTransforms: options.autoSyncTransforms,
    });
  }

  async getPhysicsSettings(): Promise<any> {
    return this.post("/physics/settings/get", {});
  }

  // Prefab operations
  async createPrefab(options: {
    instanceId: number;
    savePath?: string;
    replacePrefab?: boolean;
  }): Promise<any> {
    return this.post("/prefab/create", {
      InstanceId: options.instanceId,
      SavePath: options.savePath,
      ReplacePrefab: options.replacePrefab,
    });
  }

  async unpackPrefab(options: {
    instanceId: number;
    completely?: boolean;
  }): Promise<any> {
    return this.post("/prefab/unpack", {
      InstanceId: options.instanceId,
      Completely: options.completely ?? true,
    });
  }

  async applyPrefabOverrides(instanceId: number): Promise<any> {
    return this.post("/prefab/apply", { InstanceId: instanceId });
  }

  async revertPrefabOverrides(instanceId: number): Promise<any> {
    return this.post("/prefab/revert", { InstanceId: instanceId });
  }

  async getPrefabInfo(instanceId: number): Promise<any> {
    return this.post("/prefab/info", { InstanceId: instanceId });
  }

  // Particle System operations
  async createParticleSystem(options: {
    name?: string;
    parentId?: number;
    position?: { x: number; y: number; z: number };
    rotation?: { x: number; y: number; z: number };
    duration?: number;
    looping?: boolean;
    startLifetime?: number;
    startSpeed?: number;
    startSize?: number;
    startColor?: { r: number; g: number; b: number; a?: number };
    maxParticles?: number;
    simulationSpace?: string;
    playOnAwake?: boolean;
    emissionRate?: number;
    shape?: string;
    shapeRadius?: number;
    shapeAngle?: number;
    gravityModifier?: number;
  }): Promise<any> {
    return this.post("/particles/create", {
      Name: options.name,
      ParentId: options.parentId,
      Position: options.position,
      Rotation: options.rotation,
      Duration: options.duration,
      Looping: options.looping,
      StartLifetime: options.startLifetime,
      StartSpeed: options.startSpeed,
      StartSize: options.startSize,
      StartColor: options.startColor,
      MaxParticles: options.maxParticles,
      SimulationSpace: options.simulationSpace,
      PlayOnAwake: options.playOnAwake,
      EmissionRate: options.emissionRate,
      Shape: options.shape,
      ShapeRadius: options.shapeRadius,
      ShapeAngle: options.shapeAngle,
      GravityModifier: options.gravityModifier,
    });
  }

  async modifyParticleSystem(options: {
    instanceId: number;
    duration?: number;
    looping?: boolean;
    startLifetime?: number;
    startLifetimeMin?: number;
    startLifetimeMax?: number;
    startSpeed?: number;
    startSpeedMin?: number;
    startSpeedMax?: number;
    startSize?: number;
    startSizeMin?: number;
    startSizeMax?: number;
    startColor?: { r: number; g: number; b: number; a?: number };
    startColorMin?: { r: number; g: number; b: number; a?: number };
    startColorMax?: { r: number; g: number; b: number; a?: number };
    maxParticles?: number;
    simulationSpace?: string;
    playOnAwake?: boolean;
    gravityModifier?: number;
    simulationSpeed?: number;
    emissionRate?: number;
    shape?: string;
    shapeRadius?: number;
    shapeAngle?: number;
    shapeScale?: { x: number; y: number; z: number };
    materialPath?: string;
    renderMode?: string;
  }): Promise<any> {
    return this.post("/particles/modify", {
      InstanceId: options.instanceId,
      Duration: options.duration,
      Looping: options.looping,
      StartLifetime: options.startLifetime,
      StartLifetimeMin: options.startLifetimeMin,
      StartLifetimeMax: options.startLifetimeMax,
      StartSpeed: options.startSpeed,
      StartSpeedMin: options.startSpeedMin,
      StartSpeedMax: options.startSpeedMax,
      StartSize: options.startSize,
      StartSizeMin: options.startSizeMin,
      StartSizeMax: options.startSizeMax,
      StartColor: options.startColor,
      StartColorMin: options.startColorMin,
      StartColorMax: options.startColorMax,
      MaxParticles: options.maxParticles,
      SimulationSpace: options.simulationSpace,
      PlayOnAwake: options.playOnAwake,
      GravityModifier: options.gravityModifier,
      SimulationSpeed: options.simulationSpeed,
      EmissionRate: options.emissionRate,
      Shape: options.shape,
      ShapeRadius: options.shapeRadius,
      ShapeAngle: options.shapeAngle,
      ShapeScale: options.shapeScale,
      MaterialPath: options.materialPath,
      RenderMode: options.renderMode,
    });
  }

  async playParticleSystem(options: {
    instanceId: number;
    action: string;
    withChildren?: boolean;
  }): Promise<any> {
    return this.post("/particles/play", {
      InstanceId: options.instanceId,
      Action: options.action,
      WithChildren: options.withChildren ?? true,
    });
  }

  async getParticleSystemInfo(instanceId: number): Promise<any> {
    return this.post("/particles/info", { InstanceId: instanceId });
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
