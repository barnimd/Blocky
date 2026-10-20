using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Membuat stage asset, prefab UI, dan menyusun HUD + wiring di GameScene.
// Aman dijalankan berulang kali: objek yang sudah ada dipakai ulang.
// Menu: Blocky > Setup Game Scene
// Batch: -executeMethod BlockySceneSetup.RunBatch
public static class BlockySceneSetup
{
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const string StageFolder = "Assets/Data/Stages";
    private const string StageListPath = "Assets/Data/StageList.asset";
    private const string GoalItemPrefabPath = "Assets/Prefabs/UI/ObjectiveGoalItem.prefab";

    private static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.2f, 0.9f);
    private static readonly Color SubPanelColor = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color CardColor = new Color(0.12f, 0.14f, 0.28f, 1f);
    private static readonly Color ButtonColor = new Color(0.2f, 0.6f, 1f, 1f);

    private static Sprite uiSprite;
    private static Sprite knobSprite;

    [MenuItem("Blocky/Setup Game Scene")]
    public static void Run()
    {
        uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        StageList stageList = CreateStageAssets();
        ObjectiveGoalItem goalItemPrefab = CreateGoalItemPrefab();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        SetupScene(stageList, goalItemPrefab);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        AssetDatabase.SaveAssets();
        Debug.Log("BlockySceneSetup: selesai.");
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

        // Angka sementara untuk di-tuning. Stage 1 = contoh JSON di GDD.
        StageData[] stages =
        {
            CreateStage(1, 8, 8,
                new[] { (BlockColor.Blue, 15), (BlockColor.Red, 10) },
                8, 6, 3, 2, 5, 2),
            CreateStage(2, 8, 8,
                new[] { (BlockColor.Green, 20), (BlockColor.Yellow, 15) },
                7, 5, 3, 2, 5, 2),
            CreateStage(3, 9, 9,
                new[] { (BlockColor.Purple, 25), (BlockColor.Blue, 20), (BlockColor.Red, 15) },
                8, 8, 4, 3, 7, 3),
        };

        StageList stageList = LoadOrCreate<StageList>(StageListPath);
        SerializedObject so = new SerializedObject(stageList);
        SerializedProperty list = so.FindProperty("stages");
        list.arraySize = stages.Length;

        for (int i = 0; i < stages.Length; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(stageList);

        return stageList;
    }

    private static StageData CreateStage(
        int id,
        int width,
        int height,
        (BlockColor color, int amount)[] goals,
        int horizontal,
        int vertical,
        int diagonal,
        int rotate,
        int star3,
        int star2)
    {
        string path = $"{StageFolder}/Stage_{id:000}.asset";
        StageData stage = LoadOrCreate<StageData>(path);

        SerializedObject so = new SerializedObject(stage);
        so.FindProperty("stageId").intValue = id;
        so.FindProperty("width").intValue = width;
        so.FindProperty("height").intValue = height;
        so.FindProperty("horizontalTurns").intValue = horizontal;
        so.FindProperty("verticalTurns").intValue = vertical;
        so.FindProperty("diagonalTurns").intValue = diagonal;
        so.FindProperty("rotateTurns").intValue = rotate;
        so.FindProperty("star3MinTurnsLeft").intValue = star3;
        so.FindProperty("star2MinTurnsLeft").intValue = star2;

        SerializedProperty goalList = so.FindProperty("goals");
        goalList.arraySize = goals.Length;

        for (int i = 0; i < goals.Length; i++)
        {
            SerializedProperty goal = goalList.GetArrayElementAtIndex(i);
            goal.FindPropertyRelative("color").enumValueIndex = (int)goals[i].color;
            goal.FindPropertyRelative("amount").intValue = goals[i].amount;
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

    private static void SetupScene(StageList stageList, ObjectiveGoalItem goalItemPrefab)
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

        SetRef(objectiveManager, "boardManager", boardManager);

        SetRef(gameManager, "stageList", stageList);
        SetRef(gameManager, "boardManager", boardManager);
        SetRef(gameManager, "actionInventory", inventory);
        SetRef(gameManager, "objectiveManager", objectiveManager);
        SetRef(gameManager, "cameraFitter", cameraFitter);

        SetRef(boardInput, "gameManager", gameManager);

        Transform canvas = FindRequired("Canvas").transform;

        SetupTopBar(canvas, gameManager, inventory, objectiveManager, boardManager, goalItemPrefab);
        SetupBottomBar(canvas);
        SetupResultPanel(canvas, gameManager, inventory);

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
        ObjectiveGoalItem goalItemPrefab)
    {
        RectTransform topBar = GetOrCreateRect(canvas, "TopBar");
        SetRect(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 420f));
        SetupImage(topBar.gameObject, uiSprite, PanelColor);

        // Kiri atas: nomor stage.
        RectTransform stageRect = GetOrCreateRect(topBar, "StageText");
        SetRect(stageRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -40f), new Vector2(360f, 110f));
        TMP_Text stageText = SetupText(stageRect.gameObject, "STAGE 1", 60, TextAlignmentOptions.MidlineLeft);
        stageText.fontStyle = FontStyles.Bold;

        // Tengah atas: preview bintang.
        RectTransform starRect = GetOrCreateRect(topBar, "StarPreview");
        SetRect(starRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -50f), new Vector2(300f, 90f));
        StarDisplay starPreview = SetupStars(starRect, 80f, 14f);

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
        SetupText(objectiveLabel.gameObject, "OBJECTIVE", 30, TextAlignmentOptions.Center);

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
        SetRef(hud, "stageText", stageText);
        SetRef(hud, "turnsText", turnsText);
        SetRef(hud, "starPreview", starPreview);
        SetRef(hud, "goalContainer", goals);
        SetRef(hud, "goalItemPrefab", goalItemPrefab);
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

        // Refresh (Infinite Mode) tidak ikut layout, dan disembunyikan HUDManager.
        Transform refresh = bottomBar.Find("RefreshButton");
        if (refresh != null)
        {
            GetOrAdd<LayoutElement>(refresh.gameObject).ignoreLayout = true;
            SetRect((RectTransform)refresh, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 0f), new Vector2(-30f, 20f), new Vector2(140f, 140f));

            HUDManager hudManager = canvas.GetComponent<HUDManager>();
            if (hudManager != null)
            {
                SetRef(hudManager, "refreshButton", refresh.gameObject);
            }
        }
    }

    private static void SetupResultPanel(Transform canvas, GameManager gameManager, ActionInventory inventory)
    {
        RectTransform panel = GetOrCreateRect(canvas, "ResultPanel");
        panel.SetAsLastSibling();
        SetRect(panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        SetupImage(panel.gameObject, null, new Color(0f, 0f, 0f, 0.7f));
        CanvasGroup group = GetOrAdd<CanvasGroup>(panel.gameObject);
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        RectTransform card = GetOrCreateRect(panel, "Card");
        SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(820f, 880f));
        SetupImage(card.gameObject, uiSprite, CardColor);

        RectTransform title = GetOrCreateRect(card, "Title");
        SetRect(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -60f), new Vector2(-60f, 110f));
        TMP_Text titleText = SetupText(title.gameObject, "STAGE CLEAR!", 76, TextAlignmentOptions.Center);
        titleText.fontStyle = FontStyles.Bold;

        RectTransform starRect = GetOrCreateRect(card, "Stars");
        SetRect(starRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -210f), new Vector2(500f, 150f));
        StarDisplay stars = SetupStars(starRect, 140f, 30f);

        RectTransform detail = GetOrCreateRect(card, "Detail");
        SetRect(detail, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -400f), new Vector2(-60f, 80f));
        TMP_Text detailText = SetupText(detail.gameObject, "Turns left: 0", 44, TextAlignmentOptions.Center);

        RectTransform buttons = GetOrCreateRect(card, "Buttons");
        SetRect(buttons, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 70f), new Vector2(-60f, 140f));
        HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(buttons.gameObject);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 40f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Button retry = SetupButton(buttons, "RetryButton", "RETRY");
        Button next = SetupButton(buttons, "NextButton", "NEXT");

        ResultPanel result = GetOrAdd<ResultPanel>(panel.gameObject);
        SetRef(result, "gameManager", gameManager);
        SetRef(result, "actionInventory", inventory);
        SetRef(result, "titleText", titleText);
        SetRef(result, "detailText", detailText);
        SetRef(result, "stars", stars);
        SetRef(result, "retryButton", retry);
        SetRef(result, "nextButton", next);
    }

    // ───────────── UI helpers ─────────────

    private static StarDisplay SetupStars(RectTransform parent, float starSize, float spacing)
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
            images[i] = SetupImage(star.gameObject, knobSprite, Color.white);
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

    private static Button SetupButton(Transform parent, string name, string label)
    {
        RectTransform rect = GetOrCreateRect(parent, name);
        rect.sizeDelta = new Vector2(320f, 130f);

        Image image = SetupImage(rect.gameObject, uiSprite, ButtonColor);
        Button button = GetOrAdd<Button>(rect.gameObject);
        button.targetGraphic = image;

        RectTransform labelRect = GetOrCreateRect(rect, "Label");
        SetRect(labelRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TMP_Text text = SetupText(labelRect.gameObject, label, 48, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.Bold;

        return button;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        RectTransform rect = GetOrCreateRect(parent, name);
        return SetupImage(rect.gameObject, sprite, color);
    }

    private static Image SetupImage(GameObject target, Sprite sprite, Color color)
    {
        Image image = GetOrAdd<Image>(target);
        image.sprite = sprite;
        image.type = sprite == uiSprite && sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float size,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = GetOrCreateRect(parent, name);
        return SetupText(rect.gameObject, value, size, alignment);
    }

    private static TMP_Text SetupText(GameObject target, string value, float size,
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

    private static RectTransform GetOrCreateRect(Transform parent, string name)
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

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
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

    private static T GetOrAdd<T>(GameObject target) where T : Component
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

    private static void SetRef(Object target, string field, Object value)
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

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }

        return asset;
    }

    private static void EnsureFolder(string folder)
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
