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

    // Teks rentang sisa turn untuk tiap jumlah bintang, misal
    // ambang 6 & 2 → "6+", "2-5", "0-1". Dipakai panel hasil.
    public static string DescribeRange(int stars, int star3MinTurnsLeft, int star2MinTurnsLeft)
    {
        switch (stars)
        {
            case 3:
                return $"{star3MinTurnsLeft}+ turns left";

            case 2:
                return FormatRange(star2MinTurnsLeft, star3MinTurnsLeft - 1);

            default:
                return FormatRange(0, star2MinTurnsLeft - 1);
        }
    }

    // Berapa turn lagi yang harus disisakan untuk naik ke bintang berikutnya.
    // 0 = sudah bintang maksimal.
    public static int TurnsToNextStar(int turnsLeft, int star3MinTurnsLeft, int star2MinTurnsLeft)
    {
        int stars = Calculate(turnsLeft, star3MinTurnsLeft, star2MinTurnsLeft);

        if (stars >= MaxStars)
        {
            return 0;
        }

        int target = stars == 2 ? star3MinTurnsLeft : star2MinTurnsLeft;
        return Mathf.Max(1, target - Mathf.Max(0, turnsLeft));
    }

    private static string FormatRange(int min, int max)
    {
        max = Mathf.Max(min, max);
        string unit = min == 1 && max == 1 ? "turn" : "turns";

        return min == max
            ? $"{min} {unit} left"
            : $"{min}-{max} {unit} left";
    }
}
