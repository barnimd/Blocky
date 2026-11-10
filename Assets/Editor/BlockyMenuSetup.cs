using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static BlockySceneSetup;

// Membangun scene MainMenu: menu utama (Play Stage / Infinite Mode)
// dan level selector (1 layar = 1 bab = 12 level, 5 bab).
// Dipanggil dari Blocky > Setup All Scenes.
public static class BlockyMenuSetup
{
    public const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

    private static readonly Color BackgroundColor = new Color(0.1f, 0.12f, 0.26f, 1f);
    private static readonly Color PlayColor = new Color(1f, 0.55f, 0.2f, 1f);
    private static readonly Color InfiniteColor = new Color(0.2f, 0.6f, 1f, 1f);
    private static readonly Color StarColor = new Color(1f, 0.82f, 0.1f, 1f);

    // Warna sama dengan BoardManager (Red, Blue, Green, Yellow, Purple).
    private static readonly Color[] BlockColors =
    {
        new Color(1f, 0.25f, 0.35f),
        new Color(0.15f, 0.75f, 1f),
        new Color(0.55f, 0.9f, 0.2f),
        new Color(1f, 0.8f, 0.15f),
        new Color(0.7f, 0.3f, 1f)
    };

    public static void SetupMenuScene(StageList stageList)
    {
        LoadBuiltinSprites();

        var scene = System.IO.File.Exists(MenuScenePath)
            ? EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupCamera();
        SetupEventSystem();
        Transform canvas = SetupCanvas();

        LevelSelectUI levelSelect = SetupLevelSelect(canvas, stageList);
        MainMenuUI mainMenu = SetupMainPanel(canvas, stageList, levelSelect);

        SetRef(levelSelect, "mainMenu", mainMenu);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    // ───────────── Dasar scene ─────────────

    private static void SetupCamera()
    {
        GameObject cameraObject = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        Camera camera = GetOrAdd<Camera>(cameraObject);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BackgroundColor;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void SetupEventSystem()
    {
        GameObject eventObject = GameObject.Find("EventSystem") ?? new GameObject("EventSystem");
        GetOrAdd<EventSystem>(eventObject);
        GetOrAdd<InputSystemUIInputModule>(eventObject);
    }

    private static Transform SetupCanvas()
    {
        GameObject canvasObject = GameObject.Find("Canvas") ?? new GameObject("Canvas", typeof(RectTransform));
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = GetOrAdd<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Sama dengan GameScene: portrait 1080x1920.
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAdd<GraphicRaycaster>(canvasObject);

        return canvasObject.transform;
    }

    // ───────────── Menu utama ─────────────

    private static MainMenuUI SetupMainPanel(Transform canvas, StageList stageList, LevelSelectUI levelSelect)
    {
        RectTransform panel = GetOrCreateRect(canvas, "MainPanel");
        Stretch(panel);

        // Deretan 5 blok warna di atas judul (naik-turun pelan).
        RectTransform blocks = GetOrCreateRect(panel, "TitleBlocks");
        SetRect(blocks, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -300f), new Vector2(700f, 130f));
        HorizontalLayoutGroup blockLayout = GetOrAdd<HorizontalLayoutGroup>(blocks.gameObject);
        ConfigureLayout(blockLayout, 20f);

        for (int i = 0; i < BlockColors.Length; i++)
        {
            RectTransform block = GetOrCreateRect(blocks, $"Block{i + 1}");
            block.sizeDelta = new Vector2(110f, 110f);
            block.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 6f : -6f));
            CreateOutlinedSquare(block, BlockColors[i]);
        }

