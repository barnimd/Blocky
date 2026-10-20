using UnityEngine;

public static class StarRating
{
    public const int MaxStars = 3;

    // Bintang dihitung dari sisa TOTAL turn saat objective selesai.
    // Ambang mengikuti JSON di GDD (star_3_min / star_2_min).
    // Menang = minimal 1 bintang, termasuk menang di gerakan terakhir.
    public static int Calculate(
        int turnsLeft,
        int star3MinTurnsLeft,
        int star2MinTurnsLeft)
    {
        turnsLeft = Mathf.Max(0, turnsLeft);

        if (turnsLeft >= star3MinTurnsLeft)
        {
            return 3;
        }

        if (turnsLeft >= star2MinTurnsLeft)
        {
            return 2;
        }

        return 1;
    }
}
