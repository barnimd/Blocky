using UnityEngine;

// Menyimpan bintang terbaik tiap stage dan high score Infinite Mode
// lewat SaveStore (PlayerPrefs di mobile, localStorage di web).
public static class StageProgress
{
    // Bisa diganti test supaya tidak menimpa progres asli.
    internal static string KeyPrefix = "blocky.";

    private static string StarsKey(int stageId) => $"{KeyPrefix}stage_{stageId}_stars";
    private static string InfiniteBestKey => $"{KeyPrefix}infinite_best";
    private static string InfiniteBestTimeKey => $"{KeyPrefix}infinite_best_time";

    public static int GetStars(int stageId)
    {
        return SaveStore.Current.GetInt(StarsKey(stageId), 0);
    }

    // Hanya disimpan jika lebih baik dari sebelumnya.
    public static bool SaveStars(int stageId, int stars)
    {
        stars = Mathf.Clamp(stars, 0, StarRating.MaxStars);

        if (stars <= GetStars(stageId))
        {
            return false;
        }

        SaveStore.Current.SetInt(StarsKey(stageId), stars);
        SaveStore.Current.Save();
        return true;
    }

    // Stage pertama selalu terbuka. Stage berikutnya terbuka
    // jika stage sebelumnya sudah dimenangkan (minimal 1 bintang).
    public static bool IsUnlocked(StageList list, int stageIndex)
    {
        if (stageIndex <= 0)
        {
            return true;
        }

        StageData previous = list != null ? list.GetStage(stageIndex - 1) : null;
        return previous != null && GetStars(previous.StageId) > 0;
    }

    // Total bintang pada rentang stage (misal satu bab).
    public static int GetTotalStars(StageList list, int firstIndex, int count)
    {
        int total = 0;

        for (int i = firstIndex; i < firstIndex + count; i++)
        {
            StageData stage = list != null ? list.GetStage(i) : null;

            if (stage != null)
            {
                total += GetStars(stage.StageId);
            }
        }

        return total;
    }

    public static int InfiniteBest => SaveStore.Current.GetInt(InfiniteBestKey, 0);

    // Lama permainan (detik) saat skor terbaik dicapai.
    public static int InfiniteBestTime => SaveStore.Current.GetInt(InfiniteBestTimeKey, 0);

    public static bool SaveInfiniteBest(int score, int seconds = 0)
    {
        if (score <= InfiniteBest)
        {
            return false;
        }

        SaveStore.Current.SetInt(InfiniteBestKey, score);
        SaveStore.Current.SetInt(InfiniteBestTimeKey, Mathf.Max(0, seconds));
        SaveStore.Current.Save();
        return true;
    }

    // Untuk test: hapus semua key dengan prefix saat ini.
    internal static void ClearAll(int maxStageId)
    {
        for (int id = 1; id <= maxStageId; id++)
        {
            SaveStore.Current.DeleteKey(StarsKey(id));
        }

        SaveStore.Current.DeleteKey(InfiniteBestKey);
        SaveStore.Current.DeleteKey(InfiniteBestTimeKey);
        TutorialProgress.ClearAll();
        SaveStore.Current.Save();
    }
}
