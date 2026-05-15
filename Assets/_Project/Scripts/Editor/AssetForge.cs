using System.IO;
using UnityEditor;
using UnityEngine;

// Кузница плейсхолдер-ассетов: генерирует PNG (мишень, ловушка, фон, эффекты),
// ScriptableObject-конфиги (сложности и типы мишеней), префаб Target.
// Запускается из Bootstrap до сборки сцены.
public static class AssetForge
{
    // 1. Пути к ассетам
    public const string GeneratedArtDir = "Assets/_Project/Art/Generated";
    public const string ConfigsDir = "Assets/_Project/Configs";
    public const string PrefabsDir = "Assets/_Project/Prefabs";
    public const string VfxDir = "Assets/_Project/VFX";

    public const string NormalTargetSpritePath =
        GeneratedArtDir + "/target_normal.png";
    public const string TrapTargetSpritePath =
        GeneratedArtDir + "/target_trap.png";
    public const string BackgroundSpritePath =
        GeneratedArtDir + "/playfield_bg.png";
    public const string SparkSpritePath =
        GeneratedArtDir + "/spark.png";

    public const string NormalConfigPath =
        ConfigsDir + "/NormalTarget.asset";
    public const string TrapConfigPath =
        ConfigsDir + "/TrapTarget.asset";
    public const string DifficultyEasyPath =
        ConfigsDir + "/Difficulty_Easy.asset";
    public const string DifficultyNormalPath =
        ConfigsDir + "/Difficulty_Normal.asset";
    public const string DifficultyHardPath =
        ConfigsDir + "/Difficulty_Hard.asset";

    public const string TargetPrefabPath = PrefabsDir + "/Target.prefab";
    public const string HitEffectPrefabPath = VfxDir + "/HitEffect.prefab";
    public const string MissEffectPrefabPath = VfxDir + "/MissEffect.prefab";

