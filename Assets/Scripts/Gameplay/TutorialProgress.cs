// Mencatat tutorial mana yang sudah dilihat pemain, supaya
// tidak muncul lagi setiap kali game dibuka.
// Disimpan lewat SaveStore dengan prefix yang sama seperti StageProgress.
public enum TutorialId
{
    // Menu utama: penjelasan Stage Mode vs Infinite Mode.
    Intro,

    // Stage 1-1: board, objective, turn, bintang, aksi, cara gerak.
    Stage,

    // Pertama kali masuk Infinite Mode: skor, refresh, timer, leaderboard.
    Infinite
}

public static class TutorialProgress
{
    private static string Key(TutorialId id) => $"{StageProgress.KeyPrefix}tutorial_{id}";

    public static bool IsDone(TutorialId id)
    {
        return SaveStore.Current.GetInt(Key(id), 0) == 1;
    }

    public static void MarkDone(TutorialId id)
    {
        SaveStore.Current.SetInt(Key(id), 1);
        SaveStore.Current.Save();
    }

    // Tombol "HOW TO PLAY": semua tutorial tampil lagi.
    public static void ResetAll()
    {
        ClearAll();
        SaveStore.Current.Save();
    }

    // Untuk test: anggap semua tutorial sudah dilihat.
    internal static void MarkAllDone()
    {
        foreach (TutorialId id in System.Enum.GetValues(typeof(TutorialId)))
        {
            SaveStore.Current.SetInt(Key(id), 1);
        }

        SaveStore.Current.Save();
    }

    internal static void ClearAll()
    {
        foreach (TutorialId id in System.Enum.GetValues(typeof(TutorialId)))
        {
            SaveStore.Current.DeleteKey(Key(id));
        }
    }
}
