using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
            importer.SaveAndReimport();
        }
    }

    [MenuItem("DyingHorizon/Build Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();

        // 0. Configure all 8-bit sprites
        string[] charSprites = {
            "Assets/Sprites/Survivor.png",
            "Assets/Sprites/Survivor_Walk_0.png",
            "Assets/Sprites/Survivor_Walk_1.png",
            "Assets/Sprites/Survivor_Walk_2.png",
            "Assets/Sprites/Enemies/Kraby/spr_kraby_single.png",
            "Assets/Sprites/Enemies/Zombie/spr_zombie_single.png",
            "Assets/Sprites/Enemies/Birdie/spr_birdie_single.png"
        };
        foreach (var p in charSprites) ConfigurePixelSprite(p, 64f);

        ConfigurePixelSprite("Assets/Sprites/Environment/env_sand_tile.png", 16f);
        ConfigurePixelSprite("Assets/Sprites/Environment/env_grass_tile.png", 16f);
        ConfigurePixelSprite("Assets/Sprites/Environment/env_ocean_tile.png", 16f);
        ConfigurePixelSprite("Assets/Sprites/Environment/shadow_circle.png", 16f);
        ConfigurePixelSprite("Assets/Sprites/Items/item_supply_crate.png", 16f);
        ConfigurePixelSprite("Assets/Sprites/Environment/prop_dead_tree.png", 32f);
        ConfigurePixelSprite("Assets/Sprites/Environment/prop_shelter_campfire.png", 32f);

        // 1. GameLoop
        var loopGO = GameObject.Find("GameLoop");
        if (loopGO == null) loopGO = new GameObject("GameLoop");
        var gameLoop = loopGO.GetComponent<GameLoop>();
        if (gameLoop == null) gameLoop = loopGO.AddComponent<GameLoop>();

        // 2. Load Sprites
        var walk0 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Survivor_Walk_0.png");
        var walk1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Survivor_Walk_1.png");
        var walk2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Survivor_Walk_2.png");
        var sandSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/env_sand_tile.png");
        var grassSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/env_grass_tile.png");
        var oceanSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/env_ocean_tile.png");
        var treeSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/prop_dead_tree.png");
        var campSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/prop_shelter_campfire.png");
        var shadowSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/shadow_circle.png");
        var crateSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/item_supply_crate.png");
        var krabySpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemies/Kraby/spr_kraby_single.png");
        var zombieSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemies/Zombie/spr_zombie_single.png");
        var birdieSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemies/Birdie/spr_birdie_single.png");

        // 2.5 Build Coastal Zone Environment (Zona Litorânea)
        var oldEnv = GameObject.Find("ZonaLitoranea_Environment");
        if (oldEnv != null) Object.DestroyImmediate(oldEnv);
        var envRoot = new GameObject("ZonaLitoranea_Environment");

        for (int x = -12; x <= 12; x++)
        {
            for (int y = -8; y <= 8; y++)
            {
                var tile = new GameObject($"Tile_{x}_{y}");
                tile.transform.SetParent(envRoot.transform);
                tile.transform.position = new Vector3(x, y, 0f);
                var tsr = tile.AddComponent<SpriteRenderer>();
                tsr.sortingOrder = -10;
                if (x < -2) tsr.sprite = grassSpr;
                else if (x <= 5) tsr.sprite = sandSpr;
                else tsr.sprite = oceanSpr;
            }
        }

        Vector2[] treePositions = {
            new Vector2(-4f, 3f), new Vector2(-6f, -2f), new Vector2(-3.5f, -4f),
            new Vector2(-7f, 4f), new Vector2(-5f, 0f)
        };
        for (int i = 0; i < treePositions.Length; i++)
        {
            var tgo = new GameObject($"DeadTree_{i}");
            tgo.transform.SetParent(envRoot.transform);
            tgo.transform.position = (Vector3)treePositions[i];
            var tsr = tgo.AddComponent<SpriteRenderer>();
            tsr.sprite = treeSpr;
            tsr.sortingOrder = 1;
            tgo.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        }

        var campGO = new GameObject("Shelter_Campfire");
        campGO.transform.SetParent(envRoot.transform);
        campGO.transform.position = new Vector3(-0.8f, 0.8f, 0f);
        var csr = campGO.AddComponent<SpriteRenderer>();
        csr.sprite = campSpr;
        csr.sortingOrder = 1;

        // 3. Player
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null)
        {
            playerGO = new GameObject("Player");
            playerGO.tag = "Player";
        }
        playerGO.transform.position = Vector3.zero;
        playerGO.transform.localScale = new Vector3(1.25f, 1.25f, 1f);

        var sr = playerGO.GetComponent<SpriteRenderer>();
        if (sr == null) sr = playerGO.AddComponent<SpriteRenderer>();
        if (walk0 != null) sr.sprite = walk0;
        sr.color = Color.white;
        sr.sortingOrder = 10;

        var oldShadow = playerGO.transform.Find("Shadow");
        if (oldShadow != null) Object.DestroyImmediate(oldShadow.gameObject);
        var shadowGO = new GameObject("Shadow");
        shadowGO.transform.SetParent(playerGO.transform, false);
        shadowGO.transform.localPosition = new Vector3(0f, -0.42f, 0f);
        var shSr = shadowGO.AddComponent<SpriteRenderer>();
        shSr.sprite = shadowSpr;
        shSr.sortingOrder = 9;

        var col = playerGO.GetComponent<BoxCollider2D>();
        if (col == null) col = playerGO.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.75f, 0.85f);

        var rb = playerGO.GetComponent<Rigidbody2D>();
        if (rb == null) rb = playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var pc = playerGO.GetComponent<PlayerController2D>();
        if (pc == null) pc = playerGO.AddComponent<PlayerController2D>();
        pc.walkSprites = new Sprite[] { walk0, walk1, walk2 };

        // 4. Camera Follow (remove old CinemachineCamera so Z=-10 is preserved)
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

        // 5. Resource Pickups (5 supply crates)
        Vector2[] resourcePositions = new Vector2[]
        {
            new Vector2(2.5f, 1.8f),
            new Vector2(-2.0f, 1.5f),
            new Vector2(-2.2f, -2.5f),
            new Vector2(3.0f, -2.5f),
            new Vector2(0.5f, 3.5f)
        };

        for (int i = 0; i < resourcePositions.Length; i++)
        {
            string name = $"Resource_Node_{i + 1}";
            var resGO = GameObject.Find(name);
            if (resGO == null) resGO = new GameObject(name);
            resGO.transform.position = (Vector3)resourcePositions[i];
            resGO.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            var rSr = resGO.GetComponent<SpriteRenderer>();
            if (rSr == null) rSr = resGO.AddComponent<SpriteRenderer>();
            rSr.color = Color.white;
            if (crateSpr != null) rSr.sprite = crateSpr;
            rSr.sortingOrder = 4;

            var rCol = resGO.GetComponent<CircleCollider2D>();
            if (rCol == null) rCol = resGO.AddComponent<CircleCollider2D>();
            rCol.isTrigger = true;
            rCol.radius = 0.5f;

            var rItem = resGO.GetComponent<ResourceItem2D>();
            if (rItem == null) rItem = resGO.AddComponent<ResourceItem2D>();
            rItem.amount = 1;
            rItem.resourceName = $"Suprimento #{i + 1}";
        }

        // 6. Mutant Enemies (Kraby, Zombie, Birdie)
        Vector2[] enemyPositions = new Vector2[]
        {
            new Vector2(4.2f, 0.5f),
            new Vector2(-4.5f, -1.8f),
            new Vector2(1.5f, -4.2f)
        };
        string[] enemyNames = new string[] { "Mutante_Kraby", "Mutante_Zombie", "Mutante_Birdie" };
        Sprite[] enemySprites = new Sprite[] { krabySpr, zombieSpr, birdieSpr };

        for (int i = 0; i < enemyPositions.Length; i++)
        {
            string eName = enemyNames[i];
            var eGO = GameObject.Find(eName);
            if (eGO == null) eGO = new GameObject(eName);
            eGO.transform.position = (Vector3)enemyPositions[i];
            eGO.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

            var eSr = eGO.GetComponent<SpriteRenderer>();
            if (eSr == null) eSr = eGO.AddComponent<SpriteRenderer>();
            eSr.color = Color.white;
            if (enemySprites[i] != null) eSr.sprite = enemySprites[i];
            eSr.sortingOrder = 6;

            var eCol = eGO.GetComponent<CircleCollider2D>();
            if (eCol == null) eCol = eGO.AddComponent<CircleCollider2D>();
            eCol.radius = 0.45f;

            var eRb = eGO.GetComponent<Rigidbody2D>();
            if (eRb == null) eRb = eGO.AddComponent<Rigidbody2D>();
            eRb.gravityScale = 0f;
            eRb.freezeRotation = true;

            var eMut = eGO.GetComponent<MutantEnemy2D>();
            if (eMut == null) eMut = eGO.AddComponent<MutantEnemy2D>();
            eMut.mutantName = eName;
            eMut.targetPlayer = playerGO.transform;
            eMut.damage = 15f;
            eMut.moveSpeed = 1.3f;
            eMut.detectionRadius = 4.0f;
        }

        // 7. Canvas & HUD
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
        controlsRect.sizeDelta = new Vector2(1100, 45);
        var controlsTMP = controlsGO.GetComponent<TextMeshProUGUI>();
        if (controlsTMP == null) controlsTMP = controlsGO.AddComponent<TextMeshProUGUI>();
        controlsTMP.alignment = TextAlignmentOptions.Center;
        controlsTMP.fontSize = 22;
        controlsTMP.color = new Color(0.75f, 0.9f, 1.0f);
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

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Dying Horizon: Demo da Zona Litorânea montada com sucesso!");
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
