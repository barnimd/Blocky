using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel hasil:
// - Victory  : bintang (animasi), penjelasan kenapa dapat 1/2/3 bintang,
//              tips untuk bintang berikutnya, tombol Menu/Retry/Next.
// - Game Over: Stage gagal, atau Infinite selesai (skor, waktu, leaderboard).
[RequireComponent(typeof(CanvasGroup))]
public class ResultPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ActionInventory actionInventory;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private LeaderboardPanel leaderboardPanel;

    [Header("UI")]
    [SerializeField] private RectTransform card;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private StarDisplay stars;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button leaderboardButton;

    [Header("Star Explanation (Victory)")]
    [Tooltip("Urutan: baris 3 bintang, 2 bintang, 1 bintang.")]
    [SerializeField] private StarRuleRow[] starRules;
    [SerializeField] private GameObject starInfoGroup;
    [SerializeField] private TMP_Text hintText;

    [Header("Layout")]
    [SerializeField] private float victoryCardHeight = 1150f;
    [SerializeField] private float compactCardHeight = 820f;
    [SerializeField] private float victoryDetailY = -370f;
    [SerializeField] private float compactDetailY = -190f;

    [Header("Text")]
    [SerializeField] private string victoryTitle = "STAGE CLEAR!";
    [SerializeField] private string gameOverTitle = "OUT OF TURNS";
    [SerializeField] private string infiniteOverTitle = "NO MORE MOVES";

    [Header("Animation")]
    [SerializeField] private float showDelay = 0.25f;
    [SerializeField] private float fadeDuration = 0.25f;

    private CanvasGroup canvasGroup;

    public bool IsShown { get; private set; }
    public string Title => titleText != null ? titleText.text : string.Empty;
    public string Detail => detailText != null ? detailText.text : string.Empty;
    public string Hint => hintText != null ? hintText.text : string.Empty;
    public bool IsNextVisible => nextButton != null && nextButton.gameObject.activeSelf;
    public StarRuleRow[] StarRules => starRules;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(HandleRetry);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(HandleNext);
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(HandleMenu);
        }

        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.AddListener(HandleLeaderboard);
        }
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.StateChanged += HandleStateChanged;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.StateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.Victory)
        {
            ShowVictory();
        }
        else if (state == GameState.GameOver)
        {
            ShowGameOver();
        }
        else
        {
            Hide();
        }
    }

    private void ShowVictory()
    {
        int turnsLeft = GetTurnsLeft();
        int earned = gameManager.StarsEarned;

        SetTitle(victoryTitle);
        SetDetail($"Turns left: {turnsLeft}");
        SetLayout(true);

        if (stars != null)
        {
            stars.gameObject.SetActive(true);
            stars.PlayReveal(earned);
        }

        ShowStarExplanation(turnsLeft, earned);

        SetButtonVisible(nextButton, gameManager.HasNextStage);
        SetButtonVisible(leaderboardButton, false);

        Show();
    }

    // Tabel ambang bintang stage ini + baris hasil pemain di-highlight.
    private void ShowStarExplanation(int turnsLeft, int earned)
    {
        StageData stage = gameManager.CurrentStage;

        if (starInfoGroup != null)
        {
            starInfoGroup.SetActive(stage != null);
        }

        if (stage == null)
        {
            return;
        }

        int star3 = stage.Star3MinTurnsLeft;
        int star2 = stage.Star2MinTurnsLeft;

        for (int i = 0; i < starRules.Length; i++)
        {
            int starCount = StarRating.MaxStars - i;

            if (starRules[i] != null)
            {
                starRules[i].Setup(
                    starCount,
                    StarRating.DescribeRange(starCount, star3, star2),
                    starCount == earned
                );
            }
        }

        if (hintText != null)
        {
            int needed = StarRating.TurnsToNextStar(turnsLeft, star3, star2);

            hintText.text = needed == 0
                ? "Perfect! You earned every star."
                : $"Save {needed} more {(needed == 1 ? "turn" : "turns")} to earn {earned + 1} stars!";
        }
    }

    private void ShowGameOver()
    {
        if (gameManager.IsInfinite)
        {
            int score = scoreManager != null ? scoreManager.Score : 0;
            string best = gameManager.IsNewBestScore
                ? "NEW BEST!"
                : $"Best: {StageProgress.InfiniteBest:N0}";

            SetTitle(infiniteOverTitle);
            SetDetail($"Score: {score:N0}\nTime: {RunTimer.Format(gameManager.RunSeconds)}\n{best}");
        }
        else
        {
            SetTitle(gameOverTitle);
            SetDetail("Objective not completed.\nTry saving Rotate & Diagonal for tricky spots.");
        }

        SetLayout(false);

        if (stars != null)
        {
            stars.gameObject.SetActive(false);
        }

        if (starInfoGroup != null)
        {
            starInfoGroup.SetActive(false);
        }

        SetButtonVisible(nextButton, false);
        SetButtonVisible(leaderboardButton, gameManager.IsInfinite);

        Show();
    }

    private void Show()
    {
        IsShown = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    private void Hide()
    {
        IsShown = false;
        StopAllCoroutines();

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private IEnumerator FadeRoutine()
    {
        canvasGroup.alpha = 0f;
        yield return new WaitForSecondsRealtime(showDelay);

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private int GetTurnsLeft()
    {
        return actionInventory != null ? actionInventory.TotalTurns : 0;
    }

    // Victory butuh kartu tinggi (bintang + penjelasan),
    // Game Over cukup kartu pendek dengan detail lebih ke atas.
    private void SetLayout(bool victory)
    {
        if (card != null)
        {
            float height = victory ? victoryCardHeight : compactCardHeight;
            card.sizeDelta = new Vector2(card.sizeDelta.x, height);
        }

        if (detailText != null)
        {
            RectTransform detail = detailText.rectTransform;
            detail.anchoredPosition = new Vector2(
                detail.anchoredPosition.x,
                victory ? victoryDetailY : compactDetailY
            );
        }
    }

    private static void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
        {
            button.gameObject.SetActive(visible);
        }
    }

    private void SetTitle(string text)
    {
        if (titleText != null)
        {
            titleText.text = text;
        }
    }

    private void SetDetail(string text)
    {
        if (detailText != null)
        {
            detailText.text = text;
        }
    }

    private void HandleRetry()
    {
        gameManager.RetryStage();
    }

    private void HandleNext()
    {
        gameManager.NextStage();
    }

    private void HandleMenu()
    {
        gameManager.BackToMenu();
    }

    private void HandleLeaderboard()
    {
        if (leaderboardPanel == null)
        {
            return;
        }

        int score = scoreManager != null ? scoreManager.Score : 0;
        leaderboardPanel.ShowSubmit(score, Mathf.FloorToInt(gameManager.RunSeconds));
    }
}
