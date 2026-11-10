using UnityEngine;

// Menyimpan bintang terbaik tiap stage dan high score Infinite Mode
// di PlayerPrefs (tersimpan di perangkat).
public static class StageProgress
{
    // Bisa diganti test supaya tidak menimpa progres asli.
    internal static string KeyPrefix = "blocky.";

    private static string StarsKey(int stageId) => $"{KeyPrefix}stage_{stageId}_stars";
    private static string InfiniteBestKey => $"{KeyPrefix}infinite_best";

    public static int GetStars(int stageId)
    {
        return PlayerPrefs.GetInt(StarsKey(stageId), 0);
    }

    // Hanya disimpan jika lebih baik dari sebelumnya.
    public static bool SaveStars(int stageId, int stars)
    {
        stars = Mathf.Clamp(stars, 0, StarRating.MaxStars);

        if (stars <= GetStars(stageId))
        {
            return false;
        }

        PlayerPrefs.SetInt(StarsKey(stageId), stars);
        PlayerPrefs.Save();
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

    public static int InfiniteBest => PlayerPrefs.GetInt(InfiniteBestKey, 0);

    public static bool SaveInfiniteBest(int score)
    {
        if (score <= InfiniteBest)
        {
            return false;
        }

        PlayerPrefs.SetInt(InfiniteBestKey, score);
        PlayerPrefs.Save();
        return true;
    }

    // Untuk test: hapus semua key dengan prefix saat ini.
    internal static void ClearAll(int maxStageId)
    {
        for (int id = 1; id <= maxStageId; id++)
        {
            PlayerPrefs.DeleteKey(StarsKey(id));
        }

        PlayerPrefs.DeleteKey(InfiniteBestKey);
        PlayerPrefs.Save();
    }
}
