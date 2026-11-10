using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Membuat stage asset, prefab UI, menyusun HUD + wiring di GameScene,
// lalu membangun scene MainMenu (lewat BlockyMenuSetup).
// Aman dijalankan berulang kali: objek yang sudah ada dipakai ulang.
// Menu: Blocky > Setup All Scenes
// Batch: -executeMethod BlockySceneSetup.RunBatch
public static class BlockySceneSetup
{
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const string StageFolder = "Assets/Data/Stages";
    private const string StageListPath = "Assets/Data/StageList.asset";
    private const string InfiniteStagePath = "Assets/Data/InfiniteMode.asset";
    private const string GoalItemPrefabPath = "Assets/Prefabs/UI/ObjectiveGoalItem.prefab";

    // 5 bab x 12 level = 60 stage.
    private static readonly string[] ChapterNames =
    {
        "Sunny Start",
        "Candy Coast",
        "Neon Nights",
        "Crystal Caves",
        "Cosmic Core"
    };

    internal static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.2f, 0.9f);
    internal static readonly Color SubPanelColor = new Color(1f, 1f, 1f, 0.08f);
    internal static readonly Color CardColor = new Color(0.12f, 0.14f, 0.28f, 1f);
    internal static readonly Color ButtonColor = new Color(0.2f, 0.6f, 1f, 1f);

    internal const string StarSpritePath = "Assets/Sprites/star.png";

    internal static Sprite uiSprite;
    internal static Sprite knobSprite;
    internal static Sprite starSprite;

    [MenuItem("Blocky/Setup All Scenes")]
    public static void Run()
    {
        LoadBuiltinSprites();

        StageList stageList = CreateStageAssets();
        StageData infiniteStage = CreateInfiniteStage();
        CreateGoalItemPrefab();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // OpenScene melepas aset prefab dari memori, jadi referensi yang
        // didapat sebelum scene dibuka bisa jadi null. Load ulang dari disk.
        ObjectiveGoalItem goalItemPrefab =
            AssetDatabase.LoadAssetAtPath<ObjectiveGoalItem>(GoalItemPrefabPath);

        if (goalItemPrefab == null)
        {
            throw new System.InvalidOperationException(
                $"BlockySceneSetup: prefab '{GoalItemPrefabPath}' tidak bisa di-load.");
        }

        SetupScene(stageList, infiniteStage, goalItemPrefab);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        BlockyMenuSetup.SetupMenuScene(stageList);

        // MainMenu = scene pertama saat game dibuka.
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(BlockyMenuSetup.MenuScenePath, true),
            new EditorBuildSettingsScene(ScenePath, true)
        };

        AssetDatabase.SaveAssets();
        Debug.Log("BlockySceneSetup: selesai.");
    }

    internal static void LoadBuiltinSprites()
    {
        uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        starSprite = LoadStarSprite();
    }

    // star.png di-import sebagai Sprite tunggal (tanpa mipmap, alpha transparan).
    // Fallback ke lingkaran jika file tidak ada.
    private static Sprite LoadStarSprite()
    {
        TextureImporter importer = AssetImporter.GetAtPath(StarSpritePath) as TextureImporter;

        if (importer == null)
        {
            Debug.LogWarning($"BlockySceneSetup: '{StarSpritePath}' tidak ditemukan, pakai lingkaran.");
            return knobSprite;
        }

        if (importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.mipmapEnabled)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(StarSpritePath);
        return sprite != null ? sprite : knobSprite;
    }

    // Sprite dari Assets/Sprites (mode Multiple → ambil sub-sprite pertama).
    internal static Sprite LoadSprite(string path)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite sprite)
            {
                return sprite;
            }
        }

        Debug.LogWarning($"BlockySceneSetup: sprite '{path}' tidak ditemukan.");
        return null;
    }

    public static void RunBatch()
    {
        try
        {
            Run();
            EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    // ───────────── Stage assets ─────────────

    private static StageList CreateStageAssets()
    {
        EnsureFolder(StageFolder);

        // Target warna, jatah turn, dan kriteria bintang diacak oleh
        // StageGenerator dengan seed tetap → hasil selalu sama.
        List<StageSpec> specs = StageGenerator.Generate(
            ChapterNames.Length,
            StageList.StagesPerChapter,
            StageGenerator.DefaultSeed
        );

        StageData[] stages = new StageData[specs.Count];

        for (int i = 0; i < specs.Count; i++)
        {
            stages[i] = CreateStage($"{StageFolder}/Stage_{specs[i].StageId:000}.asset", specs[i]);
        }

        StageList stageList = LoadOrCreate<StageList>(StageListPath);
        SerializedObject so = new SerializedObject(stageList);

        SerializedProperty list = so.FindProperty("stages");
        list.arraySize = stages.Length;

        for (int i = 0; i < stages.Length; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
        }

        SerializedProperty names = so.FindProperty("chapterNames");
        names.arraySize = ChapterNames.Length;

        for (int i = 0; i < ChapterNames.Length; i++)
        {
            names.GetArrayElementAtIndex(i).stringValue = ChapterNames[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(stageList);

        return stageList;
    }

    // Infinite Mode: board 8x8, jatah awal seperti contoh GDD, tanpa target.
    private static StageData CreateInfiniteStage()
    {
        StageSpec spec = new StageSpec
        {
            StageId = 1,
            Width = 8,
            Height = 8,
            HorizontalTurns = 8,
            VerticalTurns = 6,
            DiagonalTurns = 3,
            RotateTurns = 2,
            Star3MinTurnsLeft = 5,
            Star2MinTurnsLeft = 2
        };

        return CreateStage(InfiniteStagePath, spec);
    }

    private static StageData CreateStage(string path, StageSpec spec)
    {
        StageData stage = LoadOrCreate<StageData>(path);

        SerializedObject so = new SerializedObject(stage);
        so.FindProperty("stageId").intValue = spec.StageId;
        so.FindProperty("width").intValue = spec.Width;
        so.FindProperty("height").intValue = spec.Height;
        so.FindProperty("horizontalTurns").intValue = spec.HorizontalTurns;
        so.FindProperty("verticalTurns").intValue = spec.VerticalTurns;
        so.FindProperty("diagonalTurns").intValue = spec.DiagonalTurns;
        so.FindProperty("rotateTurns").intValue = spec.RotateTurns;
        so.FindProperty("star3MinTurnsLeft").intValue = spec.Star3MinTurnsLeft;
        so.FindProperty("star2MinTurnsLeft").intValue = spec.Star2MinTurnsLeft;

        SerializedProperty goalList = so.FindProperty("goals");
        goalList.arraySize = spec.Goals.Count;

        for (int i = 0; i < spec.Goals.Count; i++)
        {
            SerializedProperty goal = goalList.GetArrayElementAtIndex(i);
            goal.FindPropertyRelative("color").enumValueIndex = (int)spec.Goals[i].color;
            goal.FindPropertyRelative("amount").intValue = spec.Goals[i].amount;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(stage);

        return stage;
    }

    // ───────────── Goal item prefab ─────────────

    private static ObjectiveGoalItem CreateGoalItemPrefab()
    {
        EnsureFolder(Path.GetDirectoryName(GoalItemPrefabPath).Replace('\\', '/'));

        GameObject root = new GameObject("ObjectiveGoalItem", typeof(RectTransform));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(190f, 100f);

        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredWidth = 190f;
        layout.preferredHeight = 100f;

        Image icon = CreateImage(root.transform, "ColorIcon", uiSprite, Color.white);
        SetRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(80f, 80f));

        TMP_Text text = CreateText(root.transform, "RemainingText", "0", 52, TextAlignmentOptions.Left);
        SetRect(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(95f, 0f), new Vector2(95f, 80f));
        text.fontStyle = FontStyles.Bold;

        ObjectiveGoalItem item = root.AddComponent<ObjectiveGoalItem>();
        SetRef(item, "colorIcon", icon);
        SetRef(item, "remainingText", text);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GoalItemPrefabPath);
        Object.DestroyImmediate(root);

        return prefab.GetComponent<ObjectiveGoalItem>();
    }

    // ───────────── Scene ─────────────

    private static void SetupScene(
        StageList stageList,
        StageData infiniteStage,
        ObjectiveGoalItem goalItemPrefab)
    {
        GameObject boardObject = FindRequired("BoardManager");
        BoardManager boardManager = boardObject.GetComponent<BoardManager>();
        BoardInput boardInput = boardObject.GetComponent<BoardInput>();
        ActionInventory inventory = boardObject.GetComponent<ActionInventory>();

        Camera mainCamera = FindRequired("Main Camera").GetComponent<Camera>();
        BoardCameraFitter cameraFitter = GetOrAdd<BoardCameraFitter>(mainCamera.gameObject);

        // GameManager + ObjectiveManager di satu objek.
        GameObject managerObject = GameObject.Find("GameManager") ?? new GameObject("GameManager");
        GameManager gameManager = GetOrAdd<GameManager>(managerObject);
        ObjectiveManager objectiveManager = GetOrAdd<ObjectiveManager>(managerObject);
        ScoreManager scoreManager = GetOrAdd<ScoreManager>(managerObject);
        RunTimer runTimer = GetOrAdd<RunTimer>(managerObject);

        SetRef(objectiveManager, "boardManager", boardManager);
        SetRef(runTimer, "gameManager", gameManager);
        SetRef(scoreManager, "boardManager", boardManager);

        SetRef(gameManager, "stageList", stageList);
        SetRef(gameManager, "infiniteStage", infiniteStage);
        SetRef(gameManager, "boardManager", boardManager);
        SetRef(gameManager, "actionInventory", inventory);
        SetRef(gameManager, "objectiveManager", objectiveManager);
        SetRef(gameManager, "scoreManager", scoreManager);
        SetRef(gameManager, "cameraFitter", cameraFitter);
        SetRef(gameManager, "runTimer", runTimer);

        SetRef(boardInput, "gameManager", gameManager);

        Transform canvas = FindRequired("Canvas").transform;

        SetupTopBar(canvas, gameManager, inventory, objectiveManager, boardManager,
            scoreManager, runTimer, goalItemPrefab);
        SetupBottomBar(canvas);
        SetupRefreshButton(canvas, gameManager);
        RectTransform boardArea = SetupBoardArea(canvas);
        ResultPanel resultPanel = SetupResultPanel(canvas, gameManager, inventory, scoreManager);

        // Urutan di Canvas = urutan gambar: panel hasil < leaderboard < tutorial.
        LeaderboardPanel leaderboard = BlockyLeaderboardSetup.SetupPanel(canvas);
        SetRef(resultPanel, "leaderboardPanel", leaderboard);

        TutorialOverlay overlay = BlockyTutorialSetup.SetupOverlay(canvas);
        GameTutorial tutorial = GetOrAdd<GameTutorial>(managerObject);
        SetRef(tutorial, "gameManager", gameManager);
        SetRef(tutorial, "boardInput", boardInput);
        SetRef(tutorial, "runTimer", runTimer);
        SetRef(tutorial, "overlay", overlay);
        BlockyTutorialSetup.SetGameSteps(tutorial, canvas, boardArea);

        // Kamera menaruh board di area kosong di antara kedua bar.
        SetRef(cameraFitter, "topBar", canvas.Find("TopBar"));
        SetRef(cameraFitter, "bottomBar", canvas.Find("BottomBar"));
    }

    private static void SetupTopBar(
        Transform canvas,
        GameManager gameManager,
        ActionInventory inventory,
        ObjectiveManager objectiveManager,
        BoardManager boardManager,
        ScoreManager scoreManager,
        RunTimer runTimer,
        ObjectiveGoalItem goalItemPrefab)
    {
        RectTransform topBar = GetOrCreateRect(canvas, "TopBar");
        SetRect(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 420f));
        SetupImage(topBar.gameObject, uiSprite, PanelColor);

        // Kiri atas: tombol kembali ke menu.
        RectTransform menuRect = GetOrCreateRect(topBar, "MenuButton");
        SetRect(menuRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(30f, -40f), new Vector2(110f, 110f));
        Button menuButton = SetupIconButton(menuRect, LoadSprite("Assets/Sprites/left_arrow.png"),
            SubPanelColor, 70f);

        // Di sebelahnya: nomor stage ("STAGE 1-5").
        RectTransform stageRect = GetOrCreateRect(topBar, "StageText");
        SetRect(stageRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(160f, -40f), new Vector2(300f, 110f));
        TMP_Text stageText = SetupText(stageRect.gameObject, "STAGE 1-1", 48, TextAlignmentOptions.MidlineLeft);
        stageText.fontStyle = FontStyles.Bold;

        // Tengah atas: preview bintang (digeser kanan memberi ruang teks stage).
        RectTransform starRect = GetOrCreateRect(topBar, "StarPreview");
        SetRect(starRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(70f, -50f), new Vector2(280f, 90f));
        StarDisplay starPreview = SetupStars(starRect, 72f, 12f);

        // Kanan atas: sisa total turn.
        RectTransform turnPanel = GetOrCreateRect(topBar, "TurnPanel");
        SetRect(turnPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-40f, -30f), new Vector2(240f, 130f));
        SetupImage(turnPanel.gameObject, uiSprite, SubPanelColor);

        RectTransform turnLabel = GetOrCreateRect(turnPanel, "Label");
        SetRect(turnLabel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -6f), new Vector2(0f, 40f));
        SetupText(turnLabel.gameObject, "TURNS", 28, TextAlignmentOptions.Center);

        RectTransform turnCount = GetOrCreateRect(turnPanel, "CountText");
        SetRect(turnCount, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 6f), new Vector2(0f, 80f));
        TMP_Text turnsText = SetupText(turnCount.gameObject, "0", 64, TextAlignmentOptions.Center);
        turnsText.fontStyle = FontStyles.Bold;

        // Bawah: Objective Tracker.
        RectTransform objectivePanel = GetOrCreateRect(topBar, "ObjectivePanel");
        SetRect(objectivePanel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 30f), new Vector2(-80f, 200f));
        SetupImage(objectivePanel.gameObject, uiSprite, SubPanelColor);

        RectTransform objectiveLabel = GetOrCreateRect(objectivePanel, "Label");
        SetRect(objectiveLabel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -8f), new Vector2(0f, 44f));
        TMP_Text objectiveLabelText = SetupText(objectiveLabel.gameObject, "OBJECTIVE", 30,
            TextAlignmentOptions.Center);

        // Infinite Mode: skor besar menggantikan daftar target.
        RectTransform scoreRect = GetOrCreateRect(objectivePanel, "ScoreText");
        SetRect(scoreRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -22f), new Vector2(-40f, -64f));
        TMP_Text scoreText = SetupText(scoreRect.gameObject, "0", 96, TextAlignmentOptions.Center);
        scoreText.fontStyle = FontStyles.Bold;
        scoreRect.gameObject.SetActive(false);

        // Infinite Mode: stopwatch di pojok kanan atas panel skor.
        RectTransform timerRect = GetOrCreateRect(objectivePanel, "TimerText");
        SetRect(timerRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-24f, -8f), new Vector2(300f, 44f));
        TMP_Text timerText = SetupText(timerRect.gameObject, "TIME 00:00", 30, TextAlignmentOptions.MidlineRight);
        timerText.fontStyle = FontStyles.Bold;
        timerText.color = new Color(1f, 0.82f, 0.1f);
        timerRect.gameObject.SetActive(false);

        RectTransform goals = GetOrCreateRect(objectivePanel, "Goals");
        SetRect(goals, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -22f), new Vector2(-40f, -64f));
        HorizontalLayoutGroup goalLayout = GetOrAdd<HorizontalLayoutGroup>(goals.gameObject);
        goalLayout.childAlignment = TextAnchor.MiddleCenter;
        goalLayout.spacing = 40f;
        goalLayout.childControlWidth = false;
        goalLayout.childControlHeight = false;
        goalLayout.childForceExpandWidth = false;
        goalLayout.childForceExpandHeight = false;

        // Item goal dibuat saat runtime dari prefab, jadi kosongkan dulu.
        for (int i = goals.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(goals.GetChild(i).gameObject);
        }

        StageInfoHUD hud = GetOrAdd<StageInfoHUD>(topBar.gameObject);
        SetRef(hud, "gameManager", gameManager);
        SetRef(hud, "actionInventory", inventory);
        SetRef(hud, "objectiveManager", objectiveManager);
        SetRef(hud, "boardManager", boardManager);
        SetRef(hud, "scoreManager", scoreManager);
        SetRef(hud, "stageText", stageText);
        SetRef(hud, "turnsText", turnsText);
        SetRef(hud, "starPreview", starPreview);
        SetRef(hud, "goalContainer", goals);
        SetRef(hud, "goalItemPrefab", goalItemPrefab);
        SetRef(hud, "menuButton", menuButton);
        SetRef(hud, "objectiveLabel", objectiveLabelText);
        SetRef(hud, "scoreText", scoreText);
        SetRef(hud, "runTimer", runTimer);
        SetRef(hud, "timerText", timerText);
    }

    // Area kosong di antara Top Bar dan Bottom Bar (tempat board).
    // Tidak terlihat; dipakai tutorial untuk spotlight board.
    private static RectTransform SetupBoardArea(Transform canvas)
    {
        RectTransform area = GetOrCreateRect(canvas, "BoardArea");
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.pivot = new Vector2(0.5f, 0.5f);
        area.offsetMin = new Vector2(20f, 330f);
        area.offsetMax = new Vector2(-20f, -430f);
        area.localScale = Vector3.one;
        area.SetSiblingIndex(0);
        return area;
    }

    private static void SetupBottomBar(Transform canvas)
    {
        RectTransform bottomBar = GetOrCreateRect(canvas, "BottomBar");
        SetRect(bottomBar, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            Vector2.zero, new Vector2(0f, 320f));
        SetupImage(bottomBar.gameObject, uiSprite, PanelColor);

        HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(bottomBar.gameObject);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 24f;

        foreach (ActionTurnSlot slot in bottomBar.GetComponentsInChildren<ActionTurnSlot>(true))
        {
            RectTransform slotRect = (RectTransform)slot.transform;
            slotRect.sizeDelta = new Vector2(220f, 220f);

            Transform icon = slot.transform.Find("Icon");
            if (icon != null)
            {
                SetRect((RectTransform)icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 25f), new Vector2(120f, 120f));
            }

            Transform count = slot.transform.Find("CountText");
            if (count != null)
            {
                SetRect((RectTransform)count, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(200f, 60f));

                TMP_Text countText = count.GetComponent<TMP_Text>();
                if (countText != null)
                {
                    countText.fontSize = 46;
                    countText.fontStyle = FontStyles.Bold;
                    countText.alignment = TextAlignmentOptions.Center;
                }
            }
        }

    }

    // Tombol Refresh (Infinite Mode) di tengah Top Bar, menggantikan
    // preview bintang. Dulu di atas Bottom Bar, tapi bisa menutupi board.
    private static void SetupRefreshButton(Transform canvas, GameManager gameManager)
    {
        Transform topBar = canvas.Find("TopBar");
        Transform refresh = topBar.Find("RefreshButton");

        if (refresh == null)
        {
            Transform bottomBar = canvas.Find("BottomBar");
            refresh = bottomBar != null ? bottomBar.Find("RefreshButton") : null;
        }

        if (refresh == null)
        {
            refresh = GetOrCreateRect(topBar, "RefreshButton");
        }

        refresh.SetParent(topBar, false);

        LayoutElement oldLayout = refresh.GetComponent<LayoutElement>();
        if (oldLayout != null)
        {
            Object.DestroyImmediate(oldLayout);
        }

        RectTransform rect = (RectTransform)refresh;
        SetRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(70f, -35f), new Vector2(240f, 120f));

        Image background = SetupImage(rect.gameObject, uiSprite, ButtonColor);
        Button button = GetOrAdd<Button>(rect.gameObject);
        button.targetGraphic = background;

        RectTransform icon = GetOrCreateRect(rect, "Icon");
        SetRect(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(18f, 0f), new Vector2(90f, 90f));
        Image iconImage = SetupImage(icon.gameObject, LoadSprite("Assets/Sprites/clockwise_arrow.png"), Color.white);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        RectTransform count = GetOrCreateRect(rect, "CountText");
        SetRect(count, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-14f, 0f), new Vector2(120f, 90f));
        TMP_Text countText = SetupText(count.gameObject, "2/2", 46, TextAlignmentOptions.Center);
        countText.fontStyle = FontStyles.Bold;

        // Disembunyikan; HUDManager memunculkannya hanya di Infinite Mode.
        rect.gameObject.SetActive(false);

        HUDManager hudManager = canvas.GetComponent<HUDManager>();
        if (hudManager != null)
        {
            SetRef(hudManager, "refreshButton", rect.gameObject);
            SetRef(hudManager, "refreshCountText", countText);
            SetRef(hudManager, "gameManager", gameManager);
        }
    }

    private static ResultPanel SetupResultPanel(
        Transform canvas,
        GameManager gameManager,
        ActionInventory inventory,
        ScoreManager scoreManager)
    {
        RectTransform panel = GetOrCreateRect(canvas, "ResultPanel");
        panel.SetAsLastSibling();
        SetRect(panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        SetupImage(panel.gameObject, null, new Color(0f, 0f, 0f, 0.7f));
        CanvasGroup group = GetOrAdd<CanvasGroup>(panel.gameObject);
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        // Tinggi kartu diatur ResultPanel saat runtime (Victory lebih tinggi).
        RectTransform card = GetOrCreateRect(panel, "Card");
        SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(860f, 1150f));
        SetupImage(card.gameObject, uiSprite, CardColor);

        RectTransform title = GetOrCreateRect(card, "Title");
        SetRect(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -60f), new Vector2(-60f, 110f));
        TMP_Text titleText = SetupText(title.gameObject, "STAGE CLEAR!", 76, TextAlignmentOptions.Center);
        titleText.fontStyle = FontStyles.Bold;

        RectTransform starRect = GetOrCreateRect(card, "Stars");
        SetRect(starRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -190f), new Vector2(520f, 160f));
        StarDisplay stars = SetupStars(starRect, 150f, 24f);

        // Detail rata atas: 1 baris (Victory) atau 3 baris (skor, waktu, best).
        RectTransform detail = GetOrCreateRect(card, "Detail");
        SetRect(detail, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -370f), new Vector2(-60f, 260f));
        TMP_Text detailText = SetupText(detail.gameObject, "Turns left: 0", 44, TextAlignmentOptions.Top);
        detailText.textWrappingMode = TextWrappingModes.Normal;

        // Penjelasan bintang: 3 baris (3 bintang, 2, 1) + tips bintang berikutnya.
        RectTransform starInfo = GetOrCreateRect(card, "StarInfo");
        SetRect(starInfo, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -450f), new Vector2(-80f, 470f));

        RectTransform rules = GetOrCreateRect(starInfo, "Rules");
        SetRect(rules, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 330f));
        VerticalLayoutGroup rulesLayout = GetOrAdd<VerticalLayoutGroup>(rules.gameObject);
        rulesLayout.spacing = 12f;
        rulesLayout.childAlignment = TextAnchor.UpperCenter;
        rulesLayout.childControlWidth = true;
        rulesLayout.childControlHeight = false;
        rulesLayout.childForceExpandWidth = true;
        rulesLayout.childForceExpandHeight = false;

        StarRuleRow[] ruleRows = new StarRuleRow[StarRating.MaxStars];

        for (int i = 0; i < ruleRows.Length; i++)
        {
            ruleRows[i] = SetupStarRuleRow(rules, StarRating.MaxStars - i);
        }

        RectTransform hint = GetOrCreateRect(starInfo, "Hint");
        SetRect(hint, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            Vector2.zero, new Vector2(0f, 110f));
        TMP_Text hintText = SetupText(hint.gameObject, "Save 2 more turns to earn 3 stars!", 38,
            TextAlignmentOptions.Center);
        hintText.textWrappingMode = TextWrappingModes.Normal;
        hintText.color = new Color(1f, 0.82f, 0.1f);

        RectTransform buttons = GetOrCreateRect(card, "Buttons");
        SetRect(buttons, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 60f), new Vector2(-60f, 140f));
        HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(buttons.gameObject);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 24f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Vector2 buttonSize = new Vector2(230f, 130f);
        Button menu = SetupButton(buttons, "MenuButton", "MENU", buttonSize);
        Button retry = SetupButton(buttons, "RetryButton", "RETRY", buttonSize);
        Button next = SetupButton(buttons, "NextButton", "NEXT", buttonSize);
        Button ranks = SetupButton(buttons, "LeaderboardButton", "RANKS", buttonSize);
        ranks.targetGraphic.color = new Color(1f, 0.55f, 0.2f);
        menu.transform.SetSiblingIndex(0);
        ranks.gameObject.SetActive(false);

        ResultPanel result = GetOrAdd<ResultPanel>(panel.gameObject);
        SetRef(result, "gameManager", gameManager);
        SetRef(result, "actionInventory", inventory);
        SetRef(result, "scoreManager", scoreManager);
        SetRef(result, "card", card);
        SetRef(result, "titleText", titleText);
        SetRef(result, "detailText", detailText);
        SetRef(result, "stars", stars);
        SetRef(result, "retryButton", retry);
        SetRef(result, "nextButton", next);
        SetRef(result, "menuButton", menu);
        SetRef(result, "leaderboardButton", ranks);
        SetRef(result, "starInfoGroup", starInfo.gameObject);
        SetRef(result, "hintText", hintText);
        SetRefArray(result, "starRules", ruleRows);

        return result;
    }

    // Satu baris penjelasan: [kotak transparan] (3 ikon bintang)  6+ turns left
    private static StarRuleRow SetupStarRuleRow(RectTransform parent, int starCount)
    {
        RectTransform row = GetOrCreateRect(parent, $"Rule{starCount}Stars");
        row.sizeDelta = new Vector2(0f, 100f);
        Image background = SetupImage(row.gameObject, uiSprite, new Color(1f, 1f, 1f, 0.06f));
        background.raycastTarget = false;

        RectTransform starRect = GetOrCreateRect(row, "Stars");
        SetRect(starRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(24f, 0f), new Vector2(210f, 70f));
        StarDisplay stars = SetupStars(starRect, 62f, 8f);

        RectTransform label = GetOrCreateRect(row, "Label");
        SetRect(label, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(125f, 0f), new Vector2(-290f, 0f));
        TMP_Text labelText = SetupText(label.gameObject, "0 turns left", 42, TextAlignmentOptions.MidlineLeft);
        labelText.fontStyle = FontStyles.Bold;

        StarRuleRow rule = GetOrAdd<StarRuleRow>(row.gameObject);
        SetRef(rule, "background", background);
        SetRef(rule, "stars", stars);
        SetRef(rule, "label", labelText);

        return rule;
    }

    internal static void SetRefArray(Object target, string field, Object[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty array = so.FindProperty(field);

        if (array == null)
        {
            throw new System.InvalidOperationException(
                $"BlockySceneSetup: field '{field}' tidak ada di {target.GetType().Name}.");
        }

        array.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ───────────── UI helpers ─────────────

    internal static StarDisplay SetupStars(RectTransform parent, float starSize, float spacing)
    {
        HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(parent.gameObject);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = spacing;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Image[] images = new Image[StarRating.MaxStars];

        for (int i = 0; i < images.Length; i++)
        {
            RectTransform star = GetOrCreateRect(parent, $"Star{i + 1}");
            star.sizeDelta = new Vector2(starSize, starSize);
            images[i] = SetupImage(star.gameObject, starSprite, Color.white);
            images[i].preserveAspect = true;
            images[i].raycastTarget = false;
        }

        StarDisplay display = GetOrAdd<StarDisplay>(parent.gameObject);
        SerializedObject so = new SerializedObject(display);
        SerializedProperty array = so.FindProperty("stars");
        array.arraySize = images.Length;

        for (int i = 0; i < images.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        return display;
    }

    internal static Button SetupButton(Transform parent, string name, string label)
    {
        return SetupButton(parent, name, label, new Vector2(320f, 130f));
    }

    internal static Button SetupButton(Transform parent, string name, string label, Vector2 size)
    {
        RectTransform rect = GetOrCreateRect(parent, name);
        rect.sizeDelta = size;

        Image image = SetupImage(rect.gameObject, uiSprite, ButtonColor);
        Button button = GetOrAdd<Button>(rect.gameObject);
        button.targetGraphic = image;

        RectTransform labelRect = GetOrCreateRect(rect, "Label");
        SetRect(labelRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TMP_Text text = SetupText(labelRect.gameObject, label, 48, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.Bold;

        return button;
    }

    // Tombol kotak dengan ikon di tengah (back, panah bab, dll).
    internal static Button SetupIconButton(RectTransform rect, Sprite icon, Color color, float iconSize)
    {
        Image background = SetupImage(rect.gameObject, uiSprite, color);
        Button button = GetOrAdd<Button>(rect.gameObject);
        button.targetGraphic = background;

        RectTransform iconRect = GetOrCreateRect(rect, "Icon");
        SetRect(iconRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(iconSize, iconSize));
        Image iconImage = SetupImage(iconRect.gameObject, icon, Color.white);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        return button;
    }

    internal static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        RectTransform rect = GetOrCreateRect(parent, name);
        return SetupImage(rect.gameObject, sprite, color);
    }

    internal static Image SetupImage(GameObject target, Sprite sprite, Color color)
    {
        Image image = GetOrAdd<Image>(target);
        image.sprite = sprite;
        image.type = sprite == uiSprite && sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        return image;
    }

    internal static TMP_Text CreateText(Transform parent, string name, string value, float size,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = GetOrCreateRect(parent, name);
        return SetupText(rect.gameObject, value, size, alignment);
    }

    internal static TMP_Text SetupText(GameObject target, string value, float size,
        TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(target);

        if (text.font == null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    internal static RectTransform GetOrCreateRect(Transform parent, string name)
    {
        Transform child = parent.Find(name);

        if (child == null)
        {
            GameObject created = new GameObject(name, typeof(RectTransform));
            created.layer = LayerMask.NameToLayer("UI");
            created.transform.SetParent(parent, false);
            child = created.transform;
        }

        return (RectTransform)child;
    }

    internal static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    // ───────────── General helpers ─────────────

    internal static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static GameObject FindRequired(string name)
    {
        GameObject found = GameObject.Find(name);

        if (found == null)
        {
            throw new System.InvalidOperationException($"BlockySceneSetup: '{name}' tidak ditemukan di scene.");
        }

        return found;
    }

    internal static void SetRef(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);

        if (property == null)
        {
            throw new System.InvalidOperationException(
                $"BlockySceneSetup: field '{field}' tidak ada di {target.GetType().Name}.");
        }

        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }

        return asset;
    }

    internal static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
