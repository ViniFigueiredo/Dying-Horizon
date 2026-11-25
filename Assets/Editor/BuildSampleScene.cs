using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using DyingHorizon;

public static class BuildSampleScene
{
    private static void ConfigurePixelSprite(string path, float ppu)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    private static TileBase[] LoadWangTileArray(string prefix)
    {
        string[] codes = {
            "0000", "0001", "0010", "0011",
            "0100", "0101", "0110", "0111",
            "1000", "1001", "1010", "1011",
            "1100", "1101", "1110", "1111"
        };
        var arr = new TileBase[16];
        for (int m = 0; m < 16; m++)
        {
            string path = $"Assets/Tilemaps/ZonaLitoral/Tiles/{prefix}_w{m:D2}_{codes[m]}.asset";
            arr[m] = AssetDatabase.LoadAssetAtPath<Tile>(path);
        }
        return arr;
    }

    private static AnimatedTile CreateOrUpdateAnimatedTile(string assetPath, Sprite[] frames, float fps, Tile.ColliderType colliderType)
    {
        var animTile = AssetDatabase.LoadAssetAtPath<AnimatedTile>(assetPath);
        if (animTile == null)
        {
            animTile = ScriptableObject.CreateInstance<AnimatedTile>();
            AssetDatabase.CreateAsset(animTile, assetPath);
        }

        animTile.m_AnimatedSprites = frames;
        animTile.m_MinSpeed = fps;
        animTile.m_MaxSpeed = fps;
        animTile.m_AnimationStartTime = 0f;
        animTile.m_AnimationStartFrame = 0;
        animTile.m_TileColliderType = colliderType;
        EditorUtility.SetDirty(animTile);
        return animTile;
    }

