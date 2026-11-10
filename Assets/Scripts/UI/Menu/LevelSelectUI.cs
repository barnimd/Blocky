using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Level selector: 1 layar = 1 bab = 12 level.
// Pindah bab lewat tombol panah atau swipe kiri/kanan.
public class LevelSelectUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private StageList stageList;

    [Header("Navigation")]
    [SerializeField] private MainMenuUI mainMenu;
    [SerializeField] private Button backButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Header("Chapter Header")]
    [SerializeField] private Image headerBackground;
    [SerializeField] private TMP_Text chapterTitleText;
    [SerializeField] private TMP_Text chapterNameText;
    [SerializeField] private TMP_Text chapterStarsText;
    [SerializeField] private Image[] chapterDots;
    [SerializeField] private Color[] chapterColors =
    {
        new Color(1f, 0.55f, 0.2f),
        new Color(1f, 0.35f, 0.6f),
        new Color(0.2f, 0.75f, 1f),
        new Color(0.45f, 0.85f, 0.35f),
        new Color(0.65f, 0.4f, 1f)
    };

    [Header("Levels")]
    [SerializeField] private LevelButton[] levelButtons;
    [SerializeField] private GameObject chapterLockedPanel;
    [SerializeField] private TMP_Text chapterLockedText;

    [Header("Swipe")]
    [Tooltip("Jarak geser minimal (pixel) untuk pindah bab.")]
    [SerializeField] private float swipeThreshold = 150f;

    private int currentChapter;
    private Vector2 pointerDownPosition;
    private bool isPointerDown;

    public int CurrentChapter => currentChapter;
    public LevelButton[] LevelButtons => levelButtons;

    private void Awake()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(HandleBack);
        }

        if (previousButton != null)
        {
            previousButton.onClick.AddListener(() => ShowChapter(currentChapter - 1));
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(() => ShowChapter(currentChapter + 1));
        }
    }

    public void Show(int chapter)
    {
        gameObject.SetActive(true);
        ShowChapter(chapter);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ShowChapter(int chapter)
    {
        if (stageList == null || stageList.ChapterCount == 0)
        {
            Debug.LogError("LevelSelectUI: Stage List belum diisi atau kosong.", this);
            return;
        }

        currentChapter = Mathf.Clamp(chapter, 0, stageList.ChapterCount - 1);

        int firstIndex = currentChapter * StageList.StagesPerChapter;
        Color chapterColor = GetChapterColor(currentChapter);

        RefreshHeader(firstIndex, chapterColor);
        RefreshLevels(firstIndex, chapterColor);
        RefreshNavigation();
    }

    private void RefreshHeader(int firstIndex, Color chapterColor)
    {
        if (headerBackground != null)
        {
            headerBackground.color = chapterColor;
        }

        if (chapterTitleText != null)
        {
            chapterTitleText.text = $"CHAPTER {currentChapter + 1}";
        }

        if (chapterNameText != null)
        {
            chapterNameText.text = stageList.GetChapterName(currentChapter).ToUpperInvariant();
        }

        if (chapterStarsText != null)
        {
            int stars = StageProgress.GetTotalStars(stageList, firstIndex, StageList.StagesPerChapter);
            int maxStars = StageList.StagesPerChapter * StarRating.MaxStars;
            chapterStarsText.text = $"{stars} / {maxStars}";
        }

        // Titik indikator bab: bab aktif menyala.
        if (chapterDots != null)
        {
            for (int i = 0; i < chapterDots.Length; i++)
            {
                if (chapterDots[i] == null)
                {
                    continue;
                }

                chapterDots[i].gameObject.SetActive(i < stageList.ChapterCount);
                chapterDots[i].color = i == currentChapter
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.3f);
            }
        }
    }

    private void RefreshLevels(int firstIndex, Color chapterColor)
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            LevelButton levelButton = levelButtons[i];

            if (levelButton == null)
            {
                continue;
            }

            int stageIndex = firstIndex + i;
            StageData stage = stageList.GetStage(stageIndex);

            levelButton.gameObject.SetActive(stage != null);

            if (stage == null)
            {
                continue;
            }

            levelButton.Setup(
                stageIndex,
                stage.LevelInChapter.ToString(),
                StageProgress.IsUnlocked(stageList, stageIndex),
                StageProgress.GetStars(stage.StageId),
                chapterColor,
                HandleLevelClicked
            );
        }

        // Bab terkunci jika level pertamanya belum terbuka.
        bool chapterUnlocked = StageProgress.IsUnlocked(stageList, firstIndex);

        if (chapterLockedPanel != null)
        {
            chapterLockedPanel.SetActive(!chapterUnlocked);
        }

        if (chapterLockedText != null && currentChapter > 0)
        {
            chapterLockedText.text = $"Clear Chapter {currentChapter} to unlock";
        }
    }

    private void RefreshNavigation()
    {
        if (previousButton != null)
        {
            previousButton.interactable = currentChapter > 0;
        }

        if (nextButton != null)
        {
            nextButton.interactable = currentChapter < stageList.ChapterCount - 1;
        }
    }

    private Color GetChapterColor(int chapter)
    {
        if (chapterColors == null || chapterColors.Length == 0)
        {
            return Color.white;
        }

        return chapterColors[chapter % chapterColors.Length];
    }

    // Swipe horizontal di mana saja di layar = ganti bab.
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pointerDownPosition = Input.mousePosition;
            isPointerDown = true;
        }
        else if (Input.GetMouseButtonUp(0) && isPointerDown)
        {
            isPointerDown = false;

            Vector2 delta = (Vector2)Input.mousePosition - pointerDownPosition;

            if (Mathf.Abs(delta.x) >= swipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * 1.5f)
            {
                // Geser ke kiri = bab berikutnya (seperti membalik halaman).
                ShowChapter(currentChapter + (delta.x < 0f ? 1 : -1));
            }
        }
    }

    private void HandleLevelClicked(int stageIndex)
    {
        GameSession.StartStage(stageIndex);
    }

    private void HandleBack()
    {
        if (mainMenu != null)
        {
            mainMenu.ShowMain();
        }
    }
}
