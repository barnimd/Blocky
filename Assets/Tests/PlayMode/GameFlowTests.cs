using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Menjalankan GameScene asli dan memainkan stage lewat kode:
// swap/rotate → resolve → cek menang/kalah → HUD & panel hasil.
public class GameFlowTests
{
    private const string SceneName = "GameScene";
    private const float Timeout = 15f;

    private GameManager game;
    private BoardManager board;
    private BoardInput input;
    private ActionInventory inventory;
    private ObjectiveManager objectives;
    private HUDManager hud;
    private StageInfoHUD stageHud;
    private ResultPanel result;

    // Test memakai prefix sendiri supaya progres asli tidak tertimpa.
    private const string TestPrefix = "blocky.test.";
    private const int MaxStageId = 100;

    private StageData Stage => game.CurrentStage;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        StageProgress.KeyPrefix = TestPrefix;
        StageProgress.ClearAll(MaxStageId);

        // Tutorial dianggap sudah dilihat supaya input tidak terkunci.
        // Test tutorial sendiri menghapus flag ini.
        TutorialProgress.MarkAllDone();

        GameManager.ResetStageIndex();
        yield return LoadGameScene();
    }

    [TearDown]
    public void TearDown()
    {
        StageProgress.ClearAll(MaxStageId);
        StageProgress.KeyPrefix = "blocky.";
        GameManager.ResetStageIndex();
    }

    private IEnumerator LoadGameScene()
    {
        SceneManager.LoadScene(SceneName);
        yield return WaitForGameScene();
    }

    private IEnumerator StartInfinite()
    {
        GameSession.StartInfinite();
        yield return WaitForGameScene();
        Assert.IsTrue(game.IsInfinite);
    }

    private IEnumerator WaitForGameScene()
    {
        yield return null;

        float time = 0f;

        while ((game = Object.FindFirstObjectByType<GameManager>()) == null ||
               game.State != GameState.PlayerInput)
        {
            time += Time.unscaledDeltaTime;
            Assert.Less(time, Timeout, "GameScene tidak sampai ke state PlayerInput.");
            yield return null;
        }

        board = Object.FindFirstObjectByType<BoardManager>();
        input = Object.FindFirstObjectByType<BoardInput>();
        inventory = Object.FindFirstObjectByType<ActionInventory>();
        objectives = Object.FindFirstObjectByType<ObjectiveManager>();
        hud = Object.FindFirstObjectByType<HUDManager>();
        stageHud = Object.FindFirstObjectByType<StageInfoHUD>();
        result = Object.FindFirstObjectByType<ResultPanel>();

        // Tunggu blok lama hasil Destroy benar-benar hilang.
        yield return null;
    }

    private IEnumerator WaitForSettled()
    {
        float time = 0f;

        while (game.State == GameState.Resolving)
        {
            time += Time.unscaledDeltaTime;
            Assert.Less(time, Timeout, "Board tidak pernah settle.");
            yield return null;
        }
    }

    // ───────────── Pola board untuk test ─────────────

    // (x + 2y) % 5: tidak pernah ada 3 warna sama berurutan.
    private static int[,] SafePattern(int width, int height)
    {
        int[,] colors = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                colors[x, y] = (x + 2 * y) % 5;
            }
        }

        return colors;
    }

    // Swap (0,2)↔(1,2) membuat 4 Biru vertikal di kolom 0.
    private static int[,] BlueMatchPattern(int width, int height)
    {
        int[,] colors = SafePattern(width, height);
        colors[0, 0] = (int)BlockColor.Blue;
        colors[0, 1] = (int)BlockColor.Blue;
        colors[1, 2] = (int)BlockColor.Blue;
        return colors;
    }

    private static bool HasMatch(int[,] colors)
    {
        int width = colors.GetLength(0);
        int height = colors.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int c = colors[x, y];

                if (x + 2 < width && colors[x + 1, y] == c && colors[x + 2, y] == c)
                {
                    return true;
                }

                if (y + 2 < height && colors[x, y + 1] == c && colors[x, y + 2] == c)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ApplyPattern(int[,] colors)
    {
        Assert.IsFalse(HasMatch(colors), "Pola test tidak boleh punya match awal.");
        board.SetColorsForTest(colors);
        Assert.AreEqual(0, board.FindMatches().Count);
    }

    // Cari swap horizontal yang pasti TIDAK membuat match.
    private static (Vector2Int, Vector2Int) FindNoMatchSwap(int[,] colors)
    {
        int width = colors.GetLength(0);
        int height = colors.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                int[,] copy = (int[,])colors.Clone();
                (copy[x, y], copy[x + 1, y]) = (copy[x + 1, y], copy[x, y]);

                if (copy[x, y] != copy[x + 1, y] && !HasMatch(copy))
                {
                    return (new Vector2Int(x, y), new Vector2Int(x + 1, y));
                }
            }
        }

        Assert.Fail("Tidak ada swap tanpa match.");
        return default;
    }

    private static string TextAt(string path)
    {
        GameObject found = GameObject.Find(path);
        Assert.IsNotNull(found, $"{path} tidak ditemukan.");
        return found.GetComponent<TMP_Text>().text;
    }

    // GameObject.Find dengan path tetap menemukan child yang nonaktif,
    // jadi cek juga activeInHierarchy untuk tahu apakah benar tampil.
    private static bool IsShown(string path)
    {
        GameObject found = GameObject.Find(path);
        return found != null && found.activeInHierarchy;
    }

    // ───────────── Tests ─────────────

    [UnityTest]
    public IEnumerator Stage1_LoadsFromStageData()
    {
        Assert.AreEqual(GameState.PlayerInput, game.State);
        Assert.IsFalse(game.IsInfinite);
        Assert.AreEqual(1, Stage.StageId);

        Assert.AreEqual(Stage.Width, board.Width);
        Assert.AreEqual(Stage.Height, board.Height);
        Assert.IsNotNull(board.GetBlock(board.Width - 1, board.Height - 1));
        Assert.AreEqual(0, board.FindMatches().Count, "Board awal tidak boleh punya match.");

        Assert.AreEqual(Stage.HorizontalTurns, inventory.HorizontalTurns);
        Assert.AreEqual(Stage.VerticalTurns, inventory.VerticalTurns);
        Assert.AreEqual(Stage.DiagonalTurns, inventory.DiagonalTurns);
        Assert.AreEqual(Stage.RotateTurns, inventory.RotateTurns);
        Assert.AreEqual(Stage.TotalTurns, inventory.TotalTurns);

        Assert.AreEqual(Stage.Goals.Count, objectives.Goals.Count);

        for (int i = 0; i < Stage.Goals.Count; i++)
        {
            Assert.AreEqual(Stage.Goals[i].Amount, objectives.Goals[i].Remaining);
        }

        Assert.IsTrue(input.CanInteract);
        Assert.IsFalse(result.IsShown);
        yield break;
    }

    [UnityTest]
    public IEnumerator Hud_ShowsStageTurnsObjectiveAndStars()
    {
        Assert.AreEqual("STAGE 1-1", TextAt("Canvas/TopBar/StageText"));
        Assert.AreEqual(Stage.TotalTurns.ToString(), TextAt("Canvas/TopBar/TurnPanel/CountText"));

        Assert.AreEqual(Stage.Goals.Count, stageHud.GoalItems.Count);
        Assert.AreEqual(Stage.Goals[0].Color, stageHud.GoalItems[0].GoalColor);
        Assert.AreEqual(Stage.Goals[0].Amount.ToString(), stageHud.GoalItems[0].RemainingLabel);

        StarDisplay preview = GameObject.Find("Canvas/TopBar/StarPreview").GetComponent<StarDisplay>();
        Assert.AreEqual(3, preview.ShownStars, "Turn masih penuh = preview 3 bintang.");

        Assert.AreEqual(Stage.HorizontalTurns.ToString(), TextAt("Canvas/BottomBar/HorizontalAction/CountText"));
        Assert.AreEqual(Stage.RotateTurns.ToString(), TextAt("Canvas/BottomBar/RotateAction/CountText"));

        Assert.IsFalse(IsShown("Canvas/TopBar/RefreshButton"),
            "Refresh button harus disembunyikan di Stage Mode.");
        Assert.IsFalse(IsShown("Canvas/TopBar/ObjectivePanel/ScoreText"),
            "Skor hanya tampil di Infinite Mode.");
        yield break;
    }

    [UnityTest]
    public IEnumerator Camera_FitsWholeBoard()
    {
        Camera camera = Camera.main;
        float viewHeight = camera.orthographicSize * 2f;
        float viewWidth = viewHeight * camera.aspect;

        Assert.LessOrEqual(board.BoardWorldSize.x, viewWidth + 0.001f);
        Assert.LessOrEqual(board.BoardWorldSize.y, viewHeight + 0.001f);

        // Board tidak boleh tertutup Top Bar / Bottom Bar.
        Vector3 boardTop = board.BoardCenter + new Vector3(0f, board.BoardWorldSize.y / 2f, 0f);
        Vector3 boardBottom = board.BoardCenter - new Vector3(0f, board.BoardWorldSize.y / 2f, 0f);
        float boardTopPixel = camera.WorldToScreenPoint(boardTop).y;
        float boardBottomPixel = camera.WorldToScreenPoint(boardBottom).y;

        Vector3[] corners = new Vector3[4];
        ((RectTransform)GameObject.Find("Canvas/TopBar").transform).GetWorldCorners(corners);
        Assert.LessOrEqual(boardTopPixel, corners[0].y + 0.5f, "Board tertutup Top Bar.");

        ((RectTransform)GameObject.Find("Canvas/BottomBar").transform).GetWorldCorners(corners);
        Assert.GreaterOrEqual(boardBottomPixel, corners[1].y - 0.5f, "Board tertutup Bottom Bar.");
        yield break;
    }

    [UnityTest]
    public IEnumerator Swap_ConsumesTurn_ThenReturnsToPlayerInput()
    {
        int[,] colors = SafePattern(board.Width, board.Height);
        ApplyPattern(colors);
        (Vector2Int from, Vector2Int to) = FindNoMatchSwap(colors);

        Block first = board.GetBlock(from.x, from.y);
        bool settled = false;
        board.BoardSettled += () => settled = true;

        Assert.IsTrue(input.TrySwapCells(from, to));
        Assert.AreEqual(GameState.Resolving, game.State);
        Assert.IsFalse(input.CanInteract, "Input harus terkunci selama resolve.");
        Assert.AreEqual(Stage.HorizontalTurns - 1, inventory.HorizontalTurns);
        Assert.AreEqual((Stage.TotalTurns - 1).ToString(), TextAt("Canvas/TopBar/TurnPanel/CountText"));

        yield return WaitForSettled();

        Assert.IsTrue(settled);
        Assert.AreEqual(GameState.PlayerInput, game.State);
        Assert.AreSame(first, board.GetBlock(to.x, to.y), "Blok harus pindah, walau tanpa match.");
        Assert.AreEqual(Stage.TotalTurns - 1, inventory.TotalTurns);
    }

    [UnityTest]
    public IEnumerator Denied_WhenQuotaEmpty_ShowsFeedbackAndKeepsBoard()
    {
        ApplyPattern(SafePattern(board.Width, board.Height));
        inventory.ResetInventory(0, 6, 3, 2);

        ActionType? denied = null;
        inventory.TurnDenied += action => denied = action;

        Block first = board.GetBlock(2, 2);
        Block second = board.GetBlock(3, 2);

        Assert.IsFalse(input.TrySwapCells(new Vector2Int(2, 2), new Vector2Int(3, 2)));

        Assert.AreEqual(ActionType.Horizontal, denied);
        Assert.IsTrue(first.IsPlayingDenied, "Blok harus bergetar merah.");
        Assert.IsTrue(second.IsPlayingDenied);
        Assert.IsTrue(hud.GetSlot(ActionType.Horizontal).IsPlayingDenied, "Slot HUD harus bergetar.");

        Assert.AreSame(first, board.GetBlock(2, 2), "Board tidak boleh berubah.");
        Assert.AreEqual(GameState.PlayerInput, game.State);
        Assert.AreEqual(11, inventory.TotalTurns);

        Vector3 restPosition = board.GetWorldPosition(2, 2);
        yield return new WaitForSeconds(0.5f);

        Assert.IsFalse(first.IsPlayingDenied);
        Assert.Less(Vector3.Distance(restPosition, first.transform.position), 0.0001f,
            "Blok harus kembali ke posisi semula setelah getar.");
    }

    [UnityTest]
    public IEnumerator Rotate_WorksWithOnlyRotateTurns_ThenGameOver()
    {
        ApplyPattern(SafePattern(board.Width, board.Height));
        objectives.Setup(new[] { new ColorGoal(BlockColor.Blue, 999) });
        inventory.ResetInventory(0, 0, 0, 1);

        Block bottomLeft = board.GetBlock(2, 2);
        List<Vector2Int> path = new List<Vector2Int>
        {
            new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(3, 3)
        };

        Assert.IsTrue(input.TryRotatePath(path));
        Assert.AreEqual(0, inventory.RotateTurns);

        yield return WaitForSettled();

        // Turn habis & objective belum selesai.
        Assert.AreEqual(GameState.GameOver, game.State);
        Assert.AreNotSame(bottomLeft, board.GetBlock(2, 2), "2x2 harus berputar.");
    }

    [UnityTest]
    public IEnumerator Match_CompletesObjective_Victory3Stars()
    {
        ApplyPattern(BlueMatchPattern(board.Width, board.Height));
        objectives.Setup(new[] { new ColorGoal(BlockColor.Blue, 3) });

        Assert.IsTrue(input.TrySwapCells(new Vector2Int(0, 2), new Vector2Int(1, 2)));

        yield return WaitForSettled();

        Assert.AreEqual(GameState.Victory, game.State);
        Assert.AreEqual(Stage.TotalTurns - 1, inventory.TotalTurns);
        Assert.AreEqual(3, game.StarsEarned, "Ambang 3 bintang selalu < total turn.");
        Assert.IsTrue(objectives.IsComplete);
        Assert.AreEqual("0", stageHud.GoalItems[0].RemainingLabel);

        Assert.IsTrue(result.IsShown);
        Assert.AreEqual("STAGE CLEAR!", result.Title);
        Assert.IsTrue(result.IsNextVisible, "Stage 1 punya stage berikutnya.");
        Assert.IsFalse(input.CanInteract, "Input terkunci setelah menang.");

        // Bintang tersimpan dan stage 2 terbuka.
        Assert.AreEqual(3, StageProgress.GetStars(1));

        // Penjelasan bintang: baris 3 bintang menyala, sesuai ambang stage.
        Assert.AreEqual(3, result.StarRules.Length);
        Assert.IsTrue(result.StarRules[0].IsHighlighted);
        Assert.IsFalse(result.StarRules[1].IsHighlighted);
        Assert.AreEqual($"{Stage.Star3MinTurnsLeft}+ turns left", result.StarRules[0].Text);
        Assert.AreEqual("Perfect! You earned every star.", result.Hint);
    }

    [UnityTest]
    public IEnumerator WinOnLastMove_IsVictoryNotGameOver()
    {
        ApplyPattern(BlueMatchPattern(board.Width, board.Height));
        objectives.Setup(new[] { new ColorGoal(BlockColor.Blue, 3) });
        inventory.ResetInventory(1, 0, 0, 0);

        Assert.IsTrue(input.TrySwapCells(new Vector2Int(0, 2), new Vector2Int(1, 2)));
        Assert.AreEqual(0, inventory.TotalTurns);

        yield return WaitForSettled();

        Assert.AreEqual(GameState.Victory, game.State);
        Assert.AreEqual(1, game.StarsEarned);

        // Baris 1 bintang menyala + tips berapa turn lagi untuk 2 bintang.
        Assert.IsTrue(result.StarRules[2].IsHighlighted);
        StringAssert.StartsWith($"Save {Stage.Star2MinTurnsLeft} more", result.Hint);
        StringAssert.EndsWith("to earn 2 stars!", result.Hint);
    }

    [UnityTest]
    public IEnumerator OutOfTurns_IsGameOver()
    {
        int[,] colors = SafePattern(board.Width, board.Height);
        ApplyPattern(colors);
        inventory.ResetInventory(1, 0, 0, 0);
        (Vector2Int from, Vector2Int to) = FindNoMatchSwap(colors);

        Assert.IsTrue(input.TrySwapCells(from, to));

        yield return WaitForSettled();

        Assert.AreEqual(GameState.GameOver, game.State);
        Assert.IsTrue(result.IsShown);
        Assert.AreEqual("OUT OF TURNS", result.Title);
        Assert.IsFalse(result.IsNextVisible);
        Assert.IsFalse(input.CanInteract);
    }

    [UnityTest]
    public IEnumerator Retry_ReloadsSameStageWithFullTurns()
    {
        int[,] colors = SafePattern(board.Width, board.Height);
        ApplyPattern(colors);
        (Vector2Int from, Vector2Int to) = FindNoMatchSwap(colors);
        input.TrySwapCells(from, to);
        yield return WaitForSettled();
        int fullTurns = Stage.TotalTurns;
        Assert.AreEqual(fullTurns - 1, inventory.TotalTurns);

        game.RetryStage();
        yield return WaitForGameScene();

        Assert.AreEqual(1, Stage.StageId);
        Assert.AreEqual(fullTurns, inventory.TotalTurns);
        Assert.AreEqual(GameState.PlayerInput, game.State);
    }

    [UnityTest]
    public IEnumerator NextStage_LoadsStage2()
    {
        Assert.IsTrue(game.HasNextStage);
        game.NextStage();
        yield return WaitForGameScene();

        Assert.AreEqual(2, Stage.StageId);
        Assert.AreEqual("STAGE 1-2", TextAt("Canvas/TopBar/StageText"));
        Assert.AreEqual(Stage.TotalTurns, inventory.TotalTurns);
        Assert.AreEqual(Stage.Goals.Count, stageHud.GoalItems.Count);
        Assert.AreEqual(Stage.Width, board.Width);
    }

    [UnityTest]
    public IEnumerator LastStage_HasNoNextStage_AndBoardFitsCamera()
    {
        GameSession.StartStage(59);
        yield return WaitForGameScene();

        Assert.AreEqual(60, Stage.StageId);
        Assert.AreEqual("STAGE 5-12", TextAt("Canvas/TopBar/StageText"));
        Assert.IsFalse(game.HasNextStage, "Stage 60 = stage terakhir.");
        Assert.IsNotNull(board.GetBlock(board.Width - 1, board.Height - 1));
        Assert.AreEqual(0, board.FindMatches().Count);

        Camera camera = Camera.main;
        Assert.LessOrEqual(board.BoardWorldSize.x, camera.orthographicSize * 2f * camera.aspect + 0.001f);
    }

    // ───────────── Infinite Mode ─────────────

    [UnityTest]
    public IEnumerator Infinite_ShowsScoreAndRefresh_NoObjective()
    {
        yield return StartInfinite();

        Assert.AreEqual("INFINITE", TextAt("Canvas/TopBar/StageText"));
        Assert.AreEqual(0, objectives.Goals.Count);
        Assert.IsTrue(IsShown("Canvas/TopBar/ObjectivePanel/ScoreText"));
        Assert.IsTrue(IsShown("Canvas/TopBar/RefreshButton"));
        Assert.AreEqual("2/2", TextAt("Canvas/TopBar/RefreshButton/CountText"));
        Assert.AreEqual(2, game.RefreshesLeft);
    }

    [UnityTest]
    public IEnumerator Infinite_Match_AddsScore()
    {
        yield return StartInfinite();

        ScoreManager score = Object.FindFirstObjectByType<ScoreManager>();
        ApplyPattern(BlueMatchPattern(board.Width, board.Height));

        Assert.IsTrue(input.TrySwapCells(new Vector2Int(0, 2), new Vector2Int(1, 2)));
        yield return WaitForSettled();

        Assert.Greater(score.Score, 0);
        Assert.AreEqual(score.Score.ToString("N0"), TextAt("Canvas/TopBar/ObjectivePanel/ScoreText"));
        Assert.AreNotEqual(GameState.Victory, game.State, "Infinite Mode tidak pernah menang.");
    }

    [UnityTest]
    public IEnumerator Infinite_Refresh_RestoresStartingTurns()
    {
        yield return StartInfinite();

        int fullTurns = inventory.TotalTurns;
        inventory.ResetInventory(0, 0, 0, 1);

        Assert.IsTrue(game.UseRefresh());
        Assert.AreEqual(fullTurns, inventory.TotalTurns);
        Assert.AreEqual(1, game.RefreshesLeft);
        Assert.AreEqual("1/2", TextAt("Canvas/TopBar/RefreshButton/CountText"));

        Assert.IsTrue(game.UseRefresh());
        Assert.IsFalse(game.UseRefresh(), "Refresh hanya 2x per sesi.");
        Assert.AreEqual(0, game.RefreshesLeft);
    }

    [UnityTest]
    public IEnumerator Infinite_EmptyTurnsWithoutRefresh_AutoRandomizes()
    {
        yield return StartInfinite();

        game.UseRefresh();
        game.UseRefresh();

        // Pola ini masih punya gerakan match (swap Biru), jadi tidak terkunci.
        ApplyPattern(BlueMatchPattern(board.Width, board.Height));
        inventory.ResetInventory(1, 0, 0, 0);

        Assert.IsTrue(input.TrySwapCells(new Vector2Int(5, 6), new Vector2Int(6, 6)));
        yield return WaitForSettled();

        Assert.AreEqual(GameState.PlayerInput, game.State);
        Assert.GreaterOrEqual(inventory.TotalTurns, 10, "Jatah harus diacak ulang.");
        Assert.Greater(inventory.HorizontalTurns, 0);
        Assert.Greater(inventory.RotateTurns, 0);
    }

    [UnityTest]
    public IEnumerator Infinite_LockedBoard_IsGameOverWithScore()
    {
        yield return StartInfinite();

        // Pola (x + 2y) % 5 tidak punya satu pun gerakan yang membuat match.
        ApplyPattern(SafePattern(board.Width, board.Height));

        Assert.IsTrue(input.TrySwapCells(new Vector2Int(0, 0), new Vector2Int(1, 0)));
        yield return WaitForSettled();

        Assert.AreEqual(GameState.GameOver, game.State);
        Assert.IsTrue(result.IsShown);
        Assert.AreEqual("NO MORE MOVES", result.Title);
        Assert.IsFalse(result.IsNextVisible);
        StringAssert.Contains("Time: ", result.Detail);
    }

    // ───────────── Timer & tutorial ─────────────

    [UnityTest]
    public IEnumerator Infinite_Timer_RunsDuringPlay()
    {
        yield return StartInfinite();

        float start = game.RunSeconds;
        yield return new WaitForSeconds(0.5f);

        Assert.Greater(game.RunSeconds, start, "Stopwatch harus berjalan di Infinite Mode.");
    }

    [UnityTest]
    public IEnumerator Stage_Timer_DoesNotRun()
    {
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(0f, game.RunSeconds, "Stage Mode tidak memakai stopwatch.");
    }

    [UnityTest]
    public IEnumerator Tutorial_Stage1_ShowsOnce_AndLocksInput()
    {
        TutorialProgress.ResetAll();
        yield return LoadGameScene();

        GameTutorial tutorial = Object.FindFirstObjectByType<GameTutorial>();
        TutorialOverlay overlay = Object.FindFirstObjectByType<TutorialOverlay>();

        Assert.IsTrue(tutorial.IsShowing, "Tutorial muncul di Stage 1-1 pertama kali.");
        Assert.AreEqual("THE BOARD", overlay.CurrentTitle);
        Assert.IsFalse(input.CanInteract, "Board terkunci selama tutorial.");

        overlay.Next();
        Assert.AreEqual(1, overlay.StepIndex);

        overlay.Skip();
        Assert.IsFalse(tutorial.IsShowing);
        Assert.IsTrue(input.CanInteract);
        Assert.IsTrue(TutorialProgress.IsDone(TutorialId.Stage));

        // Main lagi: tutorial tidak muncul.
        yield return LoadGameScene();
        Assert.IsFalse(Object.FindFirstObjectByType<GameTutorial>().IsShowing);
    }

    [UnityTest]
    public IEnumerator Tutorial_Infinite_PausesTimer()
    {
        TutorialProgress.ResetAll();
        yield return StartInfinite();

        TutorialOverlay overlay = Object.FindFirstObjectByType<TutorialOverlay>();
        Assert.IsTrue(overlay.IsPlaying);

        float frozen = game.RunSeconds;
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(frozen, game.RunSeconds, "Stopwatch berhenti selama tutorial.");

        overlay.Skip();
        yield return new WaitForSeconds(0.3f);
        Assert.Greater(game.RunSeconds, frozen);
    }

    [UnityTest]
    public IEnumerator BoardContainerOffset_IsRespectedByInput()
    {
        GameObject.Find("BoardContainer").transform.position = new Vector3(1.5f, -2f, 0f);
        board.SetupBoard(8, 8);
        yield return null;

        Vector3 cellWorld = board.GetWorldPosition(3, 4);
        Assert.AreEqual(cellWorld, board.GetBlock(3, 4).transform.position);

        Assert.IsTrue(board.TryGetCellAt(cellWorld, 0.38f, out int x, out int y));
        Assert.AreEqual(3, x);
        Assert.AreEqual(4, y);
    }
}
