using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Menu utama: PLAY STAGE (ke level selector), INFINITE MODE,
// dan LEADERBOARD global Infinite Mode.
public class MainMenuUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private StageList stageList;

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private LevelSelectUI levelSelect;

    [Header("Main Panel")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button infiniteButton;
    [SerializeField] private TMP_Text totalStarsText;
    [SerializeField] private TMP_Text infiniteBestText;
    [SerializeField] private RectTransform titleBlocks;

    [Header("Leaderboard")]
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private LeaderboardPanel leaderboardPanel;

    [Header("Juice")]
    [SerializeField] private float buttonPulseScale = 1.04f;
    [SerializeField] private float buttonPulseSpeed = 3f;
    [SerializeField] private float titleBobHeight = 12f;

    private Vector2 titleBlocksRestPosition;

    public bool IsMainVisible => mainPanel != null && mainPanel.activeSelf;

    private void Awake()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(HandlePlay);
        }

        if (infiniteButton != null)
        {
            infiniteButton.onClick.AddListener(HandleInfinite);
        }

        if (titleBlocks != null)
        {
            titleBlocksRestPosition = titleBlocks.anchoredPosition;
        }

        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.AddListener(HandleLeaderboard);
        }
    }

    private void Start()
    {
        // Kembali dari stage → langsung buka bab stage terakhir.
        if (GameSession.ConsumeReturnToLevelSelect())
        {
            ShowLevelSelect(StageList.GetChapterOf(GameSession.StageIndex));
        }
        else
        {
            ShowMain();
        }
    }

    public void ShowMain()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }

        if (levelSelect != null)
        {
            levelSelect.Hide();
        }

        RefreshMainTexts();
    }

    public void ShowLevelSelect(int chapter)
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        if (levelSelect != null)
        {
            levelSelect.Show(chapter);
        }
    }

    private void RefreshMainTexts()
    {
        if (totalStarsText != null && stageList != null)
        {
            int stars = StageProgress.GetTotalStars(stageList, 0, stageList.Count);
            totalStarsText.text = $"{stars} / {stageList.Count * StarRating.MaxStars}";
        }

        if (infiniteBestText != null)
        {
            int best = StageProgress.InfiniteBest;
            infiniteBestText.text = best > 0
                ? $"BEST {best:N0}  ·  {RunTimer.Format(StageProgress.InfiniteBestTime)}"
                : "BEST 0";
        }
    }

    // Tombol Play berdenyut pelan, blok judul naik-turun.
    private void Update()
    {
        if (!IsMainVisible)
        {
            return;
        }

        if (playButton != null)
        {
            float pulse = 1f + (buttonPulseScale - 1f) *
                (0.5f + 0.5f * Mathf.Sin(Time.time * buttonPulseSpeed));
            playButton.transform.localScale = Vector3.one * pulse;
        }

        if (titleBlocks != null)
        {
            titleBlocks.anchoredPosition = titleBlocksRestPosition +
                new Vector2(0f, Mathf.Sin(Time.time * 2f) * titleBobHeight);
        }
    }

    // Lanjut ke bab dari stage terbuka paling akhir.
    private void HandlePlay()
    {
        ShowLevelSelect(StageList.GetChapterOf(GetLatestUnlockedIndex()));
    }

    private void HandleInfinite()
    {
        GameSession.StartInfinite();
    }

    private void HandleLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.ShowView();
        }
    }

    private int GetLatestUnlockedIndex()
    {
        if (stageList == null)
        {
            return 0;
        }

        int latest = 0;

        for (int i = 0; i < stageList.Count; i++)
        {
            if (StageProgress.IsUnlocked(stageList, i))
            {
                latest = i;
            }
        }

        return latest;
    }
}
