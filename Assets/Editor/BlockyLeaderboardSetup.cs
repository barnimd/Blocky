using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BlockySceneSetup;

// Membangun panel leaderboard (dipakai MainMenu & GameScene).
// Isi kartu disusun VerticalLayoutGroup: saat bagian "submit"
// disembunyikan, daftar otomatis naik dan kartu ikut memendek.
public static class BlockyLeaderboardSetup
{
    public const int RowCount = 10;

    private static readonly Color RowColor = new Color(1f, 1f, 1f, 0.05f);
    private static readonly Color GoldColor = new Color(1f, 0.82f, 0.1f, 1f);
    private static readonly Color SubmitColor = new Color(1f, 0.55f, 0.2f, 1f);

    public static LeaderboardPanel SetupPanel(Transform canvas)
    {
        RectTransform root = GetOrCreateRect(canvas, "LeaderboardPanel");
        root.SetAsLastSibling();
        SetRect(root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        SetupImage(root.gameObject, null, new Color(0f, 0f, 0f, 0.75f));

        CanvasGroup group = GetOrAdd<CanvasGroup>(root.gameObject);
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        RectTransform card = GetOrCreateRect(root, "Card");
        SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(960f, 1600f));
        SetupImage(card.gameObject, uiSprite, CardColor);

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(card.gameObject);
        layout.padding = new RectOffset(40, 40, 50, 50);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(card.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text title = AddText(card, "Title", "LEADERBOARD", 72, 90f, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.color = GoldColor;

        TMP_Text subtitle = AddText(card, "Subtitle", "INFINITE MODE  ·  TOP 10", 32, 44f,
            TextAlignmentOptions.Center);
        subtitle.color = new Color(1f, 1f, 1f, 0.6f);

        // ── Submit: ringkasan run + input nama + tombol ──
        RectTransform submitGroup = GetOrCreateRect(card, "SubmitGroup");
        SetPreferredHeight(submitGroup, 200f);

        RectTransform summaryRect = GetOrCreateRect(submitGroup, "RunSummary");
        SetRect(summaryRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 60f));
        TMP_Text summary = SetupText(summaryRect.gameObject, "YOUR RUN  0  ·  00:00", 40,
            TextAlignmentOptions.Center);
        summary.fontStyle = FontStyles.Bold;

        RectTransform inputRow = GetOrCreateRect(submitGroup, "InputRow");
        SetRect(inputRow, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 10f), new Vector2(0f, 110f));
        HorizontalLayoutGroup rowLayout = GetOrAdd<HorizontalLayoutGroup>(inputRow.gameObject);
        rowLayout.spacing = 20f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        TMP_InputField nameInput = SetupNameInput(inputRow);
        Button submit = SetupButton(inputRow, "SubmitButton", "SUBMIT", new Vector2(260f, 110f));
        submit.targetGraphic.color = SubmitColor;
        LayoutElement submitLayout = GetOrAdd<LayoutElement>(submit.gameObject);
        submitLayout.preferredWidth = 260f;
        submitLayout.flexibleWidth = 0f;

        // ── Tabel ──
        RectTransform header = GetOrCreateRect(card, "Header");
        SetPreferredHeight(header, 44f);
        CreateColumns(header, "RANK", "PLAYER", "SCORE", "TIME", 28, new Color(1f, 1f, 1f, 0.55f));

        RectTransform rowsRect = GetOrCreateRect(card, "Rows");
        SetPreferredHeight(rowsRect, RowCount * 76f + (RowCount - 1) * 8f);
        VerticalLayoutGroup rowsLayout = GetOrAdd<VerticalLayoutGroup>(rowsRect.gameObject);
        rowsLayout.spacing = 8f;
        rowsLayout.childAlignment = TextAnchor.UpperCenter;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = false;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;

        LeaderboardRowView[] rows = new LeaderboardRowView[RowCount];

        for (int i = 0; i < RowCount; i++)
        {
            rows[i] = SetupRow(rowsRect, i);
        }

        // Pesan status (Loading, offline, belum ada skor) di tengah tabel.
        RectTransform statusRect = GetOrCreateRect(rowsRect, "Status");
        GetOrAdd<LayoutElement>(statusRect.gameObject).ignoreLayout = true;
        SetRect(statusRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(-80f, 0f));
        TMP_Text status = SetupText(statusRect.gameObject, "Loading...", 40, TextAlignmentOptions.Center);
        status.textWrappingMode = TextWrappingModes.Normal;
        status.color = new Color(1f, 1f, 1f, 0.7f);
        statusRect.SetAsLastSibling();

        TMP_Text myRank = AddText(card, "MyRank", "Play Infinite Mode to get ranked!", 34, 56f,
            TextAlignmentOptions.Center);
        myRank.fontStyle = FontStyles.Bold;
        myRank.color = GoldColor;

        RectTransform closeRow = GetOrCreateRect(card, "CloseRow");
        SetPreferredHeight(closeRow, 120f);
        Button close = SetupButton(closeRow, "CloseButton", "CLOSE", new Vector2(360f, 120f));
        SetRect((RectTransform)close.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 120f));

