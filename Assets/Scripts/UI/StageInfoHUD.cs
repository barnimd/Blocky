using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Top bar: nomor stage, preview bintang, sisa total turn,
// Objective Tracker (Stage Mode) atau skor (Infinite Mode),
// dan tombol kembali ke menu.
public class StageInfoHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ActionInventory actionInventory;
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private ScoreManager scoreManager;

    [Header("UI")]
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text turnsText;
    [SerializeField] private StarDisplay starPreview;
    [SerializeField] private Transform goalContainer;
    [SerializeField] private ObjectiveGoalItem goalItemPrefab;
    [SerializeField] private Button menuButton;

    [Header("Infinite Mode")]
    [SerializeField] private TMP_Text objectiveLabel;
    [SerializeField] private TMP_Text scoreText;

    private readonly List<ObjectiveGoalItem> goalItems = new List<ObjectiveGoalItem>();

    public IReadOnlyList<ObjectiveGoalItem> GoalItems => goalItems;

    private void Awake()
    {
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(HandleMenuClicked);
        }
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.StageLoaded += HandleStageLoaded;
        }

        if (actionInventory != null)
        {
            actionInventory.TurnsChanged += HandleTurnsChanged;
        }

        if (objectiveManager != null)
        {
            objectiveManager.GoalsReset += RebuildGoals;
            objectiveManager.GoalProgressChanged += HandleGoalProgressChanged;
        }

        if (scoreManager != null)
        {
            scoreManager.ScoreChanged += HandleScoreChanged;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.StageLoaded -= HandleStageLoaded;
        }

        if (actionInventory != null)
        {
            actionInventory.TurnsChanged -= HandleTurnsChanged;
        }

        if (objectiveManager != null)
        {
            objectiveManager.GoalsReset -= RebuildGoals;
            objectiveManager.GoalProgressChanged -= HandleGoalProgressChanged;
        }

        if (scoreManager != null)
        {
            scoreManager.ScoreChanged -= HandleScoreChanged;
        }
    }

    private void HandleStageLoaded(StageData stage)
    {
        bool isInfinite = gameManager.IsInfinite;

        if (stageText != null)
        {
            stageText.text = isInfinite ? "INFINITE" : $"STAGE {stage.DisplayName}";
        }

        // Infinite: target diganti skor, preview bintang disembunyikan.
        if (goalContainer != null)
        {
            goalContainer.gameObject.SetActive(!isInfinite);
        }

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(isInfinite);
        }

        if (starPreview != null)
        {
            starPreview.gameObject.SetActive(!isInfinite);
        }

        if (objectiveLabel != null)
        {
            objectiveLabel.text = isInfinite
                ? $"SCORE   ·   BEST {StageProgress.InfiniteBest}"
                : "OBJECTIVE";
        }

        RefreshTurns();
    }

    private void HandleTurnsChanged(ActionType action, int turns)
    {
        RefreshTurns();
    }

    // Sisa total turn + bintang yang akan didapat jika menang sekarang.
    private void RefreshTurns()
    {
        if (actionInventory == null)
        {
            return;
        }

        int turnsLeft = actionInventory.TotalTurns;

        if (turnsText != null)
        {
            turnsText.text = turnsLeft.ToString();
        }

        if (starPreview != null && gameManager != null &&
            gameManager.CurrentStage != null && !gameManager.IsInfinite)
        {
            starPreview.SetStars(gameManager.CurrentStage.GetStars(turnsLeft));
        }
    }

    private void HandleScoreChanged(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString("N0");
        }
    }

    private void RebuildGoals()
    {
        foreach (ObjectiveGoalItem item in goalItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        goalItems.Clear();

        if (goalContainer == null || goalItemPrefab == null)
        {
            Debug.LogError("StageInfoHUD: Goal Container atau Goal Item Prefab belum diisi.", this);
            return;
        }

        foreach (GoalProgress goal in objectiveManager.Goals)
        {
            ObjectiveGoalItem item = Instantiate(goalItemPrefab, goalContainer);
            item.name = $"Goal_{goal.Color}";

            Color displayColor = boardManager != null
                ? boardManager.GetColor((int)goal.Color)
                : Color.white;

            item.Setup(goal.Color, displayColor);
            item.SetRemaining(goal.Remaining);
            goalItems.Add(item);
        }
    }

    private void HandleGoalProgressChanged(GoalProgress goal)
    {
        int index = IndexOf(goal);

        if (index >= 0 && index < goalItems.Count)
        {
            goalItems[index].SetRemaining(goal.Remaining);
        }
    }

    private int IndexOf(GoalProgress goal)
    {
        for (int i = 0; i < objectiveManager.Goals.Count; i++)
        {
            if (objectiveManager.Goals[i] == goal)
            {
                return i;
            }
        }

        return -1;
    }

    private void HandleMenuClicked()
    {
        if (gameManager != null)
        {
            gameManager.BackToMenu();
        }
    }
}
