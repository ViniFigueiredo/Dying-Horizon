using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Cinemachine;
using DyingHorizon;

public static class BuildSampleScene
{
    [MenuItem("DyingHorizon/Build Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();

        // 1. GameLoop
        var loopGO = GameObject.Find("GameLoop");
        if (loopGO == null) loopGO = new GameObject("GameLoop");
        var gameLoop = loopGO.GetComponent<GameLoop>();
        if (gameLoop == null) gameLoop = loopGO.AddComponent<GameLoop>();

        // 2. Sprite assets
        var survivorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Survivor.png");

        // 3. Player
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null)
        {
            playerGO = new GameObject("Player");
            playerGO.tag = "Player";
        }
        playerGO.transform.position = Vector3.zero;

        var sr = playerGO.GetComponent<SpriteRenderer>();
        if (sr == null) sr = playerGO.AddComponent<SpriteRenderer>();
        if (survivorSprite != null) sr.sprite = survivorSprite;
        sr.sortingOrder = 5;

        var col = playerGO.GetComponent<BoxCollider2D>();
        if (col == null) col = playerGO.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1.2f);

        var rb = playerGO.GetComponent<Rigidbody2D>();
        if (rb == null) rb = playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var pc = playerGO.GetComponent<PlayerController2D>();
        if (pc == null) pc = playerGO.AddComponent<PlayerController2D>();

        // 4. Cinemachine Camera
        var vcamGO = GameObject.Find("CinemachineCamera");
        if (vcamGO == null) vcamGO = new GameObject("CinemachineCamera");
        var vcam = vcamGO.GetComponent<CinemachineCamera>();
        if (vcam == null) vcam = vcamGO.AddComponent<CinemachineCamera>();
        vcam.Target.TrackingTarget = playerGO.transform;

        // 5. Resource Pickups (5 items)
        Vector2[] resourcePositions = new Vector2[]
        {
            new Vector2(3f, 2f),
            new Vector2(-3.5f, 1.5f),
            new Vector2(4f, -3f),
            new Vector2(-2.5f, -3.5f),
            new Vector2(0.5f, 4.5f)
        };

        for (int i = 0; i < resourcePositions.Length; i++)
        {
            string name = $"Resource_Node_{i + 1}";
            var resGO = GameObject.Find(name);
            if (resGO == null) resGO = new GameObject(name);
            resGO.transform.position = (Vector3)resourcePositions[i];

            var rSr = resGO.GetComponent<SpriteRenderer>();
            if (rSr == null) rSr = resGO.AddComponent<SpriteRenderer>();
            rSr.color = new Color(0.2f, 0.85f, 0.4f, 1f); // radioactive scrap green
            if (survivorSprite != null) rSr.sprite = survivorSprite;
            resGO.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            rSr.sortingOrder = 2;

            var rCol = resGO.GetComponent<CircleCollider2D>();
            if (rCol == null) rCol = resGO.AddComponent<CircleCollider2D>();
            rCol.isTrigger = true;
            rCol.radius = 0.6f;

            var rItem = resGO.GetComponent<ResourceItem2D>();
            if (rItem == null) rItem = resGO.AddComponent<ResourceItem2D>();
            rItem.amount = 1;
            rItem.resourceName = $"Recurso #{i + 1}";
        }

        // 6. Mutant Enemies
        Vector2[] enemyPositions = new Vector2[]
        {
            new Vector2(6f, 1f),
            new Vector2(-5f, -2f)
        };
        string[] enemyNames = new string[] { "Mutante_Kraby", "Mutante_Zombie" };

        for (int i = 0; i < enemyPositions.Length; i++)
        {
            string eName = enemyNames[i];
            var eGO = GameObject.Find(eName);
            if (eGO == null) eGO = new GameObject(eName);
            eGO.transform.position = (Vector3)enemyPositions[i];

            var eSr = eGO.GetComponent<SpriteRenderer>();
            if (eSr == null) eSr = eGO.AddComponent<SpriteRenderer>();
            eSr.color = new Color(0.9f, 0.2f, 0.2f, 1f); // mutant red
            if (survivorSprite != null) eSr.sprite = survivorSprite;
            resGO_scale(eGO, new Vector3(0.8f, 0.8f, 1f));
            eSr.sortingOrder = 4;

            var eCol = eGO.GetComponent<CircleCollider2D>();
            if (eCol == null) eCol = eGO.AddComponent<CircleCollider2D>();
            eCol.radius = 0.5f;

            var eRb = eGO.GetComponent<Rigidbody2D>();
            if (eRb == null) eRb = eGO.AddComponent<Rigidbody2D>();
            eRb.gravityScale = 0f;
            eRb.freezeRotation = true;

            var eMut = eGO.GetComponent<MutantEnemy2D>();
            if (eMut == null) eMut = eGO.AddComponent<MutantEnemy2D>();
            eMut.mutantName = eName;
            eMut.targetPlayer = playerGO.transform;
            eMut.damage = 25f;
            eMut.moveSpeed = 1.8f;
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

        // Top Goal Text
        var goalGO = FindOrCreateUI(canvasGO, "GoalText");
        var goalRect = goalGO.GetComponent<RectTransform>();
        goalRect.anchorMin = new Vector2(0.5f, 1f);
        goalRect.anchorMax = new Vector2(0.5f, 1f);
        goalRect.pivot = new Vector2(0.5f, 1f);
        goalRect.anchoredPosition = new Vector2(0, -30);
        goalRect.sizeDelta = new Vector2(800, 60);
        var goalTMP = goalGO.GetComponent<TextMeshProUGUI>();
        if (goalTMP == null) goalTMP = goalGO.AddComponent<TextMeshProUGUI>();
        goalTMP.alignment = TextAlignmentOptions.Center;
        goalTMP.fontSize = 28;
        goalTMP.color = new Color(0.95f, 0.95f, 0.9f);
        hud.goalText = goalTMP;

        // Bottom Left Vitals
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
        statusRect.sizeDelta = new Vector2(600, 50);
        var statusTMP = statusGO.GetComponent<TextMeshProUGUI>();
        if (statusTMP == null) statusTMP = statusGO.AddComponent<TextMeshProUGUI>();
        statusTMP.fontSize = 20;
        statusTMP.color = new Color(0.85f, 0.85f, 0.7f);
        hud.statusText = statusTMP;

        // Win Panel
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

        // Lose Panel
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
        Debug.Log("Dying Horizon: SampleScene montada com sucesso!");
    }

    private static void resGO_scale(GameObject go, Vector3 scale)
    {
        go.transform.localScale = scale;
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
