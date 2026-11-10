using UnityEngine;

// Tutorial di GameScene:
// - Stage 1-1 (pertama kali): board, objective, turn, bintang, aksi, cara gerak.
// - Infinite Mode (pertama kali): skor & waktu, refresh, game over & leaderboard.
// Selama tutorial tampil, input board dikunci dan stopwatch berhenti.
public class GameTutorial : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BoardInput boardInput;
    [SerializeField] private RunTimer runTimer;
    [SerializeField] private TutorialOverlay overlay;

    [Header("Steps")]
    [Tooltip("Index stage (0-based) tempat tutorial Stage Mode muncul.")]
    [SerializeField, Min(0)] private int tutorialStageIndex = 0;
    [SerializeField] private TutorialStep[] stageSteps;
    [SerializeField] private TutorialStep[] infiniteSteps;

    public bool IsShowing => overlay != null && overlay.IsPlaying;

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.StageLoaded += HandleStageLoaded;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.StageLoaded -= HandleStageLoaded;
        }
    }

    private void HandleStageLoaded(StageData stage)
    {
        if (gameManager.IsInfinite)
        {
            TryPlay(TutorialId.Infinite, infiniteSteps, "GO!");
        }
        else if (gameManager.CurrentStageIndex == tutorialStageIndex)
        {
            TryPlay(TutorialId.Stage, stageSteps, "PLAY");
        }
    }

    private void TryPlay(TutorialId id, TutorialStep[] steps, string lastLabel)
    {
        if (overlay == null || TutorialProgress.IsDone(id))
        {
            return;
        }

        SetGameplayPaused(true);

        // Selesai atau di-skip sama-sama dianggap sudah dilihat.
        overlay.Play(steps, lastLabel, completed =>
        {
            TutorialProgress.MarkDone(id);
            SetGameplayPaused(false);
        });
    }

    private void SetGameplayPaused(bool paused)
    {
        if (boardInput != null)
        {
            boardInput.InputLocked = paused;
        }

        if (runTimer != null)
        {
            runTimer.IsPaused = paused;
        }
    }
}
