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

    [Header("Infinite Mode")]
    [Tooltip("Ukuran board & jatah turn awal Infinite Mode (tanpa target).")]
    [SerializeField] private StageData infiniteStage;
    [SerializeField, Min(0)] private int maxRefreshes = 2;
    [SerializeField, Min(4)] private int randomTurnsMin = 10;
    [SerializeField, Min(4)] private int randomTurnsMax = 16;

    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private ActionInventory actionInventory;
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private BoardCameraFitter cameraFitter;
    [SerializeField] private RunTimer runTimer;

    // Bobot Auto-Randomize: H paling banyak, Rotate paling langka.
    private static readonly float[] RandomTurnWeights = { 0.36f, 0.30f, 0.20f, 0.14f };

    public GameState State { get; private set; } = GameState.Init;
    public GameMode Mode => GameSession.Mode;
    public bool IsInfinite => Mode == GameMode.Infinite;
    public StageData CurrentStage { get; private set; }
    public int CurrentStageIndex => GameSession.StageIndex;
    public int StarsEarned { get; private set; }

    public int RefreshesLeft { get; private set; }
    public int MaxRefreshes => maxRefreshes;
    public bool IsNewBestScore { get; private set; }

    // Lama permainan Infinite Mode (stopwatch), dalam detik.
    public float RunSeconds => runTimer != null ? runTimer.Elapsed : 0f;

    public bool HasNextStage =>
        !IsInfinite && stageList != null && CurrentStageIndex + 1 < stageList.Count;

    public event System.Action<GameState> StateChanged;
    public event System.Action<StageData> StageLoaded;
    public event System.Action<int> RefreshesChanged;

    // Dipakai test: kembali ke Stage Mode, stage pertama.
    internal static void ResetStageIndex()
    {
        GameSession.Reset();
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

        if (IsInfinite)
        {
            LoadInfinite();
        }
        else
        {
            LoadStage(CurrentStageIndex);
        }
    }

    private bool ValidateSetup()
    {
        if (!IsInfinite && (stageList == null || stageList.Count == 0))
        {
            Debug.LogError("GameManager: Stage List belum diisi atau kosong.", this);
            return false;
        }

        if (IsInfinite && infiniteStage == null)
        {
            Debug.LogError("GameManager: Infinite Stage belum diisi.", this);
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

        GameSession.SetStageIndex(Mathf.Clamp(index, 0, stageList.Count - 1));
        CurrentStage = stageList.GetStage(CurrentStageIndex);
        StarsEarned = 0;

        if (CurrentStage == null)
        {
            Debug.LogError($"GameManager: Stage index {CurrentStageIndex} kosong.", this);
            return;
        }

        if (!PrepareBoard(CurrentStage))
        {
            return;
        }

        objectiveManager.Setup(CurrentStage.Goals);

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        Debug.Log(
            $"Stage {CurrentStage.DisplayName} dimulai: " +
            $"{CurrentStage.Width}x{CurrentStage.Height}, " +
            $"{CurrentStage.TotalTurns} turn."
        );

        StageLoaded?.Invoke(CurrentStage);
        SetState(GameState.PlayerInput);
    }

    private void LoadInfinite()
    {
        SetState(GameState.Init);

        CurrentStage = infiniteStage;
        StarsEarned = 0;
        IsNewBestScore = false;
        RefreshesLeft = maxRefreshes;

        if (!PrepareBoard(CurrentStage))
        {
            return;
        }

        // Infinite Mode tidak punya target, hanya skor.
        objectiveManager.Clear();

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        Debug.Log(
            $"Infinite Mode dimulai: {CurrentStage.TotalTurns} turn, " +
            $"{maxRefreshes}x refresh."
        );

        RefreshesChanged?.Invoke(RefreshesLeft);
        StageLoaded?.Invoke(CurrentStage);
        SetState(GameState.PlayerInput);
    }

    // Board, jatah turn, dan kamera dari data stage.
    private bool PrepareBoard(StageData stage)
    {
        if (!boardManager.SetupBoard(stage.Width, stage.Height))
        {
            return false;
        }

        ResetTurnsToStage();

        if (cameraFitter != null)
        {
            cameraFitter.Fit(boardManager.BoardCenter, boardManager.BoardWorldSize);
        }

        return true;
    }

    private void ResetTurnsToStage()
    {
        actionInventory.ResetInventory(
            CurrentStage.HorizontalTurns,
            CurrentStage.VerticalTurns,
            CurrentStage.DiagonalTurns,
            CurrentStage.RotateTurns
        );
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

        if (IsInfinite)
        {
            CheckInfiniteCondition();
        }
        else
        {
            CheckCondition();
        }
    }

    private void CheckCondition()
    {
        // Objective dicek DULUAN, jadi menang di gerakan terakhir
        // (turn = 0) tetap dihitung menang, bukan game over.
        if (objectiveManager.IsComplete)
        {
            StarsEarned = CurrentStage.GetStars(actionInventory.TotalTurns);

            // Simpan bintang terbaik → stage berikutnya terbuka.
            StageProgress.SaveStars(CurrentStage.StageId, StarsEarned);

            Debug.Log(
                $"Stage {CurrentStage.DisplayName} selesai! " +
                $"Sisa turn {actionInventory.TotalTurns}, bintang {StarsEarned}."
            );

            SetState(GameState.Victory);
            return;
        }

        if (actionInventory.TotalTurns == 0)
        {
            Debug.Log($"Stage {CurrentStage.DisplayName} gagal: turn habis.");
            SetState(GameState.GameOver);
            return;
        }

        SetState(GameState.PlayerInput);
    }

    // GDD 3.2: kalah hanya jika papan terkunci (tidak ada satu pun
    // gerakan yang bisa membuat match). Jatah habis & refresh habis
    // = Auto-Randomize jatah baru, permainan lanjut.
    private void CheckInfiniteCondition()
    {
        if (!MoveFinder.HasAnyMatchingMove(boardManager.GetColorSnapshot()))
        {
            int score = scoreManager != null ? scoreManager.Score : 0;
            int seconds = Mathf.FloorToInt(RunSeconds);
            IsNewBestScore = StageProgress.SaveInfiniteBest(score, seconds);

            Debug.Log(
                $"Infinite Mode selesai: papan terkunci. " +
                $"Skor {score}, waktu {RunTimer.Format(seconds)}."
            );
            SetState(GameState.GameOver);
            return;
        }

        if (actionInventory.AreAllTurnsEmpty() && RefreshesLeft == 0)
        {
            RandomizeTurns();
        }

        SetState(GameState.PlayerInput);
    }

    // Tombol Refresh: isi ulang jatah turn ke nilai awal.
    public bool UseRefresh()
    {
        if (!IsInfinite || RefreshesLeft <= 0 || State != GameState.PlayerInput)
        {
            return false;
        }

        RefreshesLeft--;
        ResetTurnsToStage();

        Debug.Log($"Refresh dipakai. Sisa refresh: {RefreshesLeft}.");
        RefreshesChanged?.Invoke(RefreshesLeft);
        return true;
    }

    private void RandomizeTurns()
    {
        float[] weights = new float[RandomTurnWeights.Length];

        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = RandomTurnWeights[i] * Random.Range(0.6f, 1.4f);
        }

        int max = Mathf.Max(randomTurnsMin, randomTurnsMax);
        int total = Random.Range(randomTurnsMin, max + 1);
        int[] turns = StageGenerator.SplitTurns(total, weights);

        actionInventory.ResetInventory(turns[0], turns[1], turns[2], turns[3]);

        Debug.Log($"Auto-Randomize: H{turns[0]} V{turns[1]} D{turns[2]} R{turns[3]}.");
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

    // ───────────── Tombol hasil & navigasi ─────────────

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

        GameSession.SetStageIndex(CurrentStageIndex + 1);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Stage Mode kembali ke level selector, Infinite ke menu utama.
    public void BackToMenu()
    {
        GameSession.LoadMenu(!IsInfinite);
    }
}
