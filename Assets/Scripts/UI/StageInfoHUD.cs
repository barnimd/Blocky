using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Top bar: nomor stage, preview bintang, sisa total turn,
// dan Objective Tracker.
public class StageInfoHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ActionInventory actionInventory;
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private BoardManager boardManager;

    [Header("UI")]
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text turnsText;
    [SerializeField] private StarDisplay starPreview;
    [SerializeField] private Transform goalContainer;
    [SerializeField] private ObjectiveGoalItem goalItemPrefab;

    private readonly List<ObjectiveGoalItem> goalItems = new List<ObjectiveGoalItem>();

    public IReadOnlyList<ObjectiveGoalItem> GoalItems => goalItems;

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
    }

    private void HandleStageLoaded(StageData stage)
    {
        if (stageText != null)
        {
            stageText.text = $"STAGE {stage.StageId}";
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

        if (starPreview != null && gameManager != null && gameManager.CurrentStage != null)
        {
            starPreview.SetStars(gameManager.CurrentStage.GetStars(turnsLeft));
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
}
