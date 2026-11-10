using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// Menjalankan scene MainMenu asli: menu utama → level selector → stage.
public class MenuFlowTests
{
    private const string TestPrefix = "blocky.test.";
    private const int MaxStageId = 100;
    private const float Timeout = 15f;

    private MainMenuUI menu;
    private LevelSelectUI levelSelect;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        StageProgress.KeyPrefix = TestPrefix;
        StageProgress.ClearAll(MaxStageId);
        TutorialProgress.MarkAllDone();
        GameSession.Reset();

        yield return LoadMenu();
    }

    [TearDown]
    public void TearDown()
    {
        StageProgress.ClearAll(MaxStageId);
        StageProgress.KeyPrefix = "blocky.";
        GameSession.Reset();
    }

    private IEnumerator LoadMenu()
    {
        SceneManager.LoadScene(GameSession.MenuScene);
        yield return WaitFor(() => Object.FindFirstObjectByType<MainMenuUI>() != null);
        yield return null;

        menu = Object.FindFirstObjectByType<MainMenuUI>();
        levelSelect = Object.FindFirstObjectByType<LevelSelectUI>(FindObjectsInactive.Include);
    }

    private static IEnumerator WaitFor(System.Func<bool> condition)
    {
        float time = 0f;

        while (!condition())
        {
            time += Time.unscaledDeltaTime;
            Assert.Less(time, Timeout, "Kondisi tidak pernah terpenuhi.");
            yield return null;
        }
    }

    private static void Click(string path)
    {
        GameObject found = GameObject.Find(path);
        Assert.IsNotNull(found, $"{path} tidak ditemukan.");
        found.GetComponent<Button>().onClick.Invoke();
    }

    // GameObject.Find dengan path tetap menemukan child yang nonaktif,
    // jadi cek juga activeInHierarchy untuk tahu apakah benar tampil.
    private static bool IsShown(string path)
    {
        GameObject found = GameObject.Find(path);
        return found != null && found.activeInHierarchy;
    }

    [UnityTest]
    public IEnumerator MainMenu_ShowsPlayAndInfinite()
    {
        Assert.IsTrue(menu.IsMainVisible);
        Assert.IsFalse(levelSelect.gameObject.activeSelf);
        Assert.IsNotNull(GameObject.Find("Canvas/MainPanel/PlayButton"));
        Assert.IsNotNull(GameObject.Find("Canvas/MainPanel/InfiniteButton"));
        yield break;
    }

    [UnityTest]
    public IEnumerator Play_OpensChapter1_OnlyFirstLevelUnlocked()
    {
        Click("Canvas/MainPanel/PlayButton");
        yield return null;

        Assert.IsFalse(menu.IsMainVisible);
        Assert.IsTrue(levelSelect.gameObject.activeSelf);
        Assert.AreEqual(0, levelSelect.CurrentChapter);
        Assert.AreEqual(12, levelSelect.LevelButtons.Length);

        Assert.IsTrue(levelSelect.LevelButtons[0].IsUnlocked);

        for (int i = 1; i < 12; i++)
        {
            Assert.IsFalse(levelSelect.LevelButtons[i].IsUnlocked, $"Level {i + 1} harus terkunci.");
        }
    }

    [UnityTest]
    public IEnumerator Stars_ShowOnButton_AndUnlockNext()
    {
        StageProgress.SaveStars(1, 2);

        Click("Canvas/MainPanel/PlayButton");
        yield return null;

        Assert.AreEqual(2, levelSelect.LevelButtons[0].ShownStars);
        Assert.IsTrue(levelSelect.LevelButtons[1].IsUnlocked);
        Assert.IsFalse(levelSelect.LevelButtons[2].IsUnlocked);
    }

    [UnityTest]
    public IEnumerator ChapterArrows_SwitchChapter_LockedChapterShowsOverlay()
    {
        menu.ShowLevelSelect(0);
        yield return null;

        Click("Canvas/LevelSelectPanel/NextChapter");
        yield return null;

        Assert.AreEqual(1, levelSelect.CurrentChapter);
        Assert.AreEqual(12, levelSelect.LevelButtons[0].StageIndex, "Bab 2 dimulai dari stage 13.");
        Assert.IsTrue(IsShown("Canvas/LevelSelectPanel/ChapterLocked"),
            "Bab 2 masih terkunci.");

        Click("Canvas/LevelSelectPanel/PreviousChapter");
        yield return null;

        Assert.AreEqual(0, levelSelect.CurrentChapter);
        Assert.IsFalse(IsShown("Canvas/LevelSelectPanel/ChapterLocked"),
            "Bab 1 selalu terbuka.");
    }

    [UnityTest]
    public IEnumerator LevelButton_StartsThatStage_ThenMenuReturnsToLevelSelect()
    {
        StageProgress.SaveStars(1, 3);
        menu.ShowLevelSelect(0);
        yield return null;

        Click("Canvas/LevelSelectPanel/LevelGrid/Level2");

        GameManager game = null;
        yield return WaitFor(() =>
            (game = Object.FindFirstObjectByType<GameManager>()) != null &&
            game.State == GameState.PlayerInput);

        Assert.AreEqual(GameMode.Stage, game.Mode);
        Assert.AreEqual(2, game.CurrentStage.StageId);

        game.BackToMenu();
        yield return WaitFor(() => Object.FindFirstObjectByType<MainMenuUI>() != null);
        yield return null;

        menu = Object.FindFirstObjectByType<MainMenuUI>();
        levelSelect = Object.FindFirstObjectByType<LevelSelectUI>(FindObjectsInactive.Include);

        Assert.IsFalse(menu.IsMainVisible, "Kembali dari stage langsung ke level selector.");
        Assert.IsTrue(levelSelect.gameObject.activeSelf);
        Assert.AreEqual(0, levelSelect.CurrentChapter);
    }

    [UnityTest]
    public IEnumerator Infinite_StartsInfiniteMode()
    {
        Click("Canvas/MainPanel/InfiniteButton");

        GameManager game = null;
        yield return WaitFor(() =>
            (game = Object.FindFirstObjectByType<GameManager>()) != null &&
            game.State == GameState.PlayerInput);

        Assert.IsTrue(game.IsInfinite);
        Assert.AreEqual(2, game.RefreshesLeft);
    }

    [UnityTest]
    public IEnumerator FirstLaunch_IntroTutorial_LeadsToStage1()
    {
        TutorialProgress.ResetAll();
        yield return LoadMenu();

        MenuTutorial tutorial = Object.FindFirstObjectByType<MenuTutorial>();
        TutorialOverlay overlay = Object.FindFirstObjectByType<TutorialOverlay>();
        Assert.IsTrue(tutorial.IsShowing, "Intro muncul saat game pertama kali dibuka.");

        // Tekan NEXT sampai tombol terakhir ("PLAY 1-1").
        while (overlay.IsPlaying)
        {
            overlay.Next();
        }

        GameManager game = null;
        yield return WaitFor(() =>
            (game = Object.FindFirstObjectByType<GameManager>()) != null &&
            game.State == GameState.PlayerInput);

        Assert.IsFalse(game.IsInfinite);
        Assert.AreEqual(0, game.CurrentStageIndex);
        Assert.IsTrue(TutorialProgress.IsDone(TutorialId.Intro));
        Assert.IsTrue(Object.FindFirstObjectByType<GameTutorial>().IsShowing,
            "Tutorial gameplay lanjut di Stage 1-1.");
    }

    [UnityTest]
    public IEnumerator HowToPlay_ReplaysIntro()
    {
        Assert.IsFalse(Object.FindFirstObjectByType<MenuTutorial>().IsShowing);

        Click("Canvas/MainPanel/HowToPlayButton");
        yield return null;

        Assert.IsTrue(Object.FindFirstObjectByType<MenuTutorial>().IsShowing);
        Assert.IsFalse(TutorialProgress.IsDone(TutorialId.Stage), "Semua tutorial di-reset.");
    }
}
