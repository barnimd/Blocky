using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BlockySceneSetup;

// Membangun overlay tutorial (dipakai MainMenu & GameScene) dan
// mengisi teks setiap langkah tutorial.
// Mau mengubah teks tutorial? Ubah di sini, lalu jalankan
// Blocky > Setup All Scenes supaya scene ikut ter-update.
public static class BlockyTutorialSetup
{
    private static readonly Color DimColor = new Color(0.02f, 0.03f, 0.08f, 0.78f);
    private static readonly Color HighlightColor = new Color(1f, 0.82f, 0.1f, 1f);
    private static readonly Color TitleColor = new Color(1f, 0.82f, 0.1f, 1f);
    private static readonly Color NextColor = new Color(1f, 0.55f, 0.2f, 1f);

    // ───────────── Teks tutorial ─────────────

    // Menu utama (pertama kali buka game).
    public static void SetMenuSteps(MenuTutorial tutorial, Transform canvas)
    {
        RectTransform play = Find(canvas, "MainPanel/PlayButton");
        RectTransform infinite = Find(canvas, "MainPanel/InfiniteButton");

        SetSteps(tutorial, "introSteps", new[]
        {
            new TutorialStep(
                "WELCOME TO COLOR SHIFT!",
                "Swap and rotate colorful blocks to line up 3 or more of the same color. " +
                "There are two ways to play.",
                null),
            new TutorialStep(
                "STAGE MODE",
                "Clear the color objective before your turns run out. " +
                "60 stages in 5 chapters. Finish with turns to spare to earn up to 3 stars.",
                play),
            new TutorialStep(
                "INFINITE MODE",
                "No objective. Score as high as you can until the board locks, " +
                "beat the clock and climb the global leaderboard.",
                infinite),
            new TutorialStep(
                "LET'S LEARN THE BASICS",
                "We'll start with Stage 1-1. Follow the tips and clear your first stage!",
                null)
        });
    }

    // GameScene: Stage 1-1 dan Infinite Mode (masing-masing pertama kali).
    public static void SetGameSteps(GameTutorial tutorial, Transform canvas, RectTransform boardArea)
    {
        RectTransform objective = Find(canvas, "TopBar/ObjectivePanel");
        RectTransform turns = Find(canvas, "TopBar/TurnPanel");
        RectTransform stars = Find(canvas, "TopBar/StarPreview");
        RectTransform actions = Find(canvas, "BottomBar");
        RectTransform refresh = Find(canvas, "TopBar/RefreshButton");

        SetSteps(tutorial, "stageSteps", new[]
        {
            new TutorialStep(
                "THE BOARD",
                "Line up 3 or more blocks of the same color in a row or column to clear them. " +
                "New blocks fall in, and chain reactions make combos!",
                boardArea, TutorialCardPlacement.Bottom),
            new TutorialStep(
                "OBJECTIVE",
                "Clear this many blocks of each color to win. " +
                "The numbers count down as you clear them.",
                objective),
            new TutorialStep(
                "TURNS LEFT",
                "Your total moves. Every move costs 1 turn, even if nothing matches. " +
                "Finish the objective before it hits 0!",
                turns),
            new TutorialStep(
                "STARS",
                "The more turns you have left when you win, the more stars you get. " +
                "Watch them drop as you spend turns.",
                stars),
            new TutorialStep(
                "ACTION TURNS",
                "Each move type has its own limit: Horizontal, Vertical, Diagonal and Rotate. " +
                "A faded slot means that move is used up.",
                actions),
            new TutorialStep(
                "HOW TO MOVE",
                "SWAP: drag a block onto a neighbor (diagonal too) or tap two blocks.\n" +
                "ROTATE: slide across 3 blocks in an L-shape to spin a 2x2 group.",
                boardArea, TutorialCardPlacement.Bottom),
            new TutorialStep(
                "YOUR TURN!",
                "Clear the objective to win Stage 1-1. Good luck!",
                null)
        });

        SetSteps(tutorial, "infiniteSteps", new[]
        {
            new TutorialStep(
                "SCORE & TIME",
                "Every block you clear scores points, and combos multiply them. " +
                "The clock runs until your run ends.",
                objective),
            new TutorialStep(
                "REFRESH",
                "Out of action turns? Tap Refresh to refill them (2 per run). " +
                "After that, you get a random set of turns instead.",
                refresh),
            new TutorialStep(
                "GAME OVER",
                "The run ends when no move can make a match. " +
                "Then tap RANKS to submit your score to the global leaderboard!",
                boardArea, TutorialCardPlacement.Bottom)
        });
    }

    // ───────────── Overlay ─────────────