        RectTransform title = GetOrCreateRect(panel, "Title");
        SetRect(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -480f), new Vector2(-80f, 150f));
        TMP_Text titleText = SetupText(title.gameObject, "COLOR SHIFT", 130, TextAlignmentOptions.Center);
        titleText.fontStyle = FontStyles.Bold;

        RectTransform subtitle = GetOrCreateRect(panel, "Subtitle");
        SetRect(subtitle, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -630f), new Vector2(-80f, 80f));
        TMP_Text subtitleText = SetupText(subtitle.gameObject, "P U Z Z L E", 56, TextAlignmentOptions.Center);
        subtitleText.color = new Color(1f, 1f, 1f, 0.7f);

        // Tombol utama.
        Vector2 bigButton = new Vector2(680f, 180f);

        Button play = SetupButton(panel, "PlayButton", "PLAY STAGE", bigButton);
        SetRect((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), bigButton);
        StyleBigButton(play, PlayColor);

        TMP_Text totalStars = SetupStarCounter(panel, "TotalStars", new Vector2(0f, -240f));

        Button infinite = SetupButton(panel, "InfiniteButton", "INFINITE MODE", bigButton);
        SetRect((RectTransform)infinite.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -420f), bigButton);
        StyleBigButton(infinite, InfiniteColor);

        RectTransform best = GetOrCreateRect(panel, "InfiniteBest");
        SetRect(best, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -560f), new Vector2(600f, 60f));
        TMP_Text bestText = SetupText(best.gameObject, "BEST 0", 40, TextAlignmentOptions.Center);
        bestText.color = new Color(1f, 1f, 1f, 0.75f);

        MainMenuUI menu = GetOrAdd<MainMenuUI>(canvas.gameObject);
        SetRef(menu, "stageList", stageList);
        SetRef(menu, "mainPanel", panel.gameObject);
        SetRef(menu, "levelSelect", levelSelect);
        SetRef(menu, "playButton", play);
        SetRef(menu, "infiniteButton", infinite);
        SetRef(menu, "totalStarsText", totalStars);
        SetRef(menu, "infiniteBestText", bestText);
        SetRef(menu, "titleBlocks", blocks);

        return menu;
    }

    // ───────────── Level selector ─────────────

    private static LevelSelectUI SetupLevelSelect(Transform canvas, StageList stageList)
    {
        RectTransform panel = GetOrCreateRect(canvas, "LevelSelectPanel");
        Stretch(panel);

        // Header berwarna bab.
        RectTransform header = GetOrCreateRect(panel, "Header");
        SetRect(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 440f));
        Image headerImage = SetupImage(header.gameObject, uiSprite, PlayColor);

        RectTransform backRect = GetOrCreateRect(header, "BackButton");
        SetRect(backRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(30f, -40f), new Vector2(120f, 120f));
        Button back = SetupIconButton(backRect, LoadSprite("Assets/Sprites/left_arrow.png"),
            new Color(0f, 0f, 0f, 0.2f), 76f);

        RectTransform select = GetOrCreateRect(header, "SelectLabel");
        SetRect(select, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -60f), new Vector2(-340f, 80f));
        SetupText(select.gameObject, "SELECT STAGE", 44, TextAlignmentOptions.Center);

        RectTransform chapterTitle = GetOrCreateRect(header, "ChapterTitle");
        SetRect(chapterTitle, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -165f), new Vector2(-80f, 100f));
        TMP_Text chapterTitleText = SetupText(chapterTitle.gameObject, "CHAPTER 1", 84, TextAlignmentOptions.Center);
        chapterTitleText.fontStyle = FontStyles.Bold;

        RectTransform chapterName = GetOrCreateRect(header, "ChapterName");
        SetRect(chapterName, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -265f), new Vector2(-80f, 70f));
        TMP_Text chapterNameText = SetupText(chapterName.gameObject, "SUNNY START", 46, TextAlignmentOptions.Center);
        chapterNameText.color = new Color(1f, 1f, 1f, 0.85f);

        TMP_Text chapterStars = SetupStarCounter(header, "ChapterStars", Vector2.zero);
        RectTransform chapterStarsRect = (RectTransform)chapterStars.transform.parent;
        SetRect(chapterStarsRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -345f), new Vector2(360f, 70f));

        // Grid 3 kolom x 4 baris = 12 level.
        RectTransform grid = GetOrCreateRect(panel, "LevelGrid");
        SetRect(grid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -110f), new Vector2(880f, 1240f));
        GridLayoutGroup gridLayout = GetOrAdd<GridLayoutGroup>(grid.gameObject);
        gridLayout.cellSize = new Vector2(260f, 280f);
        gridLayout.spacing = new Vector2(40f, 40f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        gridLayout.childAlignment = TextAnchor.UpperCenter;

        LevelButton[] levelButtons = new LevelButton[StageList.StagesPerChapter];

        for (int i = 0; i < levelButtons.Length; i++)
        {
            levelButtons[i] = SetupLevelButton(grid, i);
        }

        // Penutup saat bab belum terbuka.
        RectTransform locked = GetOrCreateRect(panel, "ChapterLocked");
        SetRect(locked, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -110f), new Vector2(920f, 1280f));
        SetupImage(locked.gameObject, uiSprite, new Color(0.05f, 0.06f, 0.14f, 0.85f));

        RectTransform lockedLabel = GetOrCreateRect(locked, "Label");
        SetRect(lockedLabel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-80f, 0f));
        TMP_Text lockedText = SetupText(lockedLabel.gameObject, "Clear the previous chapter to unlock", 54,
            TextAlignmentOptions.Center);
        lockedText.textWrappingMode = TextWrappingModes.Normal;
        lockedText.fontStyle = FontStyles.Bold;
        locked.gameObject.SetActive(false);

        // Navigasi bab: panah kiri/kanan + titik indikator.
        Vector2 arrowSize = new Vector2(150f, 150f);

        RectTransform prevRect = GetOrCreateRect(panel, "PreviousChapter");
        SetRect(prevRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(40f, 60f), arrowSize);
        Button previous = SetupIconButton(prevRect, LoadSprite("Assets/Sprites/left_arrow.png"),
            SubPanelColor, 96f);

        RectTransform nextRect = GetOrCreateRect(panel, "NextChapter");
        SetRect(nextRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-40f, 60f), arrowSize);
        Button next = SetupIconButton(nextRect, LoadSprite("Assets/Sprites/right_arrow.png"),
            SubPanelColor, 96f);

        RectTransform dots = GetOrCreateRect(panel, "ChapterDots");
        SetRect(dots, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 115f), new Vector2(400f, 40f));
        HorizontalLayoutGroup dotLayout = GetOrAdd<HorizontalLayoutGroup>(dots.gameObject);
        ConfigureLayout(dotLayout, 24f);

        int chapterCount = Mathf.Max(1, stageList.ChapterCount);
        Image[] dotImages = new Image[chapterCount];

        for (int i = 0; i < chapterCount; i++)
        {
            RectTransform dot = GetOrCreateRect(dots, $"Dot{i + 1}");
            dot.sizeDelta = new Vector2(30f, 30f);
            dotImages[i] = SetupImage(dot.gameObject, knobSprite, Color.white);
        }

        LevelSelectUI levelSelect = GetOrAdd<LevelSelectUI>(panel.gameObject);
        SetRef(levelSelect, "stageList", stageList);
        SetRef(levelSelect, "backButton", back);
        SetRef(levelSelect, "previousButton", previous);
        SetRef(levelSelect, "nextButton", next);
        SetRef(levelSelect, "headerBackground", headerImage);
        SetRef(levelSelect, "chapterTitleText", chapterTitleText);
        SetRef(levelSelect, "chapterNameText", chapterNameText);
        SetRef(levelSelect, "chapterStarsText", chapterStars);
        SetRef(levelSelect, "chapterLockedPanel", locked.gameObject);
        SetRef(levelSelect, "chapterLockedText", lockedText);
        SetArray(levelSelect, "levelButtons", levelButtons);
        SetArray(levelSelect, "chapterDots", dotImages);

        return levelSelect;
    }

    private static LevelButton SetupLevelButton(RectTransform grid, int index)
    {
        RectTransform rect = GetOrCreateRect(grid, $"Level{index + 1}");

        Image background = SetupImage(rect.gameObject, uiSprite, PlayColor);
        Button button = GetOrAdd<Button>(rect.gameObject);
        button.targetGraphic = background;

        // Bayangan tipis di bawah tombol supaya terlihat "timbul".
        Shadow shadow = GetOrAdd<Shadow>(rect.gameObject);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(0f, -10f);

        RectTransform number = GetOrCreateRect(rect, "Number");
        SetRect(number, new Vector2(0f, 0.3f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        TMP_Text numberText = SetupText(number.gameObject, (index + 1).ToString(), 110, TextAlignmentOptions.Center);
        numberText.fontStyle = FontStyles.Bold;

        RectTransform starRect = GetOrCreateRect(rect, "Stars");
        SetRect(starRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 24f), new Vector2(220f, 56f));
        StarDisplay stars = SetupStars(starRect, 52f, 8f);

        RectTransform lockRect = GetOrCreateRect(rect, "LockLabel");
        SetRect(lockRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 26f), new Vector2(240f, 50f));
        TMP_Text lockText = SetupText(lockRect.gameObject, "LOCKED", 32, TextAlignmentOptions.Center);
        lockText.color = new Color(1f, 1f, 1f, 0.45f);
        lockRect.gameObject.SetActive(false);

        LevelButton levelButton = GetOrAdd<LevelButton>(rect.gameObject);
        SetRef(levelButton, "button", button);
        SetRef(levelButton, "background", background);
        SetRef(levelButton, "numberText", numberText);
        SetRef(levelButton, "stars", stars);
        SetRef(levelButton, "lockLabel", lockRect.gameObject);

        return levelButton;
    }

    // ───────────── Helper ─────────────

    // Ikon bintang (lingkaran kuning) + teks "0 / 36".
    private static TMP_Text SetupStarCounter(Transform parent, string name, Vector2 position)
    {
        RectTransform root = GetOrCreateRect(parent, name);
        SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            position, new Vector2(360f, 70f));
        HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(root.gameObject);
        ConfigureLayout(layout, 14f);

        RectTransform icon = GetOrCreateRect(root, "Icon");
        icon.sizeDelta = new Vector2(52f, 52f);
        SetupImage(icon.gameObject, knobSprite, StarColor).raycastTarget = false;

        RectTransform count = GetOrCreateRect(root, "Count");
        count.sizeDelta = new Vector2(240f, 70f);
        TMP_Text text = SetupText(count.gameObject, "0 / 0", 46, TextAlignmentOptions.MidlineLeft);
        text.fontStyle = FontStyles.Bold;

        return text;
    }

    // Kotak berwarna dengan outline lebih gelap (gaya blok di board).
    private static void CreateOutlinedSquare(RectTransform rect, Color color)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        SetupImage(rect.gameObject, uiSprite, Color.HSVToRGB(h, s, v * 0.65f)).raycastTarget = false;

        RectTransform fill = GetOrCreateRect(rect, "Fill");
        SetRect(fill, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, -18f));
        SetupImage(fill.gameObject, uiSprite, color).raycastTarget = false;
    }

    private static void StyleBigButton(Button button, Color color)
    {
        button.targetGraphic.color = color;

        Shadow shadow = GetOrAdd<Shadow>(button.gameObject);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(0f, -12f);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.fontSize = 64;
        }
    }

    private static void ConfigureLayout(HorizontalLayoutGroup layout, float spacing)
    {
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = spacing;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private static void Stretch(RectTransform rect)
    {
        SetRect(rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    private static void SetArray(Object target, string field, Object[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty array = so.FindProperty(field);
        array.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
