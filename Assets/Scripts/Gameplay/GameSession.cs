using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameMode
{
    Stage,
    Infinite
}

// Menyimpan pilihan pemain saat pindah scene:
// mode apa yang dimainkan dan stage mana yang dipilih.
// Static = tetap ada walau scene berganti.
public static class GameSession
{
    public const string MenuScene = "MainMenu";
    public const string GameScene = "GameScene";

    private static bool returnToLevelSelect;

    public static GameMode Mode { get; private set; } = GameMode.Stage;
    public static int StageIndex { get; private set; }

    // Reset saat Play mode dimulai, walau Domain Reload dimatikan.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    internal static void Reset()
    {
        Mode = GameMode.Stage;
        StageIndex = 0;
        returnToLevelSelect = false;
    }

    public static void StartStage(int stageIndex)
    {
        Mode = GameMode.Stage;
        SetStageIndex(stageIndex);
        SceneManager.LoadScene(GameScene);
    }

    public static void StartInfinite()
    {
        Mode = GameMode.Infinite;
        SceneManager.LoadScene(GameScene);
    }

    public static void SetStageIndex(int stageIndex)
    {
        StageIndex = Mathf.Max(0, stageIndex);
    }

    // Dari GameScene kembali ke menu. Stage Mode langsung membuka
    // level selector di bab stage terakhir yang dimainkan.
    public static void LoadMenu(bool openLevelSelect)
    {
        returnToLevelSelect = openLevelSelect;
        SceneManager.LoadScene(MenuScene);
    }

    // Dibaca sekali oleh menu, lalu di-reset.
    public static bool ConsumeReturnToLevelSelect()
    {
        bool value = returnToLevelSelect;
        returnToLevelSelect = false;
        return value;
    }
}