        LeaderboardPanel panel = GetOrAdd<LeaderboardPanel>(root.gameObject);
        SetRefArray(panel, "rows", rows);
        SetRef(panel, "statusText", status);
        SetRef(panel, "myRankText", myRank);
        SetRef(panel, "submitGroup", submitGroup.gameObject);
        SetRef(panel, "runSummaryText", summary);
        SetRef(panel, "nameInput", nameInput);
        SetRef(panel, "submitButton", submit);
        SetRef(panel, "closeButton", close);

        return panel;
    }

    private static LeaderboardRowView SetupRow(RectTransform parent, int index)
    {
        RectTransform row = GetOrCreateRect(parent, $"Row{index + 1}");
        row.sizeDelta = new Vector2(0f, 76f);
        Image background = SetupImage(row.gameObject, uiSprite, RowColor);
        background.raycastTarget = false;

        TMP_Text[] columns = CreateColumns(row, $"#{index + 1}", "Player", "0", "00:00", 36, Color.white);
        columns[0].fontStyle = FontStyles.Bold;
        columns[2].fontStyle = FontStyles.Bold;

        LeaderboardRowView view = GetOrAdd<LeaderboardRowView>(row.gameObject);
        SetRef(view, "background", background);
        SetRef(view, "rankText", columns[0]);
        SetRef(view, "nameText", columns[1]);
        SetRef(view, "scoreText", columns[2]);
        SetRef(view, "timeText", columns[3]);

        row.gameObject.SetActive(false);
        return view;
    }

    // 4 kolom dengan lebar proporsional: rank | nama | skor | waktu.
    private static TMP_Text[] CreateColumns(RectTransform parent, string rank, string player, string score,
        string time, float size, Color color)
    {
        float[] edges = { 0f, 0.15f, 0.6f, 0.83f, 1f };
        string[] names = { "Rank", "Name", "Score", "Time" };
        string[] values = { rank, player, score, time };
        TextAlignmentOptions[] alignments =
        {
            TextAlignmentOptions.Center,
            TextAlignmentOptions.MidlineLeft,
            TextAlignmentOptions.MidlineRight,
            TextAlignmentOptions.MidlineRight
        };

        TMP_Text[] texts = new TMP_Text[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            RectTransform column = GetOrCreateRect(parent, names[i]);
            SetRect(column, new Vector2(edges[i], 0f), new Vector2(edges[i + 1], 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(-24f, 0f));
            texts[i] = SetupText(column.gameObject, values[i], size, alignments[i]);
            texts[i].color = color;
            texts[i].overflowMode = TextOverflowModes.Ellipsis;
        }

        return texts;
    }

    private static TMP_InputField SetupNameInput(RectTransform parent)
    {
        Transform existing = parent.Find("NameInput");
        GameObject inputObject;

        if (existing != null)
        {
            inputObject = existing.gameObject;
        }
        else
        {
            inputObject = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources
            {
                standard = uiSprite,
                inputField = uiSprite
            });
            inputObject.name = "NameInput";
            inputObject.transform.SetParent(parent, false);
            SetLayerRecursive(inputObject.transform, LayerMask.NameToLayer("UI"));
        }

        inputObject.transform.SetSiblingIndex(0);

        LayoutElement layout = GetOrAdd<LayoutElement>(inputObject);
        layout.flexibleWidth = 1f;
        layout.preferredHeight = 110f;

        Image background = inputObject.GetComponent<Image>();
        background.sprite = uiSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color(1f, 1f, 1f, 0.12f);

        TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
        input.characterLimit = LeaderboardClient.MaxNameLength;
        input.lineType = TMP_InputField.LineType.SingleLine;

        StyleInputText(input.textComponent, "", Color.white);

        if (input.placeholder is TMP_Text placeholder)
        {
            StyleInputText(placeholder, "Your name", new Color(1f, 1f, 1f, 0.4f));
            placeholder.fontStyle = FontStyles.Italic;
        }

        return input;
    }

    private static void StyleInputText(TMP_Text text, string value, Color color)
    {
        if (text == null)
        {
            return;
        }

        if (text.font == null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        text.text = value;
        text.fontSize = 44;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private static TMP_Text AddText(RectTransform parent, string name, string value, float size, float height,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = GetOrCreateRect(parent, name);
        SetPreferredHeight(rect, height);
        return SetupText(rect.gameObject, value, size, alignment);
    }

    private static void SetPreferredHeight(RectTransform rect, float height)
    {
        LayoutElement layout = GetOrAdd<LayoutElement>(rect.gameObject);
        layout.preferredHeight = height;
        layout.minHeight = height;
    }

    private static void SetLayerRecursive(Transform target, int layer)
    {
        target.gameObject.layer = layer;

        foreach (Transform child in target)
        {
            SetLayerRecursive(child, layer);
        }
    }
}
