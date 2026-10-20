using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel hasil: Victory (bintang, Retry, Next) atau Game Over (Retry).
[RequireComponent(typeof(CanvasGroup))]
public class ResultPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ActionInventory actionInventory;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private StarDisplay stars;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;

    [Header("Text")]
    [SerializeField] private string victoryTitle = "STAGE CLEAR!";
    [SerializeField] private string gameOverTitle = "OUT OF TURNS";

    [Header("Animation")]
    [SerializeField] private float showDelay = 0.25f;
    [SerializeField] private float fadeDuration = 0.25f;

    private CanvasGroup canvasGroup;

    public bool IsShown { get; private set; }
    public string Title => titleText != null ? titleText.text : string.Empty;
    public bool IsNextVisible => nextButton != null && nextButton.gameObject.activeSelf;

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
        SetTitle(victoryTitle);
        SetDetail($"Turns left: {GetTurnsLeft()}");

        if (stars != null)
        {
            stars.gameObject.SetActive(true);
            stars.SetStars(gameManager.StarsEarned);
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(gameManager.HasNextStage);
        }

        Show();
    }

    private void ShowGameOver()
    {
        SetTitle(gameOverTitle);
        SetDetail("Objective not completed.");

        if (stars != null)
        {
            stars.gameObject.SetActive(false);
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
        }

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
}
