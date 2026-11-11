using System.Collections.Generic;
using UnityEngine;

// Hasil generate satu stage, sebelum ditulis jadi asset StageData.
public class StageSpec
{
    public int StageId;
    public int Width;
    public int Height;
    public List<(BlockColor color, int amount)> Goals = new List<(BlockColor, int)>();
    public int HorizontalTurns;
    public int VerticalTurns;
    public int DiagonalTurns;
    public int RotateTurns;
    public int Star3MinTurnsLeft;
    public int Star2MinTurnsLeft;

    public int TotalTurns =>
        HorizontalTurns + VerticalTurns + DiagonalTurns + RotateTurns;
}

// Membuat target warna, jatah turn, dan kriteria bintang secara acak
// untuk semua stage. Memakai seed tetap: hasilnya selalu sama setiap
// kali setup dijalankan, jadi progres pemain tidak ikut berubah.
public static class StageGenerator
{
    public const int DefaultSeed = 20261021;

    // Bobot pembagian turn: H paling banyak, Rotate paling langka.
    private static readonly float[] TurnWeights = { 0.36f, 0.30f, 0.20f, 0.14f };

    public static List<StageSpec> Generate(int chapterCount, int stagesPerChapter, int seed)
    {
        System.Random random = new System.Random(seed);
        List<StageSpec> stages = new List<StageSpec>();
        int totalStages = chapterCount * stagesPerChapter;

        for (int i = 0; i < totalStages; i++)
        {
            int chapter = i / stagesPerChapter;

            // 0 = stage pertama, 1 = stage terakhir.
            float progress = totalStages > 1 ? i / (float)(totalStages - 1) : 0f;

            stages.Add(GenerateStage(random, i + 1, chapter, progress));
        }

        return stages;
    }

    private static StageSpec GenerateStage(
        System.Random random,
        int stageId,
        int chapter,
        float progress)
    {
        StageSpec spec = new StageSpec { StageId = stageId };

        // ── Board ──
        // Mulai 8x8. Bab 3+ kadang 9x9 (lebih luas), bab 4+ kadang 7x7 (lebih sempit).
        int size = 8;
        double boardRoll = random.NextDouble();

        if (chapter >= 2 && boardRoll < 0.25)
        {
            size = 9;
        }
        else if (chapter >= 3 && boardRoll > 0.8)
        {
            size = 7;
        }

        spec.Width = size;
        spec.Height = size;

        // ── Target warna ──
        // Bab 1-2: 1-2 warna, Bab 3-4: 2-3 warna, Bab 5: 3-4 warna.
        int minGoals = 1 + chapter / 2;
        int goalCount = random.Next(minGoals, Mathf.Min(4, minGoals + 1) + 1);

        List<BlockColor> colors = new List<BlockColor>
        {
            BlockColor.Red, BlockColor.Blue, BlockColor.Green,
            BlockColor.Yellow, BlockColor.Purple
        };
        Shuffle(colors, random);

        // Jumlah per warna naik dari ~8 (awal) sampai ~30 (akhir).
        float baseAmount = Mathf.Lerp(8f, 30f, progress);
        int totalTarget = 0;

        for (int g = 0; g < goalCount; g++)
        {
            int amount = Mathf.Max(5, Mathf.RoundToInt(baseAmount * Range(random, 0.8f, 1.2f)));
            spec.Goals.Add((colors[g], amount));
            totalTarget += amount;
        }

        // ── Jatah turn ──
        // Perkiraan: satu aksi menghancurkan ~3 blok warna target.
        // Slack = kelonggaran: besar di awal (mudah), kecil di akhir (sulit).
        float slack = Mathf.Lerp(1.7f, 1.2f, progress);
        int totalTurns = Mathf.CeilToInt(totalTarget / 3f * slack) + random.Next(0, 3);
        totalTurns = Mathf.Max(8, totalTurns);

        float[] weights = new float[TurnWeights.Length];

        for (int w = 0; w < weights.Length; w++)
        {
            weights[w] = TurnWeights[w] * Range(random, 0.7f, 1.3f);
        }

        int[] turns = SplitTurns(totalTurns, weights);
        spec.HorizontalTurns = turns[0];
        spec.VerticalTurns = turns[1];
        spec.DiagonalTurns = turns[2];
        spec.RotateTurns = turns[3];

        // ── Kriteria bintang (sisa total turn saat menang) ──
        // Mengikuti GDD: ⭐⭐⭐ sekitar ≥50%, ⭐⭐ sekitar ≥20%, ⭐ = menang.
        int total = spec.TotalTurns;
        int star3 = Mathf.RoundToInt(total * Range(random, 0.38f, 0.5f));
        int star2 = Mathf.RoundToInt(total * Range(random, 0.15f, 0.25f));

        spec.Star3MinTurnsLeft = Mathf.Clamp(star3, 2, total - 1);
        spec.Star2MinTurnsLeft = Mathf.Clamp(star2, 1, spec.Star3MinTurnsLeft - 1);

        return spec;
    }

    // Membagi total turn sesuai bobot. Setiap aksi minimal dapat 1.
    public static int[] SplitTurns(int total, float[] weights)
    {
        int count = weights.Length;
        int[] result = new int[count];
        total = Mathf.Max(total, count);

        float weightSum = 0f;

        foreach (float weight in weights)
        {
            weightSum += Mathf.Max(0.01f, weight);
        }

        // Jatah minimal 1 per aksi, sisanya dibagi proporsional.
        int remaining = total - count;
        float[] remainders = new float[count];
        int assigned = 0;

        for (int i = 0; i < count; i++)
        {
            float exact = remaining * Mathf.Max(0.01f, weights[i]) / weightSum;
            int whole = Mathf.FloorToInt(exact);

            result[i] = 1 + whole;
            remainders[i] = exact - whole;
            assigned += whole;
        }

        // Sisa pembulatan diberikan ke pecahan terbesar (largest remainder).
        while (assigned < remaining)
        {
            int best = 0;

            for (int i = 1; i < count; i++)
            {
                if (remainders[i] > remainders[best])
                {
                    best = i;
                }
            }

            result[best]++;
            remainders[best] = -1f;
            assigned++;
        }

        return result;
    }

    private static float Range(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }

    private static void Shuffle<T>(List<T> list, System.Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
