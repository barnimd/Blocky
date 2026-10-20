using UnityEngine;
using UnityEngine.SceneManagement;

// State utama game (GDD 6.2).
// ValidateTurn & ExecuteMove terjadi di BoardInput dalam satu frame,
// ProcessMatch & Cascading = Resolving (coroutine BoardManager).
public enum GameState
{
    Init,
    PlayerInput,
    Resolving,
    Victory,
    GameOver
}

public class GameManager : MonoBehaviour
{
    [Header("Stages")]
    [SerializeField] private StageList stageList;

    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private ActionInventory actionInventory;
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private BoardCameraFitter cameraFitter;

    // Bertahan saat scene di-reload (Retry / Next Stage).
    private static int currentStageIndex;

    public GameState State { get; private set; } = GameState.Init;
    public StageData CurrentStage { get; private set; }
    public int CurrentStageIndex => currentStageIndex;
    public int StarsEarned { get; private set; }

    public bool HasNextStage =>
        stageList != null && currentStageIndex + 1 < stageList.Count;

    public event System.Action<GameState> StateChanged;
    public event System.Action<StageData> StageLoaded;

    // Reset index saat Play mode dimulai, walau Domain Reload dimatikan.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    internal static void ResetStageIndex()
    {
        currentStageIndex = 0;
    }

    private void OnEnable()
    {
        if (boardManager != null)
        {
            boardManager.ResolveStarted += HandleResolveStarted;
            boardManager.BoardSettled += HandleBoardSettled;
        }
    }

    private void OnDisable()
    {
        if (boardManager != null)
        {
            boardManager.ResolveStarted -= HandleResolveStarted;
            boardManager.BoardSettled -= HandleBoardSettled;
        }
    }

    private void Start()
    {
        if (!ValidateSetup())
        {
            return;
        }

        LoadStage(currentStageIndex);
    }

    private bool ValidateSetup()
    {
        if (stageList == null || stageList.Count == 0)
        {
            Debug.LogError("GameManager: Stage List belum diisi atau kosong.", this);
            return false;
        }

        if (boardManager == null || actionInventory == null || objectiveManager == null)
        {
            Debug.LogError(
                "GameManager: Board Manager, Action Inventory, " +
                "atau Objective Manager belum diisi.",
                this
            );
            return false;
        }

        return true;
    }

    // ───────────── State_Init ─────────────

    private void LoadStage(int index)
    {
        SetState(GameState.Init);

        currentStageIndex = Mathf.Clamp(index, 0, stageList.Count - 1);
        CurrentStage = stageList.GetStage(currentStageIndex);
        StarsEarned = 0;

        if (CurrentStage == null)
        {
            Debug.LogError($"GameManager: Stage index {currentStageIndex} kosong.", this);
            return;
        }

        if (!boardManager.SetupBoard(CurrentStage.Width, CurrentStage.Height))
        {
            return;
        }

        actionInventory.ResetInventory(
            CurrentStage.HorizontalTurns,
            CurrentStage.VerticalTurns,
            CurrentStage.DiagonalTurns,
            CurrentStage.RotateTurns
        );

        objectiveManager.Setup(CurrentStage.Goals);

        if (cameraFitter != null)
        {
            cameraFitter.Fit(boardManager.BoardCenter, boardManager.BoardWorldSize);
        }

        Debug.Log(
            $"Stage {CurrentStage.StageId} dimulai: " +
            $"{CurrentStage.Width}x{CurrentStage.Height}, " +
            $"{CurrentStage.TotalTurns} turn."
        );

        StageLoaded?.Invoke(CurrentStage);
        SetState(GameState.PlayerInput);
    }

    // ───────────── State_Resolving ─────────────

    private void HandleResolveStarted()
    {
        if (State == GameState.PlayerInput)
        {
            SetState(GameState.Resolving);
        }
    }

    // ───────────── State_CheckCondition ─────────────

    private void HandleBoardSettled()
    {
        if (State != GameState.Resolving)
        {
            return;
        }

        CheckCondition();
    }

    private void CheckCondition()
    {
        // Objective dicek DULUAN, jadi menang di gerakan terakhir
        // (turn = 0) tetap dihitung menang, bukan game over.
        if (objectiveManager.IsComplete)
        {
            StarsEarned = CurrentStage.GetStars(actionInventory.TotalTurns);

            Debug.Log(
                $"Stage {CurrentStage.StageId} selesai! " +
                $"Sisa turn {actionInventory.TotalTurns}, bintang {StarsEarned}."
            );

            SetState(GameState.Victory);
            return;
        }

        if (actionInventory.TotalTurns == 0)
        {
            Debug.Log($"Stage {CurrentStage.StageId} gagal: turn habis.");
            SetState(GameState.GameOver);
            return;
        }

        SetState(GameState.PlayerInput);
    }

    private void SetState(GameState newState)
    {
        if (State == newState)
        {
            return;
        }

        State = newState;
        StateChanged?.Invoke(newState);
    }

    // ───────────── Tombol hasil ─────────────

    public void RetryStage()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextStage()
    {
        if (!HasNextStage)
        {
            return;
        }

        currentStageIndex++;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