    [MenuItem("DyingHorizon/Build Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();

        // 0. Configure 8-bit character, item, prop & animated wave sprites (Point, Uncompressed)
        var charSpritesList = new System.Collections.Generic.List<string> {
            "Assets/Sprites/Survivor.png",
            "Assets/Sprites/Survivor_Walk_0.png",
            "Assets/Sprites/Survivor_Walk_1.png",
            "Assets/Sprites/Survivor_Walk_2.png",
            "Assets/Sprites/Enemies/Kraby/spr_kraby_single.png",
            "Assets/Sprites/Enemies/Zombie/spr_zombie_single.png",
            "Assets/Sprites/Enemies/Birdie/spr_birdie_single.png"
        };
        for (int i = 0; i < 10; i++)
        {
            charSpritesList.Add($"Assets/Sprites/Enemies/Zombie/spr_zombie_walk_{i}.png");
        }
        foreach (var p in charSpritesList) ConfigurePixelSprite(p, 64f);

        ConfigurePixelSprite("Assets/Sprites/Environment/shadow_circle.png", 16f);
        ConfigurePixelSprite("Assets/Sprites/Items/item_supply_crate.png", 16f);
        ConfigurePixelSprite("Assets/Sprites/Environment/prop_shelter_campfire.png", 32f);
        ConfigurePixelSprite("Assets/Sprites/Environment/prop_dead_tree.png", 24f);
        ConfigurePixelSprite("Assets/Sprites/Environment/ZonaLitoral/Props/prop_dead_tree.png", 24f);
        ConfigurePixelSprite("Assets/Sprites/Environment/ZonaLitoral/Props/prop_coastal_palm.png", 24f);
        ConfigurePixelSprite("Assets/Sprites/Environment/ZonaLitoral/Props/prop_coastal_pine.png", 24f);

        // Configure 8-bit weapons & projectile sprites (Point, Uncompressed)
        string[] weaponSprites = {
            "Assets/Sprites/Weapons/wpn_pistol_1911.png",
            "Assets/Sprites/Weapons/wpn_shotgun_rusty.png",
            "Assets/Sprites/Weapons/wpn_rifle_assault.png"
        };
        foreach (var wp in weaponSprites) ConfigurePixelSprite(wp, 32f);
        ConfigurePixelSprite("Assets/Sprites/Weapons/spr_bullet_8bit.png", 16f);
        ConfigurePixelSprite("Assets/Sprites/Weapons/spr_muzzle_flash_8bit.png", 16f);

        // Configure 8-bit ocean wave & surf foam frames (16x16, PPU=16, Point, Uncompressed)
        var oceanWaveFrames = new Sprite[4];
        var oceanFoamFrames = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            string wPath = $"Assets/Sprites/Environment/ZonaLitoral/ts_ocean_waves/ts_ocean_wave_frame_{i}.png";
            string fPath = $"Assets/Sprites/Environment/ZonaLitoral/ts_ocean_waves/ts_ocean_foam_frame_{i}.png";
            ConfigurePixelSprite(wPath, 16f);
            ConfigurePixelSprite(fPath, 16f);
            oceanWaveFrames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(wPath);
            oceanFoamFrames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(fPath);
        }

        // Create AnimatedTile assets (3.5 FPS retro NES wave cadence)
        var oceanAnimTile = CreateOrUpdateAnimatedTile(
            "Assets/Tilemaps/ZonaLitoral/AnimatedTile_OceanoOndas.asset",
            oceanWaveFrames,
            3.5f,
            Tile.ColliderType.Grid);

        var foamAnimTile = CreateOrUpdateAnimatedTile(
            "Assets/Tilemaps/ZonaLitoral/AnimatedTile_OndasEspuma.asset",
            oceanFoamFrames,
            3.5f,
            Tile.ColliderType.None);

        string[] coastalProps = {
            "prop_wrecked_boat",
            "prop_rusted_car",
            "prop_abandoned_shack",
            "prop_coastal_palm",
            "prop_coastal_pine",
            "prop_dead_tree",
            "prop_coastal_rock",
            "prop_sea_rock_pixellab",
            "prop_driftwood",
            "prop_spike_trap",
            "prop_rusted_barrel"
        };
        var propSprites = new Dictionary<string, Sprite>();
        var propTiles = new Dictionary<string, Tile>();
        foreach (var pName in coastalProps)
        {
            string pPath = $"Assets/Sprites/Environment/ZonaLitoral/Props/{pName}.png";
            ConfigurePixelSprite(pPath, 16f);
            propSprites[pName] = AssetDatabase.LoadAssetAtPath<Sprite>(pPath);
            propTiles[pName] = AssetDatabase.LoadAssetAtPath<Tile>($"Assets/Tilemaps/ZonaLitoral/Tiles/tile_{pName}.asset");
        }

        // 1. GameLoop
        var loopGO = GameObject.Find("GameLoop");
        if (loopGO == null) loopGO = new GameObject("GameLoop");
        var gameLoop = loopGO.GetComponent<GameLoop>();
        if (gameLoop == null) gameLoop = loopGO.AddComponent<GameLoop>();

        // 2. Load Sprites & Tiles
        var walk0 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Survivor_Walk_0.png");
        var walk1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Survivor_Walk_1.png");
        var walk2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Survivor_Walk_2.png");
        var treeSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/prop_dead_tree.png");
        var campSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/prop_shelter_campfire.png");
        var shadowSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/shadow_circle.png");
        var crateSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/item_supply_crate.png");
        var krabySpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemies/Kraby/spr_kraby_single.png");
        var zombieSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemies/Zombie/spr_zombie_single.png");
        var birdieSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemies/Birdie/spr_birdie_single.png");

        var oceanBeachTiles = LoadWangTileArray("ts_litoral_ocean_beach");
        var beachGrassTiles = LoadWangTileArray("ts_litoral_beach_grass");
        var dirtGrassTiles = LoadWangTileArray("ts_litoral_dirt_grass");
        var oceanBarrierTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/ZonaLitoral/Tiles/tile_ocean_barrier.asset");
        var rulePraia = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tilemaps/ZonaLitoral/RuleTiles/RuleTile_PraiaAreia.asset");
        var ruleGrama = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tilemaps/ZonaLitoral/RuleTiles/RuleTile_VegetacaoCosteira.asset");
        var ruleTrilha = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tilemaps/ZonaLitoral/RuleTiles/RuleTile_TrilhaTerra.asset");

        // 3. Player (Proporção 8-bits calibrada: 0.85x, colisor 0.55x0.65, sombra 0.55x)
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null)
        {
            playerGO = new GameObject("Player");
            playerGO.tag = "Player";
        }
        playerGO.transform.position = Vector3.zero;
        playerGO.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        var srPlayer = playerGO.GetComponent<SpriteRenderer>();
        if (srPlayer == null) srPlayer = playerGO.AddComponent<SpriteRenderer>();
        if (walk0 != null) srPlayer.sprite = walk0;
        srPlayer.color = Color.white;
        srPlayer.sortingOrder = 10;