    public static void BuildAll()
    {
        EnsureDir(GeneratedArtDir);
        EnsureDir(ConfigsDir);
        EnsureDir(PrefabsDir);
        EnsureDir(VfxDir);

        // 2. PNG-плейсхолдеры (генерируются всегда — перерисовать при изменении)
        WritePng(
            path: NormalTargetSpritePath,
            tex: MakeDiskTexture(
                size: 128,
                background: new Color32(0, 0, 0, 0),
                disk: new Color32(80, 200, 100, 255),
                ringColor: new Color32(255, 255, 255, 255),
                ringWidth: 6));
        WritePng(
            path: TrapTargetSpritePath,
            tex: MakeDiskTexture(
                size: 128,
                background: new Color32(0, 0, 0, 0),
                disk: new Color32(200, 60, 60, 255),
                ringColor: new Color32(255, 255, 255, 255),
                ringWidth: 6,
                drawCross: true));
        WritePng(
            path: BackgroundSpritePath,
            tex: MakeSolid(64, new Color32(35, 40, 55, 255)));
        WritePng(
            path: SparkSpritePath,
            tex: MakeDiskTexture(
                size: 32,
                background: new Color32(0, 0, 0, 0),
                disk: new Color32(255, 255, 255, 255),
                ringColor: new Color32(255, 255, 255, 0),
                ringWidth: 0));

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        // 3. ScriptableObject-конфиги
        var normalConfig = ForgeNormalTargetConfig();
        var trapConfig = ForgeTrapTargetConfig();
        ForgeEasyDifficulty();
        ForgeNormalDifficulty();
        ForgeHardDifficulty();

        // 4. Префабы (мишень, эффекты)
        var hitFx = ForgeHitEffectPrefab();
        var missFx = ForgeMissEffectPrefab();

        // 5. Связки конфигов с эффектами/звуком (звуков пока нет — null)
        normalConfig.hitEffectPrefab = hitFx;
        // У ловушки тоже зелёный «hit», красный эффект достанется промахам
        trapConfig.hitEffectPrefab = missFx;
        EditorUtility.SetDirty(normalConfig);
        EditorUtility.SetDirty(trapConfig);

        ForgeTargetPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AssetForge] Все ассеты построены.");
    }

    // ===== ScriptableObject =====

    static TargetTypeConfig ForgeNormalTargetConfig()
    {
        var cfg = LoadOrCreate<TargetTypeConfig>(NormalConfigPath);
        cfg.id = "normal";
        cfg.displayName = "Обычная";
        cfg.sprite = LoadSprite(NormalTargetSpritePath);
        cfg.tint = Color.white;
        cfg.scale = 1f;
        cfg.basePoints = 10;
        cfg.isTrap = false;
        EditorUtility.SetDirty(cfg);
        return cfg;
    }

    static TargetTypeConfig ForgeTrapTargetConfig()
    {
        var cfg = LoadOrCreate<TargetTypeConfig>(TrapConfigPath);
        cfg.id = "trap";
        cfg.displayName = "Ловушка";
        cfg.sprite = LoadSprite(TrapTargetSpritePath);
        cfg.tint = Color.white;
        cfg.scale = 1f;
        cfg.basePoints = 15;     // штраф крупнее обычной награды
        cfg.isTrap = true;
        EditorUtility.SetDirty(cfg);
        return cfg;
    }

    static DifficultyConfig ForgeEasyDifficulty()
    {
        var cfg = LoadOrCreate<DifficultyConfig>(DifficultyEasyPath);
        cfg.id = "easy";
        cfg.displayName = "Легко";
        cfg.roundDuration = 30f;
        cfg.spawnIntervalMin = 0.9f;
        cfg.spawnIntervalMax = 1.6f;
        cfg.targetLifetime = 2.5f;
        cfg.trapChance = 0f;
        cfg.spawnAreaPadding = new Vector2(1.4f, 1.4f);
        cfg.fastReactionBonus = 1.5f;
        cfg.fastReactionWindow = 1.0f;
        EditorUtility.SetDirty(cfg);
        return cfg;
    }

    static DifficultyConfig ForgeNormalDifficulty()
    {
        var cfg = LoadOrCreate<DifficultyConfig>(DifficultyNormalPath);
        cfg.id = "normal";
        cfg.displayName = "Нормально";
        cfg.roundDuration = 30f;
        cfg.spawnIntervalMin = 0.55f;
        cfg.spawnIntervalMax = 1.1f;
        cfg.targetLifetime = 1.8f;
        cfg.trapChance = 0.15f;
        cfg.spawnAreaPadding = new Vector2(1.2f, 1.2f);
        cfg.fastReactionBonus = 2.0f;
        cfg.fastReactionWindow = 0.8f;
        EditorUtility.SetDirty(cfg);
        return cfg;
    }

    static DifficultyConfig ForgeHardDifficulty()
    {
        var cfg = LoadOrCreate<DifficultyConfig>(DifficultyHardPath);
        cfg.id = "hard";
        cfg.displayName = "Сложно";
        cfg.roundDuration = 30f;
        cfg.spawnIntervalMin = 0.35f;
        cfg.spawnIntervalMax = 0.7f;
        cfg.targetLifetime = 1.2f;
        cfg.trapChance = 0.25f;
        cfg.spawnAreaPadding = new Vector2(1f, 1f);
        cfg.fastReactionBonus = 2.5f;
        cfg.fastReactionWindow = 0.55f;
        EditorUtility.SetDirty(cfg);
        return cfg;
    }

    // ===== Префабы =====

    static GameObject ForgeTargetPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPath);
        if (existing != null) AssetDatabase.DeleteAsset(TargetPrefabPath);

        var go = new GameObject("Target");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(NormalTargetSpritePath);
        sr.sortingOrder = 10;

        // 6. Круглый коллайдер ≈ под радиус спрайта (1 unit диаметр)
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.45f;
        col.isTrigger = false;

        go.AddComponent<Target>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, TargetPrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject ForgeHitEffectPrefab()
    {
        return ForgeParticlePrefab(
            path: HitEffectPrefabPath,
            name: "HitEffect",
            color: new Color(0.4f, 1f, 0.5f),
            burstCount: 14,
            speed: 4f);
    }

    static GameObject ForgeMissEffectPrefab()
    {
        return ForgeParticlePrefab(
            path: MissEffectPrefabPath,
            name: "MissEffect",
            color: new Color(1f, 0.35f, 0.35f),
            burstCount: 10,
            speed: 3f);
    }

    static GameObject ForgeParticlePrefab(
        string path, string name, Color color, int burstCount, float speed)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);

        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();

        // 7. Main: короткая жизнь, авто-уничтожение
        var main = ps.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = 0.4f;
        main.startSpeed = speed;
        main.startSize = 0.15f;
        main.startColor = color;
        main.gravityModifier = 0f;
        main.maxParticles = 50;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // 8. Burst — одна вспышка
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, burstCount)
        });

        // 9. Круглая зона эмиссии
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        // 10. Материал — Sprites/Default + текстура искры
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var sparkSprite = LoadSprite(SparkSpritePath);
        var mat = new Material(Shader.Find("Sprites/Default"));
        if (sparkSprite != null) mat.mainTexture = sparkSprite.texture;
        renderer.material = mat;
        renderer.sortingOrder = 25;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ===== PNG-генератор =====

    static Texture2D MakeSolid(int size, Color32 color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    static Texture2D MakeDiskTexture(int size,
        Color32 background, Color32 disk, Color32 ringColor, int ringWidth,
        bool drawCross = false)
    {
        // 11. Заливаем фон, рисуем диск, обводку и опционально крест
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = background;

        float cx = (size - 1) / 2f;
        float cy = (size - 1) / 2f;
        float radius = size / 2f - 1f;
        float ringInner = radius - ringWidth;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > radius) continue;

                if (ringWidth > 0 && d >= ringInner)
                    pixels[y * size + x] = ringColor;
                else
                    pixels[y * size + x] = disk;

                if (drawCross)
                {
                    // Толстый крест ±2px от диагоналей
                    var crossThickness = 4f;
                    if (Mathf.Abs(dx - dy) <= crossThickness ||
                        Mathf.Abs(dx + dy) <= crossThickness)
                    {
                        if (d <= ringInner + 1)
                            pixels[y * size + x] = ringColor;
                    }
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    static void WritePng(string path, Texture2D tex)
    {
        var bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        Object.DestroyImmediate(tex);
    }

    // ===== Утилиты =====

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        var so = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(so, path);
        return so;
    }

    static Sprite LoadSprite(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogWarning($"[AssetForge] Спрайт не найден: {path}");
        return s;
    }

    static void EnsureDir(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }
}
