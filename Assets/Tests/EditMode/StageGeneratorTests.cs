using System.Collections.Generic;
using NUnit.Framework;

public class StageGeneratorTests
{
    private static List<StageSpec> Generate()
    {
        return StageGenerator.Generate(5, StageList.StagesPerChapter, StageGenerator.DefaultSeed);
    }

    [Test]
    public void SameSeed_GivesSameStages()
    {
        List<StageSpec> first = Generate();
        List<StageSpec> second = Generate();

        for (int i = 0; i < first.Count; i++)
        {
            Assert.AreEqual(first[i].TotalTurns, second[i].TotalTurns);
            Assert.AreEqual(first[i].Goals[0], second[i].Goals[0]);
            Assert.AreEqual(first[i].Star2MinTurnsLeft, second[i].Star2MinTurnsLeft);
        }
    }

    [Test]
    public void Generates60Stages_WithValidGoalsAndStars()
    {
        List<StageSpec> stages = Generate();
        Assert.AreEqual(60, stages.Count);

        foreach (StageSpec stage in stages)
        {
            Assert.That(stage.Goals.Count, Is.InRange(1, 4), $"Stage {stage.StageId}");

            // Tiap target memakai warna berbeda.
            HashSet<BlockColor> colors = new HashSet<BlockColor>();
            foreach ((BlockColor color, int amount) in stage.Goals)
            {
                Assert.IsTrue(colors.Add(color), $"Stage {stage.StageId}: warna ganda.");
                Assert.GreaterOrEqual(amount, 5);
            }

            // Setiap aksi minimal punya 1 jatah.
            Assert.GreaterOrEqual(stage.HorizontalTurns, 1);
            Assert.GreaterOrEqual(stage.VerticalTurns, 1);
            Assert.GreaterOrEqual(stage.DiagonalTurns, 1);
            Assert.GreaterOrEqual(stage.RotateTurns, 1);

            Assert.GreaterOrEqual(stage.Star2MinTurnsLeft, 1);
            Assert.Less(stage.Star2MinTurnsLeft, stage.Star3MinTurnsLeft);
            Assert.Less(stage.Star3MinTurnsLeft, stage.TotalTurns);
        }
    }

    [Test]
    public void LaterChapters_HaveMoreGoals()
    {
        List<StageSpec> stages = Generate();

        // Bab 1: 1-2 target. Bab 5: 3-4 target.
        for (int i = 0; i < 12; i++)
        {
            Assert.LessOrEqual(stages[i].Goals.Count, 2);
            Assert.GreaterOrEqual(stages[48 + i].Goals.Count, 3);
        }
    }

    [Test]
    public void SplitTurns_KeepsTotal_AndMinimumOne()
    {
        int[] turns = StageGenerator.SplitTurns(13, new[] { 0.36f, 0.30f, 0.20f, 0.14f });

        Assert.AreEqual(13, turns[0] + turns[1] + turns[2] + turns[3]);

        foreach (int count in turns)
        {
            Assert.GreaterOrEqual(count, 1);
        }

        Assert.GreaterOrEqual(turns[0], turns[3], "Horizontal lebih banyak dari Rotate.");
    }
}