        var ysortPlayer = playerGO.GetComponent<YSort2D>();
        if (ysortPlayer == null) ysortPlayer = playerGO.AddComponent<YSort2D>();
        ysortPlayer.pivotOffsetY = -0.36f;
        ysortPlayer.isStatic = false;

        var oldShadow = playerGO.transform.Find("Shadow");
        if (oldShadow != null) Object.DestroyImmediate(oldShadow.gameObject);
        var shadowGO = new GameObject("Shadow");
        shadowGO.transform.SetParent(playerGO.transform, false);
        shadowGO.transform.localPosition = new Vector3(0f, -0.36f, 0f);
        shadowGO.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        var shSr = shadowGO.AddComponent<SpriteRenderer>();
        shSr.sprite = shadowSpr;
        shSr.sortingOrder = 9;

        var col = playerGO.GetComponent<BoxCollider2D>();
        if (col == null) col = playerGO.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.55f, 0.65f);

        var rb = playerGO.GetComponent<Rigidbody2D>();
        if (rb == null) rb = playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var pc = playerGO.GetComponent<PlayerController2D>();
        if (pc == null) pc = playerGO.AddComponent<PlayerController2D>();
        pc.walkSprites = new Sprite[] { walk0, walk1, walk2 };

        // 4. Camera Follow
        var oldVcam = GameObject.Find("CinemachineCamera");
        if (oldVcam != null) Object.DestroyImmediate(oldVcam);

        var mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0f, 0f, -10f);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5.5f;
            var camFollow = mainCam.GetComponent<CameraFollow2D>();
            if (camFollow == null) camFollow = mainCam.gameObject.AddComponent<CameraFollow2D>();
            camFollow.target = playerGO.transform;
        }

        // 5. Build Coastal Zone Grid + CoastalProceduralGenerator
        var oldEnv = GameObject.Find("ZonaLitoranea_Environment");
        if (oldEnv != null) Object.DestroyImmediate(oldEnv);
        var oldGrid = GameObject.Find("ZonaLitoranea_Grid");
        if (oldGrid != null) Object.DestroyImmediate(oldGrid);

        var gridGO = new GameObject("ZonaLitoranea_Grid");
        var grid = gridGO.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        grid.cellSize = new Vector3(1f, 1f, 0f);
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

        var groundGO = new GameObject("Tilemap_Terreno");
        groundGO.transform.SetParent(gridGO.transform, false);
        var groundTm = groundGO.AddComponent<Tilemap>();
        var groundTr = groundGO.AddComponent<TilemapRenderer>();
        groundTr.sortingOrder = -20;

        var oceanGO = new GameObject("Tilemap_Oceano_Colisao");
        oceanGO.transform.SetParent(gridGO.transform, false);
        var oceanTm = oceanGO.AddComponent<Tilemap>();
        var oceanTr = oceanGO.AddComponent<TilemapRenderer>();
        oceanTr.sortingOrder = -25;
        var oceanRb = oceanGO.AddComponent<Rigidbody2D>();
        oceanRb.bodyType = RigidbodyType2D.Static;
        var oceanTmCol = oceanGO.AddComponent<TilemapCollider2D>();
        oceanTmCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        var oceanCompCol = oceanGO.AddComponent<CompositeCollider2D>();
        oceanCompCol.geometryType = CompositeCollider2D.GeometryType.Polygons;
        oceanCompCol.generationType = CompositeCollider2D.GenerationType.Synchronous;

        var foamGO = new GameObject("Tilemap_Ondas_Espuma");
        foamGO.transform.SetParent(gridGO.transform, false);
        var foamTm = foamGO.AddComponent<Tilemap>();
        var foamTr = foamGO.AddComponent<TilemapRenderer>();
        foamTr.sortingOrder = -18;

        var decoGO = new GameObject("Tilemap_Detalhes");
        decoGO.transform.SetParent(gridGO.transform, false);
        var decoTm = decoGO.AddComponent<Tilemap>();
        var decoTr = decoGO.AddComponent<TilemapRenderer>();
        decoTr.sortingOrder = -10;

        var propsRoot = new GameObject("Structures_And_Props");
        propsRoot.transform.SetParent(gridGO.transform, false);

        // Wire Procedural Coastal Generator
        var generator = gridGO.AddComponent<CoastalProceduralGenerator>();
        generator.seed = 2026;
        generator.useRandomSeed = false;
        generator.generateOnAwake = true;

        generator.groundTilemap = groundTm;
        generator.oceanTilemap = oceanTm;
        generator.waveFoamTilemap = foamTm;
        generator.decoTilemap = decoTm;
        generator.propsContainer = propsRoot.transform;

        generator.oceanAnimatedTile = oceanAnimTile;
        generator.oceanFoamAnimatedTile = foamAnimTile;
        generator.oceanBarrierTile = oceanBarrierTile;

        generator.oceanBeachTiles = oceanBeachTiles;
        generator.beachGrassTiles = beachGrassTiles;
        generator.dirtGrassTiles = dirtGrassTiles;

        generator.rulePraiaAreia = rulePraia;
        generator.ruleVegetacaoCosteira = ruleGrama;
        generator.ruleTrilhaTerra = ruleTrilha;

        generator.driftwoodTile = propTiles["prop_driftwood"];
        generator.spikeTrapTile = propTiles["prop_spike_trap"];

        generator.campfireSprite = campSpr;
        generator.wreckedBoatSprite = propSprites["prop_wrecked_boat"];
        generator.abandonedShackSprite = propSprites["prop_abandoned_shack"];
        generator.rustedCarSprite = propSprites["prop_rusted_car"];
        generator.coastalPalmSprite = propSprites["prop_coastal_palm"];
        generator.deadTreeSprite = propSprites["prop_dead_tree"];
        generator.coastalPineSprite = propSprites["prop_coastal_pine"];
        generator.coastalRockSprite = propSprites["prop_coastal_rock"];
        generator.seaRockSprite = propSprites["prop_sea_rock_pixellab"];
        generator.rustedBarrelSprite = propSprites["prop_rusted_barrel"];
        generator.supplyCrateSprite = crateSpr;

        generator.pistolSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Weapons/wpn_pistol_1911.png");
        generator.shotgunSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Weapons/wpn_shotgun_rusty.png");
        generator.rifleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Weapons/wpn_rifle_assault.png");
        generator.bulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Weapons/spr_bullet_8bit.png");
        generator.muzzleFlashSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Weapons/spr_muzzle_flash_8bit.png");

        // Proporções calibradas dos personagens
        generator.playerScale = 0.85f;
        generator.playerColliderSize = new Vector2(0.55f, 0.65f);
        generator.playerShadowScale = 0.55f;

        generator.krabySprite = krabySpr;
        generator.krabyScale = 1.50f;
        generator.krabyColliderRadius = 0.50f;

        generator.zombieSprite = zombieSpr;
        var zombieWalk = new Sprite[10];
        for (int i = 0; i < 10; i++)
        {
            zombieWalk[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Enemies/Zombie/spr_zombie_walk_{i}.png");
        }
        generator.zombieWalkSprites = zombieWalk;
        generator.zombieScale = 1.30f;
        generator.zombieColliderRadius = 0.45f;

        generator.birdieSprite = birdieSpr;
        generator.birdieScale = 1.20f;
        generator.birdieColliderRadius = 0.40f;

        // Executar geração procedural inicial para popular a cena
        generator.GenerateMap();

        // 6. Canvas & HUD
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null) canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.GetComponent<Canvas>();
        if (canvas == null) canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var raycaster = canvasGO.GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = canvasGO.AddComponent<GraphicRaycaster>();

        var hud = canvasGO.GetComponent<GameHUD>();
        if (hud == null) hud = canvasGO.AddComponent<GameHUD>();

        var goalGO = FindOrCreateUI(canvasGO, "GoalText");
        var goalRect = goalGO.GetComponent<RectTransform>();
        goalRect.anchorMin = new Vector2(0.5f, 1f);
        goalRect.anchorMax = new Vector2(0.5f, 1f);
        goalRect.pivot = new Vector2(0.5f, 1f);
        goalRect.anchoredPosition = new Vector2(0, -30);
        goalRect.sizeDelta = new Vector2(900, 60);
        var goalTMP = goalGO.GetComponent<TextMeshProUGUI>();
        if (goalTMP == null) goalTMP = goalGO.AddComponent<TextMeshProUGUI>();
        goalTMP.alignment = TextAlignmentOptions.Center;
        goalTMP.fontSize = 30;
        goalTMP.color = new Color(0.95f, 0.95f, 0.9f);
        hud.goalText = goalTMP;

        var controlsGO = FindOrCreateUI(canvasGO, "ControlsText");
        var controlsRect = controlsGO.GetComponent<RectTransform>();
        controlsRect.anchorMin = new Vector2(0.5f, 1f);
        controlsRect.anchorMax = new Vector2(0.5f, 1f);
        controlsRect.pivot = new Vector2(0.5f, 1f);
        controlsRect.anchoredPosition = new Vector2(0, -85);
        controlsRect.sizeDelta = new Vector2(1450, 45);
        var controlsTMP = controlsGO.GetComponent<TextMeshProUGUI>();
        if (controlsTMP == null) controlsTMP = controlsGO.AddComponent<TextMeshProUGUI>();
        controlsTMP.alignment = TextAlignmentOptions.Center;
        controlsTMP.fontSize = 21;
        controlsTMP.color = new Color(0.75f, 0.9f, 1.0f);
        controlsTMP.text = "<b>[WASD]</b> Mover | <b>[Mouse + Clique]</b> Mirar e Atirar | <b>[1]</b> Pistola <b>[2]</b> Escopeta <b>[3]</b> Fuzil <b>[0]</b> Guardar | <b>[R]</b> Reiniciar";
        hud.controlsText = controlsTMP;

        var vitalsGO = FindOrCreateUI(canvasGO, "VitalsText");
        var vitalsRect = vitalsGO.GetComponent<RectTransform>();
        vitalsRect.anchorMin = new Vector2(0f, 0f);
        vitalsRect.anchorMax = new Vector2(0f, 0f);
        vitalsRect.pivot = new Vector2(0f, 0f);
        vitalsRect.anchoredPosition = new Vector2(40, 40);
        vitalsRect.sizeDelta = new Vector2(600, 100);
        var vitalsTMP = vitalsGO.GetComponent<TextMeshProUGUI>();
        if (vitalsTMP == null) vitalsTMP = vitalsGO.AddComponent<TextMeshProUGUI>();
        vitalsTMP.fontSize = 24;
        vitalsTMP.color = new Color(0.2f, 0.9f, 0.4f);
        hud.healthText = vitalsTMP;

        var statusGO = FindOrCreateUI(canvasGO, "StatusText");
        var statusRect = statusGO.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(0f, 0f);
        statusRect.pivot = new Vector2(0f, 0f);
        statusRect.anchoredPosition = new Vector2(40, 110);
        statusRect.sizeDelta = new Vector2(700, 50);
        var statusTMP = statusGO.GetComponent<TextMeshProUGUI>();
        if (statusTMP == null) statusTMP = statusGO.AddComponent<TextMeshProUGUI>();
        statusTMP.fontSize = 20;
        statusTMP.color = new Color(0.85f, 0.85f, 0.7f);
        hud.statusText = statusTMP;

        var winGO = FindOrCreateUI(canvasGO, "WinPanel");
        var winRect = winGO.GetComponent<RectTransform>();
        winRect.anchorMin = new Vector2(0.5f, 0.5f);
        winRect.anchorMax = new Vector2(0.5f, 0.5f);
        winRect.pivot = new Vector2(0.5f, 0.5f);
        winRect.anchoredPosition = Vector2.zero;
        winRect.sizeDelta = new Vector2(700, 300);
        var winImage = winGO.GetComponent<Image>();
        if (winImage == null) winImage = winGO.AddComponent<Image>();
        winImage.color = new Color(0.05f, 0.25f, 0.1f, 0.9f);

        var winTextGO = FindOrCreateUI(winGO, "WinText");
        var winTextRect = winTextGO.GetComponent<RectTransform>();
        winTextRect.anchorMin = Vector2.zero;
        winTextRect.anchorMax = Vector2.one;
        winTextRect.sizeDelta = Vector2.zero;
        var winTMP = winTextGO.GetComponent<TextMeshProUGUI>();
        if (winTMP == null) winTMP = winTextGO.AddComponent<TextMeshProUGUI>();
        winTMP.alignment = TextAlignmentOptions.Center;
        winTMP.fontSize = 32;
        winTMP.text = "<b>ABRIGO CONSTRUÍDO!</b>\n\nVocê sobreviveu à Zona Litoral de Dying Horizon!\nPressione <b>[R]</b> para reiniciar a partida.";
        winGO.SetActive(false);
        hud.winPanel = winGO;

        var loseGO = FindOrCreateUI(canvasGO, "LosePanel");
        var loseRect = loseGO.GetComponent<RectTransform>();
        loseRect.anchorMin = new Vector2(0.5f, 0.5f);
        loseRect.anchorMax = new Vector2(0.5f, 0.5f);
        loseRect.pivot = new Vector2(0.5f, 0.5f);
        loseRect.anchoredPosition = Vector2.zero;
        loseRect.sizeDelta = new Vector2(700, 300);
        var loseImage = loseGO.GetComponent<Image>();
        if (loseImage == null) loseImage = loseGO.AddComponent<Image>();
        loseImage.color = new Color(0.3f, 0.05f, 0.05f, 0.9f);

        var loseTextGO = FindOrCreateUI(loseGO, "LoseText");
        var loseTextRect = loseTextGO.GetComponent<RectTransform>();
        loseTextRect.anchorMin = Vector2.zero;
        loseTextRect.anchorMax = Vector2.one;
        loseTextRect.sizeDelta = Vector2.zero;
        var loseTMP = loseTextGO.GetComponent<TextMeshProUGUI>();
        if (loseTMP == null) loseTMP = loseTextGO.AddComponent<TextMeshProUGUI>();
        loseTMP.alignment = TextAlignmentOptions.Center;
        loseTMP.fontSize = 32;
        loseTMP.text = "<b>VOCÊ SUCUMBIU!</b>\n\nAs ameaças e a radiação tomaram conta do sobrevivente.\nPressione <b>[R]</b> para reiniciar a partida.";
        loseGO.SetActive(false);
        hud.losePanel = loseGO;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Dying Horizon: Cena reconstruída com proporções calibradas, gerador procedural e AnimatedTile das ondas!");
    }

    private static GameObject FindOrCreateUI(GameObject parent, string name)
    {
        var child = parent.transform.Find(name);
        if (child != null) return child.gameObject;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }
}