    public static TutorialOverlay SetupOverlay(Transform canvas)
    {
        RectTransform root = GetOrCreateRect(canvas, "TutorialOverlay");
        root.SetAsLastSibling();
        SetRect(root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // Transparan tapi menangkap klik: tombol di belakang tidak bisa ditekan.
        SetupImage(root.gameObject, null, new Color(0f, 0f, 0f, 0f));

        CanvasGroup group = GetOrAdd<CanvasGroup>(root.gameObject);
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        RectTransform dimTop = CreateDim(root, "DimTop");
        RectTransform dimBottom = CreateDim(root, "DimBottom");
        RectTransform dimLeft = CreateDim(root, "DimLeft");
        RectTransform dimRight = CreateDim(root, "DimRight");
        RectTransform frame = CreateHighlightFrame(root);
        RectTransform card = CreateCard(root,
            out TMP_Text title, out TMP_Text body, out TMP_Text step,
            out Button next, out TMP_Text nextLabel, out Button skip);

        TutorialOverlay overlay = GetOrAdd<TutorialOverlay>(root.gameObject);
        SetRef(overlay, "dimTop", dimTop);
        SetRef(overlay, "dimBottom", dimBottom);
        SetRef(overlay, "dimLeft", dimLeft);
        SetRef(overlay, "dimRight", dimRight);
        SetRef(overlay, "highlightFrame", frame);
        SetRef(overlay, "card", card);
        SetRef(overlay, "titleText", title);
        SetRef(overlay, "bodyText", body);
        SetRef(overlay, "stepText", step);
        SetRef(overlay, "nextButton", next);
        SetRef(overlay, "nextLabel", nextLabel);
        SetRef(overlay, "skipButton", skip);

        return overlay;
    }

    private static RectTransform CreateDim(RectTransform root, string name)
    {
        RectTransform dim = GetOrCreateRect(root, name);
        SetRect(dim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            Vector2.zero, Vector2.zero);
        SetupImage(dim.gameObject, null, DimColor);
        return dim;
    }

    // Bingkai kuning dari 4 garis tipis yang menempel di tepi.
    private static RectTransform CreateHighlightFrame(RectTransform root)
    {
        RectTransform frame = GetOrCreateRect(root, "HighlightFrame");
        SetRect(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            Vector2.zero, Vector2.zero);

        const float thickness = 8f;
        CreateEdge(frame, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, thickness));
        CreateEdge(frame, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, thickness));
        CreateEdge(frame, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(thickness, 0f));
        CreateEdge(frame, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(thickness, 0f));

        frame.gameObject.SetActive(false);
        return frame;
    }

    private static void CreateEdge(RectTransform frame, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 size)
    {
        RectTransform edge = GetOrCreateRect(frame, name);
        SetRect(edge, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        SetupImage(edge.gameObject, null, HighlightColor).raycastTarget = false;
    }

    private static RectTransform CreateCard(RectTransform root,
        out TMP_Text title, out TMP_Text body, out TMP_Text step,
        out Button next, out TMP_Text nextLabel, out Button skip)
    {
        RectTransform card = GetOrCreateRect(root, "Card");
        SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(940f, 600f));
        SetupImage(card.gameObject, uiSprite, CardColor);

        Shadow shadow = GetOrAdd<Shadow>(card.gameObject);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(0f, -12f);

        // Garis aksen kuning di atas kartu.
        RectTransform accent = GetOrCreateRect(card, "Accent");
        SetRect(accent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 12f));
        SetupImage(accent.gameObject, null, TitleColor).raycastTarget = false;

        RectTransform titleRect = GetOrCreateRect(card, "Title");
        SetRect(titleRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-70f, -40f), new Vector2(-220f, 80f));
        title = SetupText(titleRect.gameObject, "TITLE", 58, TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold;
        title.color = TitleColor;
        title.enableAutoSizing = true;
        title.fontSizeMin = 36f;
        title.fontSizeMax = 58f;

        RectTransform stepRect = GetOrCreateRect(card, "StepText");
        SetRect(stepRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-40f, -40f), new Vector2(140f, 80f));
        step = SetupText(stepRect.gameObject, "1 / 7", 32, TextAlignmentOptions.MidlineRight);
        step.color = new Color(1f, 1f, 1f, 0.55f);

        RectTransform bodyRect = GetOrCreateRect(card, "Body");
        SetRect(bodyRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 35f), new Vector2(-100f, -330f));
        body = SetupText(bodyRect.gameObject, "Body", 40, TextAlignmentOptions.TopLeft);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.enableAutoSizing = true;
        body.fontSizeMin = 28f;
        body.fontSizeMax = 40f;
        body.lineSpacing = 6f;

        RectTransform skipRect = GetOrCreateRect(card, "SkipButton");
        SetRect(skipRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(50f, 40f), new Vector2(220f, 110f));
        skip = SetupButton(card, "SkipButton", "SKIP", new Vector2(220f, 110f));
        skip.targetGraphic.color = SubPanelColor;
        skip.GetComponentInChildren<TMP_Text>().fontSize = 40;

        RectTransform nextRect = GetOrCreateRect(card, "NextButton");
        SetRect(nextRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-50f, 40f), new Vector2(340f, 110f));
        next = SetupButton(card, "NextButton", "NEXT", new Vector2(340f, 110f));
        next.targetGraphic.color = NextColor;
        nextLabel = next.GetComponentInChildren<TMP_Text>();

        return card;
    }

    // ───────────── Helper ─────────────

    private static RectTransform Find(Transform canvas, string path)
    {
        Transform found = canvas.Find(path);

        if (found == null)
        {
            Debug.LogWarning($"BlockyTutorialSetup: '{path}' tidak ditemukan, langkah tanpa spotlight.");
        }

        return found as RectTransform;
    }

    private static void SetSteps(Object target, string field, TutorialStep[] steps)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty array = so.FindProperty(field);
        array.arraySize = steps.Length;

        for (int i = 0; i < steps.Length; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("title").stringValue = steps[i].title;
            element.FindPropertyRelative("body").stringValue = steps[i].body;
            element.FindPropertyRelative("target").objectReferenceValue = steps[i].target;
            element.FindPropertyRelative("placement").enumValueIndex = (int)steps[i].placement;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
